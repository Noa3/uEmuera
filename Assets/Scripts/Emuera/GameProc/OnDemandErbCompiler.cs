using System;
using System.Collections.Generic;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Interpreter-owned on-demand ERB compiler (uEmuera Phase 6 — Fast-boot lazy
    /// model).
    ///
    /// <para>Replaces the unsafe <see cref="BackgroundErbLoader"/> mutation model for
    /// the Fast boot path. The invariant is that ONLY the interpreter thread ever
    /// mutates Emuera semantic state (LabelDictionary, function metadata). Deferred
    /// ERB files are compiled synchronously from that thread at the exact moment the
    /// running game first references one of their functions — so there is no
    /// cross-thread race by construction, and time-to-title stays short because
    /// non-priority files are not parsed at boot.</para>
    ///
    /// <para>Boot flow:</para>
    /// <list type="number">
    ///   <item><c>ErbLoader.LoadErbFilesLazy</c> compiles priority files
    ///     (SYSTEM_*, GAMEBASE, TITLE, START, COMMON) synchronously.</item>
    ///   <item><see cref="Activate"/> indexes every deferred file from the
    ///     <see cref="FunctionCatalog"/> (metadata already scanned).</item>
    ///   <item>At runtime, CALL / expression-method / event dispatch route through
    ///     <see cref="ErbOnDemand.EnsureCompiled"/> / <see cref="ErbOnDemand.EnsureEventLoaded"/>,
    ///     which compile the containing file on the interpreter thread.</item>
    /// </list>
    ///
    /// <para>Thread-safety: all members are touched only from the interpreter thread
    /// (the same thread that runs <c>Process.DoInstruction</c>). No locks are needed.</para>
    /// </summary>
    internal sealed class OnDemandErbCompiler
    {
        // ------------------------------------------------------------------ //
        //  Singleton                                                           //
        // ------------------------------------------------------------------ //

        public static OnDemandErbCompiler Instance { get; private set; }

        /// <summary>
        /// Activates lazy compilation for the current game session. Called by
        /// <see cref="ErbLoader.LoadErbFilesLazy"/> after priority files are loaded
        /// and the FunctionCatalog is built (interpreter thread).
        /// </summary>
        /// <param name="loader">The ErbLoader that loaded the priority files (shares
        ///   labelDic / exm / output so on-demand loads land in the same state).</param>
        /// <param name="labelDic">Shared label dictionary.</param>
        /// <param name="catalog">Built FunctionCatalog.</param>
        /// <param name="deferredFiles">(display-name, full-path) pairs NOT loaded at boot.</param>
        /// <param name="alreadyCompiledPaths">Full paths already compiled (priority files).</param>
        public static void Activate(
            ErbLoader loader,
            LabelDictionary labelDic,
            FunctionCatalog catalog,
            IEnumerable<KeyValuePair<string, string>> deferredFiles,
            IEnumerable<string> alreadyCompiledPaths)
        {
            var inst = new OnDemandErbCompiler(loader, labelDic, catalog);
            bool sessionOk = GameSession.IsValid(inst.sessionId_);

            // Build the deferred-file set once (O(n)).
            var deferredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (deferredFiles != null)
            {
                foreach (var kv in deferredFiles)
                    if (!string.IsNullOrEmpty(kv.Value))
                        deferredSet.Add(kv.Value);
            }

            if (alreadyCompiledPaths != null)
                foreach (var p in alreadyCompiledPaths)
                    if (!string.IsNullOrEmpty(p))
                        inst.fileStates_[p] = FunctionCompileState.Compiled;

            // Index deferred functions from the catalog metadata.
            foreach (var meta in catalog.AllOrdered)
            {
                FunctionCompileState fileState;
                if (inst.fileStates_.TryGetValue(meta.FilePath, out fileState) &&
                    fileState == FunctionCompileState.Compiled)
                {
                    // Already compiled at boot (priority file) — reflect that.
                    if (meta.State == FunctionCompileState.Catalogued ||
                        meta.State == FunctionCompileState.Queued)
                        meta.State = FunctionCompileState.Compiled;
                    continue;
                }
                if (!deferredSet.Contains(meta.FilePath))
                    continue;

                if (!inst.fileToDisplayName_.ContainsKey(meta.FilePath))
                    inst.fileToDisplayName_[meta.FilePath] = meta.FileName;

                if (!inst.declFilesByFunc_.TryGetValue(meta.Name, out var files))
                {
                    files = new List<string>(1);
                    inst.declFilesByFunc_[meta.Name] = files;
                }
                if (!files.Contains(meta.FilePath))
                    files.Add(meta.FilePath);
            }

            inst.remaining_ = inst.fileToDisplayName_.Count;

            // Nothing deferred (all files were priority) or session already ended:
            // do not take ownership — the normal "function not found" path applies.
            if (!sessionOk || inst.remaining_ == 0)
            {
                Instance = null;
                return;
            }

            Instance = inst;
            UnityEngine.Debug.Log(string.Format(
                "[OnDemandErbCompiler] Lazy compile active: {0} deferred files, {1} functions indexed.",
                inst.remaining_, inst.declFilesByFunc_.Count));
        }

        /// <summary>
        /// Clears the singleton (game teardown / full reload). Also called by the
        /// loader when a full <c>LoadErbFiles</c> (Safe) or reload replaces the lazy
        /// session.
        /// </summary>
        public static void Clear()
        {
            Instance = null;
        }

        // ------------------------------------------------------------------ //
        //  State                                                               //
        // ------------------------------------------------------------------ //

        readonly ErbLoader loader_;
        readonly LabelDictionary labelDic_;
        readonly FunctionCatalog catalog_;

        // function name (OrdinalIgnoreCase) → files declaring it, in declaration order
        readonly Dictionary<string, List<string>> declFilesByFunc_;
        // file path (OrdinalIgnoreCase) → display name used by loadErb
        readonly Dictionary<string, string> fileToDisplayName_;
        // File state is separate from per-function metadata state. In particular,
        // Compiling must never be treated as Compiled after a parser failure.
        readonly Dictionary<string, FunctionCompileState> fileStates_;
        readonly Dictionary<string, LazyCompileFailure> failures_;
        // session at activation — abort on stale requests after restart
        readonly int sessionId_;
        // number of deferred files not yet compiled
        int remaining_;

        /// <summary>Number of deferred files not yet compiled (0 = all done).</summary>
        public int RemainingFiles => remaining_;

        /// <summary>True while the lazy compiler owns the session and has work left.</summary>
        public bool IsActive => Instance == this && remaining_ > 0;

        private OnDemandErbCompiler(ErbLoader loader, LabelDictionary labelDic, FunctionCatalog catalog)
        {
            loader_       = loader;
            labelDic_     = labelDic;
            catalog_      = catalog;
            declFilesByFunc_ = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            fileToDisplayName_ = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            fileStates_ = new Dictionary<string, FunctionCompileState>(StringComparer.OrdinalIgnoreCase);
            failures_ = new Dictionary<string, LazyCompileFailure>(StringComparer.OrdinalIgnoreCase);
            sessionId_     = GameSession.Current;
        }

        // ------------------------------------------------------------------ //
        //  Public API (interpreter thread)                                    //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Compiles the file containing <paramref name="name"/> (non-event lookup —
        /// first declaration) on the interpreter thread if not already compiled.
        /// Returns true if the function should now be resolvable.
        /// </summary>
        public bool EnsureFunction(string name)
        {
            return EnsureCompiled(name, compileAllDecls: false);
        }

        /// <summary>
        /// Compiles EVERY file that declares <paramref name="name"/> — used for event
        /// dispatch so all #PRI / #NORMAL / #LATER copies of an event are available.
        /// </summary>
        public bool EnsureEventLoaded(string name)
        {
            return EnsureCompiled(name, compileAllDecls: true);
        }

        /// <summary>
        /// True while <paramref name="name"/> is defined in a deferred file that has
        /// not been compiled yet (used during syntax checks to suppress false
        /// "function not found" warnings — the runtime fallback compiles on demand).
        /// </summary>
        public bool IsFunctionPending(string name)
        {
            if (!IsActive || string.IsNullOrEmpty(name)) return false;
            if (!declFilesByFunc_.TryGetValue(name, out var files) || files.Count == 0)
                return false;
            for (int i = 0; i < files.Count; i++)
            {
                FunctionCompileState state;
                if (!fileStates_.TryGetValue(files[i], out state) ||
                    (state != FunctionCompileState.Compiled && state != FunctionCompileState.Failed))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True while <paramref name="name"/> is defined anywhere in the game's ERBs
        /// (catalog) but not yet resolvable — used to turn forward references into
        /// lazily-resolved method terms instead of "unrecognized identifier" warnings.
        /// </summary>
        public bool IsKnownFunction(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return catalog_ != null && catalog_.IsReady && catalog_.FunctionExists(name);
        }

        public LazyCompileFailure GetFailure(string name)
        {
            if (string.IsNullOrEmpty(name) || !declFilesByFunc_.TryGetValue(name, out var files))
                return null;
            for (int i = 0; i < files.Count; i++)
            {
                LazyCompileFailure failure;
                if (failures_.TryGetValue(files[i], out failure))
                    return failure;
            }
            return null;
        }

        // ------------------------------------------------------------------ //
        //  Internals                                                           //
        // ------------------------------------------------------------------ //

        bool EnsureCompiled(string name, bool compileAllDecls)
        {
            if (!IsActive || string.IsNullOrEmpty(name))
                return false;
            if (!declFilesByFunc_.TryGetValue(name, out var files) || files.Count == 0)
                return false;

            if (compileAllDecls)
            {
                foreach (var f in files)
                    CompileFile(f);
                return true;
            }

            // Non-event: first declaration is the one that registers into the
            // nonevent dictionary under a full load (order-preserving).
            CompileFile(files[0]);
            FunctionCompileState state;
            return fileStates_.TryGetValue(files[0], out state) &&
                (state == FunctionCompileState.Compiled || state == FunctionCompileState.Failed);
        }

        void CompileFile(string fullPath)
        {
            FunctionCompileState existingState;
            if (fileStates_.TryGetValue(fullPath, out existingState) &&
                (existingState == FunctionCompileState.Compiled ||
                 existingState == FunctionCompileState.Failed ||
                 existingState == FunctionCompileState.Compiling))
                return;

            // Abort if the game session advanced (restart while a reference was
            // outstanding). The caller falls back to a clean "function not found".
            if (!GameSession.IsValid(sessionId_))
                return;

            // Mark before parsing only as an in-flight guard. Promote to Compiled
            // only after the complete file has parsed and linked successfully.
            fileStates_[fullPath] = FunctionCompileState.Compiling;

            string display = fileToDisplayName_[fullPath];
            try
            {
                GenericUtils.SetLoadingStatus("Loading function file: " + display);
                LazyCompileFailure failure;
                if (!loader_.LoadSingleErbLazy(fullPath, display, out failure))
                {
                    failures_[fullPath] = failure ?? LazyCompileFailure.CreateGeneric(display, fullPath);
                    fileStates_[fullPath] = FunctionCompileState.Failed;
                    MarkFile(fullPath, FunctionCompileState.Failed);
                    remaining_--;
                    return;
                }

                fileStates_[fullPath] = FunctionCompileState.Compiled;
                MarkFile(fullPath, FunctionCompileState.Compiled);
                remaining_--;
                UnityEngine.Debug.Log(string.Format(
                    "[OnDemandErbCompiler] Compiled '{0}' ({1} deferred files remain)", display, remaining_));
                if (remaining_ <= 0)
                    GenericUtils.SetLoadingStatus("");
            }
            catch (Exception ex)
            {
                failures_[fullPath] = LazyCompileFailure.CreateException(display, fullPath, ex);
                fileStates_[fullPath] = FunctionCompileState.Failed;
                MarkFile(fullPath, FunctionCompileState.Failed);
                remaining_--;
                if (remaining_ < 0) remaining_ = 0;
                UnityEngine.Debug.LogWarning(string.Format(
                    "[OnDemandErbCompiler] Failed to compile '{0}': {1}\n{2}",
                    display, ex.Message, ex.StackTrace));
            }
        }

        void MarkFile(string fullPath, FunctionCompileState state)
        {
            foreach (var meta in catalog_.AllOrdered)
            {
                if (!string.Equals(meta.FilePath, fullPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (meta.State == FunctionCompileState.Catalogued ||
                    meta.State == FunctionCompileState.Queued)
                    meta.State = state;
            }
        }
    }

    /// <summary>
    /// Compatibility facade for older call sites. New runtime resolution belongs to
    /// <see cref="FunctionResolver"/>; this type only forwards to the active lazy
    /// compiler and contains no background wait path.
    /// </summary>
    internal static class ErbOnDemand
    {
        public static bool IsFunctionPending(string name)
        {
            var od = OnDemandErbCompiler.Instance;
            return od != null && od.IsFunctionPending(name);
        }

        public static bool IsKnownFunction(string name)
        {
            var od = OnDemandErbCompiler.Instance;
            return od != null && od.IsKnownFunction(name);
        }

        /// <summary>
        /// Ensures the function's file is compiled. With the lazy compiler this
        /// compiles synchronously on the calling (interpreter) thread. With the
        /// legacy background loader it waits for the background thread (up to the
        /// loader timeout). Returns true when the function should be resolvable now.
        /// </summary>
        public static bool EnsureCompiled(string name)
        {
            var od = OnDemandErbCompiler.Instance;
            return od != null && od.EnsureFunction(name);
        }

        /// <summary>Ensures all files declaring an event function are compiled.</summary>
        public static bool EnsureEventLoaded(string name)
        {
            var od = OnDemandErbCompiler.Instance;
            return od != null && od.EnsureEventLoaded(name);
        }

        /// <summary>True while any deferred/progressive loader still has work left.</summary>
        public static bool AnythingLoading()
        {
            var od = OnDemandErbCompiler.Instance;
            return od != null && od.IsActive;
        }
    }
}
