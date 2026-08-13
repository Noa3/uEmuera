using System;
using System.Threading;
using System.Threading.Tasks;

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

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_state != RuntimeState.Ready)
                throw new InvalidOperationException(
                    $"[EraElectronRuntime] StartAsync called in state {_state}.");

            cancellationToken.ThrowIfCancellationRequested();

            _context?.Profiler?.Mark("EreRuntime_Started");

            // WebView host not yet implemented (Milestone 3 spike pending).
            // Fault the task so LaunchEreGameCoroutine shows an error dialog and
            // returns the user to the launcher instead of a silent black screen.
            _state = RuntimeState.Faulted;
            string req = _game?.RequiredRuntimeVersion ?? "unknown";
            throw new NotSupportedException(
                $"EraElectron runtime not yet available.\n\n" +
                $"Game:   {_game?.Title ?? "?"}\n" +
                $"Requires engine version ≥ {req}\n\n" +
                "The embedded EraElectron host (WebView2 / Android WebView) has not " +
                "been implemented yet. Follow progress in:\n" +
                "  Docs/ADR/WEB_RUNTIME_HOST.md");
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
            };

        public void Dispose()
        {
            if (_state == RuntimeState.Running || _state == RuntimeState.Stopping)
            {
                try { _host?.StopAsync().Wait(2000); } catch { }
            }
            try { _host?.Dispose(); } catch { }
            _host  = null;
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
