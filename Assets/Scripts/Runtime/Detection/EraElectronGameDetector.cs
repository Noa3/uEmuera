using System;
using System.Collections.Generic;
using System.IO;

namespace uEmuera.Runtime.Detection
{
    /// <summary>
    /// Detects EraElectron (ERE) game packages.
    ///
    /// Fingerprints verified against EraUma 3.0.00 (erauma-master, Aug 2026).
    ///
    /// EraUma source layout (representative of all source-form ERE games):
    ///   .ere-min-version   — engine version requirement, e.g. "2200" (DEFINITIVE)
    ///   ere/era-electron.js — SDK source aliased as #/era-electron (STRONG)
    ///   ere/main.js         — game entry point (STRONG)
    ///   webpack.config.js   — dev-only, may be absent in distributions
    ///   package.json        — generic; useful only when ere-webpack-plugin present
    ///
    /// NOTE: "ere.config.json" / "era.config.json" do NOT appear in real EraUma.
    /// The previous assumption was wrong and has been removed.
    /// </summary>
    public sealed class EraElectronGameDetector : IGameDetector
    {
        public RuntimeKind Kind => RuntimeKind.EraElectron;

        // ------------------------------------------------------------------ //
        //  Verified fingerprints (EraUma 3.0.00, Aug 2026)                    //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Definitive top-level file: ".ere-min-version".
        /// Present in every ERE source package; contains the minimum engine
        /// version string (e.g. "2200").  Not present in unrelated JS projects.
        /// </summary>
        const string EreMinVersionFile = ".ere-min-version";

        /// <summary>
        /// SDK source file inside the ere/ sub-directory.
        /// Verified present in EraUma 3.0.00 as "ere/era-electron.js".
        /// This is the file aliased as "#/era-electron" by ere-webpack-plugin.
        /// </summary>
        static readonly string SdkSourcePath =
            Path.Combine("ere", "era-electron.js");

        /// <summary>
        /// Game entry source inside the ere/ sub-directory.
        /// Verified present in EraUma 3.0.00 as "ere/main.js".
        /// webpack.config.js entry: { import: './main.js', library: 'game' }.
        /// </summary>
        static readonly string GameEntrySourcePath =
            Path.Combine("ere", "main.js");

        /// <summary>
        /// Browser-ready distribution bundle pairs. A generic root main.js alone is
        /// intentionally NOT sufficient evidence because many unrelated JavaScript
        /// projects contain that filename.
        /// </summary>
        static readonly string[][] BuiltBundlePairs =
        {
            new[] { "era.bundle.js", "main.bundle.js" },
            new[] { Path.Combine("dist", "era.bundle.js"),
                    Path.Combine("dist", "main.bundle.js") },
        };

        // ------------------------------------------------------------------ //

        public DetectionResult TryDetect(
            string directory,
            IReadOnlyList<string> topLevelFiles)
        {
            var evidence = new List<string>();
            var warnings = new List<string>();

            // Build case-insensitive lookup of top-level file names.
            // Directory.GetFiles("*") includes dot-files on both Windows and Linux.
            var topNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in topLevelFiles)
                topNames.Add(Path.GetFileName(f));

            // ---- Primary: .ere-min-version --------------------------------
            bool hasMinVersion = topNames.Contains(EreMinVersionFile);
            if (hasMinVersion)
            {
                string ver = ReadFirstLine(Path.Combine(directory, EreMinVersionFile));
                evidence.Add(string.IsNullOrEmpty(ver)
                    ? ".ere-min-version present"
                    : $".ere-min-version = {ver}");
            }

            // ---- Strong: ere/era-electron.js (SDK source) -----------------
            bool hasSdkSource = File.Exists(Path.Combine(directory, SdkSourcePath));
            if (hasSdkSource)
                evidence.Add($"sdk: {SdkSourcePath}");

            // ---- Strong: ere/main.js (game entry source) ------------------
            bool hasGameEntry = File.Exists(Path.Combine(directory, GameEntrySourcePath));
            if (hasGameEntry)
                evidence.Add($"entry: {GameEntrySourcePath}");

            // ---- Fallback: compiled/distribution bundle pair ---------------
            bool hasBuiltEntry = false;
            string builtMainEntry = null;
            if (!hasGameEntry)
            {
                foreach (var pair in BuiltBundlePairs)
                {
                    if (File.Exists(Path.Combine(directory, pair[0])) &&
                        File.Exists(Path.Combine(directory, pair[1])))
                    {
                        hasBuiltEntry = true;
                        builtMainEntry = pair[1];
                        evidence.Add($"built-sdk: {pair[0]}");
                        evidence.Add($"built-entry: {pair[1]}");
                        break;
                    }
                }
            }

