using System;
using System.Collections.Generic;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData.Function
{
    internal static partial class FunctionMethodCreator
    {
        /// <summary>EM+EE DataTable methods added in the semantic parity pass.</summary>
        private static class DtFunctionHelpers
        {
            public static string Name(ExpressionMediator exm, IOperandTerm term)
            {
                return term == null ? string.Empty : term.GetStrValue(exm);
            }

            public static bool IsString(IOperandTerm term)
            {
                return term != null && term.GetOperandType() == typeof(string);
            }

            public static bool IsInt(IOperandTerm term)
            {
                return term != null && term.GetOperandType() == typeof(Int64);
            }

            public static string ValidateHeader(string name, IOperandTerm[] arguments, int min, int max)
            {
                if (arguments.Length < min || arguments.Length > max) return name + " argument count";
                return null;
            }

            public static string ValidateString(string name, IOperandTerm term, int index)
            {
                return IsString(term) ? null : name + " argument " + index + " must be string";
            }

            public static string ValidateInt(string name, IOperandTerm term, int index)
            {
                return IsInt(term) ? null : name + " argument " + index + " must be integer";
            }

            public static EraDataTable GetTable(ExpressionMediator exm, string key)
            {
                EraDataTable table;
                exm.VEvaluator.VariableData.DataDataTables.TryGetValue(key, out table);
                return table;
            }

            public static bool TryGetArray(IOperandTerm term, out string[] strings, out long[] integers)
            {
                strings = null;
                integers = null;
                VariableTerm variable = term as VariableTerm;
                if (variable == null) return false;
                Array array = variable.Identifier.GetArray() as Array;
                strings = array as string[];
                integers = array as Int64[];
                return strings != null || integers != null;
            }
        }

        internal sealed class DtSemanticColumnMethod : FunctionMethod
        {
            internal enum Operation { Add, Names, Check, Remove }
            readonly Operation operation;

            internal DtSemanticColumnMethod(Operation operation)
            {
                this.operation = operation;
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                int min = operation == Operation.Names ? 1 : 2;
                int max = operation == Operation.Add ? 4 : 2;
                string error = DtFunctionHelpers.ValidateHeader(name, arguments, min, max);
                if (error != null) return error;
                error = DtFunctionHelpers.ValidateString(name, arguments[0], 1);
                if (error != null) return error;
                error = DtFunctionHelpers.ValidateString(name, arguments[1], 2);
                if (error != null && operation != Operation.Names) return error;
                if (operation == Operation.Add && arguments.Length >= 3 && !DtFunctionHelpers.IsString(arguments[2]) && !DtFunctionHelpers.IsInt(arguments[2])) return name + " invalid column type";
                if (operation == Operation.Add && arguments.Length == 4) return DtFunctionHelpers.ValidateInt(name, arguments[3], 4);
                return null;
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string key = DtFunctionHelpers.Name(exm, arguments[0]);
                EraDataTable table = DtFunctionHelpers.GetTable(exm, key);
                if (table == null) return -1L;
                if (operation == Operation.Names)
                {
                    string[] output = exm.VEvaluator.VariableData.DataStringArray[(int)(VariableCode.RESULTS & VariableCode.__LOWERCASE__)];
                    if (arguments.Length > 1)
                    {
                        VariableTerm variable = arguments[1] as VariableTerm;
                        if (variable != null) output = variable.Identifier.GetArray() as string[];
                    }
                    string[] names = table.ColNames();
                    int count = output == null ? 0 : Math.Min(output.Length, names.Length);
                    for (int i = 0; i < count; i++) output[i] = names[i];
                    return names.Length;
                }

                string columnName = DtFunctionHelpers.Name(exm, arguments[1]);
                if (operation == Operation.Check) return table.ColExist(columnName);
                if (operation == Operation.Remove) return table.RemoveCol(columnName) ? 1L : 0L;

                EraDataTable.ColType type = EraDataTable.ColType.String;
                if (arguments.Length >= 3)
                {
                    string typeText = DtFunctionHelpers.IsString(arguments[2]) ? arguments[2].GetStrValue(exm) : arguments[2].GetIntValue(exm).ToString();
                    if (!EraDataTable.TryParseType(typeText, out type)) throw new CodeEE("DT_COLUMN_ADD unsupported DataTable type");
                }
                bool nullable = arguments.Length < 4 || arguments[3].GetIntValue(exm) != 0;
                return table.AddColumn(columnName, type, nullable) ? 1L : 0L;
            }
        }

        internal sealed class DtSemanticColumnOptionsMethod : FunctionMethod
        {
            public DtSemanticColumnOptionsMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 4 || ((arguments.Length - 2) & 1) != 0) return name + " argument count";
                if (!DtFunctionHelpers.IsString(arguments[0]) || !DtFunctionHelpers.IsString(arguments[1])) return name + " invalid column arguments";
                for (int i = 2; i < arguments.Length; i += 2)
                {
                    if (!DtFunctionHelpers.IsString(arguments[i])) return name + " option must be keyword";
                    if (!DtFunctionHelpers.IsString(arguments[i + 1]) && !DtFunctionHelpers.IsInt(arguments[i + 1])) return name + " invalid option value";
                }
                return null;
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return -1L;
                string column = DtFunctionHelpers.Name(exm, arguments[1]);
                if (table.FindColumn(column) == null) return 0L;
                for (int i = 2; i < arguments.Length; i += 2)
                {
                    string option = DtFunctionHelpers.Name(exm, arguments[i]);
                    if (string.Equals(option, "DEFAULT", StringComparison.OrdinalIgnoreCase))
                    {
                        object value = DtFunctionHelpers.IsString(arguments[i + 1])
                            ? (object)arguments[i + 1].GetStrValue(exm)
                            : arguments[i + 1].GetIntValue(exm);
                        try { table.SetDefault(column, value); }
                        catch (Exception) { return 0L; }
                    }
                }
                return 1L;
            }
        }

        /// <summary>
        /// Keyword literal used by DT_COLUMN_OPTIONS, e.g. DT_COLUMN_OPTIONS "db", "age", DEFAULT, 5.
        /// The expression parser represents bare identifiers as variables unless they are registered
        /// methods; registering this literal keeps the reference syntax executable without weakening
        /// unknown-identifier handling globally.
        /// </summary>
        internal sealed class DtDefaultKeywordMethod : FunctionMethod
        {
            public DtDefaultKeywordMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[0];
                CanRestructure = true;
            }

            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return "DEFAULT";
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return 0L;
            }
        }

        internal sealed class DtSemanticLengthMethod : FunctionMethod
        {
            readonly bool columns;
            public DtSemanticLengthMethod(bool columns)
            {
                this.columns = columns;
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = false;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return -1L;
                return columns ? table.ColumnCount : table.RowCount;
            }
        }

        internal sealed class DtSemanticRowSetMethod : FunctionMethod
        {
            readonly bool set;
            public DtSemanticRowSetMethod(bool set)
            {
                this.set = set;
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                int start = set ? 2 : 1;
                if (arguments.Length < start) return name + " argument count";
                if (!DtFunctionHelpers.IsString(arguments[0])) return name + " table name must be string";
                if (set && !DtFunctionHelpers.IsInt(arguments[1])) return name + " row id must be integer";
                if (((arguments.Length - start) & 1) != 0 && arguments.Length != start + 3) return name + " column/value pairs are incomplete";
                if (arguments.Length == start + 3)
                {
                    if (!DtFunctionHelpers.TryGetArray(arguments[start], out _, out _)) return name + " column array must be a reference";
                    if (!DtFunctionHelpers.TryGetArray(arguments[start + 1], out _, out _)) return name + " value array must be a reference";
                    if (!DtFunctionHelpers.IsInt(arguments[start + 2])) return name + " array count must be integer";
                    return null;
                }
                for (int i = start; i < arguments.Length; i += 2)
                {
                    if (!DtFunctionHelpers.IsString(arguments[i])) return name + " column name must be string";
                    if (!DtFunctionHelpers.IsString(arguments[i + 1]) && !DtFunctionHelpers.IsInt(arguments[i + 1])) return name + " value must be string or integer";
                }
                return null;
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string key = DtFunctionHelpers.Name(exm, arguments[0]);
                EraDataTable table = DtFunctionHelpers.GetTable(exm, key);
                if (table == null) return -1L;
                int start = set ? 2 : 1;
                Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (arguments.Length == start + 3)
                {
                    string[] names;
                    long[] nameInts;
                    string[] stringValues;
                    long[] intValues;
                    DtFunctionHelpers.TryGetArray(arguments[start], out names, out nameInts);
                    DtFunctionHelpers.TryGetArray(arguments[start + 1], out stringValues, out intValues);
                    int count = (int)Math.Max(0L, arguments[start + 2].GetIntValue(exm));
                    if (names == null || (stringValues == null && intValues == null)) return -1L;
                    count = Math.Min(count, names.Length);
                    if (stringValues != null) count = Math.Min(count, stringValues.Length);
                    if (intValues != null) count = Math.Min(count, intValues.Length);
                    for (int i = 0; i < count; i++) values[names[i]] = stringValues != null ? (object)stringValues[i] : intValues[i];
                }
                else
                {
                    for (int i = start; i < arguments.Length; i += 2)
                        values[DtFunctionHelpers.Name(exm, arguments[i])] = DtFunctionHelpers.IsString(arguments[i + 1]) ? (object)arguments[i + 1].GetStrValue(exm) : arguments[i + 1].GetIntValue(exm);
                }

                if (set)
                {
                    foreach (KeyValuePair<string, object> value in values)
                    {
                        string error;
                        if (!table.TrySet(arguments[1].GetIntValue(exm), value.Key, value.Value, true, out error)) return -2L;
                    }
                    return values.Count;
                }
                long id;
                string addError;
                return table.TryAddRow(values, out id, out addError) ? id : -1L;
            }
        }

        internal sealed class DtSemanticRowRemoveMethod : FunctionMethod
        {
            public DtSemanticRowRemoveMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2 || arguments.Length > 3) return name + " argument count";
                if (!DtFunctionHelpers.IsString(arguments[0]) || !DtFunctionHelpers.IsInt(arguments[1])) return name + " invalid row id";
                if (arguments.Length == 3 && !DtFunctionHelpers.IsInt(arguments[2])) return name + " invalid array count";
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return -1L;
                if (arguments.Length == 2) return table.RemoveRowById(arguments[1].GetIntValue(exm)) ? 1L : 0L;
                VariableTerm variable = arguments[1] as VariableTerm;
                long[] ids = variable == null ? null : variable.Identifier.GetArray() as long[];
                if (ids == null) return 0L;
                int count = Math.Min(ids.Length, Math.Max(0, (int)arguments[2].GetIntValue(exm)));
                List<long> selected = new List<long>(count);
                for (int i = 0; i < count; i++) selected.Add(ids[i]);
                return table.RemoveRowsById(selected);
            }
        }

        internal sealed class DtSemanticCellGetMethod : FunctionMethod
        {
            internal enum Operation { Int, String, IsNull }
            readonly Operation operation;
            internal DtSemanticCellGetMethod(Operation operation)
            {
                this.operation = operation;
                ReturnType = operation == Operation.String ? typeof(string) : typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 3 || arguments.Length > 4) return name + " argument count";
                if (!DtFunctionHelpers.IsString(arguments[0]) || !DtFunctionHelpers.IsInt(arguments[1]) || !DtFunctionHelpers.IsString(arguments[2])) return name + " invalid cell arguments";
                if (arguments.Length == 4) return DtFunctionHelpers.ValidateInt(name, arguments[3], 4);
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return operation == Operation.IsNull ? -1L : 0L;
                long row = arguments[1].GetIntValue(exm);
                bool byId = arguments.Length == 4 && arguments[3].GetIntValue(exm) != 0;
                string column = DtFunctionHelpers.Name(exm, arguments[2]);
                if (table.ColExist(column) == 0) return operation == Operation.IsNull ? -2L : 0L;
                object ignored;
                if (!table.TryGet(row, column, byId, out ignored)) return operation == Operation.IsNull ? -2L : 0L;
                if (operation == Operation.IsNull) return table.IsNull(row, column, byId) ? 1L : 0L;
                return table.GetInt(row, column, byId);
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return string.Empty;
                bool byId = arguments.Length == 4 && arguments[3].GetIntValue(exm) != 0;
                return table.GetStr(arguments[1].GetIntValue(exm), DtFunctionHelpers.Name(exm, arguments[2]), byId);
            }
        }

        internal sealed class DtSemanticCellSetMethod : FunctionMethod
        {
            public DtSemanticCellSetMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 3 || arguments.Length > 5) return name + " argument count";
                if (!DtFunctionHelpers.IsString(arguments[0]) || !DtFunctionHelpers.IsInt(arguments[1]) || !DtFunctionHelpers.IsString(arguments[2])) return name + " invalid cell arguments";
                if (arguments.Length >= 4 && !DtFunctionHelpers.IsString(arguments[3]) && !DtFunctionHelpers.IsInt(arguments[3])) return name + " invalid cell value";
                if (arguments.Length == 5) return DtFunctionHelpers.ValidateInt(name, arguments[4], 5);
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return -1L;
                string column = DtFunctionHelpers.Name(exm, arguments[2]);
                if (table.ColExist(column) == 0) return -3L;
                if (string.Equals(column, "id", StringComparison.OrdinalIgnoreCase)) return 0L;
                bool byId = arguments.Length == 5 && arguments[4].GetIntValue(exm) != 0;
                object existing;
                if (!table.TryGet(arguments[1].GetIntValue(exm), column, byId, out existing)) return -3L;
                object value = arguments.Length < 4 ? null : (DtFunctionHelpers.IsString(arguments[3]) ? (object)arguments[3].GetStrValue(exm) : arguments[3].GetIntValue(exm));
                string error;
                return table.TrySet(arguments[1].GetIntValue(exm), column, value, byId, out error) ? 1L : -2L;
            }
        }

        internal sealed class DtSemanticSelectMethod : FunctionMethod
        {
            public DtSemanticSelectMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1 || arguments.Length > 4) return name + " argument count";
                if (!DtFunctionHelpers.IsString(arguments[0])) return name + " table name must be string";
                if (arguments.Length >= 2 && arguments[1] != null && !DtFunctionHelpers.IsString(arguments[1])) return name + " filter must be string";
                if (arguments.Length >= 3 && arguments[2] != null && !DtFunctionHelpers.IsString(arguments[2])) return name + " sort must be string";
                if (arguments.Length == 4 && !(arguments[3] is VariableTerm)) return name + " output must be array reference";
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return -1L;
                string filter = arguments.Length >= 2 && arguments[1] != null ? arguments[1].GetStrValue(exm) : null;
                string sort = arguments.Length >= 3 && arguments[2] != null ? arguments[2].GetStrValue(exm) : null;
                List<int> matches = DataTableExpressionParser.Select(table, filter, sort);
                long[] output = exm.VEvaluator.VariableData.DataIntegerArray[(int)(VariableCode.RESULT & VariableCode.__LOWERCASE__)];
                bool explicitOutput = arguments.Length == 4;
                if (explicitOutput)
                {
                    VariableTerm variable = arguments[3] as VariableTerm;
                    output = variable == null ? null : variable.Identifier.GetArray() as long[];
                }
                if (output != null)
                {
                    int offset = explicitOutput ? 0 : 1;
                    int count = Math.Min(matches.Count, Math.Max(0, output.Length - offset));
                    for (int i = 0; i < count; i++) output[i + offset] = table.GetRowByPosition(matches[i]).Id;
                }
                return matches.Count;
            }
        }

        internal sealed class DtSemanticToXmlMethod : FunctionMethod
        {
            public DtSemanticToXmlMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1 || arguments.Length > 2) return name + " argument count";
                if (!DtFunctionHelpers.IsString(arguments[0])) return name + " table name must be string";
                if (arguments.Length == 2 && !(arguments[1] is VariableTerm)) return name + " schema output must be reference";
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                EraDataTable table = DtFunctionHelpers.GetTable(exm, DtFunctionHelpers.Name(exm, arguments[0]));
                if (table == null) return string.Empty;
                string schema = table.ToXmlSchema();
                if (arguments.Length == 2)
                {
                    VariableTerm variable = arguments[1] as VariableTerm;
                    string[] output = variable == null ? null : variable.Identifier.GetArray() as string[];
                    if (output != null && output.Length > 0) output[0] = schema;
                }
                else
                {
                    string[] results = exm.VEvaluator.VariableData.DataStringArray[(int)(VariableCode.RESULTS & VariableCode.__LOWERCASE__)];
                    if (results.Length > 1) results[1] = schema;
                }
                return table.ToXml();
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) { return 0L; }
        }

        internal sealed class DtSemanticFromXmlMethod : FunctionMethod
        {
            public DtSemanticFromXmlMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) };
                CanRestructure = false;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string key = DtFunctionHelpers.Name(exm, arguments[0]);
                try
                {
                    EraDataTable table = EraDataTable.FromXml(arguments[1].GetStrValue(exm), arguments[2].GetStrValue(exm));
                    exm.VEvaluator.VariableData.DataDataTables[key] = table;
                    return 1L;
                }
                catch (Exception) { return 0L; }
            }
        }
    }
}
