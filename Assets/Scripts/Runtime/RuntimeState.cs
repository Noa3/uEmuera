namespace uEmuera.Runtime
{
    /// <summary>
    /// Lifecycle state of an <see cref="IGameRuntime"/> instance.
    /// Transitions are one-way except Suspended ↔ Running.
    /// </summary>
    public enum RuntimeState
    {
        /// <summary>Created; <c>InitializeAsync</c> has not been called yet.</summary>
        Created = 0,

        /// <summary><c>InitializeAsync</c> is in progress.</summary>
        Initializing,

        /// <summary>Initialized and ready to call <c>StartAsync</c>.</summary>
        Ready,

        /// <summary>Running — game is active and accepting input.</summary>
        Running,

        /// <summary>Temporarily paused (e.g. app backgrounded or overlaid).</summary>
        Suspended,

        /// <summary>Stop requested; cleaning up assets and flushing saves.</summary>
        Stopping,

        /// <summary>Stopped cleanly; safe to dispose.</summary>
        Stopped,

        /// <summary>An unrecoverable error occurred; inspect diagnostics.</summary>
        Faulted,
    }
}
