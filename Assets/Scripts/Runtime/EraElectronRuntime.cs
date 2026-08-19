using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using uEmuera.Runtime.EraElectron;

namespace uEmuera.Runtime
{
    /// <summary>
    /// EraElectron game runtime (JavaScript / Vue / Element Plus).
    /// Owns one platform host, loopback origin, bridge dispatcher, and data model
    /// for the lifetime of a game session.
    ///
    /// The Windows embedded host is implemented with WebView2. Other platforms
    /// currently surface a clear <see cref="NotSupportedException"/> through the
    /// null host until their platform host is implemented.
    /// </summary>
    public sealed class EraElectronRuntime : IGameRuntime
    {
        GameDescriptor         _game;
        RuntimeContext         _context;
        RuntimeState           _state      = RuntimeState.Created;
        EraElectronHostMode    _hostMode   = EraElectronHostMode.Auto;
        IEraElectronHost       _host;
        EreLocalFileServer     _fileServer;
        EreDataModel           _data;
        EreApiDispatcher       _bridge;
        DateTime               _startedAt;
        readonly Func<EraElectronHostMode, IEraElectronHost> _hostFactory;

        public EraElectronRuntime()
            : this(PlatformWebViewBridge.Create)
        {
        }

        internal EraElectronRuntime(
            Func<EraElectronHostMode, IEraElectronHost> hostFactory)
        {
            _hostFactory = hostFactory ?? throw new ArgumentNullException(nameof(hostFactory));
        }

        // ------------------------------------------------------------------ //
        //  IGameRuntime                                                        //
        // ------------------------------------------------------------------ //

        public RuntimeKind  Kind  => RuntimeKind.EraElectron;
        public RuntimeState State => _state;

        public Task InitializeAsync(
            GameDescriptor    game,
            RuntimeContext    context,
            CancellationToken cancellationToken = default)
        {
            if (_state != RuntimeState.Created)
                throw new InvalidOperationException(
                    $"[EraElectronRuntime] InitializeAsync called in state {_state}.");

            _game    = game    ?? throw new ArgumentNullException(nameof(game));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _state   = RuntimeState.Initializing;

            try
            {
                context.Logger?.Info(
                    $"[EraElectronRuntime] Initializing: {game.Title} v{game.Version}");
                context.Profiler?.Mark("EreRuntime_InitStart");

                cancellationToken.ThrowIfCancellationRequested();

                _hostMode = ResolveHostMode(game);
                context.Logger?.Info($"[EraElectronRuntime] Host mode: {_hostMode}");

                if (string.IsNullOrWhiteSpace(game.GameRoot))
                    throw new InvalidOperationException(
                        "[EraElectronRuntime] GameDescriptor.GameRoot is empty.");

                game.GameRoot = Path.GetFullPath(game.GameRoot);
                if (!Directory.Exists(game.GameRoot))
                    throw new DirectoryNotFoundException(
                        $"[EraElectronRuntime] Game directory does not exist: {game.GameRoot}");

                context.Profiler?.Mark("EreRuntime_InitReady");
                cancellationToken.ThrowIfCancellationRequested();

                _state = RuntimeState.Ready;
                context.Logger?.Info("[EraElectronRuntime] Ready.");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _state = RuntimeState.Faulted;
                return Task.FromException(ex);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_state != RuntimeState.Ready)
                throw new InvalidOperationException(
                    $"[EraElectronRuntime] StartAsync called in state {_state}.");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _host = _hostFactory(_hostMode)
                    ?? throw new InvalidOperationException(
                        "[EraElectronRuntime] Host factory returned null.");

                string engineVersion = PlatformWebViewBridge.ReadEreMinVersion(_game);
                _context?.Logger?.Info(
                    $"[EraElectronRuntime] Host={_host.HostMode}, ereMinVersion={engineVersion}");

                _data = EreDataModel.Create(_game);
                _bridge = new EreApiDispatcher(_data, _context);
                _bridge.SetEngineVersion(engineVersion);

                string bootstrapJs = EraElectronBridgeScript.Build(engineVersion);
                _fileServer = new EreLocalFileServer(_game.GameRoot, bootstrapJs);
                _fileServer.Start();
                _context?.Logger?.Info(
                    $"[EraElectronRuntime] File server: {_fileServer.BaseUrl}");

                await _host.InitializeAsync(_game, _bridge, cancellationToken);
                _context?.Profiler?.Mark("EreRuntime_HostReady");

                await _host.LoadGameAsync(_fileServer.BaseUrl, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                _host.Show();
                _startedAt = DateTime.UtcNow;
                _state = RuntimeState.Running;
                _context?.Profiler?.Mark("EreRuntime_GameVisible");
            }
            catch
            {
                await CleanupResourcesAsync();
                _state = RuntimeState.Faulted;
                throw;
            }
        }

