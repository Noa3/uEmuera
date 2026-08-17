using System;
using System.Collections.Generic;
using System.IO;

namespace uEmuera.Runtime.Detection
{
    /// <summary>
    /// Multi-runtime game directory scanner.
    /// Runs all registered <see cref="IGameDetector"/> implementations against a
    /// directory tree and returns a <see cref="GameDescriptor"/> for every recognised
    /// game package.
    ///
    /// This replaces the single-runtime <see cref="GameDiscovery"/> class for
    /// launcher use.  <see cref="GameDiscovery"/> is kept so that existing Emuera
    /// code that calls it directly continues to compile without changes.
    /// </summary>
    public sealed class GameDetector
    {
        readonly IReadOnlyList<IGameDetector> _detectors;

        // ------------------------------------------------------------------ //
        //  Factory                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a detector with the default Emuera + EraElectron detectors.
        /// The Emuera detector is listed first because it is the current primary
        /// runtime; ordering only matters for tie-breaking when confidence is equal.
        /// </summary>
        public static GameDetector CreateDefault() =>
            new GameDetector(new IGameDetector[]
            {
                new EmueraGameDetector(),
                new EraElectronGameDetector(),
            });

        public GameDetector(IReadOnlyList<IGameDetector> detectors)
        {
            _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Scan <paramref name="root"/> for game packages recognized by any
        /// registered detector.  Checks the root itself and all first-level
        /// sub-directories (plus contents of any "game/" sub-directory, matching
        /// the existing GameDiscovery convention).
        /// Results are sorted by title (case-insensitive).
        /// </summary>
        public IReadOnlyList<GameDescriptor> DiscoverAll(string root)
        {
            var results = new List<GameDescriptor>();
            var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return results;

            string normalRoot = Path.GetFullPath(root);
            TryAdd(normalRoot, results, seen);

            try
            {
                foreach (var dir in Directory.GetDirectories(
                    normalRoot, "*", SearchOption.TopDirectoryOnly))
                {
                    // Support the game/ convention used by GameDiscovery
                    if (string.Equals(Path.GetFileName(dir), "game",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            foreach (var sub in Directory.GetDirectories(
                                dir, "*", SearchOption.TopDirectoryOnly))
                                TryAdd(sub, results, seen);
                        }
                        catch { }
                        continue;
                    }
                    TryAdd(dir, results, seen);
                }
            }
            catch { }

            results.Sort((a, b) =>
                string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            return results;
        }

        /// <summary>
        /// Detect a single directory and return its <see cref="GameDescriptor"/>,
        /// or null if no registered detector recognises it.
        /// </summary>
        public GameDescriptor Detect(string directory)
        {
            if (!Directory.Exists(directory))
                return null;

            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                files = Array.Empty<string>();
            }

            return RunDetectors(directory, files);
        }

        // ------------------------------------------------------------------ //
        //  Internals                                                           //
        // ------------------------------------------------------------------ //

        void TryAdd(string dir, List<GameDescriptor> results, HashSet<string> seen)
        {
            string key = Path.GetFullPath(dir);
            if (!seen.Add(key)) return;
            var desc = Detect(dir);
            if (desc != null) results.Add(desc);
        }

        GameDescriptor RunDetectors(string directory, string[] topLevelFiles)
        {
            DetectionResult   bestResult   = null;
            IGameDetector     bestDetector = null;
            RuntimeKind?      ambiguous    = null;

            foreach (var detector in _detectors)
            {
                DetectionResult r;
                try { r = detector.TryDetect(directory, topLevelFiles); }
                catch { continue; }

                if (r == null) continue;

                if (bestResult == null || r.Confidence > bestResult.Confidence)
                {
                    if (bestResult != null)
                        ambiguous = bestDetector.Kind;
                    bestResult   = r;
                    bestDetector = detector;
                }
                else if (r.Confidence == bestResult.Confidence)
                {
                    // Tie — flag ambiguity so launcher can ask the user
                    ambiguous = detector.Kind;
                    bestResult.Warnings.Add(
                        $"Ambiguous: {bestDetector.Kind} and {detector.Kind} " +
                        "indicators have equal confidence.");
                }
            }

            if (bestResult == null || bestDetector == null)
                return null;

            if (ambiguous.HasValue)
                bestResult.AmbiguousAlternative = ambiguous;

            GameDescriptor desc;
            try { desc = bestDetector.BuildDescriptor(directory, bestResult); }
            catch { return null; }

            return desc;
        }
    }
}
