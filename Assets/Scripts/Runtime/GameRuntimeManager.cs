using System;
using System.Threading;
using System.Threading.Tasks;
using uEmuera.Runtime.Detection;

namespace uEmuera.Runtime
{
    /// <summary>
    /// Coordinates game lifecycle across all registered runtimes.
    ///
    /// Responsibilities:
    /// - Creates the correct <see cref="IGameRuntime"/> for a <see cref="GameDescriptor"/>.
    /// - Ensures only one runtime is active at a time.
    /// - Manages the Stop→new-Start transition cleanly.
    /// - Provides a single diagnostic snapshot.
    ///
    /// Usage (launcher side):
    ///   var mgr = GameRuntimeManager.Instance;
    ///   var desc = GameDetector.CreateDefault().Detect(gamePath);
    ///   await mgr.LaunchAsync(desc, context);
    ///   // ... player plays ...
    ///   await mgr.StopCurrentAsync();
    /// </summary>
    public sealed class GameRuntimeManager
    {
        // ------------------------------------------------------------------ //
        //  Singleton                                                           //
        // ------------------------------------------------------------------ //

        static readonly GameRuntimeManager instance_ = new GameRuntimeManager();
        public static GameRuntimeManager Instance => instance_;
        GameRuntimeManager() { }

        // ------------------------------------------------------------------ //
        //  State                                                               //
        // ------------------------------------------------------------------ //

        IGameRuntime   _current;
        GameDescriptor _currentGame;
        readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>The currently active runtime, or null if no game is running.</summary>
        public IGameRuntime CurrentRuntime => _current;

        /// <summary>Descriptor of the game currently running, or null.</summary>
        public GameDescriptor CurrentGame => _currentGame;

        /// <summary>True while a game is in Running or Suspended state.</summary>
        public bool IsRunning =>
            _current?.State == RuntimeState.Running ||
            _current?.State == RuntimeState.Suspended;

        /// <summary>
        /// Stop any running game, then create and launch the runtime for
        /// <paramref name="game"/>.
        ///
        /// Thread-safe; concurrent calls are serialized.
        /// </summary>
        public async Task LaunchAsync(
            GameDescriptor     game,
            RuntimeContext     context,
            CancellationToken  cancellationToken = default)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));

            await _lock.WaitAsync(cancellationToken);
            IGameRuntime runtime = null;
            try
            {
                // Stop whatever is running first.
                await StopCurrentInternalAsync();

                // Create the new runtime.
                runtime = CreateRuntime(game, context);

                context?.Logger?.Info(
                    $"[GameRuntimeManager] Launching {game.Title} " +
                    $"via {runtime.Kind} runtime.");
                context?.Profiler?.Mark("GameRuntimeManager_LaunchStart");

                await runtime.InitializeAsync(game, context, cancellationToken);
                await runtime.StartAsync(cancellationToken);

                _current     = runtime;
                _currentGame = game;

                context?.Logger?.Info(
                    $"[GameRuntimeManager] {game.Title} is running.");
            }
            catch
            {
                // Ensure a faulted runtime is not left as current.
                _current     = null;
                _currentGame = null;
                // LaunchAsync assigns _current only after StartAsync succeeds,
                // so a partial startup must be disposed through the local value.
                // Otherwise a failed EraElectron host can leak its loopback server
                // and WebView2 STA thread.
                if (runtime != null)
                {
                    try { runtime.Dispose(); }
                    catch (Exception cleanupError)
                    {
                        context?.Logger?.Warn(
                            $"[GameRuntimeManager] Failed to clean up faulted runtime: {cleanupError.Message}");
                    }
                }
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop the currently running game cleanly and return to the launcher.
        /// No-op if no game is running.
        /// </summary>
        public async Task StopCurrentAsync()
        {
            await _lock.WaitAsync();
            try
            {
                await StopCurrentInternalAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>Diagnostic snapshot from the current runtime (null = no game).</summary>
        public RuntimeDiagnostics GetDiagnostics()
        {
            return _current?.GetDiagnostics();
        }

        // ------------------------------------------------------------------ //
        //  Internals                                                           //
        // ------------------------------------------------------------------ //

        async Task StopCurrentInternalAsync()
        {
            var old = _current;
            if (old == null) return;
            _current     = null;
            _currentGame = null;

            try
            {
                if (old.State == RuntimeState.Running ||
                    old.State == RuntimeState.Suspended)
                {
                    await old.StopAsync();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    $"[GameRuntimeManager] Error stopping {old.Kind} runtime: {ex.Message}");
            }
            finally
            {
                try { old.Dispose(); } catch { }
            }
        }

        static IGameRuntime CreateRuntime(GameDescriptor game, RuntimeContext context)
        {
            switch (game.RuntimeKind)
            {
                case RuntimeKind.Emuera:
                    return new EmueraRuntimeAdapter();

                case RuntimeKind.EraElectron:
                    return new EraElectronRuntime();

                case RuntimeKind.Unknown:
                default:
                    context?.Logger?.Error(
                        $"[GameRuntimeManager] Unknown RuntimeKind '{game.RuntimeKind}' " +
                        $"for game '{game.Title}'. Cannot create runtime.");
                    throw new NotSupportedException(
                        $"RuntimeKind.{game.RuntimeKind} is not supported. " +
                        $"Verify game detection for: {game.GameRoot}");
            }
        }
    }
}
