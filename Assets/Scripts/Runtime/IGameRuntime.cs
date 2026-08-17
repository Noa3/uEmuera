using System;
using System.Threading;
using System.Threading.Tasks;

namespace uEmuera.Runtime
{
    /// <summary>
    /// Abstraction over a single game runtime (Emuera or EraElectron).
    ///
    /// Rules:
    /// - One instance per game session; create a new instance for each game launch.
    /// - Emuera runtime state NEVER leaks into EraElectron runtime and vice versa.
    /// - Callers must await StopAsync before Disposing.
    /// </summary>
    public interface IGameRuntime : IDisposable
    {
        /// <summary>Which runtime family this instance represents.</summary>
        RuntimeKind Kind { get; }

        /// <summary>Current lifecycle state.</summary>
        RuntimeState State { get; }

        /// <summary>
        /// Perform async initialization: parse configuration, build catalogs,
        /// create the interpreter or web context, register services with the context.
        ///
        /// Does NOT show the first game screen; call <see cref="StartAsync"/> for that.
        /// </summary>
        Task InitializeAsync(
            GameDescriptor game,
            RuntimeContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Begin execution. After this returns (or the returned Task completes)
        /// the game is showing its title or first screen and accepting input.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Suspend execution without destroying state (e.g. app goes to background).
        /// Must be matched by a call to <see cref="ResumeAsync"/>.
        /// </summary>
        Task SuspendAsync();

        /// <summary>Resume after <see cref="SuspendAsync"/>.</summary>
        Task ResumeAsync();

        /// <summary>
        /// Stop the runtime cleanly:
        /// flush in-flight saves, cancel pending work, release runtime-owned resources.
        /// After this returns the instance transitions to Stopped and may be Disposed.
        /// </summary>
        Task StopAsync();

        /// <summary>Non-throwing diagnostic snapshot for the inspector / developer overlay.</summary>
        RuntimeDiagnostics GetDiagnostics();
    }

    // ------------------------------------------------------------------ //
    //  Diagnostics                                                         //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Diagnostic snapshot from a running <see cref="IGameRuntime"/>.
    /// All fields are optional; callers must handle nulls.
    /// </summary>
    public sealed class RuntimeDiagnostics
    {
        public RuntimeKind  Kind           { get; set; }
        public RuntimeState State          { get; set; }
        public string       GameTitle      { get; set; }
        public string       GameVersion    { get; set; }
        public string       RuntimeVersion { get; set; }
        public string       SessionId      { get; set; }
        public long         UptimeMs       { get; set; }

        // Emuera-specific (null when running EraElectron)
        public string EmueraBootStrategy { get; set; }
        public int?   EmueraLabelCount   { get; set; }
        public int?   Emuera_PendingFiles { get; set; }

        // EraElectron-specific (null when running Emuera)
        public string WebRuntimeState  { get; set; }
        public string WebRuntimeHost   { get; set; }
        public long?  JsHeapUsedBytes  { get; set; }
        public int?   LoadedModules    { get; set; }

        // Shared
        public long?  TextureMemoryBytes { get; set; }
        public int?   LoadedImageCount   { get; set; }
        public string SavePath           { get; set; }
        public string[] ActivePermissions { get; set; }
    }
}
