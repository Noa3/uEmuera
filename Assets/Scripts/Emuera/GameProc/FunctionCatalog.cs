using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Compile state of a function body (distinct from its metadata being known).
    /// </summary>
    public enum FunctionCompileState
    {
        /// <summary>Metadata known; body not yet parsed.</summary>
        Catalogued,
        /// <summary>Body parse queued (for future idle-time scheduler).</summary>
        Queued,
        /// <summary>Body parse in progress on the interpreter thread.</summary>
        Compiling,
        /// <summary>Body parsed and linked; callable.</summary>
        Compiled,
        /// <summary>Body parse failed; calls will throw a CodeEE.</summary>
        Failed,
    }

    /// <summary>
    /// Return-type category derived from #FUNCTION / #FUNCTIONS directives.
    /// </summary>
    public enum FunctionReturnKind
    {
        /// <summary>No #FUNCTION directive — normal (void-ish) ERB function.</summary>
        Void,
        /// <summary>#FUNCTION — in-expression integer function.</summary>
        Int64,
        /// <summary>#FUNCTIONS — in-expression string function.</summary>
        String,
    }

    /// <summary>
    /// Metadata for one ERB function declaration, extracted by a lightweight
    /// line-by-line scan (uEmuera Phase 6 #11–#12).
    ///
    /// All fields are immutable after construction except <see cref="State"/>.
    /// </summary>
    public sealed class FunctionMetadata
    {
        /// <summary>Function name exactly as declared (@ stripped).</summary>
        public readonly string Name;
        /// <summary>Display name (short filename as shown in errors).</summary>
        public readonly string FileName;
        /// <summary>Full absolute path of the ERB file.</summary>
        public readonly string FilePath;
        /// <summary>1-based line number of the @ declaration inside <see cref="FilePath"/>.</summary>
        public readonly int LineNumber;
        /// <summary>Whether this function is an in-expression function (#FUNCTION or #FUNCTIONS).</summary>
        public readonly bool IsMethod;
        /// <summary>Return type derived from directives (Void / Int64 / String).</summary>
        public readonly FunctionReturnKind ReturnKind;
        /// <summary>#PRI flag.</summary>
        public readonly bool IsPri;
        /// <summary>#LATER flag.</summary>
        public readonly bool IsLater;
        /// <summary>#ONLY flag.</summary>
        public readonly bool IsOnly;
        /// <summary>#SINGLE flag.</summary>
        public readonly bool IsSingle;

        /// <summary>Compile state — mutable after construction (interpreter thread only).</summary>
        public volatile FunctionCompileState State;

        public FunctionMetadata(
            string name, string fileName, string filePath, int lineNumber,
            FunctionReturnKind returnKind, bool isPri, bool isLater, bool isOnly, bool isSingle)
        {
            Name       = name;
            FileName   = fileName;
            FilePath   = filePath;
            LineNumber = lineNumber;
            ReturnKind = returnKind;
            IsMethod   = returnKind != FunctionReturnKind.Void;
            IsPri      = isPri;
            IsLater    = isLater;
            IsOnly     = isOnly;
            IsSingle   = isSingle;
            State      = FunctionCompileState.Catalogued;
        }

        /// <summary>CLR type used by IOperandTerm constructors.</summary>
        public Type ClrReturnType
        {
            get
            {
                switch (ReturnKind)
                {
                    case FunctionReturnKind.Int64:  return typeof(Int64);
                    case FunctionReturnKind.String: return typeof(string);
                    default:                        return typeof(void);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("@{0} [{1}] {2}:{3}", Name, ReturnKind, FileName, LineNumber);
        }
    }

    /// <summary>
    /// Lightweight catalog of every ERB function declaration in the current game
    /// (uEmuera Phase 6 #11–#14).
    ///
    /// <para>Built once by a line-by-line scan of all ERB files, using the game's
    /// configured encoding (respects BOM, never hard-codes Shift-JIS).  The scan
    /// records the minimal metadata needed for:</para>
    /// <list type="bullet">
    ///   <item><see cref="FunctionExists"/> / <see cref="GetReturnKind"/> — lets
    ///     <c>EXISTFUNCTION</c> answer correctly before body compilation.</item>
    ///   <item>Correct return-type inference for
    ///     <c>PendingUserDefinedMethodTerm</c> (fixes Phase 6 #8).</item>
    ///   <item>Function-metadata-based loading priority for a future Fast-boot
    ///     compiler (Phase 6 #6 / #19).</item>
    /// </list>
    ///
    /// <para>Thread-safety: Build is single-threaded on the interpreter thread;
    /// all read methods are lock-free after <see cref="IsReady"/> becomes true.</para>
    /// </summary>
    public sealed class FunctionCatalog
    {
        // ---- singleton -------------------------------------------------------
        static FunctionCatalog instance_;

        /// <summary>
        /// The current game's catalog, or null if no catalog has been built yet.
        /// </summary>
        public static FunctionCatalog Instance => instance_;

        /// <summary>
        /// Replaces the current singleton catalog (interpreter thread only).
        /// </summary>
        static void SetInstance(FunctionCatalog catalog)
        {
            instance_ = catalog;
        }

        // ---- fields ----------------------------------------------------------
        // name → list of all declarations (event-ordered duplicates allowed)
        readonly Dictionary<string, List<FunctionMetadata>> byName_;
        // Ordered list for priority scheduling
        readonly List<FunctionMetadata> ordered_;
        bool isReady_;

        FunctionCatalog(int capacity)
        {
            byName_  = new Dictionary<string, List<FunctionMetadata>>(
                capacity, StringComparer.OrdinalIgnoreCase);
            ordered_ = new List<FunctionMetadata>(capacity);
        }

        // ---- public API -------------------------------------------------------

        /// <summary>True once the catalog has been fully built.</summary>
        public bool IsReady => isReady_;

        /// <summary>Total number of unique function names in the catalog.</summary>
        public int Count => byName_.Count;

        /// <summary>
        /// Returns true if at least one declaration of <paramref name="name"/> exists
        /// in any ERB file (regardless of compile state).
        /// </summary>
        public bool FunctionExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return byName_.ContainsKey(name);
        }

        /// <summary>
        /// Returns the return kind of <paramref name="name"/>: Void / Int64 / String.
        /// When a function has multiple declarations (events), returns the return kind of
        /// the first declaration that has a non-Void kind; otherwise Void.
        /// Returns Void when the function is not in the catalog.
        /// </summary>
        public FunctionReturnKind GetReturnKind(string name)
        {
            if (string.IsNullOrEmpty(name)) return FunctionReturnKind.Void;
            List<FunctionMetadata> list;
            if (!byName_.TryGetValue(name, out list))
                return FunctionReturnKind.Void;
            for (int i = 0; i < list.Count; i++)
                if (list[i].ReturnKind != FunctionReturnKind.Void)
                    return list[i].ReturnKind;
            return FunctionReturnKind.Void;
        }

        /// <summary>
        /// Returns the CLR type (<c>typeof(Int64)</c>, <c>typeof(string)</c>, or
        /// <c>typeof(void)</c>) for the first matching function — suitable for
        /// constructing <c>IOperandTerm</c> instances.
        /// </summary>
        public Type GetClrReturnType(string name)
        {
            FunctionReturnKind kind = GetReturnKind(name);
            switch (kind)
            {
                case FunctionReturnKind.Int64:  return typeof(Int64);
                case FunctionReturnKind.String: return typeof(string);
                default:                        return typeof(void);
            }
        }

        /// <summary>
        /// Returns the first metadata entry for <paramref name="name"/>, or null
        /// if not catalogued.
        /// </summary>
        public FunctionMetadata GetFirst(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            List<FunctionMetadata> list;
            if (!byName_.TryGetValue(name, out list) || list.Count == 0)
                return null;
            return list[0];
        }

        /// <summary>
        /// Returns all declarations of <paramref name="name"/>, or an empty list.
        /// </summary>
        public IReadOnlyList<FunctionMetadata> GetAll(string name)
        {
            List<FunctionMetadata> list;
            if (!string.IsNullOrEmpty(name) && byName_.TryGetValue(name, out list))
                return list;
            return Array.Empty<FunctionMetadata>();
        }

        /// <summary>Ordered list of all declarations (stable declaration order).</summary>
        public IReadOnlyList<FunctionMetadata> AllOrdered => ordered_;

        /// <summary>EXISTFUNCTION return value (0/1/2/3) matching the reference spec.</summary>
        public int ExistFunctionValue(string name)
        {
            if (!FunctionExists(name)) return 0;
            FunctionReturnKind kind = GetReturnKind(name);
            if (kind == FunctionReturnKind.Int64)  return 2;
            if (kind == FunctionReturnKind.String) return 3;
            return 1;
        }

        // ---- builder ---------------------------------------------------------

        /// <summary>
        /// Scans all <paramref name="erbFiles"/> for function declarations and stores
        /// the result as the process-wide singleton catalog.
        /// Call from the interpreter thread after encoding is configured.
        /// </summary>
        /// <param name="erbFiles">Pairs of (display-name, full-path) as returned by
        ///   <c>Config.GetFiles</c>.</param>
        /// <param name="encoding">Game's configured text encoding. Pass
        ///   <c>Config.Encode</c>; the reader auto-detects BOM on top of this.</param>
        /// <returns>The newly built catalog (also stored as <see cref="Instance"/>).</returns>
        public static FunctionCatalog Build(
            IList<KeyValuePair<string, string>> erbFiles,
            Encoding encoding)
        {
            // Persistent cache: if a matching cache exists (same files, lengths,
            // timestamps, encoding and ICFunction flag) reuse it instead of
            // re-reading every ERB line by line. Best-effort — any failure falls
            // through to a full scan.
            FunctionCatalog cached;
            if (CatalogCacheStore.TryLoadFunctionCatalog(erbFiles, encoding, out cached))
            {
                SetInstance(cached);
                return cached;
            }

            var catalog = new FunctionCatalog(erbFiles.Count * 4);
            for (int fi = 0; fi < erbFiles.Count; fi++)
            {
                string displayName = erbFiles[fi].Key;
                string filePath    = erbFiles[fi].Value;
                ScanFile(catalog, displayName, filePath, encoding);
            }
            catalog.isReady_ = true;
            SetInstance(catalog);
            CatalogCacheStore.SaveFunctionCatalog(catalog, erbFiles, encoding);
            return catalog;
        }

        /// <summary>
        /// Rebuilds a ready catalog from previously serialized metadata records
        /// (cache load). Declaration order is preserved; every entry starts in
        /// <see cref="FunctionCompileState.Catalogued"/>.
        /// </summary>
        internal static FunctionCatalog FromMetadata(IList<FunctionMetadata> records)
        {
            var catalog = new FunctionCatalog(records.Count);
            for (int i = 0; i < records.Count; i++)
                Commit(catalog, records[i].Name, records[i].FileName, records[i].FilePath,
                    records[i].LineNumber, records[i].ReturnKind,
                    records[i].IsPri, records[i].IsLater, records[i].IsOnly, records[i].IsSingle);
            catalog.isReady_ = true;
            return catalog;
        }

        /// <summary>
        /// Clears the singleton (call on game teardown before the next game starts).
        /// </summary>
        public static void Clear()
        {
            instance_ = null;
        }

        // ---- per-file scan ---------------------------------------------------

        static void ScanFile(
            FunctionCatalog catalog,
            string displayName,
            string filePath,
            Encoding encoding)
        {
            try
            {
                // detectEncodingFromByteOrderMarks: true — respects UTF-8 / UTF-16 BOM
                // so games that mix encodings (common in ERA) are handled correctly.
                using (var sr = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true))
                {
                    string funcName  = null;
                    int    funcLine  = 0;
                    bool   isPri     = false;
                    bool   isLater   = false;
                    bool   isOnly    = false;
                    bool   isSingle  = false;
                    FunctionReturnKind returnKind = FunctionReturnKind.Void;

                    int lineNo = 0;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineNo++;
                        if (line.Length == 0) continue;

                        char first = line[0];

                        // ---- New function declaration -------------------------
                        if (first == '@')
                        {
                            // Commit previous function (if any)
                            if (funcName != null)
                                Commit(catalog, funcName, displayName, filePath,
                                       funcLine, returnKind, isPri, isLater, isOnly, isSingle);

                            // Reset state
                            isPri     = false;
                            isLater   = false;
                            isOnly    = false;
                            isSingle  = false;
                            returnKind = FunctionReturnKind.Void;

                            // Parse name (@@FOO is a goto label, not a function)
                            if (line.Length < 2 || line[1] == '@')
                            {
                                funcName = null;
                                continue;
                            }

                            int end = 1;
                            while (end < line.Length)
                            {
                                char c = line[end];
                                if (c == '(' || c == ' ' || c == '\t' || c == ';' || c == ',')
                                    break;
                                end++;
                            }
                            string raw = line.Substring(1, end - 1).Trim();
                            if (raw.Length == 0)
                            {
                                funcName = null;
                                continue;
                            }
                            funcName = Config.ICFunction ? raw.ToUpper() : raw;
                            funcLine = lineNo;
                            continue;
                        }

                        // ---- Directives only meaningful inside a function -----
                        if (funcName == null) continue;

                        // Skip blank / comment lines
                        if (first == ';') continue;
                        if (first == ' ' || first == '\t')
                        {
                            // Trim for '#' directives that may be indented
                            string trimmed = line.TrimStart();
                            if (trimmed.Length > 0 && trimmed[0] == '#')
                                line = trimmed;
                            else
                                continue;
                            first = '#';
                        }

                        if (first != '#') continue;

                        // Compare directive names (case-insensitive per Emuera spec)
                        if (StartsWithCI(line, "#FUNCTIONS"))
                        {
                            returnKind = FunctionReturnKind.String;
                        }
                        else if (StartsWithCI(line, "#FUNCTION") &&
                                 !StartsWithCI(line, "#FUNCTIONS"))
                        {
                            returnKind = FunctionReturnKind.Int64;
                        }
                        else if (StartsWithCI(line, "#PRI"))
                        {
                            isPri = true;
                        }
                        else if (StartsWithCI(line, "#LATER"))
                        {
                            isLater = true;
                        }
                        else if (StartsWithCI(line, "#ONLY"))
                        {
                            isOnly = true;
                        }
                        else if (StartsWithCI(line, "#SINGLE"))
                        {
                            isSingle = true;
                        }
                    }
                    // Commit the last function in the file
                    if (funcName != null)
                        Commit(catalog, funcName, displayName, filePath,
                               funcLine, returnKind, isPri, isLater, isOnly, isSingle);
                }
            }
            catch (Exception ex)
            {
                // Unreadable file: skip silently (same policy as QuickScanFunctionNames).
                UnityEngine.Debug.LogWarning(
                    string.Format("[FunctionCatalog] Could not scan '{0}': {1}",
                        filePath, ex.Message));
            }
        }

        static void Commit(
            FunctionCatalog catalog,
            string funcName, string displayName, string filePath, int lineNo,
            FunctionReturnKind returnKind,
            bool isPri, bool isLater, bool isOnly, bool isSingle)
        {
            var meta = new FunctionMetadata(
                funcName, displayName, filePath, lineNo,
                returnKind, isPri, isLater, isOnly, isSingle);

            List<FunctionMetadata> list;
            if (!catalog.byName_.TryGetValue(funcName, out list))
            {
                list = new List<FunctionMetadata>(2);
                catalog.byName_[funcName] = list;
            }
            list.Add(meta);
            catalog.ordered_.Add(meta);
        }

        /// <summary>Case-insensitive prefix match (faster than ToUpper alloc).</summary>
        static bool StartsWithCI(string line, string prefix)
        {
            if (line.Length < prefix.Length) return false;
            return string.Compare(line, 0, prefix, 0, prefix.Length,
                StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
