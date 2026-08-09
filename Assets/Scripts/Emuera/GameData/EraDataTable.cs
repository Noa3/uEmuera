using System;
using System.Collections.Generic;
using System.Text;

namespace MinorShift.Emuera.GameData
{
    /// <summary>
    /// Lightweight DataTable for DT_* commands. All cell values stored as strings;
    /// typed coercion at get/set. IL2CPP-compatible (no System.Data dependency).
    /// </summary>
    public sealed class EraDataTable
    {
        public enum ColType { Str = 0, Int = 1, Float = 2 }

        public sealed class ColDef
        {
            public string Name;
            public ColType Type;
        }

        public bool IgnoreCase = false;

        readonly List<ColDef> cols = new List<ColDef>();
        readonly List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

        StringComparison Cmp => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public int RowCount => rows.Count;
        public int ColumnCount => cols.Count;

        ColDef FindCol(string name) => cols.Find(c => string.Equals(c.Name, name, Cmp));

        /// <summary>Returns canonical column name (original case from AddCol), preserving case for row dict keys.</summary>
        string KeyFor(string col)
        {
            ColDef c = FindCol(col);
            return c != null ? c.Name : col;
        }

        /// <summary>Returns column type+1 if column exists, 0 if absent.</summary>
        public int ColExist(string name)
        {
            ColDef c = FindCol(name);
            return c == null ? 0 : (int)c.Type + 1;
        }

        /// <summary>Adds column. Returns false if column already exists.</summary>
        public bool AddCol(string name, ColType type)
        {
            if (FindCol(name) != null) return false;
            cols.Add(new ColDef { Name = name, Type = type });
            return true;
        }

        /// <summary>Removes column and its data from all rows. Returns false if not found.</summary>
        public bool RemoveCol(string name)
        {
            ColDef c = FindCol(name);
            if (c == null) return false;
            string key = c.Name; // use canonical key for row dict removal
            cols.Remove(c);
            for (int i = 0; i < rows.Count; i++) rows[i].Remove(key);
            return true;
        }

        /// <summary>Returns array of column names in definition order.</summary>
        public string[] ColNames()
        {
            string[] names = new string[cols.Count];
            for (int i = 0; i < cols.Count; i++) names[i] = cols[i].Name;
            return names;
        }

        /// <summary>Adds an empty row. Returns new row index.</summary>
        public int AddRow()
        {
            rows.Add(new Dictionary<string, string>());
            return rows.Count - 1;
        }

        /// <summary>Removes row at idx. Returns false if out of range.</summary>
        public bool RemoveRow(int idx)
        {
            if (idx < 0 || idx >= rows.Count) return false;
            rows.RemoveAt(idx);
            return true;
        }

        /// <summary>Clears all rows (keeps column definitions).</summary>
        public void Clear() { rows.Clear(); }

        public string GetStr(int row, string col)
        {
            if (row < 0 || row >= rows.Count) return "";
            rows[row].TryGetValue(KeyFor(col), out string v);
            return v ?? "";
        }

        public long GetInt(int row, string col)
        {
            string s = GetStr(row, col);
            return long.TryParse(s, out long v) ? v : 0;
        }

        public double GetFloat(int row, string col)
        {
            string s = GetStr(row, col);
            return double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0.0;
        }

        public void SetStr(int row, string col, string val)
        {
            if (row < 0 || row >= rows.Count) return;
            rows[row][KeyFor(col)] = val ?? "";
        }

        public void SetInt(int row, string col, long val)
        {
            SetStr(row, col, val.ToString());
        }

        public void SetFloat(int row, string col, double val)
        {
            SetStr(row, col, val.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Returns index of first row where col==val (respecting IgnoreCase), -1 if not found.</summary>
        public int Find(string col, string val)
        {
            string key = KeyFor(col);
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].TryGetValue(key, out string v);
                if (string.Equals(v ?? "", val, Cmp)) return i;
            }
            return -1;
        }

        /// <summary>Stable sort rows by column value (string comparison).</summary>
        public void Sort(string col, bool ascending)
        {
            string key = KeyFor(col);
            rows.Sort((a, b) =>
            {
                a.TryGetValue(key, out string av);
                b.TryGetValue(key, out string bv);
                int cmp = string.Compare(av ?? "", bv ?? "", Cmp);
                return ascending ? cmp : -cmp;
            });
        }

        public string ToCsv()
        {
            var sb = new StringBuilder();
            // header
            string[] header = new string[cols.Count];
            for (int i = 0; i < cols.Count; i++) header[i] = EscapeCsv(cols[i].Name);
            sb.AppendLine(string.Join(",", header));
            // rows
            for (int r = 0; r < rows.Count; r++)
            {
                string[] fields = new string[cols.Count];
                for (int i = 0; i < cols.Count; i++)
                {
                    rows[r].TryGetValue(cols[i].Name, out string v);
                    fields[i] = EscapeCsv(v ?? "");
                }
                sb.AppendLine(string.Join(",", fields));
            }
            return sb.ToString();
        }

        public string ToXml()
        {
            var sb = new StringBuilder("<datatable>");
            for (int r = 0; r < rows.Count; r++)
            {
                sb.Append("<row>");
                for (int i = 0; i < cols.Count; i++)
                {
                    rows[r].TryGetValue(cols[i].Name, out string v);
                    string tag = cols[i].Name;
                    sb.Append('<').Append(tag).Append('>')
                      .Append(XmlEscape(v ?? ""))
                      .Append("</").Append(tag).Append('>');
                }
                sb.Append("</row>");
            }
            sb.Append("</datatable>");
            return sb.ToString();
        }

        static string EscapeCsv(string s)
        {
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        static string XmlEscape(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;");
        }
    }
}