        public Task SuspendAsync()
        {
            if (_state == RuntimeState.Running)
            {
                _state = RuntimeState.Suspended;
                _host?.Hide();
            }
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            if (_state == RuntimeState.Suspended)
            {
                _state = RuntimeState.Running;
                _host?.Show();
            }
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_state == RuntimeState.Stopped || _state == RuntimeState.Stopping)
                return;

            _state = RuntimeState.Stopping;
            _context?.Logger?.Info("[EraElectronRuntime] Stopping.");

            await CleanupResourcesAsync();

            _state = RuntimeState.Stopped;
            _context?.Logger?.Info("[EraElectronRuntime] Stopped.");
        }

        public RuntimeDiagnostics GetDiagnostics() =>
            new RuntimeDiagnostics
            {
                Kind            = RuntimeKind.EraElectron,
                State           = _state,
                GameTitle       = _game?.Title,
                GameVersion     = _game?.Version,
                RuntimeVersion  = "EraElectron-uEmuera-0.1.0",
                SessionId       = _context?.SessionId,
                UptimeMs        = _state == RuntimeState.Running || _state == RuntimeState.Suspended
                                  ? (long)(DateTime.UtcNow - _startedAt).TotalMilliseconds
                                  : 0L,
                WebRuntimeState = _state.ToString(),
                WebRuntimeHost  = _hostMode.ToString(),
                SavePath        = _fileServer?.BaseUrl,
            };

        public void Dispose()
        {
            try { CleanupResourcesAsync().GetAwaiter().GetResult(); }
            catch (Exception ex)
            {
                _context?.Logger?.Warn(
                    $"[EraElectronRuntime] Dispose cleanup failed: {ex.Message}");
            }
            _state = RuntimeState.Stopped;
        }

        async Task CleanupResourcesAsync()
        {
            var host = _host;
            _host = null;
            if (host != null)
            {
                try { await host.StopAsync(); }
                catch (Exception ex)
                {
                    _context?.Logger?.Warn(
                        $"[EraElectronRuntime] Host stop failed: {ex.Message}");
                }
                try { host.Dispose(); }
                catch (Exception ex)
                {
                    _context?.Logger?.Warn(
                        $"[EraElectronRuntime] Host dispose failed: {ex.Message}");
                }
            }

            var fileServer = _fileServer;
            _fileServer = null;
            if (fileServer != null)
            {
                try { fileServer.Dispose(); }
                catch (Exception ex)
                {
                    _context?.Logger?.Warn(
                        $"[EraElectronRuntime] File server dispose failed: {ex.Message}");
                }
            }

            _bridge = null;
            var data = _data;
            _data = null;
            try { data?.Dispose(); }
            catch (Exception ex)
            {
                _context?.Logger?.Warn(
                    $"[EraElectronRuntime] Data model dispose failed: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------ //
        //  Internals                                                           //
        // ------------------------------------------------------------------ //

        static EraElectronHostMode ResolveHostMode(GameDescriptor game)
        {
            // Per-game override wins.
            if (game.UserSettings?.EraElectronHostMode != null)
            {
                if (System.Enum.TryParse<EraElectronHostMode>(
                    game.UserSettings.EraElectronHostMode, true, out var overrideMode))
                    return overrideMode;
            }

            // Source-form ERE packages contain CommonJS entry files and cannot
            // execute directly in a browser WebView. Prefer the official engine
            // when it is configured; compiled bundles remain eligible for the
            // embedded host.
            if (!HasCompiledBundles(game) && OfficialSidecarHost.IsAvailable(game))
                return EraElectronHostMode.OfficialSidecar;
            return EraElectronHostMode.Auto;
        }

        static bool HasCompiledBundles(GameDescriptor game)
        {
            if (game == null || string.IsNullOrEmpty(game.GameRoot)) return false;
            string root = game.GameRoot;
            return (File.Exists(Path.Combine(root, "era.bundle.js")) &&
                    File.Exists(Path.Combine(root, "main.bundle.js"))) ||
                   (File.Exists(Path.Combine(root, "dist", "era.bundle.js")) &&
                    File.Exists(Path.Combine(root, "dist", "main.bundle.js")));
        }
    }
}
