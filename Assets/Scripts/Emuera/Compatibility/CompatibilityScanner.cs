using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.Compatibility
{
    /// <summary>
    /// Static analyzer that scans Emuera game source (ERB/ERH files) and cross-references
    /// every discovered token against the engine's *own* registries (ground truth):
    /// built-in instructions (<see cref="FunctionIdentifier"/>) and built-in methods
    /// (<see cref="FunctionMethodCreator"/>). Nothing is hard-coded: if a token is not in a
    /// registry, the scanner cannot claim support for it.
    /// </summary>
    internal static class CompatibilityScanner
    {
        #region registry (ground truth)

        static readonly HashSet<string> instructionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> methodSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static bool registriesLoaded = false;

        static void EnsureRegistries()
        {
            if (registriesLoaded)
                return;
            foreach (KeyValuePair<string, FunctionIdentifier> pair in FunctionIdentifier.GetInstructionNameDic())
                instructionSet.Add(pair.Key);
            Dictionary<string, FunctionMethod> methodDic = FunctionMethodCreator.GetMethodList();
            if (methodDic != null)
                foreach (string name in methodDic.Keys)
                    methodSet.Add(name);
            registriesLoaded = true;
        }

        public static bool IsInstruction(string name) { EnsureRegistries(); return instructionSet.Contains(name); }

        /// <summary>All built-in instructions the engine registry declares (ground truth for diffs).</summary>
        public static IEnumerable<string> AllInstructions() { EnsureRegistries(); return instructionSet; }

        /// <summary>All built-in methods the engine registry declares (ground truth for diffs).</summary>
        public static IEnumerable<string> AllMethods() { EnsureRegistries(); return methodSet; }

        // SET is Emuera's explicit-assignment keyword. The engine registers it separately
        // (FunctionIdentifier.setFunc), not inside the instruction dictionary, so mirror that here.
        static bool IsAssignmentKeyword(string name) { return name.Equals("SET", StringComparison.OrdinalIgnoreCase); }
        public static bool IsMethod(string name) { EnsureRegistries(); return methodSet.Contains(name); }
        public static int InstructionCount { get { EnsureRegistries(); return instructionSet.Count; } }
        public static int MethodCount { get { EnsureRegistries(); return methodSet.Count; } }

        #endregion

        #region tokenizer

        // Identifier terminators, mirroring LexicalAnalyzer.ReadSingleIdentifier.
        const string ID_BREAK = " \t+-*/%=!<>|&^~?#(){}[]$\\'\"@.;,:" + "\u3000";

        static bool IsBreak(char c) { return ID_BREAK.IndexOf(c) >= 0; }

        /// <summary>First word of a logical line (the statement token). Null for label/comment lines.</summary>
        public static string ReadStatementWord(string line)
        {
            if (line == null)
                return null;
            int i = 0, n = line.Length;
            while (i < n && (line[i] == ' ' || line[i] == '\t' || line[i] == '\u3000'))
                i++;
            if (i >= n)
                return null;
            char c = line[i];
            if (c == ';' || c == '@' || c == '#' || c == '$' || c == '\\')
                return null;
            int start = i;
            while (i < n && !IsBreak(line[i]))
                i++;
            return line.Substring(start, i - start);
        }

        /// <summary>Label of `@LABEL` at the head of <paramref name="rest"/>, else null.</summary>
        public static string ReadTargetLabel(string rest)
        {
            if (rest == null)
                return null;
            int i = 0, n = rest.Length;
            while (i < n && (rest[i] == ' ' || rest[i] == '\t' || rest[i] == '\u3000'))
                i++;
            if (i >= n || rest[i] != '@')
                return null;
            i++;
            int start = i;
            while (i < n && !IsBreak(rest[i]) && rest[i] != '(')
                i++;
            if (i == start)
                return null;
            return rest.Substring(start, i - start);
        }

        /// <summary>
        /// Walk every identifier in a line (skipping string literals and trailing comments),
        /// pairing each with whether a '(' immediately follows (a method-call candidate).
        /// </summary>
        public static IEnumerable<IdentifierUse> ReadIdentifierUses(string line)
        {
            if (line == null)
                yield break;
            int i = 0, n = line.Length;
            bool inString = false;
            while (i < n)
            {
                char c = line[i];
                if (inString)
                {
                    if (c == '"')
                        inString = false;
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inString = true;
                    i++;
                    continue;
                }
                if (c == ';')
                    yield break;
                if (IsBreak(c))
                {
                    i++;
                    continue;
                }
                int start = i;
                while (i < n && !IsBreak(line[i]))
                    i++;
                string id = line.Substring(start, i - start);
                int j = i;
                while (j < n && (line[j] == ' ' || line[j] == '\t'))
                    j++;
                bool call = j < n && line[j] == '(';
                yield return new IdentifierUse(id, call);
            }
        }

        #endregion

        #region corpus scan

        public static CompatibilityReport ScanDirectory(string root, IEnumerable<string> targetFragments = null)
        {
            EnsureRegistries();
            var report = new CompatibilityReport();
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return report;
            var files = new List<string>();
            CollectErbFiles(root, files);
            if (targetFragments != null)
            {
                var fragments = new List<string>(targetFragments);
                files.RemoveAll(f => !ContainsAny(f, fragments));
            }
            report.FilesScanned = files.Count;
            // Pre-collect user-defined methods so they are not flagged as unknown.
            HashSet<string> userFunctions = CollectUserFunctions(root);
            foreach (string file in files)
            {
                try { ScanFile(file, report, userFunctions); }
                catch (Exception e) { report.FileErrors.Add(Path.GetFileName(file) + " :: " + e.Message); }
            }
            report.FilesWithErrors = report.FileErrors.Count;
            return report;
        }

        /// <summary>
        /// Collect user-defined method names (labels followed by #FUNCTION/#FUNCTIONS)
        /// from the corpus so they are classified as user methods, not unknown tokens.
        /// Mirrors LogicalLineParser.cs #FUNCTION handling.
        /// </summary>
        public static HashSet<string> CollectUserFunctions(string root)
        {
            var ret = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return ret;
            var files = new List<string>();
            CollectErbFiles(root, files);
            foreach (string file in files)
                CollectUserFunctionsFromFile(file, ret);
            return ret;
        }

        static void CollectUserFunctionsFromFile(string path, HashSet<string> into)
        {
            string src;
            try { src = EraEncoding.ReadText(path); }
            catch { return; }
            string currentLabel = null;
            foreach (string raw in src.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                string word = ReadStatementWord(line);
                // #FUNCTION / #FUNCTIONS are directives that start with '#', which
                // ReadStatementWord deliberately rejects (it returns null). Handle them
                // explicitly before label detection.
                string directive = ReadDirectiveWord(line);
                if (directive != null)
                {
                    // #FUNCTION/#FUNCTIONS mark the head of a user-defined method. Other
                    // directives (#DIM, #IF, ...) may appear between the @label and the
                    // FUNCTION marker, so they must not clear currentLabel.
                    if ((directive.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase) ||
                         directive.Equals("FUNCTIONS", StringComparison.OrdinalIgnoreCase)) && currentLabel != null)
                        into.Add(currentLabel);
                    continue;
                }
                if (word == null)
                {
                    // @label? Remember it.
                    string label = ReadLabelDirective(line);
                    if (label != null)
                        currentLabel = label;
                    continue;
                }
                // A statement between label and FUNCTION marker ends the association.
                currentLabel = null;
            }
        }

        /// <summary>First word of a line when it starts with '#' (directive), else null.</summary>
        static string ReadDirectiveWord(string line)
        {
            if (line == null)
                return null;
            int i = 0, n = line.Length;
            while (i < n && (line[i] == ' ' || line[i] == '\t' || line[i] == '\u3000'))
                i++;
            if (i >= n || line[i] != '#')
                return null;
            i++; // consume '#'
            int start = i;
            while (i < n && !IsBreak(line[i]))
                i++;
            return (i == start) ? null : line.Substring(start, i - start);
        }

        static string ReadLabelDirective(string line)
        {
            if (line == null)
                return null;
            int i = 0, n = line.Length;
            while (i < n && (line[i] == ' ' || line[i] == '\t' || line[i] == '\u3000'))
                i++;
            if (i >= n || line[i] != '@')
                return null;
            i++;
            int start = i;
            while (i < n && !IsBreak(line[i]))
                i++;
            if (i == start)
                return null;
            return line.Substring(start, i - start);
        }

        static bool ContainsAny(string s, List<string> fragments)
        {
            foreach (string f in fragments)
                if (s.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        static void CollectErbFiles(string dir, List<string> into)
        {
            GameVirtualFileSystem vfs;
            try { vfs = new GameVirtualFileSystem(dir); }
            catch { return; }
            string[] erbFiles = vfs.EnumerateFiles(string.Empty, "*.ERB", true);
            string[] erhFiles = vfs.EnumerateFiles(string.Empty, "*.ERH", true);
            for (int i = 0; i < erbFiles.Length; i++)
            {
                string fullPath;
                if (vfs.TryResolve(erbFiles[i], out fullPath) && File.Exists(fullPath))
                    into.Add(fullPath);
            }
            for (int i = 0; i < erhFiles.Length; i++)
            {
                string fullPath;
                if (vfs.TryResolve(erhFiles[i], out fullPath) && File.Exists(fullPath))
                    into.Add(fullPath);
            }
        }

        static IEnumerable<string> SafeDirectories(string dir)
        {
            try { return Directory.GetDirectories(dir); }
            catch { return new string[0]; }
        }

        static IEnumerable<string> SafeFiles(string dir)
        {
            try { return Directory.GetFiles(dir); }
            catch { return new string[0]; }
        }

        static void ScanFile(string path, CompatibilityReport report, HashSet<string> userFunctions)
        {
            string src;
            try { src = EraEncoding.ReadText(path); }
            catch { return; }
            string[] lines = src.Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0)
                    continue;
                report.LogicalLinesSeen++;
                string word = ReadStatementWord(line);
                if (word == null)
                    continue; // label / comment / macro line
                if (IsInstruction(word) || IsAssignmentKeyword(word))
                {
                    report.Instructions.Increment(word);
                    report.FeatureAreas.Increment(ClassifyFeature(word));
                    // CALL @LABEL style targets
                    string label = ReadTargetLabel(line.Substring(word.Length));
                    if (label != null)
                        report.CallTargets.Increment(label);
                    continue;
                }
                // Not an instruction: could be a bare method, an assignment to a variable
                // whose name shadows nothing, or a user-defined function. Classify by registry.
                foreach (IdentifierUse use in ReadIdentifierUses(line))
                {
                    if (IsMethod(use.Name))
                    {
                        report.Methods.Increment(use.Name);
                        continue;
                    }
                    if (IsUserFunction(use.Name, userFunctions))
                    {
                        report.UserFunctions.Increment(use.Name);
                        continue;
                    }
                    if (use.IsCallLike)
                        report.UnknownTokens.Increment(use.Name);
                }
            }
        }

        static bool IsUserFunction(string name, HashSet<string> userFunctions)
        {
            return userFunctions != null && userFunctions.Contains(name);
        }

        /// <summary>
        /// Map a statement token to the feature area it belongs to (for the combined
        /// games report, brief §4). Conservative: only well-known feature areas are
        /// classified; everything else falls into OUTPUT/CONTROL by simple prefix rule.
        /// </summary>
        public static string ClassifyFeature(string token)
        {
            string t = token.ToUpperInvariant();
            if (t == "HTML_PRINT" || t == "HTML_TAGSPLIT" || t == "HTML_POPPRINTINGSTR" ||
                t == "HTML_GETPRINTEDSTR" || t == "PRINT_IMG" || t == "PRINT_RECT" || t == "PRINT_SPACE")
                return "HTML";
            if (t.StartsWith("G") && (t.Contains("DRAW") || t == "GCREATE" || t == "GCREATEFROMFILE" ||
                t == "GDISPOSE" || t == "GCLEAR" || t == "GFILLRECTANGLE" || t == "GSETCOLOR" ||
                t == "GSETBRUSH" || t == "GSETFONT" || t == "GSETPEN" || t == "GSAVE" || t == "GLOAD" ||
                t == "GGETCOLOR" || t == "GRED")
                && !t.StartsWith("GOTO"))
                return "Graphics";
            if (t.StartsWith("SPRITE") || t == "SETANIMETIMER")
                return "Sprite";
            if (t.StartsWith("CBG"))
                return "CBG";
            if (t.StartsWith("PLAY") || t.StartsWith("STOP") || t.Contains("SOUND") || t.Contains("BGM"))
                return "Audio";
            if (t == "SAVEGAME" || t == "LOADGAME" || t == "SAVEDATA" || t == "LOADDATA" ||
                t == "SAVEGLOBAL" || t == "LOADGLOBAL" || t == "DELDATA" || t == "CHKDATA" ||
                t == "GETTIME" || t == "SAVEVAR" || t == "LOADVAR" || t == "SAVECHARA" ||
                t == "LOADCHARA" || t.StartsWith("PUTFORM") || t == "SAVENOS")
                return "Save";
            if (t == "INPUT" || t == "INPUTS" || t == "TINPUT" || t == "TINPUTS" ||
                t == "ONEINPUT" || t == "ONEINPUTS" || t == "TONEINPUT" || t == "TONEINPUTS" ||
                t == "WAIT" || t == "TWAIT" || t == "WAITANYKEY" || t == "FORCEWAIT" ||
                t == "BINPUT" || t == "BINPUTS" || t == "INPUTMOUSEKEY" || t == "AWAIT" ||
                t == "GETKEY" || t == "GETKEYTRIGGERED" || t == "MOUSEX" || t == "MOUSEY" || t == "MOUSEB" ||
                t == "ISACTIVE")
                return "Input";
            if (t == "PRINT" || t == "PRINTL" || t == "PRINTW" || t == "PRINTFORM" ||
                t == "PRINTFORML" || t == "PRINTFORMW" || t == "PRINTS" || t == "PRINTSL" ||
                t == "PRINTSW" || t == "PRINTC" || t == "PRINTLC" || t == "PRINTD" ||
                t == "PRINTSINGLE" || t == "PRINTDATA" || t == "PRINTBUTTON" || t == "PRINTPLAIN")
                return "Console";
            if (t == "SETCOLOR" || t == "SETCOLORBYNAME" || t == "RESETCOLOR" ||
                t == "SETBGCOLOR" || t == "RESETBGCOLOR" || t == "SETFONT" ||
                t == "FONTBOLD" || t == "FONTITALIC" || t == "FONTREGULAR" ||
                t == "FONTSTYLE" || t == "ALIGNMENT" || t == "REDRAW" || t == "SKIPDISP")
                return "Console";
            return "Control";
        }

        #endregion

        /// <summary>
        /// Detect era-script text encoding.
        /// Priority: UTF-8 BOM → UTF-16 LE/BE BOM → UTF-8 validity check → CP932 fallback → UTF-8.
        /// CP932 (Shift-JIS) detection covers legacy era games that predate UTF-8 adoption.
        /// Requires CodePagesEncodingProvider to be registered on .NET Core runtimes;
        /// Unity's Mono runtime exposes CP932 via I18N.CJK.dll without registration.
        /// Kept lossless so the scanner never mutates tokens.
        /// </summary>
        public static Encoding DetectEncoding(string path)
        {
            return EraEncoding.Detect(path);
        }
    }

    /// <summary>An identifier occurrence in a line, plus call-like lookahead.</summary>
    internal struct IdentifierUse
    {
        public readonly string Name;
        public readonly bool IsCallLike;
        public IdentifierUse(string name, bool isCallLike)
        {
            Name = name;
            IsCallLike = isCallLike;
        }
    }

    /// <summary>Execution-free summary used for JSON export and human review.</summary>
    internal sealed class CompatibilityReport
    {
        public int FilesScanned;
        public int FilesWithErrors;
        public int LogicalLinesSeen;
        public readonly TokenCounter Instructions = new TokenCounter();
        public readonly TokenCounter Methods = new TokenCounter();
        public readonly TokenCounter UserFunctions = new TokenCounter();
        public readonly TokenCounter UnknownTokens = new TokenCounter();
        public readonly TokenCounter CallTargets = new TokenCounter();
        public readonly TokenCounter FeatureAreas = new TokenCounter();
        public readonly List<string> FileErrors = new List<string>();

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"files_scanned\": " + FilesScanned + ",");
            sb.AppendLine("  \"files_with_errors\": " + FilesWithErrors + ",");
            sb.AppendLine("  \"logical_lines\": " + LogicalLinesSeen + ",");
            AppendCounter(sb, "instructions", Instructions, true);
            AppendCounter(sb, "methods", Methods, true);
            AppendCounter(sb, "user_functions", UserFunctions, true);
            AppendCounter(sb, "unknown_tokens", UnknownTokens, true);
            AppendCounter(sb, "feature_areas", FeatureAreas, true);
            AppendCounter(sb, "call_targets", CallTargets, FileErrors.Count > 0);
            if (FileErrors.Count > 0)
            {
                sb.AppendLine("  \"file_errors\": [");
                for (int i = 0; i < FileErrors.Count; i++)
                    sb.Append("    \"" + FileErrors[i].Replace("\"", "\\\"") + "\"" +
                        (i < FileErrors.Count - 1 ? "," : "") + Environment.NewLine);
                sb.AppendLine("  ]");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        void AppendCounter(StringBuilder sb, string name, TokenCounter c, bool appendComma)
        {
            sb.Append("  \"" + name + "\": {");
            int i = 0, n = c.Map.Count;
            foreach (KeyValuePair<string, int> pair in c.Map)
            {
                if (i++ == 0)
                    sb.Append(Environment.NewLine);
                sb.Append("    \"" + pair.Key.Replace("\"", "\\\"") + "\": " + pair.Value +
                    (i < n ? "," : "") + Environment.NewLine);
            }
            if (n > 0)
                sb.Append("  ");
            sb.AppendLine("}" + (appendComma ? "," : ""));
        }
    }

    /// <summary>Case-insensitive occurrence counter.</summary>
    internal sealed class TokenCounter
    {
        public readonly SortedDictionary<string, int> Map = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public void Increment(string token)
        {
            int v;
            if (Map.TryGetValue(token, out v))
                Map[token] = v + 1;
            else
                Map[token] = 1;
        }
    }
}
