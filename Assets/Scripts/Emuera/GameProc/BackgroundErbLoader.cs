using System;
using System.Collections.Generic;
using System.Threading;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Coordinates background ERB file loading for progressive game startup.
    ///
    /// Workflow:
    ///   1. ErbLoader loads priority files (SYSTEM_*.ERB) synchronously.
    ///   2. BackgroundErbLoader.Activate() is called with the remaining file list
    ///      and the function-name-to-file index built by the quick-scan.
    ///   3. A background thread loads remaining ERB files via ErbLoader.
    ///   4. DoInstruction() calls WaitForFunction() when it encounters a function
    ///      that isn't yet in LabelDictionary. If the function is in the pending
    ///      index the caller blocks (up to timeoutMs) until that file is loaded.
    ///   5. When all files are loaded, IsActive becomes false; no further locking
    ///      overhead is incurred.
    /// </summary>
    internal sealed class BackgroundErbLoader
    {
        // ------------------------------------------------------------------ //
        //  Singleton                                                           //
        // ------------------------------------------------------------------ //
        public static BackgroundErbLoader Instance { get; private set; }

        /// <summary>Called by ErbLoader to set up background loading.</summary>
        /// <param name="alreadyLoadedPaths">File paths that were already loaded synchronously
        /// (priority files). These are pre-seeded so <see cref="IsFunctionPending"/> treats
        /// functions from them as already available.</param>
        public static void Activate(
            Dictionary<string, string> functionFileIndex,
            List<KeyValuePair<string, string>> pendingFiles,
            LabelDictionary labelDictionary,
            ErbLoader loader,
            IEnumerable<string> alreadyLoadedPaths = null)
        {
            var inst = new BackgroundErbLoader(functionFileIndex, pendingFiles, labelDictionary, loader);
            if (alreadyLoadedPaths != null)
                foreach (var p in alreadyLoadedPaths) inst.loadedFilePaths_.Add(p);
            Instance = inst;
            inst.StartBackground();
        }

        /// <summary>Called when progressive mode is not needed (e.g. analysis mode).</summary>
        public static void Deactivate()
        {
            var inst = Instance;
            if (inst != null)
            {
                inst.isActive_ = false;
                inst.fileLoadedEvent_.Set();
                Instance = null;
            }
        }

        // ------------------------------------------------------------------ //
        //  State                                                               //
        // ------------------------------------------------------------------ //

        // function name (upper-case if ICFunction) â†’ file path
        private readonly Dictionary<string, string> functionFileIndex_;

        // files still to be loaded
        private readonly List<KeyValuePair<string, string>> pendingFiles_;

        // reference to the shared label dictionary
        private readonly LabelDictionary labelDic_;

        // ErbLoader instance used for background loading
        private readonly ErbLoader loader_;

        // files whose loading has completed (path â†’ true)
        private readonly HashSet<string> loadedFilePaths_ = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly object lockObj_ = new object();

        // signalled whenever a batch of files finishes loading
        private readonly ManualResetEventSlim fileLoadedEvent_ = new ManualResetEventSlim(false);

        private volatile bool isActive_;
        private volatile int loaded_;
        private int total_;
        // Session ID captured at Activate time. If the game session advances (restart /
        // return-to-launcher), background work stops in BackgroundWork and the
        // singleton is cleared so no further label mutations reach the new session.
        private readonly int sessionId_;

        /// <summary>True while background loading is in progress.</summary>
        public bool IsActive => isActive_;

        /// <summary>Number of background files loaded so far.</summary>
        public int Loaded => loaded_;

        /// <summary>Total number of background files.</summary>
        public int Total => total_;

        // ------------------------------------------------------------------ //
        //  Construction                                                        //
        // ------------------------------------------------------------------ //
        private BackgroundErbLoader(
            Dictionary<string, string> functionFileIndex,
            List<KeyValuePair<string, string>> pendingFiles,
            LabelDictionary labelDic,
            ErbLoader loader)
        {
            functionFileIndex_ = functionFileIndex;
            pendingFiles_       = pendingFiles;
            labelDic_           = labelDic;
            loader_             = loader;
            total_              = pendingFiles.Count;
            isActive_           = total_ > 0;
            // Capture session at activation time so we can abort if the game
            // is restarted / cleared while background loading is in progress.
            // (Phase 6 #21/#22)
            sessionId_          = GameSession.Current;
        }

        // ------------------------------------------------------------------ //
        //  Background thread                                                   //
        // ------------------------------------------------------------------ //
        private void StartBackground()
        {
            if (!isActive_) return;
            var thread = new Thread(BackgroundWork)
            {
                Name       = "ProgressiveErbLoader",
                IsBackground = true,
                Priority   = ThreadPriority.BelowNormal,
            };
            thread.Start();
        }

        private void BackgroundWork()
        {
            const int BatchSize = 20;
            try
            {
                for (int i = 0; i < pendingFiles_.Count; i += BatchSize)
                {
                    // Abort if the game session has advanced (restart / return-to-launcher).
                    if (!GameSession.IsValid(sessionId_))
                    {
                        UnityEngine.Debug.Log(
                            "[BackgroundErbLoader] Session changed — aborting background load.");
                        return;
                    }

                    int end = Math.Min(i + BatchSize, pendingFiles_.Count);
                    for (int j = i; j < end; j++)
                    {
                        if (!GameSession.IsValid(sessionId_))
                            return;

                        var kv = pendingFiles_[j];
                        try
                        {
                            // Load and register labels - ErbLoader.loadErbPublic() is a wrapper
                            // we add in the ErbLoader changes.
                            loader_.LoadSingleErbBackground(kv.Value, kv.Key);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning(
                                $"[BackgroundErbLoader] Error loading {kv.Key}: {ex.Message}");
                        }
                    }

// Flush new labels into the runtime lookup dictionaries.
                    lock (lockObj_)
                    {
                        loaded_ += (end - i);
                        foreach (var kv in pendingFiles_.GetRange(i, end - i))
                            loadedFilePaths_.Add(kv.Value);
                    }
                    fileLoadedEvent_.Set();
                    fileLoadedEvent_.Reset();

                    // Yield briefly so the game thread can run.
                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[BackgroundErbLoader] Fatal error: {ex}");
            }
            finally
            {
                isActive_ = false;
                fileLoadedEvent_.Set(); // wake any waiters
                Instance = null;
                GenericUtils.SetLoadingStatus(""); // clear loading status
            }
        }

        // ------------------------------------------------------------------ //
        //  Called from game thread (DoInstruction)                            //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Blocks the calling thread until the file containing <paramref name="functionName"/>
        /// has been loaded in the background, or until <paramref name="timeoutMs"/> elapses.
        /// </summary>
        /// <returns>True if the function should now be available; false on timeout or
        /// if the function was not found in the index.</returns>
        public bool WaitForFunction(string functionName, int timeoutMs = 15000)
        {
            if (!isActive_) return false;

            string key = Config.ICFunction ? functionName.ToUpper() : functionName;
            string filePath;
            lock (lockObj_)
            {
                if (!functionFileIndex_.TryGetValue(key, out filePath))
                    return false; // Not in any known file â€” truly missing.
            }

            // Show loading status so the player knows we're working.
            GenericUtils.SetLoadingStatus($"Loading function @{functionName}...");

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                bool loaded;
                lock (lockObj_)
                {
                    loaded = loadedFilePaths_.Contains(filePath);
                    if (loaded)
                    {
                        // Make sure the label is visible to the game thread before
                        // returning â€” flush any pending label registration + sort now.
                        loader_.FlushLabelsBackground();
                    }
                }
                if (loaded)
                    return labelDic_.GetNonEventLabel(key) != null;
                fileLoadedEvent_.Wait(200);
                if (!isActive_) break;
            }

            lock (lockObj_)
            {
                if (loadedFilePaths_.Contains(filePath))
                    loader_.FlushLabelsBackground();
            }
            return labelDic_.GetNonEventLabel(key) != null;
        }

        /// <summary>
        /// True while <paramref name="functionName"/> is defined in a file that has
        /// not been loaded yet (used during checkScript to suppress false
        /// "function not found" warnings for functions that load later in the
        /// background). When background loading is finished this always returns false.
        /// </summary>
        public bool IsFunctionPending(string functionName)
        {
            if (!isActive_) return false;

            string key = Config.ICFunction ? functionName.ToUpper() : functionName;
            lock (lockObj_)
            {
                if (!functionFileIndex_.TryGetValue(key, out string filePath))
                    return false;
                return !loadedFilePaths_.Contains(filePath);
            }
        }

/// <summary>
        /// True while <paramref name="functionName"/> is defined in any ERB file of the
        /// game (per the quick-scan index), regardless of whether its file has been
        /// loaded yet. Used by the expression parser to turn forward references into
        /// lazily-resolved terms instead of "unrecognized identifier" warnings while
        /// progressive loading is active.
        /// </summary>
        public bool IsKnownFunction(string functionName)
        {
            if (!isActive_) return false;
            string key = Config.ICFunction ? functionName.ToUpper() : functionName;
            lock (lockObj_)
            {
                return functionFileIndex_.ContainsKey(key);
            }
        }

        /// <summary>
        /// Called by LabelDictionary to acquire the background lock before
        /// modifying shared dictionaries.
        /// </summary>
        public static void AcquireWriteLock(Action action)
        {
            var inst = Instance;
            if (inst == null) { action(); return; }
            lock (inst.lockObj_) { action(); }
        }

        /// <summary>
        /// Called by LabelDictionary to acquire the background lock before
        /// reading shared dictionaries (while background loading is active).
        /// </summary>
        public static T AcquireReadLock<T>(Func<T> func)
        {
            var inst = Instance;
            if (inst == null) return func();
            lock (inst.lockObj_) { return func(); }
        }
    }
}
