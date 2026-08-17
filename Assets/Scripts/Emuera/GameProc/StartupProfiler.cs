using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Simple start-up stage profiler (uEmuera Phase 6 #53–#55).
    ///
    /// Usage:
    /// <code>
    ///   StartupProfiler.Begin();
    ///   // ... do work ...
    ///   StartupProfiler.Mark("ConfigLoaded");
    ///   // ... more work ...
    ///   StartupProfiler.Mark("ErbCatalogReady");
    ///   UnityEngine.Debug.Log(StartupProfiler.Report());
    /// </code>
    ///
    /// All marks are thread-safe. The profiler is not reset between games
    /// automatically — call <see cref="Begin"/> at the top of each boot.
    /// </summary>
    public static class StartupProfiler
    {
        static readonly Stopwatch sw_ = new Stopwatch();
        static readonly List<KeyValuePair<string, long>> stages_ =
            new List<KeyValuePair<string, long>>(32);
        static readonly object lock_ = new object();
        static long mainThreadThresholdMs_ = 8;

        /// <summary>
        /// Restarts the profiler clock and clears previous stage marks.
        /// Call once at the beginning of each game boot.
        /// </summary>
        public static void Begin()
        {
            lock (lock_)
            {
                stages_.Clear();
            }
            sw_.Restart();
        }

        /// <summary>
        /// Records a stage with the elapsed time since <see cref="Begin"/>.
        /// Thread-safe.
        /// </summary>
        public static void Mark(string stage)
        {
            if (string.IsNullOrEmpty(stage))
                return;
            long ms = sw_.ElapsedMilliseconds;
            lock (lock_)
            {
                stages_.Add(new KeyValuePair<string, long>(stage, ms));
            }
        }

        /// <summary>
        /// Records how many ms a single operation took. Call this around expensive
        /// main-thread operations; it logs a warning if the threshold is exceeded
        /// (Phase 6 #54).
        /// </summary>
        public static void RecordMainThreadOp(string opName, long elapsedMs)
        {
            if (elapsedMs >= mainThreadThresholdMs_)
                UnityEngine.Debug.LogWarning(string.Format(
                    "[StartupProfiler] Main-thread stall: {0} took {1} ms (threshold {2} ms). " +
                    "See Docs/STARTUP_PROFILING.md.", opName, elapsedMs, mainThreadThresholdMs_));
        }

        /// <summary>Threshold above which a main-thread stall is logged (default 8 ms).</summary>
        public static long MainThreadStallThresholdMs
        {
            get { return mainThreadThresholdMs_; }
            set { mainThreadThresholdMs_ = value; }
        }

        /// <summary>
        /// Returns a human-readable startup report (Phase 6 #55).
        /// </summary>
        public static string Report()
        {
            lock (lock_)
            {
                if (stages_.Count == 0)
                    return "[StartupProfiler] No stages recorded.";
                var sb = new StringBuilder();
                sb.AppendLine("[StartupProfiler] Startup stages:");
                long prev = 0;
                for (int i = 0; i < stages_.Count; i++)
                {
                    var s = stages_[i];
                    long delta = s.Value - prev;
                    sb.AppendLine(string.Format("  {0,35}  {1,5} ms total  (+{2,4} ms)",
                        s.Key, s.Value, delta));
                    prev = s.Value;
                }
                return sb.ToString();
            }
        }

        /// <summary>Snapshot of all recorded stages (stage name → ms since Begin).</summary>
        public static IReadOnlyList<KeyValuePair<string, long>> Stages
        {
            get
            {
                lock (lock_)
                {
                    return stages_.ToArray();
                }
            }
        }

        /// <summary>Elapsed ms since the last Begin call.</summary>
        public static long ElapsedMs => sw_.ElapsedMilliseconds;
    }
}
