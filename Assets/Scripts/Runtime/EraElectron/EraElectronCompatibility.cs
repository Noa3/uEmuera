using System;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// Explicit compatibility profile for the embedded EraElectron runtime.
    ///
    /// These values describe what uEmuera currently claims to emulate, not the
    /// minimum version requested by the loaded game. Never echo a game's
    /// .ere-min-version back as the runtime version.
    /// </summary>
    public static class EraElectronCompatibility
    {
        /// <summary>
        /// Engine compatibility level currently targeted by the embedded runtime.
        /// EraUma 3.0.00 in the captured corpus requires 2200.
        /// </summary>
        public const int EmulatedEngineVersion = 2200;

        /// <summary>
        /// SDK surface used by the current captured parity baseline.
        /// Upgrade only after the upstream API delta is reviewed and tests pass.
        /// </summary>
        public const string EmulatedSdkVersion = "4.7.0";

        /// <summary>Version of uEmuera's bridge implementation itself.</summary>
        public const string BridgeVersion = "0.2.0";

        public static bool TryParseRequiredEngineVersion(
            string value, out int requiredVersion)
        {
            requiredVersion = 0;
            if (string.IsNullOrWhiteSpace(value))
                return true;

            return int.TryParse(value.Trim(), out requiredVersion) &&
                   requiredVersion >= 0;
        }

        /// <summary>
        /// Returns whether the embedded runtime may truthfully claim to satisfy
        /// the game's declared minimum engine version.
        /// </summary>
        public static bool CanRunEmbedded(
            string requiredVersionText, out string reason)
        {
            if (string.IsNullOrWhiteSpace(requiredVersionText))
            {
                reason = null;
                return true;
            }

            if (!TryParseRequiredEngineVersion(
                    requiredVersionText, out int requiredVersion))
            {
                reason =
                    $"Invalid .ere-min-version value: '{requiredVersionText}'.";
                return false;
            }

            if (requiredVersion > EmulatedEngineVersion)
            {
                reason =
                    $"Game requires EraElectron engine {requiredVersion}, but " +
                    $"the embedded uEmuera compatibility target is " +
                    $"{EmulatedEngineVersion}.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
