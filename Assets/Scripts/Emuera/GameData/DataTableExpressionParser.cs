using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MinorShift.Emuera.GameData
{
    /// <summary>
    /// Small deterministic subset of System.Data.DataTable.Select's expression grammar.
    /// It intentionally accepts no executable code and never delegates to eval/reflection.
    /// </summary>
    public sealed class DataTableExpressionParser
    {
        enum TokenKind { End, Identifier, String, Number, Operator, Open, Close, Comma }
        sealed class Token
        {
            public TokenKind Kind;
            public string Text;
            public Token(TokenKind kind, string text) { Kind = kind; Text = text; }
        }

        readonly EraDataTable table;
        readonly List<Token> tokens;
        int position;

        DataTableExpressionParser(EraDataTable table, string expression)
        {
            this.table = table;
            tokens = Lex(expression ?? string.Empty);
        }

        public static List<int> Select(EraDataTable table, string filterExpression, string sortRule)
        {
            if (table == null) throw new ArgumentNullException("table");
            DataTableExpressionParser parser = new DataTableExpressionParser(table, filterExpression);
            List<int> result = new List<int>();
            for (int i = 0; i < table.RowCount; i++)
            {
                if (parser.Matches(table.GetRowByPosition(i))) result.Add(i);
            }
            if (!string.IsNullOrWhiteSpace(sortRule)) parser.Sort(result, sortRule);
            return result;
        }

        bool Matches(EraDataTable.Row row)
        {
            if (tokens.Count == 1) return true;
            bool result = ParseOr(row);
            if (Current().Kind != TokenKind.End) throw new FormatException("Unexpected DataTable filter token: " + Current().Text);
            return result;
        }

        bool ParseOr(EraDataTable.Row row)
        {
            bool result = ParseAnd(row);
            while (IsKeyword("OR")) { Advance(); result = ParseAnd(row) || result; }
            return result;
        }

        bool ParseAnd(EraDataTable.Row row)
        {
            bool result = ParseUnary(row);
            while (IsKeyword("AND")) { Advance(); result = ParseUnary(row) && result; }
            return result;
        }

        bool ParseUnary(EraDataTable.Row row)
        {
            if (IsKeyword("NOT")) { Advance(); return !ParseUnary(row); }
            if (Current().Kind == TokenKind.Open)
            {
                Advance();
                bool result = ParseOr(row);
                Expect(TokenKind.Close, ")");
                return result;
            }
            object left = ParseOperand(row);
            if (IsKeyword("IS"))
            {
                Advance();
                bool not = IsKeyword("NOT");
                if (not) Advance();
                ExpectKeyword("NULL");
                bool isNull = left == null;
                return not ? !isNull : isNull;
            }
            if (IsKeyword("LIKE"))
            {
                Advance();
                object right = ParseOperand(row);
                return left != null && right != null && Like(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), table.CaseSensitive);
            }
            if (IsKeyword("IN"))
            {
                Advance();
                Expect(TokenKind.Open, "(");
                bool found = false;
                while (Current().Kind != TokenKind.Close)
                {
                    object right = ParseOperand(row);
                    if (Equal(left, right)) found = true;
                    if (Current().Kind != TokenKind.Comma) break;
                    Advance();
                }
                Expect(TokenKind.Close, ")");
                return found;
            }
            if (Current().Kind != TokenKind.Operator) return IsTruthy(left);
            string op = Advance().Text;
            object rightValue = ParseOperand(row);
            int compare = Compare(left, rightValue);
            switch (op)
            {
                case "=": case "==": return compare == 0;
                case "<>": case "!=": return compare != 0;
                case ">": return compare > 0;
                case ">=": return compare >= 0;
                case "<": return compare < 0;
                case "<=": return compare <= 0;
                default: throw new FormatException("Unsupported DataTable operator: " + op);
            }
        }

        object ParseOperand(EraDataTable.Row row)
        {
            Token token = Current();
            if (token.Kind == TokenKind.Identifier)
            {
                Advance();
                if (string.Equals(token.Text, "NULL", StringComparison.OrdinalIgnoreCase)) return null;
                int column = table.ColumnIndex(token.Text);
                if (column < 0) throw new FormatException("Unknown DataTable column: " + token.Text);
                return table.Cell(row, column);
            }
            if (token.Kind == TokenKind.String) { Advance(); return token.Text; }
            if (token.Kind == TokenKind.Number)
            {
                Advance();
                long number;
                if (!long.TryParse(token.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) throw new FormatException("Invalid DataTable number");
                return number;
            }
            throw new FormatException("Expected DataTable operand");
        }

        void Sort(List<int> indices, string rule)
        {
            List<SortKey> keys = ParseSort(rule);
            indices.Sort(delegate(int leftIndex, int rightIndex)
            {
                EraDataTable.Row left = table.GetRowByPosition(leftIndex);
                EraDataTable.Row right = table.GetRowByPosition(rightIndex);
                for (int i = 0; i < keys.Count; i++)
                {
                    int column = table.ColumnIndex(keys[i].Name);
                    if (column < 0) throw new FormatException("Unknown DataTable sort column: " + keys[i].Name);
                    int cmp = table.CompareValues(table.Cell(left, column), table.Cell(right, column), table.Columns[column]);
                    if (cmp != 0) return keys[i].Descending ? -cmp : cmp;
                }
                // The reference preserves source order when sort keys compare equal.
                return leftIndex.CompareTo(rightIndex);
            });
        }

        sealed class SortKey
        {
            public string Name;
            public bool Descending;
        }

        static List<SortKey> ParseSort(string rule)
        {
            List<SortKey> keys = new List<SortKey>();
            string[] parts = rule.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string[] words = parts[i].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                if (words.Length > 2 || (words.Length == 2 && !string.Equals(words[1], "ASC", StringComparison.OrdinalIgnoreCase) && !string.Equals(words[1], "DESC", StringComparison.OrdinalIgnoreCase)))
                    throw new FormatException("Invalid DataTable sort rule");
                keys.Add(new SortKey { Name = words[0], Descending = words.Length == 2 && string.Equals(words[1], "DESC", StringComparison.OrdinalIgnoreCase) });
            }
            return keys;
        }

        bool IsKeyword(string keyword)
        {
            return Current().Kind == TokenKind.Identifier && string.Equals(Current().Text, keyword, StringComparison.OrdinalIgnoreCase);
        }

        void ExpectKeyword(string keyword)
        {
            if (!IsKeyword(keyword)) throw new FormatException("Expected DataTable keyword: " + keyword);
            Advance();
        }

        void Expect(TokenKind kind, string text)
        {
            if (Current().Kind != kind) throw new FormatException("Expected DataTable token: " + text);
            Advance();
        }

        Token Current() { return tokens[position]; }
        Token Advance() { Token token = tokens[position]; if (position < tokens.Count - 1) position++; return token; }

        static bool Equal(object left, object right)
        {
            if (left == null || right == null) return left == null && right == null;
            if (left is string || right is string) return string.Equals(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            return Convert.ToInt64(left, CultureInfo.InvariantCulture) == Convert.ToInt64(right, CultureInfo.InvariantCulture);
        }

        int Compare(object left, object right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            if (left is string || right is string) return string.Compare(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), table.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return Convert.ToInt64(left, CultureInfo.InvariantCulture).CompareTo(Convert.ToInt64(right, CultureInfo.InvariantCulture));
        }

        static bool IsTruthy(object value)
        {
            if (value == null) return false;
            if (value is bool) return (bool)value;
            long number;
            return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number != 0;
        }

        static bool Like(string value, string pattern, bool caseSensitive)
        {
            StringBuilder regex = new StringBuilder("^");
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (c == '*' || c == '%') regex.Append(".*");
                else if (c == '_') regex.Append('.');
                else regex.Append(Regex.Escape(c.ToString()));
            }
            regex.Append('$');
            RegexOptions options = caseSensitive ? RegexOptions.CultureInvariant : RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
            return Regex.IsMatch(value, regex.ToString(), options);
        }

        static List<Token> Lex(string source)
        {
            List<Token> result = new List<Token>();
            int index = 0;
            while (index < source.Length)
            {
                char c = source[index];
                if (char.IsWhiteSpace(c)) { index++; continue; }
                if (c == '(') { result.Add(new Token(TokenKind.Open, "(")); index++; continue; }
                if (c == ')') { result.Add(new Token(TokenKind.Close, ")")); index++; continue; }
                if (c == ',') { result.Add(new Token(TokenKind.Comma, ",")); index++; continue; }
                if (c == '\'' || c == '"')
                {
                    char quote = c;
                    index++;
                    StringBuilder text = new StringBuilder();
                    while (index < source.Length)
                    {
                        if (source[index] == quote)
                        {
                            if (index + 1 < source.Length && source[index + 1] == quote) { text.Append(quote); index += 2; continue; }
                            index++; break;
                        }
                        text.Append(source[index++]);
                    }
                    result.Add(new Token(TokenKind.String, text.ToString()));
                    continue;
                }
                if (char.IsDigit(c) || (c == '-' && index + 1 < source.Length && char.IsDigit(source[index + 1])))
                {
                    int start = index++;
                    while (index < source.Length && char.IsDigit(source[index])) index++;
                    result.Add(new Token(TokenKind.Number, source.Substring(start, index - start)));
                    continue;
                }
                if (char.IsLetter(c) || c == '_' || c == '[')
                {
                    int start = index;
                    if (c == '[')
                    {
                        index++;
                        while (index < source.Length && source[index] != ']') index++;
                        if (index < source.Length) index++;
                        result.Add(new Token(TokenKind.Identifier, source.Substring(start + 1, index - start - 2)));
                    }
                    else
                    {
                        index++;
                        while (index < source.Length && (char.IsLetterOrDigit(source[index]) || source[index] == '_')) index++;
                        result.Add(new Token(TokenKind.Identifier, source.Substring(start, index - start)));
                    }
                    continue;
                }
                if (c == '=' || c == '<' || c == '>' || c == '!')
                {
                    int start = index++;
                    if (index < source.Length && (source[index] == '=' || source[index] == '>')) index++;
                    result.Add(new Token(TokenKind.Operator, source.Substring(start, index - start)));
                    continue;
                }
                throw new FormatException("Invalid DataTable filter character: " + c);
            }
            result.Add(new Token(TokenKind.End, string.Empty));
            return result;
        }
    }
}
