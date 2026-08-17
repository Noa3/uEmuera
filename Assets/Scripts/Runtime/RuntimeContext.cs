using System;
using System.Threading.Tasks;

namespace uEmuera.Runtime
{
    /// <summary>
    /// Platform services injected into a runtime at initialization.
    ///
    /// Runtimes receive a RuntimeContext rather than accessing global statics so that:
    /// - Two runtimes never share live service instances.
    /// - Services can be replaced in tests without modifying production code.
    /// - Session isolation is enforced structurally.
    ///
    /// Services that are not yet implemented may be null; runtimes must null-check.
    /// </summary>
    public sealed class RuntimeContext
    {
        /// <summary>Virtual filesystem rooted at the game package.</summary>
        public IGameFileSystem FileSystem { get; set; }

        /// <summary>Persistent save / load storage, isolated per game.</summary>
        public IGameStorage Storage { get; set; }

        /// <summary>Runtime-specific structured logger.</summary>
        public IRuntimeLogger Logger { get; set; }

        /// <summary>Permission gate for sensitive operations (network, clipboard, etc.).</summary>
        public IPermissionService Permissions { get; set; }

        /// <summary>Startup stage profiler shared between launcher and runtime.</summary>
        public IStartupProfiler Profiler { get; set; }

        /// <summary>Dispatches actions onto the Unity main thread from any thread.</summary>
        public IMainThreadDispatcher MainThread { get; set; }

        /// <summary>
        /// Session identifier. All work belonging to one game launch shares one token.
        /// Used for stale-callback detection and log correlation.
        /// </summary>
        public string SessionId { get; set; }
    }

    // ------------------------------------------------------------------ //
    //  Service interfaces                                                  //
    //  (expand signatures as Phase 8 implementation progresses)           //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Read-only virtual filesystem rooted at the game package.
    /// Paths are virtual (e.g. "/game/main.js"); never expose raw host paths to
    /// untrusted game code.
    /// </summary>
    public interface IGameFileSystem
    {
        bool     FileExists(string virtualPath);
        byte[]   ReadAllBytes(string virtualPath);
        string   ReadAllText(string virtualPath);
        string[] ListFiles(string virtualDirectory, string pattern = "*", bool recursive = false);
    }

    /// <summary>
    /// Persistent key/value save storage, namespaced per game.
    /// Implementations must be atomic-write on supported platforms.
    /// </summary>
    public interface IGameStorage
    {
        byte[]   LoadSlot(string slotKey);
        void     SaveSlot(string slotKey, byte[] data);
        bool     SlotExists(string slotKey);
        void     DeleteSlot(string slotKey);
        string[] ListSlots(string prefix = "");
    }

    /// <summary>Structured runtime logger; implementations write to Unity console or file.</summary>
    public interface IRuntimeLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception exception = null);
    }

    /// <summary>
    /// Permission gate for sensitive operations.
    /// Default implementation grants only the minimum required for local gameplay.
    /// </summary>
    public interface IPermissionService
    {
        bool IsGranted(string permission);
        Task<bool> RequestAsync(string permission, string rationale = null);
    }

    /// <summary>
    /// Startup stage profiler (wraps or delegates to StartupProfiler).
    /// Both Emuera and EraElectron runtimes mark their own stage names.
    /// </summary>
    public interface IStartupProfiler
    {
        void   Begin();
        void   Mark(string stage);
        string Report();
    }

    /// <summary>
    /// Posts an action to the Unity main thread from any background thread.
    /// </summary>
    public interface IMainThreadDispatcher
    {
        void Post(Action action);
    }
}
