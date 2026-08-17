using System;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Startup strategy for ERB loading / semantic warmup (uEmuera Phase 6).
    ///
    /// <para><b>Safe</b> — traditional complete ERB startup path
    /// (<see cref="ErbLoader.LoadErbFiles"/>). Everything is parsed and syntax-checked
    /// on the interpreter thread before the title runs. No background semantic mutation.
    /// This is the compatibility baseline.</para>
    ///
    /// <para><b>Fast</b> — FunctionCatalog-backed interpreter-owned lazy path
    /// (<see cref="ErbLoader.LoadErbFilesLazy"/>). Required declarations compile on the
    /// interpreter thread; deferred bodies never mutate semantic state from a worker.</para>
    ///
    /// <para><b>Auto</b> — default compatibility baseline. It remains Safe until the
    /// Fast differential release gate passes.</para>
    /// </summary>
    public enum BootStrategy
    {
        Auto = 0,
        Safe = 1,
        Fast = 2,
    }

    /// <summary>
    /// Process-wide boot configuration. Engine-agnostic on purpose so a future
    /// EraElectron runtime can share the same coordinator (Phase 6S) without pulling
    /// in Emuera-specific types.
    /// </summary>
    public static class BootConfig
    {
        /// <summary>
        /// Active boot strategy. Defaults to <see cref="BootStrategy.Auto"/>, which
        /// currently resolves to Safe. Change via config/UI to opt into Fast.
        /// </summary>
        public static BootStrategy Strategy = BootStrategy.Auto;

        /// <summary>
        /// True when the progressive/Fast loader should be used. Auto resolves to Safe
        /// until the progressive path is proven race-free, so only an explicit
        /// <see cref="BootStrategy.Fast"/> enables it.
        /// </summary>
        public static bool UseProgressiveLoading
        {
            get { return Strategy == BootStrategy.Fast; }
        }

        /// <summary>Human-readable reason recorded when Auto/Fast fell back to Safe.</summary>
        public static string FallbackReason = null;

        /// <summary>Record a Fast→Safe fallback with a developer-facing reason.</summary>
        public static void RecordFastFallback(string reason)
        {
            FallbackReason = reason;
            UnityEngine.Debug.LogWarning(
                "Fast boot fallback: " + reason + " Restarting with full compatibility loader.");
        }
    }
}
