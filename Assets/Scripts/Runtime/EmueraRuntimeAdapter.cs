using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace uEmuera.Runtime
{
    /// <summary>
    /// Wraps the existing Emuera runtime stack (EmueraMain + EmueraThread + Process)
    /// behind the <see cref="IGameRuntime"/> interface.
    ///
    /// INVARIANT: This adapter MUST NOT change any Emuera runtime semantics.
    /// It is a pure routing layer.  All game logic lives in the existing
    /// EmueraMain / EmueraThread / Process / EmueraConsole stack unchanged.
    ///
    /// Usage (launcher side):
    ///   var adapter = new EmueraRuntimeAdapter();
    ///   await adapter.InitializeAsync(descriptor, context);
    ///   await adapter.StartAsync();
    ///   // ... game runs ...
    ///   await adapter.StopAsync();
    ///   adapter.Dispose();
    /// </summary>
    public sealed class EmueraRuntimeAdapter : IGameRuntime
    {
        EmueraMain     _emueraMain;
        GameDescriptor _game;
        RuntimeContext _context;
        RuntimeState   _state = RuntimeState.Created;
        DateTime       _startedAt;

        // ------------------------------------------------------------------ //
        //  IGameRuntime                                                        //
        // ------------------------------------------------------------------ //

        public RuntimeKind  Kind  => RuntimeKind.Emuera;
        public RuntimeState State => _state;

        public async Task InitializeAsync(
            GameDescriptor     game,
            RuntimeContext     context,
            CancellationToken  cancellationToken = default)
        {
            if (_state != RuntimeState.Created)
                throw new InvalidOperationException(
                    $"[EmueraRuntimeAdapter] InitializeAsync called in state {_state}.");

            _state   = RuntimeState.Initializing;
            _game    = game    ?? throw new ArgumentNullException(nameof(game));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            context.Logger?.Info($"[EmueraRuntimeAdapter] Initializing: {game.Title}");
            context.Profiler?.Mark("EmueraAdapter_InitStart");

            cancellationToken.ThrowIfCancellationRequested();

            // Locate EmueraMain in the currently loaded scene.
            // It must already exist — the scene owns it.
            _emueraMain = UnityEngine.Object.FindAnyObjectByType<EmueraMain>();
            if (_emueraMain == null)
            {
                _state = RuntimeState.Faulted;
                throw new InvalidOperationException(
                    "[EmueraRuntimeAdapter] EmueraMain not found in scene. " +
                    "Ensure the Main scene is loaded before calling InitializeAsync.");
            }

            // Configure game root through the existing path globals.
            // This replicates what FirstWindow.Run used to do directly.
            MinorShift._Library.Sys.SetGameFolder(game.GameRoot);
            uEmuera.Utils.ResourcePrepare();

            context.Profiler?.Mark("EmueraAdapter_ResourcePrepared");

            await Task.Yield(); // yield to allow Unity main thread to process one frame

            _state = RuntimeState.Ready;
            context.Logger?.Info("[EmueraRuntimeAdapter] Ready.");
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_state != RuntimeState.Ready)
                throw new InvalidOperationException(
                    $"[EmueraRuntimeAdapter] StartAsync called in state {_state}.");

            cancellationToken.ThrowIfCancellationRequested();

            _state     = RuntimeState.Running;
            _startedAt = DateTime.UtcNow;

            _context?.Logger?.Info("[EmueraRuntimeAdapter] Starting game.");
            _context?.Profiler?.Mark("EmueraAdapter_GameStarted");

            // Delegate to the existing EmueraMain.Run path — no semantic change.
            EmueraContent.instance.SetNoReady();
            _emueraMain.Run();

            return Task.CompletedTask;
        }

        public Task SuspendAsync()
        {
            // Emuera does not have a formal suspend model; the game loop keeps
            // running on its background thread.  A future implementation can
            // pause the timer or mute input here without stopping the thread.
            return Task.CompletedTask;
        }

        public Task ResumeAsync() => Task.CompletedTask;

        public async Task StopAsync()
        {
            if (_state == RuntimeState.Stopped || _state == RuntimeState.Stopping)
                return;

            _state = RuntimeState.Stopping;
            _context?.Logger?.Info("[EmueraRuntimeAdapter] Stopping.");

            // Delegate teardown to EmueraMain.Clear(), which already handles:
            //   GameSession.Bump → thread stop → asset release → show launcher.
            if (_emueraMain != null)
                _emueraMain.Clear();

            // Poll until the game thread actually stops (Clear is a coroutine).
            const int maxWaitMs = 5000;
            const int pollMs    = 50;
            for (int i = 0; i < maxWaitMs / pollMs; i++)
            {
                await Task.Delay(pollMs);
                if (!EmueraThread.instance.Running())
                    break;
            }

            _state = RuntimeState.Stopped;
            _context?.Logger?.Info("[EmueraRuntimeAdapter] Stopped.");
        }

        public RuntimeDiagnostics GetDiagnostics()
        {
            int? labelCount    = null;
            int? pendingFiles  = null;

            try
            {
                var ld = MinorShift.Emuera.GlobalStatic.LabelDictionary;
                if (ld != null) labelCount = ld.Count;
            }
            catch { /* GlobalStatic may be null between sessions */ }

            try
            {
                var odc = MinorShift.Emuera.GameProc.OnDemandErbCompiler.Instance;
                if (odc != null) pendingFiles = odc.RemainingFiles;
            }
            catch { }

            return new RuntimeDiagnostics
            {
                Kind             = RuntimeKind.Emuera,
                State            = _state,
                GameTitle        = _game?.Title,
                GameVersion      = _game?.Version,
                RuntimeVersion   = "Emuera-1.824-uEmuera",
                SessionId        = MinorShift.Emuera.GameProc.GameSession.Current.ToString(),
                UptimeMs         = _state == RuntimeState.Running
                                   ? (long)(DateTime.UtcNow - _startedAt).TotalMilliseconds
                                   : 0L,
                EmueraBootStrategy  = MinorShift.Emuera.GameProc.BootConfig.Strategy.ToString(),
                EmueraLabelCount    = labelCount,
                Emuera_PendingFiles = pendingFiles,
                SavePath            = MinorShift.Emuera.Config.SavDir,
            };
        }

        public void Dispose()
        {
            if (_state == RuntimeState.Running || _state == RuntimeState.Stopping)
            {
                // Best-effort synchronous stop on unexpected Dispose.
                try { _emueraMain?.Clear(); } catch { }
            }
            _state = RuntimeState.Stopped;
        }
    }
}