            // ---- Reject if nothing matches --------------------------------
            if (!hasMinVersion && !hasSdkSource && !hasGameEntry && !hasBuiltEntry)
                return null;

            // ---- Confidence -----------------------------------------------
            DetectionConfidence confidence;

            if (hasMinVersion && (hasSdkSource || hasGameEntry))
            {
                // Best case: definitive marker + source tree present
                confidence = DetectionConfidence.Certain;
            }
            else if (hasMinVersion || (hasSdkSource && hasGameEntry))
            {
                // .ere-min-version alone is strong; so is both SDK + game entry together
                confidence = DetectionConfidence.High;
                if (!hasMinVersion)
                    warnings.Add(
                        ".ere-min-version absent; detected via ERE source tree only. " +
                        "Could be a partial or hand-assembled package.");
            }
            else if (hasSdkSource || hasGameEntry)
            {
                // Only one source indicator — plausible but unconfirmed
                confidence = DetectionConfidence.Medium;
                warnings.Add(
                    "ERE source file found but .ere-min-version is absent. " +
                    "Could be a partial or manually assembled package.");
            }
            else
            {
                // A complete era.bundle.js + main.bundle.js pair is strong enough
                // to identify a browser-ready ERE distribution without source files.
                confidence = DetectionConfidence.High;
            }

            return new DetectionResult
            {
                Confidence = confidence,
                Evidence   = evidence,
                Warnings   = warnings,
            };
        }

        public GameDescriptor BuildDescriptor(string directory, DetectionResult result)
        {
            string folderName = Path.GetFileName(
                directory.TrimEnd(Path.DirectorySeparatorChar,
                                  Path.AltDirectorySeparatorChar));

            // Read engine version requirement
            string ereMinVersion = ReadFirstLine(
                Path.Combine(directory, EreMinVersionFile));

            // Determine entry point: prefer source layout, fall back to built bundle
            string entryRel = null;
            if (File.Exists(Path.Combine(directory, GameEntrySourcePath)))
                entryRel = GameEntrySourcePath;
            else if (File.Exists(Path.Combine(directory, SdkSourcePath)))
                entryRel = SdkSourcePath;   // unusual but non-null
            else
            {
                foreach (var pair in BuiltBundlePairs)
                {
                    if (File.Exists(Path.Combine(directory, pair[0])) &&
                        File.Exists(Path.Combine(directory, pair[1])))
                    {
                        entryRel = pair[1];
                        break;
                    }
                }
            }

            string title = ReadPackageString(directory, "name") ?? folderName;
            string version = ReadPackageString(directory, "version") ?? string.Empty;

            string gameId = EmueraGameDetector.MakeId("ere-", directory);

            return new GameDescriptor
            {
                GameId                  = gameId,
                Title                   = title,
                Version                 = version,
                RuntimeKind             = RuntimeKind.EraElectron,
                GameRoot                = directory,
                EntryPoint              = entryRel,
                ConfigurationPath       = null,         // no ere.config.json in real games
                Language                = "ja",
                RequiredRuntimeVersion  = ereMinVersion,
                SaveNamespace           = gameId,
                DetectionResult         = result,
            };
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        static string ReadFirstLine(string path)
        {
            try
            {
                using (var r = new StreamReader(path, System.Text.Encoding.UTF8))
                    return r.ReadLine()?.Trim() ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        static string ReadPackageString(string directory, string property)
        {
            string pkgPath = Path.Combine(directory, "package.json");
            if (!File.Exists(pkgPath) || string.IsNullOrEmpty(property))
                return null;

            try
            {
                string text = File.ReadAllText(pkgPath, System.Text.Encoding.UTF8);
                string needle = "\"" + property + "\"";
                int keyIndex = text.IndexOf(needle, StringComparison.Ordinal);
                if (keyIndex < 0) return null;
                int colon = text.IndexOf(':', keyIndex + needle.Length);
                if (colon < 0) return null;
                int q1 = text.IndexOf('"', colon + 1);
                if (q1 < 0) return null;

                // Handle simple JSON string escapes while looking for the closing quote.
                int q2 = q1 + 1;
                bool escaped = false;
                for (; q2 < text.Length; q2++)
                {
                    char ch = text[q2];
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (ch == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    if (ch == '"')
                        break;
                }
                if (q2 >= text.Length) return null;

                string value = text.Substring(q1 + 1, q2 - q1 - 1)
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }
            catch { return null; }
        }
    }
}
