namespace uEmuera.Runtime
{
    /// <summary>
    /// Selects how EraElectron games are hosted.
    ///
    /// Auto (default) prefers Embedded; falls back to OfficialSidecar when a
    /// required capability is not yet available in the embedded host.
    /// </summary>
    public enum EraElectronHostMode
    {
        /// <summary>
        /// Prefer Embedded; fall back to OfficialSidecar automatically if the
        /// embedded host reports a missing required capability.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Use the built-in platform WebView (WebView2 / Android WebView / Linux).
        /// If the embedded host cannot fulfil the game's requirements, the launch
        /// fails rather than falling back — use Auto to allow fallback.
        /// </summary>
        Embedded = 1,

        /// <summary>
        /// Launch the official EraElectron compatible executable as a child process.
        /// Desktop only (Windows / Linux where EraElectron is available).
        /// Maximum compatibility; trust level is higher than Embedded.
        /// </summary>
        OfficialSidecar = 2,
    }
}
