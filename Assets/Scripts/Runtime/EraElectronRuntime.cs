using System;
using System.Threading;
using System.Threading.Tasks;
using uEmuera.Runtime.EraElectron;

namespace uEmuera.Runtime
{
    /// <summary>
    /// EraElectron game runtime (JavaScript / Vue / Element Plus).
    ///
    /// CURRENT STATUS: STUB — state machine only; web host not yet implemented.
    ///   - InitializeAsync validates the game descriptor.
    ///   - StartAsync transitions state but does NOT launch a WebView.
    ///   - All era.* API calls return NotImplementedException stubs.
    ///
    /// See Docs/ADR/ERAELECTRON_RUNTIME.md for the full architecture plan.
    /// See ReferenceParity/EraElectron/API.generated.json for the API inventory.
    ///
    /// Implementation milestone: M4 (synthetic game) begins filling this in.
    /// </summary>
    public sealed class EraElectronRuntime : IGameRuntime
    {
        GameDescriptor         _game;
        RuntimeContext         _context;
        RuntimeState           _state      = RuntimeState.Created;
        EraElectronHostMode    _hostMode   = EraElectronHostMode.Auto;
        IEraElectronHost       _host;
        EreLocalFileServer     _fileServer;
        DateTime               _startedAt;

        // ------------------------------------------------------------------ //
        //  IGameRuntime                                                        //
        // ------------------------------------------------------------------ //

        public RuntimeKind  Kind  => RuntimeKind.EraElectron;
        public RuntimeState State => _state;

        public async Task InitializeAsync(
            GameDescriptor    game,
            RuntimeContext    context,
            CancellationToken cancellationToken = default)
        {
            if (_state != RuntimeState.Created)
                throw new InvalidOperationException(
                    $"[EraElectronRuntime] InitializeAsync called in state {_state}.");

            _state   = RuntimeState.Initializing;
            _game    = game    ?? throw new ArgumentNullException(nameof(game));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            context.Logger?.Info(
                $"[EraElectronRuntime] Initializing: {game.Title} v{game.Version}");
            context.Logger?.Warn(
                "[EraElectronRuntime] STUB — no WebView host implemented yet. " +
                "Runtime will transition to Ready but cannot launch game JS.");
            context.Profiler?.Mark("EreRuntime_InitStart");

            cancellationToken.ThrowIfCancellationRequested();

            // Resolve host mode from per-game settings or global default.
            _hostMode = ResolveHostMode(game);
            context.Logger?.Info($"[EraElectronRuntime] Host mode: {_hostMode}");

            // Validate game package basics.
            if (string.IsNullOrEmpty(game.GameRoot))
            {
                _state = RuntimeState.Faulted;
                throw new InvalidOperationException(
                    "[EraElectronRuntime] GameDescriptor.GameRoot is empty.");
            }

            context.Profiler?.Mark("EreRuntime_InitReady");
            await Task.Yield();

            _state = RuntimeState.Ready;
            context.Logger?.Info("[EraElectronRuntime] Ready (stub).");
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_state != RuntimeState.Ready)
                throw new InvalidOperationException(
                    $"[EraElectronRuntime] StartAsync called in state {_state}.");

            cancellationToken.ThrowIfCancellationRequested();

            _context?.Profiler?.Mark("EreRuntime_Started");
            _state     = RuntimeState.Running;
            _startedAt = DateTime.UtcNow;

            // Create the platform host via bridge factory.
            _host = PlatformWebViewBridge.Create(_hostMode);

            // Read engine version for bridge injection.
            string engineVersion = PlatformWebViewBridge.ReadEreMinVersion(_game);
            _context?.Logger?.Info(
                $"[EraElectronRuntime] Host={_host.HostMode}, ereMinVersion={engineVersion}");

            // Build and configure the era.* bridge dispatcher.
            var data   = EreDataModel.Create(_game);
            var bridge = new EreApiDispatcher(data, _context);
            bridge.SetEngineVersion(engineVersion);

            // Start loopback file server — serves game files over a private HTTP
            // origin. The WebView navigates to fileServer.BaseUrl/index.html.
            string bootstrapJs = EraElectronBridgeScript.Build(engineVersion);
            _fileServer = new EreLocalFileServer(_game.GameRoot, bootstrapJs);
            _fileServer.Start();
            _context?.Logger?.Info($"[EraElectronRuntime] File server: {_fileServer.BaseUrl}");

            // Initialize the host (creates WebView context, registers bridge).
            await _host.InitializeAsync(_game, bridge, cancellationToken);
            _context?.Profiler?.Mark("EreRuntime_HostReady");

            // Load the game JS bundles — throws NotSupportedException on NullHost
            // which LaunchEreGameCoroutine catches and shows as an error dialog.
            await _host.LoadGameAsync(cancellationToken);

            _host.Show();
            _context?.Profiler?.Mark("EreRuntime_GameVisible");
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

            if (_host != null)
            {
                try { await _host.StopAsync(); } catch { }
                try { _host.Dispose(); }         catch { }
                _host = null;
            }

            try { _fileServer?.Stop(); _fileServer?.Dispose(); } catch { }
            _fileServer = null;

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
                RuntimeVersion  = "EraElectron-STUB-uEmuera",
                SessionId       = _context?.SessionId,
                UptimeMs        = _state == RuntimeState.Running
                                  ? (long)(DateTime.UtcNow - _startedAt).TotalMilliseconds
                                  : 0L,
                WebRuntimeState = _state.ToString(),
                WebRuntimeHost  = _hostMode.ToString(),
                SavePath        = _fileServer?.BaseUrl,
            };

        public void Dispose()
        {
            if (_state == RuntimeState.Running || _state == RuntimeState.Stopping)
            {
                try { _host?.StopAsync().Wait(2000); } catch { }
            }
            try { _host?.Dispose(); } catch { }
            _host  = null;
            try { _fileServer?.Stop(); _fileServer?.Dispose(); } catch { }
            _fileServer = null;
            _state = RuntimeState.Stopped;
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
            return EraElectronHostMode.Auto;
        }
    }
}
