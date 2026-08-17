using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// ERA variable and character data model for the EraElectron runtime.
    ///
    /// Owns all EraElectron game state separately from the Emuera runtime.
    /// NEVER shares memory with <c>MinorShift.Emuera.GlobalStatic</c>.
    ///
    /// Variable addressing:
    ///   era.get("callname:1")      → character 1's callname (string)
    ///   era.get("flag:5")          → global flag 5 (integer)
    ///   era.get("abl:0:3")         → character 0's Abl index 3 (integer)
    ///   era.set("flag:5", 1)       → set global flag 5 = 1
    ///   era.add("flag:5", 10)      → flag[5] += 10
    /// </summary>
    public sealed class EreDataModel : IDisposable
    {
        bool _disposed;

        // ---- Sparse variable storage ---------------------------------------
        // Key: "tablename:charIdx:arrayIdx" (normalized lower-case)
        // All integer variables share one dict; string variables share another.
        readonly Dictionary<string, long>   _intVars = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, string> _strVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ---- CSV metadata: tableName → { index → name } -------------------
        readonly Dictionary<string, Dictionary<int, string>> _tableNames =
            new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

        // Tables known to hold string values (populated from CSV scan)
        readonly HashSet<string> _stringTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "callname", "name", "nickname",    // character strings
            "str", "cstr",                     // generic strings
            "global.str", "globalstr",
        };

        // Tables known to be per-character (require charIdx before arrayIdx)
        readonly HashSet<string> _charaTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "abl", "base", "cflag", "cstr", "equip", "ex", "exp",
            "juel", "mark", "maxbase", "nowex", "param",
            "palam", "stain", "talent", "tequip", "tflag",
            "callname", "name", "nickname",
        };

        // Added character list (by chara CSV id)
        readonly List<int> _addedCharacters = new List<int>();
        readonly List<int> _trainCharacters = new List<int>();

        // ---- Config --------------------------------------------------------
        EreGameConfig  _config;
        EreFixedConfig _fixed;

        // ---- GameBase metadata ----------------------------------------
        public string GameTitle   { get; private set; } = "";
        public string GameAuthor  { get; private set; } = "";
        public string GameVersion { get; private set; } = "";

        // ------------------------------------------------------------------ //
        //  Construction                                                        //
        // ------------------------------------------------------------------ //

        public static EreDataModel Create(GameDescriptor game)
        {
            var model = new EreDataModel();
            model.LoadConfig(game.GameRoot);
            model.LoadAllCsv(game.GameRoot);
            return model;
        }

        EreDataModel() { }

        // ------------------------------------------------------------------ //
        //  Configuration                                                       //
        // ------------------------------------------------------------------ //

        public EreGameConfig  Config => _config ?? (_config = new EreGameConfig());
        public EreFixedConfig Fixed  => _fixed  ?? (_fixed  = new EreFixedConfig());

        void LoadConfig(string gameRoot)
        {
            _config = EreJsonConfig.Load<EreGameConfig>(
                Path.Combine(gameRoot, "csv", "_config.json")) ?? new EreGameConfig();
            _fixed  = EreJsonConfig.Load<EreFixedConfig>(
                Path.Combine(gameRoot, "csv", "_fixed.json")) ?? new EreFixedConfig();
        }

        // ------------------------------------------------------------------ //
        //  CSV loading                                                         //
        // ------------------------------------------------------------------ //

        void LoadAllCsv(string gameRoot)
        {
            string csvDir = Path.Combine(gameRoot, "csv");
            if (!Directory.Exists(csvDir)) return;

            foreach (var file in Directory.GetFiles(csvDir, "*.csv", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (name.StartsWith("_", StringComparison.Ordinal)) continue; // _config etc.
                if (name.Equals("GameBase", StringComparison.OrdinalIgnoreCase))
                { LoadGameBase(file); continue; }
                if (name.Equals("_Replace", StringComparison.OrdinalIgnoreCase)) continue;

                LoadIndexTable(file, name);
            }

            // Register extended chara tables from _fixed.json
            if (_fixed?.ExtendedCharaTables != null)
                foreach (var t in _fixed.ExtendedCharaTables)
                    _charaTables.Add(t);
        }

        void LoadGameBase(string path)
        {
            foreach (var kv in EraCsvParser.ParseKeyValue(path))
            {
                switch (kv.Key.ToUpperInvariant())
                {
                    case "タイトル":   case "TITLE":   GameTitle   = kv.Value; break;
                    case "作者":       case "AUTHOR":  GameAuthor  = kv.Value; break;
                    case "バージョン": case "VERSION": GameVersion = kv.Value; break;
                }
            }
        }

        void LoadIndexTable(string path, string tableName)
        {
            var meta = new Dictionary<int, string>();
            foreach (var entry in EraCsvParser.ParseIndexTable(path))
                meta[entry.Key] = entry.Value;

            if (meta.Count > 0)
                _tableNames[tableName] = meta;
        }

        // ------------------------------------------------------------------ //
        //  era.get / era.set / era.add                                        //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Parse and resolve "tableName[:charIdx][:arrayIdx]" → current value.
        /// Returns null if the address is malformed or the table doesn't exist.
        /// </summary>
        public object Get(string varName)
        {
            VarAddress addr;
            if (!VarAddress.TryParse(varName, out addr)) return null;

            string key = addr.ToKey();
            if (_stringTables.Contains(addr.Table))
            {
                string sv;
                return _strVars.TryGetValue(key, out sv) ? (object)sv : "";
            }
            long iv;
            return _intVars.TryGetValue(key, out iv) ? (object)iv : 0L;
        }

        public object Set(string varName, object value)
        {
            VarAddress addr;
            if (!VarAddress.TryParse(varName, out addr)) return value;

            string key = addr.ToKey();
            if (_stringTables.Contains(addr.Table))
                _strVars[key] = value == null ? "" : value.ToString();
            else
                _intVars[key] = ToLong(value);

            return value;
        }

        public object Add(string varName, object value)
        {
            VarAddress addr;
            if (!VarAddress.TryParse(varName, out addr)) return value;

            string key = addr.ToKey();
            if (_stringTables.Contains(addr.Table))
            {
                string sv;
                _strVars.TryGetValue(key, out sv);
                _strVars[key] = (sv ?? "") + (value?.ToString() ?? "");
            }
            else
            {
                long cur;
                _intVars.TryGetValue(key, out cur);
                _intVars[key] = cur + ToLong(value);
            }
            return Get(varName);
        }

        // ------------------------------------------------------------------ //
        //  Metadata helpers                                                    //
        // ------------------------------------------------------------------ //

        /// <summary>Get the CSV name for an index in a named table, or null.</summary>
        public string GetName(string tableName, int index)
        {
            Dictionary<int, string> meta;
            if (!_tableNames.TryGetValue(tableName, out meta)) return null;
            string name;
            return meta.TryGetValue(index, out name) ? name : null;
        }

        /// <summary>Maximum declared index in a named table (for array sizing).</summary>
        public int GetTableMaxIndex(string tableName)
        {
            Dictionary<int, string> meta;
            if (!_tableNames.TryGetValue(tableName, out meta)) return 0;
            int max = 0;
            foreach (var k in meta.Keys)
                if (k > max) max = k;
            return max;
        }

        /// <summary>True when a variable address is per-character (requires charIdx).</summary>
        public bool IsCharaTable(string tableName) => _charaTables.Contains(tableName);

        // ------------------------------------------------------------------ //
        //  Character management                                                //
        // ------------------------------------------------------------------ //

        public bool AddCharacter(int charaId)
        {
            if (_addedCharacters.Contains(charaId)) return false;
            _addedCharacters.Add(charaId);
            return true;
        }

        public void RemoveCharacter(int charaId)
        {
            _addedCharacters.Remove(charaId);
            _trainCharacters.Remove(charaId);
        }

        public void AddCharacterForTrain(int charaId)
        {
            if (!_trainCharacters.Contains(charaId))
                _trainCharacters.Add(charaId);
        }

        public IReadOnlyList<int> AddedCharacters  => _addedCharacters;
        public IReadOnlyList<int> CharactersInTrain => _trainCharacters;
        // All declared chara CSV ids (not yet tracked separately)
        public IReadOnlyList<int> AllCharacters => _addedCharacters;

        // ------------------------------------------------------------------ //
        //  Save / Load                                                         //
        // ------------------------------------------------------------------ //

        public byte[] Serialize(string comment = null)
        {
            // STUB — proper format documented in ERAELECTRON_SAVE_FORMAT.md
            return new byte[] { 0x45, 0x52, 0x45, 0x53 }; // "ERES"
        }

        public bool Deserialize(byte[] data)
        {
            _ = data;
            return data != null && data.Length >= 4;
        }

        // ------------------------------------------------------------------ //
        //  Reset                                                               //
        // ------------------------------------------------------------------ //

        public void ResetAll()
        {
            _intVars.Clear();
            _strVars.Clear();
            _addedCharacters.Clear();
            _trainCharacters.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ResetAll();
        }

        // ------------------------------------------------------------------ //
        //  Utility                                                             //
        // ------------------------------------------------------------------ //

        static long ToLong(object v)
        {
            if (v == null) return 0L;
            if (v is long l) return l;
            if (v is int  i) return i;
            if (v is double d) return (long)d;
            long r;
            return long.TryParse(v.ToString(), out r) ? r : 0L;
        }
    }

    // ======================================================================
    //  VarAddress — parses "tableName[:charIdx][:arrayIdx]"
    // ======================================================================

    /// <summary>Parsed ERA variable address from a colon-delimited string.</summary>
    public struct VarAddress
    {
        public string Table;   // e.g. "flag", "abl", "callname"
        public int    CharIdx; // -1 = global (no character index)
        public int    ArrayIdx;

        public bool IsChara => CharIdx >= 0;

        public string ToKey()
        {
            return CharIdx < 0
                ? $"{Table}:{ArrayIdx}"
                : $"{Table}:{CharIdx}:{ArrayIdx}";
        }

        public static bool TryParse(string input, out VarAddress addr)
        {
            addr = default;
            if (string.IsNullOrEmpty(input)) return false;

            var parts = input.Split(':');
            if (parts.Length < 2) return false;

            addr.Table    = parts[0].Trim().ToLowerInvariant();
            addr.CharIdx  = -1;
            addr.ArrayIdx = 0;

            if (parts.Length == 2)
            {
                // "tableName:index" — global
                int.TryParse(parts[1].Trim(), out addr.ArrayIdx);
            }
            else
            {
                // "tableName:charIdx:arrayIdx"
                int.TryParse(parts[1].Trim(), out addr.CharIdx);
                int.TryParse(parts[2].Trim(), out addr.ArrayIdx);
            }
            return true;
        }
    }

    // ======================================================================
    //  EraCsvParser — reads ERA CSV format (Shift-JIS, index,name pairs)
    // ======================================================================

    internal static class EraCsvParser
    {
        static readonly Encoding Sjis = GetSjis();

        static Encoding GetSjis()
        {
            try   { return Encoding.GetEncoding(932); }
            catch { return Encoding.UTF8; }
        }

        /// <summary>
        /// Parse a standard ERA CSV: "index,name[,...]\n" with ; comments.
        /// Returns (index, firstName) pairs.
        /// </summary>
        public static IEnumerable<KeyValuePair<int, string>> ParseIndexTable(string path)
        {
            if (!File.Exists(path)) yield break;
            string[] lines;
            try   { lines = File.ReadAllLines(path, Sjis); }
            catch { yield break; }

            foreach (var raw in lines)
            {
                string line = StripComment(raw).Trim();
                if (line.Length == 0) continue;

                var parts = line.Split(',');
                if (parts.Length < 2) continue;

                int idx;
                if (!int.TryParse(parts[0].Trim(), out idx)) continue;

                string name = parts[1].Trim();
                if (name.Length > 0)
                    yield return new KeyValuePair<int, string>(idx, name);
            }
        }

        /// <summary>
        /// Parse GameBase.csv or similar key→value files.
        /// Format: "key,value1|value2|..." — returns first value only.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> ParseKeyValue(string path)
        {
            if (!File.Exists(path)) yield break;
            string[] lines;
            try   { lines = File.ReadAllLines(path, Sjis); }
            catch { yield break; }

            foreach (var raw in lines)
            {
                string line = StripComment(raw).Trim();
                if (line.Length == 0) continue;

                int comma = line.IndexOf(',');
                if (comma < 0) continue;

                string key   = line.Substring(0, comma).Trim();
                string value = line.Substring(comma + 1).Trim();
                // Use first pipe-delimited segment
                int pipe = value.IndexOf('|');
                if (pipe >= 0) value = value.Substring(0, pipe).Trim();

                if (key.Length > 0)
                    yield return new KeyValuePair<string, string>(key, value);
            }
        }

        static string StripComment(string line)
        {
            int sc = line.IndexOf(';');
            return sc < 0 ? line : line.Substring(0, sc);
        }
    }

    // ======================================================================
    //  Config objects — schema from _config.json / _fixed.json
    // ======================================================================

    [Serializable]
    public sealed class EreGameConfig
    {
        public bool UseReplace     = false;
        public bool HideUserInput  = false;
        public bool SaveCompressed = false;
        public int  AudioVolume    = 0;
        public bool AutoMaximize   = false;
        public int  WindowHeight   = 916;
        public int  WindowWidth    = 1000;
    }

    [Serializable]
    public sealed class EreFixedConfig
    {
        public bool     CollapseBlankLines  = false;
        public string[] ExtendedCharaTables = Array.Empty<string>();
        public int      Orientation         = 0;
    }

    // ======================================================================
    //  JSON loader (minimal — replace with proper parser once chosen)
    // ======================================================================

    internal static class EreJsonConfig
    {
        public static T Load<T>(string path) where T : class, new()
        {
            if (!File.Exists(path)) return null;
            try
            {
                // TODO: replace with a proper JSON parser (JsonUtility, etc.)
                // For now, parse only the fields that are critical for boot.
                string text = File.ReadAllText(path, Encoding.UTF8);
                var result = new T();
                if (result is EreGameConfig cfg)   ParseGameConfig(text, cfg);
                if (result is EreFixedConfig fixed_) ParseFixedConfig(text, fixed_);
                return result;
            }
            catch { return null; }
        }

        static void ParseGameConfig(string json, EreGameConfig cfg)
        {
            cfg.SaveCompressed  = Contains(json, "\"saveCompressedData\"") &&
                                  !Contains(json, "\"saveCompressedData\": false") &&
                                  !Contains(json, "\"saveCompressedData\":false");
            cfg.HideUserInput   = Contains(json, "\"hideUserInput\": true") ||
                                  Contains(json, "\"hideUserInput\":true");
            // Window dimensions: simple regex-free extraction
            cfg.WindowHeight    = ParseInt(json, "\"height\"", 916);
            cfg.WindowWidth     = ParseInt(json, "\"width\"",  1000);
        }

        static void ParseFixedConfig(string json, EreFixedConfig cfg)
        {
            cfg.CollapseBlankLines = Contains(json, "\"collapseBlankLines\": true") ||
                                     Contains(json, "\"collapseBlankLines\":true");
            cfg.Orientation = ParseInt(json, "\"orientation\"", 0);
            // ExtendedCharaTables: parsed properly only when JSON parser available
        }

        static bool Contains(string text, string fragment) =>
            text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

        static int ParseInt(string text, string key, int fallback)
        {
            int idx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return fallback;
            int colon = text.IndexOf(':', idx);
            if (colon < 0) return fallback;
            int start = colon + 1;
            while (start < text.Length && (text[start] == ' ' || text[start] == '\t'))
                start++;
            int end = start;
            while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '-'))
                end++;
            int v;
            return int.TryParse(text.Substring(start, end - start), out v) ? v : fallback;
        }
    }
}
