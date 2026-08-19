using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace MinorShift.Emuera.GameData
{
    /// <summary>
    /// IL2CPP-safe DataTable model for the EM+EE DT_* API.
    ///
    /// The reference implementation uses System.Data.DataTable. This class keeps the
    /// observable contract without making System.Data part of the Unity runtime:
    /// typed integer columns, nullable cells, defaults, stable id values, case mode,
    /// position/id addressing, deterministic selection and schema/data XML.
    /// </summary>
    public sealed class EraDataTable
    {
        public enum ColType
        {
            Int8 = 1,
            Int16 = 2,
            Int32 = 3,
            Int64 = 4,
            String = 5,
            // Compatibility aliases for the pre-Phase-4 approximation.
            Int = Int32,
            Float = Int64,
            Str = String,
        }

        public sealed class ColDef
        {
            public string Name;
            public ColType Type;
            public bool Nullable = true;
            public bool HasDefault;
            public object DefaultValue;
        }

        public sealed class Row
        {
            internal Row(long id, int columnCount)
            {
                Id = id;
                Values = new object[columnCount];
            }

            public readonly long Id;
            internal object[] Values;
        }

        static long nextRowId = DateTime.UtcNow.Ticks;
        readonly List<ColDef> cols = new List<ColDef>();
        readonly List<Row> rows = new List<Row>();
        readonly StringComparer columnComparer = StringComparer.OrdinalIgnoreCase;

        public EraDataTable()
        {
            CaseSensitive = true;
            AddColumn("id", ColType.Int64, false);
        }

        /// <summary>Reference equivalent of DataTable.CaseSensitive.</summary>
        public bool CaseSensitive { get; set; }

        /// <summary>Compatibility alias used by the earlier uEmuera implementation.</summary>
        public bool IgnoreCase
        {
            get { return !CaseSensitive; }
            set { CaseSensitive = !value; }
        }

        public int RowCount { get { return rows.Count; } }
        public int ColumnCount { get { return cols.Count; } }
        public IReadOnlyList<ColDef> Columns { get { return cols; } }
        public IReadOnlyList<Row> Rows { get { return rows; } }

        StringComparison ValueComparison
        {
            get { return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; }
        }

        int FindColumnIndex(string name)
        {
            if (name == null) return -1;
            for (int i = 0; i < cols.Count; i++)
                if (columnComparer.Equals(cols[i].Name, name)) return i;
            return -1;
        }

        public ColDef FindColumn(string name)
        {
            int index = FindColumnIndex(name);
            return index < 0 ? null : cols[index];
        }

        public int ColExist(string name)
        {
            int index = FindColumnIndex(name);
            return index < 0 ? 0 : (int)cols[index].Type;
        }

        public bool AddCol(string name, ColType type)
        {
            return AddColumn(name, type, true);
        }

        public bool AddColumn(string name, ColType type, bool nullable)
        {
            if (string.IsNullOrEmpty(name) || FindColumnIndex(name) >= 0) return false;
            ColDef column = new ColDef { Name = name, Type = type, Nullable = nullable };
            cols.Add(column);
            for (int i = 0; i < rows.Count; i++)
                Array.Resize(ref rows[i].Values, cols.Count);
            return true;
        }

        public bool RemoveCol(string name)
        {
            int index = FindColumnIndex(name);
            if (index < 0 || string.Equals(cols[index].Name, "id", StringComparison.OrdinalIgnoreCase)) return false;
            cols.RemoveAt(index);
            for (int i = 0; i < rows.Count; i++)
            {
                object[] old = rows[i].Values;
                object[] replacement = new object[old.Length - 1];
                if (index > 0) Array.Copy(old, 0, replacement, 0, index);
                if (index + 1 < old.Length) Array.Copy(old, index + 1, replacement, index, old.Length - index - 1);
                // Rebuild the row after removing a column so the id remains stable.
                rows[i] = CopyRow(rows[i].Id, replacement);
            }
            return true;
        }

        static Row CopyRow(long id, object[] values)
        {
            Row row = new Row(id, values.Length);
            Array.Copy(values, row.Values, values.Length);
            return row;
        }

        public string[] ColNames()
        {
            string[] result = new string[cols.Count];
            for (int i = 0; i < cols.Count; i++) result[i] = cols[i].Name;
            return result;
        }

        public void SetDefault(string name, object value)
        {
            ColDef column = FindColumn(name);
            if (column == null) throw new KeyNotFoundException(name);
            object converted;
            if (!TryConvertValue(value, column, out converted)) throw new InvalidCastException(name);
            column.HasDefault = true;
            column.DefaultValue = converted;
        }

        static long NewId()
        {
            return System.Threading.Interlocked.Increment(ref nextRowId);
        }

        static void ObserveId(long id)
        {
            long current;
            do
            {
                current = nextRowId;
                if (id <= current) return;
            }
            while (System.Threading.Interlocked.CompareExchange(ref nextRowId, id, current) != current);
        }

        Row CreateRow()
        {
            Row row = new Row(NewId(), cols.Count);
            row.Values[0] = row.Id;
            for (int i = 1; i < cols.Count; i++)
                row.Values[i] = cols[i].HasDefault ? cols[i].DefaultValue : null;
            return row;
        }

        /// <summary>Adds a row and returns the generated id, never the list position.</summary>
        public long AddRow()
        {
            Row row = CreateRow();
            rows.Add(row);
            return row.Id;
        }

        public bool TryAddRow(IDictionary<string, object> values, out long id, out string error)
        {
            return TryAddRowWithId(NewId(), values, out id, out error);
        }

        bool TryAddRowWithId(long rowId, IDictionary<string, object> values, out long id, out string error)
        {
            Row row = new Row(rowId, cols.Count);
            row.Values[0] = row.Id;
            for (int i = 1; i < cols.Count; i++) row.Values[i] = cols[i].HasDefault ? cols[i].DefaultValue : null;
            id = 0;
            error = null;
            if (rowId <= 0)
            {
                error = "id";
                return false;
            }
            if (values != null)
            {
                foreach (KeyValuePair<string, object> pair in values)
                {
                    int index = FindColumnIndex(pair.Key);
                    if (index < 0 || index == 0)
                    {
                        error = pair.Key;
                        return false;
                    }
                    object converted;
                    if (!TryConvertValue(pair.Value, cols[index], out converted))
                    {
                        error = pair.Key;
                        return false;
                    }
                    row.Values[index] = converted;
                }
            }
            for (int i = 0; i < cols.Count; i++)
            {
                if (!cols[i].Nullable && row.Values[i] == null)
                {
                    error = cols[i].Name;
                    return false;
                }
            }
            rows.Add(row);
            ObserveId(row.Id);
            id = row.Id;
            return true;
        }

        public bool RemoveRow(int index)
        {
            if (index < 0 || index >= rows.Count) return false;
            rows.RemoveAt(index);
            return true;
        }

        public int RemoveRowsById(IList<long> ids)
        {
            if (ids == null || ids.Count == 0) return 0;
            int removed = 0;
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < ids.Count; j++)
                {
                    if (rows[i].Id == ids[j])
                    {
                        rows.RemoveAt(i);
                        removed++;
                        break;
                    }
                }
            }
            return removed;
        }

        public bool RemoveRowById(long id)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Id == id)
                {
                    rows.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            rows.Clear();
            if (cols.Count > 1) cols.RemoveRange(1, cols.Count - 1);
        }

        public Row GetRowById(long id)
        {
            for (int i = 0; i < rows.Count; i++) if (rows[i].Id == id) return rows[i];
            return null;
        }

        public Row GetRowByPosition(int index)
        {
            return index < 0 || index >= rows.Count ? null : rows[index];
        }

        static int CheckedIndex(long index)
        {
            if (index < int.MinValue || index > int.MaxValue) return -1;
            return (int)index;
        }

        bool TryGetCell(Row row, string column, out object value)
        {
            value = null;
            int index = FindColumnIndex(column);
            if (row == null || index < 0 || index >= row.Values.Length) return false;
            value = row.Values[index];
            return true;
        }

        public bool IsNull(long position, string column, bool asId = false)
        {
            Row row = asId ? GetRowById(position) : GetRowByPosition(CheckedIndex(position));
            object value;
            return row != null && TryGetCell(row, column, out value) && value == null;
        }

        public bool TryGet(long position, string column, bool asId, out object value)
        {
            Row row = asId ? GetRowById(position) : GetRowByPosition(CheckedIndex(position));
            return TryGetCell(row, column, out value);
        }

        public string GetStr(long row, string col, bool asId = false)
        {
            object value;
            if (!TryGet(row, col, asId, out value) || value == null) return string.Empty;
            string text = value as string;
            return text ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public long GetInt(long row, string col, bool asId = false)
        {
            object value;
            if (!TryGet(row, col, asId, out value) || value == null) return 0;
            if (value is long) return (long)value;
            if (value is int) return (int)value;
            if (value is short) return (short)value;
            if (value is sbyte) return (sbyte)value;
            long result;
            return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
        }

        public bool TrySet(long position, string column, object value, bool asId, out string error)
        {
            error = null;
            Row row = asId ? GetRowById(position) : GetRowByPosition(CheckedIndex(position));
            int index = FindColumnIndex(column);
            if (row == null) { error = "row"; return false; }
            if (index < 0) { error = "column"; return false; }
            if (index == 0) { error = "id"; return false; }
            if (value == null && cols[index].HasDefault) { error = cols[index].Name; return false; }
            object converted;
            if (!TryConvertValue(value, cols[index], out converted)) { error = cols[index].Name; return false; }
            row.Values[index] = converted;
            return true;
        }

        // Compatibility wrappers retained while older built-in registrations are
        // migrated to the DT_CELL_* API.
        public void SetStr(int position, string column, string value)
        {
            string error;
            TrySet(position, column, value, false, out error);
        }

        public void SetInt(int position, string column, long value)
        {
            string error;
            TrySet(position, column, value, false, out error);
        }

        public int Find(string column, string value)
        {
            int columnIndex = FindColumnIndex(column);
            if (columnIndex < 0) return -1;
            for (int i = 0; i < rows.Count; i++)
            {
                object cell = rows[i].Values[columnIndex];
                string text = cell == null ? string.Empty : Convert.ToString(cell, CultureInfo.InvariantCulture);
                if (string.Equals(text, value, ValueComparison)) return i;
            }
            return -1;
        }

        public void Sort(string column, bool ascending)
        {
            int columnIndex = FindColumnIndex(column);
            if (columnIndex < 0) return;
            ColDef definition = cols[columnIndex];
            rows.Sort(delegate(Row left, Row right)
            {
                int result = CompareValues(left.Values[columnIndex], right.Values[columnIndex], definition);
                return ascending ? result : -result;
            });
        }

        static bool TryConvertValue(object value, ColDef column, out object converted)
        {
            converted = null;
            if (value == null)
            {
                return column.Nullable;
            }
            if (column.Type == ColType.String)
            {
                if (!(value is string)) return false;
                converted = value;
                return true;
            }
            if (value is string) return false;
            long number;
            try { number = Convert.ToInt64(value, CultureInfo.InvariantCulture); }
            catch (Exception) { return false; }
            switch (column.Type)
            {
                case ColType.Int8: converted = (sbyte)Math.Max(sbyte.MinValue, Math.Min(sbyte.MaxValue, number)); return true;
                case ColType.Int16: converted = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, number)); return true;
                case ColType.Int32: converted = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, number)); return true;
                case ColType.Int64: converted = number; return true;
                default: return false;
            }
        }

        internal int CompareValues(object left, object right, ColDef column)
        {
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            if (column.Type == ColType.String)
                return string.Compare((string)left, (string)right, ValueComparison);
            long a = Convert.ToInt64(left, CultureInfo.InvariantCulture);
            long b = Convert.ToInt64(right, CultureInfo.InvariantCulture);
            return a.CompareTo(b);
        }

        internal bool TextEquals(string left, string right)
        {
            return string.Equals(left, right, ValueComparison);
        }

        internal int ColumnIndex(string name) { return FindColumnIndex(name); }
        internal object Cell(Row row, int index) { return row == null || index < 0 || index >= row.Values.Length ? null : row.Values[index]; }

        public string ToCsv()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < cols.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(EscapeCsv(cols[i].Name));
            }
            sb.AppendLine();
            for (int r = 0; r < rows.Count; r++)
            {
                for (int c = 0; c < cols.Count; c++)
                {
                    if (c > 0) sb.Append(',');
                    sb.Append(EscapeCsv(GetStr(r, cols[c].Name)));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        static string EscapeCsv(string value)
        {
            if (value == null) return string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        public string ToXmlSchema()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("emueraDataTableSchema");
                for (int i = 0; i < cols.Count; i++)
                {
                    ColDef c = cols[i];
                    writer.WriteStartElement("column");
                    writer.WriteAttributeString("name", c.Name);
                    writer.WriteAttributeString("type", ((int)c.Type).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("nullable", c.Nullable ? "1" : "0");
                    if (c.HasDefault) writer.WriteAttributeString("default", GetStrValue(c.DefaultValue));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            return sb.ToString();
        }

        string GetStrValue(object value)
        {
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public string ToXml()
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("emueraDataTable");
                for (int r = 0; r < rows.Count; r++)
                {
                    writer.WriteStartElement("row");
                    writer.WriteAttributeString("id", rows[r].Id.ToString(CultureInfo.InvariantCulture));
                    for (int c = 0; c < cols.Count; c++)
                    {
                        object value = rows[r].Values[c];
                        if (value == null) continue;
                        writer.WriteStartElement("cell");
                        writer.WriteAttributeString("column", cols[c].Name);
                        writer.WriteString(GetStrValue(value));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            return sb.ToString();
        }

        public static EraDataTable FromXml(string schemaXml, string dataXml)
        {
            EraDataTable table = new EraDataTable();
            XmlDocument schema = SafeLoad(schemaXml);
            XmlNodeList columnNodes = schema.SelectNodes("/emueraDataTableSchema/column");
            if (columnNodes == null) throw new FormatException("DataTable schema has no columns");
            for (int i = 0; i < columnNodes.Count; i++)
            {
                XmlElement element = columnNodes[i] as XmlElement;
                if (element == null) continue;
                ColType type;
                if (!TryParseType(element.GetAttribute("type"), out type)) throw new FormatException("Unknown DataTable type");
                bool nullable = element.GetAttribute("nullable") != "0";
                string columnName = element.GetAttribute("name");
                if (!string.Equals(columnName, "id", StringComparison.OrdinalIgnoreCase))
                {
                    if (!table.AddColumn(columnName, type, nullable)) throw new FormatException("Duplicate DataTable column");
                    if (element.HasAttribute("default")) table.SetDefault(columnName, ParseValue(element.GetAttribute("default"), table.FindColumn(columnName)));
                }
            }
            XmlDocument data = SafeLoad(dataXml);
            XmlNodeList rowNodes = data.SelectNodes("/emueraDataTable/row");
            if (rowNodes == null) return table;
            for (int i = 0; i < rowNodes.Count; i++)
            {
                XmlElement rowElement = rowNodes[i] as XmlElement;
                Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                XmlNodeList cells = rowElement.SelectNodes("cell");
                for (int c = 0; c < cells.Count; c++)
                {
                    XmlElement cell = cells[c] as XmlElement;
                    ColDef column = table.FindColumn(cell.GetAttribute("column"));
                    if (column != null && !string.Equals(column.Name, "id", StringComparison.OrdinalIgnoreCase)) values[column.Name] = ParseValue(cell.InnerText, column);
                }
                long restoredId;
                if (!long.TryParse(rowElement.GetAttribute("id"), NumberStyles.Integer, CultureInfo.InvariantCulture, out restoredId) || restoredId <= 0)
                    throw new FormatException("Invalid DataTable row id");
                long id;
                string error;
                if (!table.TryAddRowWithId(restoredId, values, out id, out error)) throw new FormatException("Invalid DataTable row: " + error);
            }
            return table;
        }

        static XmlDocument SafeLoad(string xml)
        {
            if (string.IsNullOrEmpty(xml)) throw new FormatException("Missing DataTable XML");
            XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersFromEntities = 0 };
            XmlDocument document = new XmlDocument { XmlResolver = null };
            using (StringReader text = new StringReader(xml))
            using (XmlReader reader = XmlReader.Create(text, settings)) document.Load(reader);
            return document;
        }

        static object ParseValue(string value, ColDef column)
        {
            if (column.Type == ColType.String) return value;
            long number;
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) throw new FormatException("Invalid DataTable integer");
            object converted;
            if (!TryConvertValue(number, column, out converted)) throw new FormatException("Invalid DataTable integer range");
            return converted;
        }

        public static bool TryParseType(string value, out ColType type)
        {
            type = ColType.String;
            if (string.IsNullOrEmpty(value)) return true;
            int numeric;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numeric) && numeric >= 1 && numeric <= 5)
            {
                type = (ColType)numeric;
                return true;
            }
            switch (value.Trim().ToLowerInvariant())
            {
                case "int8": type = ColType.Int8; return true;
                case "int16": type = ColType.Int16; return true;
                case "int32": type = ColType.Int32; return true;
                case "int64": type = ColType.Int64; return true;
                case "string": type = ColType.String; return true;
                default: return false;
            }
        }
    }
}
