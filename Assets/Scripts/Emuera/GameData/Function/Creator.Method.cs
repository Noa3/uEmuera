using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameProc;
using MinorShift._Library;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData;
//using System.Drawing;
//using Microsoft.VisualBasic;
//using System.Windows.Forms;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Content;
using uEmuera.Drawing;
using uEmuera.VisualBasic;

namespace MinorShift.Emuera.GameData.Function
{

    internal static partial class FunctionMethodCreator
    {
        #region Character data
        private sealed class GetcharaMethod : FunctionMethod
        {
            public GetcharaMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 2, with 1 optional, so 1-2 arguments are required.
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");

                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (arguments[0].GetOperandType() != typeof(Int64))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                //2 is optional
                if ((arguments.Length == 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                return null;
            }
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 integer = arguments[0].GetIntValue(exm);
                if (!Config.CompatiSPChara)
				{
					//if ((arguments.Length > 1) && (arguments[1] != null) && (arguments[1].GetIntValue(exm) != 0))
					return exm.VEvaluator.GetChara(integer);
				}
				//Legacy processing below for compatibility
                bool CheckSp = false;
                if ((arguments.Length > 1) && (arguments[1] != null) && (arguments[1].GetIntValue(exm) != 0))
                    CheckSp = true;
                if (CheckSp)
                {
                    long chara = exm.VEvaluator.GetChara_UseSp(integer, false);
                    if (chara != -1)
                        return chara;
                    else
                        return exm.VEvaluator.GetChara_UseSp(integer, true);
                }
                else
                    return exm.VEvaluator.GetChara_UseSp(integer, false);
            }
        }

        private sealed class GetspcharaMethod : FunctionMethod
        {
            public GetspcharaMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = false;
            }
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
				if(!Config.CompatiSPChara)
					throw new CodeEE(GameMessages.T("SP character-related functions are not available by default (enable the \"Use SP Characters\" compatibility option)"));
                Int64 integer = arguments[0].GetIntValue(exm);
                return exm.VEvaluator.GetChara_UseSp(integer, true);
            }
        }

        private sealed class CsvStrDataMethod : FunctionMethod
        {
            readonly CharacterStrData charaStr;
            public CsvStrDataMethod()
            {
                ReturnType = typeof(string);
				argumentTypeArray = null;
                charaStr = CharacterStrData.NAME;
                CanRestructure = true;
            }
            public CsvStrDataMethod(CharacterStrData cStr)
            {
                ReturnType = typeof(string);
				argumentTypeArray = null;
				charaStr = cStr;
				CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!arguments[0].IsInteger)
                    return name + GameMessages.T(" function: argument #1 is not a number");
                if (arguments.Length == 1)
                    return null;
                if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 is not a number");
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long x = arguments[0].GetIntValue(exm);
				long y = (arguments.Length > 1 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
				if (!Config.CompatiSPChara && y != 0)
					throw new CodeEE(GameMessages.T("SP character-related functions are not available by default (enable the \"Use SP Characters\" compatibility option)"));
                return exm.VEvaluator.GetCharacterStrfromCSVData(x, charaStr, (y != 0), 0);
            }
        }

        private sealed class CsvcstrMethod : FunctionMethod
        {
            public CsvcstrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments.Length > 3)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!arguments[0].IsInteger)
                    return name + GameMessages.T(" function: argument #1 is not a number");
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != typeof(Int64))
                    return name + GameMessages.T(" function: argument #2 is not a number");
                if (arguments.Length == 2)
                    return null;
                if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 is not a number");
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long x = arguments[0].GetIntValue(exm);
                long y = arguments[1].GetIntValue(exm);
                long z = (arguments.Length == 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
				if(!Config.CompatiSPChara && z != 0)
					throw new CodeEE(GameMessages.T("SP character-related functions are not available by default (enable the \"Use SP Characters\" compatibility option)"));
                return exm.VEvaluator.GetCharacterStrfromCSVData(x, CharacterStrData.CSTR, (z != 0), y);
            }
        }

        private sealed class CsvDataMethod : FunctionMethod
        {
            readonly CharacterIntData charaInt;
            public CsvDataMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                charaInt = CharacterIntData.BASE;
                CanRestructure = true;
            }
            public CsvDataMethod(CharacterIntData cInt)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
				charaInt = cInt;
				CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments.Length > 3)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!arguments[0].IsInteger)
                    return name + GameMessages.T(" function: argument #1 is not a number");
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != typeof(Int64))
                    return name + GameMessages.T(" function: argument #2 is not a number");
                if (arguments.Length == 2)
                    return null;
                if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 is not a number");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long x = arguments[0].GetIntValue(exm);
                long y = arguments[1].GetIntValue(exm);
                long z = (arguments.Length == 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
				if(!Config.CompatiSPChara && z != 0)
					throw new CodeEE(GameMessages.T("SP character-related functions are not available by default (enable the \"Use SP Characters\" compatibility option)"));
                return exm.VEvaluator.GetCharacterIntfromCSVData(x, charaInt, (z != 0), y);
            }
        }

        private sealed class FindcharaMethod : FunctionMethod
        {
            public FindcharaMethod(bool last)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
                isLast = last;
            }

            readonly bool isLast;
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 3, with 1 optional, so 2-3 arguments are required.
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments.Length > 4)
                    return name + GameMessages.T(" function: too many arguments");

                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                if (!(((VariableTerm)arguments[0]).Identifier.IsCharacterData))
                    return name + GameMessages.T(" function: argument #1 is not a character variable");
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != arguments[0].GetOperandType())
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                //The 3rd is optional
                if ((arguments.Length >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 has an invalid type");
                //The 4th is optional
                if ((arguments.Length >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #4 has an invalid type");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                VariableTerm vTerm = (VariableTerm)arguments[0];
                VariableToken varID = vTerm.Identifier;

                Int64 elem = 0;
                if (vTerm.Identifier.IsArray1D)
                    elem = vTerm.GetElementInt(1, exm);
                else if (vTerm.Identifier.IsArray2D)
                {
                    elem = vTerm.GetElementInt(1, exm) << 32;
                    elem += vTerm.GetElementInt(2, exm);
                }
                Int64 startindex = 0;
                Int64 lastindex = exm.VEvaluator.CHARANUM;
                if (arguments.Length >= 3 && arguments[2] != null)
                    startindex = arguments[2].GetIntValue(exm);
                if (arguments.Length >= 4 && arguments[3] != null)
                    lastindex = arguments[3].GetIntValue(exm);
                if (startindex < 0 || startindex >= exm.VEvaluator.CHARANUM)
                    throw new CodeEE((isLast ? "" : "") + GameMessages.T(" function: argument #3 (") + startindex.ToString() + GameMessages.T(") is outside the character position range"));
                if (lastindex < 0 || lastindex > exm.VEvaluator.CHARANUM)
                    throw new CodeEE((isLast ? "" : "") + GameMessages.T(" function: argument #4 (") + lastindex.ToString() + GameMessages.T(") is outside the character position range"));
                long ret;
                if (varID.IsString)
                {
                    string word = arguments[1].GetStrValue(exm);
                    ret = exm.VEvaluator.FindChara(varID, elem, word, startindex, lastindex, isLast);
                }
                else
                {
                    Int64 word = arguments[1].GetIntValue(exm);
                    ret = exm.VEvaluator.FindChara(varID, elem, word, startindex, lastindex, isLast);
                }
                return (ret);
            }
        }

        private sealed class ExistCsvMethod : FunctionMethod
        {
            public ExistCsvMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!arguments[0].IsInteger)
                    return name + GameMessages.T(" function: argument #1 is not a number");
                if (arguments.Length == 1)
                    return null;
                if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 is not a number");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 no = arguments[0].GetIntValue(exm);
                bool isSp =(arguments.Length == 2 && arguments[1] != null) ? (arguments[1].GetIntValue(exm) != 0) : false;
				if(!Config.CompatiSPChara && isSp)
					throw new CodeEE(GameMessages.T("SP character-related functions are not available by default (enable the \"Use SP Characters\" compatibility option)"));

                return (exm.VEvaluator.ExistCsv(no, isSp));
            }
        }
        #endregion

        #region General processing
        private sealed class VarsizeMethod : FunctionMethod
        {
            public VarsizeMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = true;
				//1808beta009: got more troublesome with the addition of reference-type variables
				HasUniqueRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!arguments[0].IsString)
                    return name + GameMessages.T(" function: argument #1 is not a string");
                if (arguments[0] is SingleTerm)
                {
                    string varName = ((SingleTerm)arguments[0]).Str;
                    if (GlobalStatic.IdentifierDictionary.GetVariableToken(varName, null, true) == null)
                        return name + GameMessages.T(" function: argument #1 is not a variable name");
                }
                if (arguments.Length == 1)
                    return null;
                if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 is not a number");
                if (arguments.Length == 2)
                    return null;
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                VariableToken var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
                if (var == null)
                    throw new CodeEE(GameMessages.T("VARSIZE function: argument #1 (\"") + arguments[0].GetStrValue(exm) + GameMessages.T("\") is not a variable name"));
                int dim = 0;
                if (arguments.Length == 2 && arguments[1] != null)
                    dim = (int)arguments[1].GetIntValue(exm);
                return (var.GetLength(dim));
            }
			public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				arguments[0].Restructure(exm);
				if (arguments.Length > 1)
					arguments[1].Restructure(exm);
				if (arguments[0] is SingleTerm && (arguments.Length == 1 || arguments[1] is SingleTerm))
				{
					VariableToken var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
					if (var == null || var.IsReference)//Cannot be const-ified if variable-length
						return false;
					return true;
				}
				return false;
			}
        }

        private sealed class CheckfontMethod : FunctionMethod
        {
			public CheckfontMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = true;//Probably won't change during runtime...
			}
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                //string str = arguments[0].GetStrValue(exm);
                //System.Drawing.Text.InstalledFontCollection ifc = new System.Drawing.Text.InstalledFontCollection();
                //Int64 isInstalled = 0;
                //foreach (System.Drawing.FontFamily ff in ifc.Families)
                //{
                //    if (ff.Name == str)
                //    {
                //        isInstalled = 1;
                //        break;
                //    }
                //}
                //return (isInstalled);
                //TODO
                return 1;
            }

        }

        private sealed class CheckdataMethod : FunctionMethod
        {
			public CheckdataMethod(EraSaveFileType type)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = false;
				this.type = type;
            }

            readonly EraSaveFileType type;
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 target = arguments[0].GetIntValue(exm);
                if (target < 0)
                    throw new CodeEE(Name + GameMessages.T(" function: a negative value (") + target.ToString() + GameMessages.T(") was specified"));
                else if (target > int.MaxValue)
                    throw new CodeEE(Name + GameMessages.T(" function: value (") + target.ToString() + GameMessages.T(") is too large"));
                EraDataResult result = exm.VEvaluator.CheckData((int)target, type);
                exm.VEvaluator.RESULTS = result.DataMes;
                return ((long)result.State);
            }
        }

		/// <summary>
		/// Version that specifies the file name as a string; CHKVARDATA and CHKCHARADATA fall into this category
		/// </summary>
		private sealed class CheckdataStrMethod : FunctionMethod
		{
			public CheckdataStrMethod(EraSaveFileType type)
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
				this.type = type;
			}

            readonly EraSaveFileType type;
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string datFilename = arguments[0].GetStrValue(exm);
                EraDataResult result = exm.VEvaluator.CheckData(datFilename, type);
                exm.VEvaluator.RESULTS = result.DataMes;
				return ((long)result.State);
			}
		}

		/// <summary>
		/// File search function
		/// </summary>
		private sealed class FindFilesMethod : FunctionMethod
		{
			public FindFilesMethod(EraSaveFileType type)
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
				this.type = type;
			}

            readonly EraSaveFileType type;

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length > 1)
					return name + GameMessages.T(" function: too many arguments");
				if (arguments.Length == 0 || arguments[0] == null)
					return null;
				if (!arguments[0].IsString)
					return name + GameMessages.T(" function: argument #1 is not a string");
				return null;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string pattern = "*";
				if (arguments.Length > 0 && arguments[0] != null)
					pattern = arguments[0].GetStrValue(exm);
                List<string> filepathes = exm.VEvaluator.GetDatFiles(type == EraSaveFileType.CharVar, pattern);
                string[] results = exm.VEvaluator.VariableData.DataStringArray[(int)(VariableCode.RESULTS & VariableCode.__LOWERCASE__)];
				if (filepathes.Count <= results.Length)
					filepathes.CopyTo(results);
				else
					filepathes.CopyTo(0, results, 0, results.Length);
				return filepathes.Count;
			}
		}
		

        private sealed class IsSkipMethod : FunctionMethod
        {
            public IsSkipMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return exm.Process.SkipPrint ? 1L : 0L;
            }
        }

		private sealed class MesSkipMethod : FunctionMethod
		{
			public MesSkipMethod(bool warn)
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
				this.warn = warn;
			}

            readonly bool warn;
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length > 0)
					return name + GameMessages.T(" function: too many arguments");
				if (warn)
					ParserMediator.Warn(GameMessages.T("The function MOUSESKIP() is not recommended. Use the function MESSKIP() instead"), GlobalStatic.Process.GetScaningLine(), 1, false, false, null);
                return null;
            }
			public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				return GlobalStatic.Console.MesSkip ? 1L : 0L;
			}
		}


        private sealed class GetColorMethod : FunctionMethod
        {
            public GetColorMethod(bool isDef)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = isDef;
                defaultColor = isDef;
            }

            readonly bool defaultColor;
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Color color = (defaultColor) ? Config.ForeColor : GlobalStatic.Console.StringStyle.Color;
                return (color.ToArgb() & 0xFFFFFF);
            }
        }

        private sealed class GetFocusColorMethod : FunctionMethod
        {
            public GetFocusColorMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return (Config.FocusColor.ToArgb() & 0xFFFFFF);
            }
        }

        private sealed class GetBGColorMethod : FunctionMethod
        {
            public GetBGColorMethod(bool isDef)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = isDef;
                defaultColor = isDef;
            }

            readonly bool defaultColor;
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Color color = (defaultColor) ? Config.BackColor : GlobalStatic.Console.bgColor;
                return (color.ToArgb() & 0xFFFFFF);
            }
        }

        private sealed class GetStyleMethod : FunctionMethod
        {
            public GetStyleMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                FontStyle fontstyle = GlobalStatic.Console.StringStyle.FontStyle;
                long ret = 0;
                if ((fontstyle & FontStyle.Bold) == FontStyle.Bold)
                    ret |= 1;
                if ((fontstyle & FontStyle.Italic) == FontStyle.Italic)
                    ret |= 2;
                if ((fontstyle & FontStyle.Strikeout) == FontStyle.Strikeout)
                    ret |= 4;
                if ((fontstyle & FontStyle.Underline) == FontStyle.Underline)
                    ret |= 8;
                return (ret);
            }
        }

        private sealed class GetFontMethod : FunctionMethod
        {
            public GetFontMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return (GlobalStatic.Console.StringStyle.Fontname);
            }
        }

        private sealed class BarStringMethod : FunctionMethod
        {
            public BarStringMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(long), typeof(long), typeof(long) };
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long var = arguments[0].GetIntValue(exm);
                long max = arguments[1].GetIntValue(exm);
                long length = arguments[2].GetIntValue(exm);
                return exm.CreateBar(var, max, length);
            }
        }

        private sealed class CurrentAlignMethod : FunctionMethod
        {
            public CurrentAlignMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (exm.Console.Alignment == GameView.DisplayLineAlignment.LEFT)
                    return "LEFT";
                else if (exm.Console.Alignment == GameView.DisplayLineAlignment.CENTER)
                    return "CENTER";
                else
                    return "RIGHT";
            }
        }

        private sealed class CurrentRedrawMethod : FunctionMethod
        {
            public CurrentRedrawMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return (exm.Console.Redraw == GameView.ConsoleRedraw.None) ? 0L : 1L;
            }
        }

		private sealed class ColorFromNameMethod : FunctionMethod
		{
			public ColorFromNameMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = true;
			}
			public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string colorName = arguments[0].GetStrValue(exm);
				Color color = Color.FromName(colorName);
                int i;
                if (color.A > 0)
					i = (color.R << 16) + (color.G << 8) + color.B;
				else
				{
					if (colorName.Equals("transparent", StringComparison.OrdinalIgnoreCase))
						throw new CodeEE(GameMessages.T("A fully transparent (Transparent) color cannot be specified"));
					//throw new CodeEE("The specified color name \"" + colorName + "\" is invalid");
					i = -1;
				}
				return i;
			}
		}

		private sealed class ColorFromRGBMethod : FunctionMethod
		{
			public ColorFromRGBMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(long), typeof(long), typeof(long) };
				CanRestructure = true;
			}
			public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				long r = arguments[0].GetIntValue(exm);
				if(r < 0 || r > 255)
					throw new CodeEE(GameMessages.T("argument #1 is out of range 0-255"));
				long g = arguments[1].GetIntValue(exm);
				if(g< 0 || g > 255)
					throw new CodeEE(GameMessages.T("argument #2 is out of range 0-255"));
				long b = arguments[2].GetIntValue(exm);
				if(b < 0 || b > 255)
					throw new CodeEE(GameMessages.T("argument #3 is out of range 0-255"));
				return (r << 16) + (g << 8) + b;
			}
		}
		/// <summary>
		/// 1810: created but put on hold
		/// </summary>
		private sealed class GetRefMethod : FunctionMethod
		{
			public GetRefMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = null;
				CanRestructure = false;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 1)
					return name + GameMessages.T(" function requires at least 1 argument");
				if (arguments.Length > 1)
					return name + GameMessages.T(" function: too many arguments");
				if (arguments[0] == null)
					return name + GameMessages.T(" function: argument #1 cannot be omitted");
				if (!(arguments[0] is UserDefinedRefMethodNoArgTerm))
					return name + GameMessages.T(" function: argument #1 is not a function reference");
				return null;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				return ((UserDefinedRefMethodNoArgTerm)arguments[0]).GetRefName();
			}
		}
        #endregion

        #region Constant retrieval
        private sealed class MoneyStrMethod : FunctionMethod
        {
            public MoneyStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 2, with 1 optional, so 1-2 arguments are required.
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (arguments[0].GetOperandType() != typeof(Int64))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                if ((arguments.Length >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(string)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long money = arguments[0].GetIntValue(exm);
                if ((arguments.Length < 2) || (arguments[1] == null))
                    return (Config.MoneyFirst) ? Config.MoneyLabel + money.ToString() : money.ToString() + Config.MoneyLabel;
                string format = arguments[1].GetStrValue(exm);
                string ret;
                try
                {
                    ret = money.ToString(format);
                }
                catch (FormatException)
                {
                    throw new CodeEE(GameMessages.T("MONEYSTR function: argument #2 format specifier is invalid"));
                }
                return (Config.MoneyFirst) ? Config.MoneyLabel + ret : ret + Config.MoneyLabel;
            }
        }

        private sealed class GetPrintCPerLineMethod : FunctionMethod
        {
            public GetPrintCPerLineMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return (Config.PrintCPerLine);
            }
        }

        private sealed class PrintCLengthMethod : FunctionMethod
        {
            public PrintCLengthMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }
            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return (Config.PrintCLength);
            }
        }

        private sealed class GetSaveNosMethod : FunctionMethod
        {
            public GetSaveNosMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return (Config.SaveDataNos);
            }
        }

        private sealed class GettimeMethod : FunctionMethod
        {
            public GettimeMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                long date = DateTime.Now.Year;
                date = date * 100 + DateTime.Now.Month;
                date = date * 100 + DateTime.Now.Day;
                date = date * 100 + DateTime.Now.Hour;
                date = date * 100 + DateTime.Now.Minute;
                date = date * 100 + DateTime.Now.Second;
                date = date * 1000 + DateTime.Now.Millisecond;
                return (date);//17 digits, around 20 quadrillion.
            }
        }

        private sealed class GettimesMethod : FunctionMethod
        {
            public GettimesMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return (DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            }
        }

        private sealed class GetmsMethod : FunctionMethod
        {
            public GetmsMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                //Elapsed time since January 1, 0001, in milliseconds.
                //Ticks are in 100-nanosecond units, but in practice they lack such precision, so it's pointless.
                return (DateTime.Now.Ticks / 10000);
            }
        }

        private sealed class GetSecondMethod : FunctionMethod
        {
            public GetSecondMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                //Elapsed time since January 1, 0001, in seconds.
                //Ticks are in 100-nanosecond units, but in practice they lack such precision, so it's pointless.
                return (DateTime.Now.Ticks / 10000000);
            }
        }
        #endregion

        #region Math functions
        private sealed class RandMethod : FunctionMethod
        {
            public RandMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 2, with 1 optional, so 1-2 arguments are required.
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments.Length == 1)
                {
                    if (arguments[0] == null)
                        return name + GameMessages.T(" function requires at least 1 argument");
                    if ((arguments[0].GetOperandType() != typeof(Int64)))
                        return name + GameMessages.T(" function: argument #1 has an invalid type");
                    return null;
                }
                //The 1st is optional
                if ((arguments[0] != null) && (arguments[0].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 min = 0;
                long max;
                if (arguments.Length == 1)
                    max = arguments[0].GetIntValue(exm);
                else
                {
                    if (arguments[0] != null)
                        min = arguments[0].GetIntValue(exm);
                    max = arguments[1].GetIntValue(exm);
                }
                if (max <= min)
                {
                    if (min == 0)
                        throw new CodeEE(GameMessages.T("RAND: a maximum value of 0 or less (") + max.ToString() + GameMessages.T(") was specified"));
                    else
                        throw new CodeEE(GameMessages.T("RAND: a maximum value below the minimum (") + max.ToString() + GameMessages.T(") was specified"));
                }
                return (exm.VEvaluator.GetNextRand(max - min) + min);
            }
        }

        private sealed class MaxMethod : FunctionMethod
        {
            readonly bool isMax;
            public MaxMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isMax = true;
                CanRestructure = true;
            }
            public MaxMethod(bool max)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isMax = max;
                CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" cannot be omitted");
                    if (arguments[i].GetOperandType() != typeof(Int64))
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" has an invalid type");
                }
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = arguments[0].GetIntValue(exm);

                for (int i = 1; i < arguments.Length; i++)
                {
                    Int64 newRet = arguments[i].GetIntValue(exm);
                    if (isMax)
                    {
                        if (ret < newRet)
                            ret = newRet;
                    }
                    else
                    {
                        if (ret > newRet)
                            ret = newRet;
                    }
                }
                return (ret);
            }
        }

        private sealed class AbsMethod : FunctionMethod
        {
            public AbsMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = arguments[0].GetIntValue(exm);
                return (Math.Abs(ret));
            }
        }

        private sealed class PowerMethod : FunctionMethod
        {
            public PowerMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 x = arguments[0].GetIntValue(exm);
                Int64 y = arguments[1].GetIntValue(exm);
                double pow = Math.Pow(x, y);
                if (double.IsNaN(pow))
                    throw new CodeEE(GameMessages.T("power result is not a number"));
                else if (double.IsInfinity(pow))
                    throw new CodeEE(GameMessages.T("power result is infinite"));
                else if ((pow >= Int64.MaxValue) || (pow <= Int64.MinValue))
                    throw new CodeEE(GameMessages.T("power result (") + pow.ToString() + GameMessages.T(") is out of range for a 64-bit signed integer"));
                return ((long)pow);
            }
        }

        private sealed class SqrtMethod : FunctionMethod
        {
            public SqrtMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = arguments[0].GetIntValue(exm);
                if (ret < 0)
                    throw new CodeEE(GameMessages.T("SQRT function: a negative value was specified"));
                return ((Int64)Math.Sqrt(ret));
            }
        }

        private sealed class CbrtMethod : FunctionMethod
        {
            public CbrtMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = arguments[0].GetIntValue(exm);
                if (ret < 0)
                    throw new CodeEE(GameMessages.T("CBRT function: a negative value was specified"));
                return ((Int64)Math.Pow((double)ret, 1.0 / 3.0));
            }
        }

        private sealed class LogMethod : FunctionMethod
        {
            readonly double Base;
            public LogMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                Base = Math.E;
                CanRestructure = true;
            }
            public LogMethod(double b)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                Base = b;
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = arguments[0].GetIntValue(exm);
                if (ret <= 0)
                    throw new CodeEE(GameMessages.T("log function: a value of 0 or less was specified"));
                if (Base <= 0.0d)
                    throw new CodeEE(GameMessages.T("log function: a base of 0 or less was specified"));
                double dret = (double)ret;
                if (Base == Math.E)
                    dret = Math.Log(dret);
                else
                    dret = Math.Log10(dret);
                if (double.IsNaN(dret))
                    throw new CodeEE(GameMessages.T("calculated value is not a number"));
                else if (double.IsInfinity(dret))
                    throw new CodeEE(GameMessages.T("calculated value is infinite"));
                else if ((dret >= Int64.MaxValue) || (dret <= Int64.MinValue))
                    throw new CodeEE(GameMessages.T("calculated result (") + dret.ToString() + GameMessages.T(") is out of range for a 64-bit signed integer"));
                return ((Int64)dret);
            }
        }

        private sealed class ExpMethod : FunctionMethod
        {
            public ExpMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = arguments[0].GetIntValue(exm);
                double dret = Math.Exp((double)ret);
                if (double.IsNaN(dret))
                    throw new CodeEE(GameMessages.T("calculated value is not a number"));
                else if (double.IsInfinity(dret))
                    throw new CodeEE(GameMessages.T("calculated value is infinite"));
                else if ((dret >= Int64.MaxValue) || (dret <= Int64.MinValue))
                    throw new CodeEE(GameMessages.T("calculated result (") + dret.ToString() + GameMessages.T(") is out of range for a 64-bit signed integer"));

                return ((Int64)dret);
            }
        }

        private sealed class SignMethod : FunctionMethod
        {

            public SignMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = arguments[0].GetIntValue(exm);
                return (Math.Sign(ret));
            }
        }

        private sealed class GetLimitMethod : FunctionMethod
        {
            public GetLimitMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 value = arguments[0].GetIntValue(exm);
                Int64 min = arguments[1].GetIntValue(exm);
                Int64 max = arguments[2].GetIntValue(exm);
                long ret;
                if (value < min)
                    ret = min;
                else if (value > max)
                    ret = max;
                else
                    ret = value;
                return (ret);
            }
        }
        #endregion

        #region Variable operations
        private sealed class SumArrayMethod : FunctionMethod
        {
            readonly bool isCharaRange;
            public SumArrayMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = false;
                CanRestructure = false;
            }
            public SumArrayMethod(bool isChara)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = isChara;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 3)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm))
                    return name + GameMessages.T(" function: argument #1 is not a variable");
                VariableTerm varToken = (VariableTerm)arguments[0];
                if (varToken.IsString)
                    return name + GameMessages.T(" function: argument #1 is not a numeric variable");
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + GameMessages.T(" function: argument #1 is not a character variable");
                if (!isCharaRange && !varToken.Identifier.IsArray1D && !varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D)
                    return name + GameMessages.T(" function: argument #1 is not an array variable");
                if (arguments.Length == 1)
                    return null;
                if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 is not a number");
                if (arguments.Length == 2)
                    return null;
                if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 is not a number");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                VariableTerm varTerm = (VariableTerm)arguments[0];
                Int64 index1 = (arguments.Length >= 2 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
                Int64 index2 = (arguments.Length == 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLastLength());

                FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);
                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(index1, index2, "SUMARRAY", 2L, 3L);
                    return (exm.VEvaluator.GetArraySum(p, index1, index2));
                }
                else
                {
                    Int64 charaNum = exm.VEvaluator.CHARANUM;
                    if (index1 >= charaNum || index1 < 0 || index2 > charaNum || index2 < 0)
                        throw new CodeEE(GameMessages.T("SUMCARRAY function: range exceeds the character array bounds (") + index1.ToString() + GameMessages.T(" to ") + index2.ToString() + ")");
                    return (exm.VEvaluator.GetArraySumChara(p, index1, index2));
                }
            }
        }

        private sealed class MatchMethod : FunctionMethod
        {
            readonly bool isCharaRange;
            public MatchMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = false;
                CanRestructure = false;
                HasUniqueRestructure = true;
            }
            public MatchMethod(bool isChara)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = isChara;
                CanRestructure = false;
                HasUniqueRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments.Length > 4)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm))
                    return name + GameMessages.T(" function: argument #1 is not a variable");
                VariableTerm varToken = (VariableTerm)arguments[0];
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + GameMessages.T(" function: argument #1 is not a character variable");
                if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
                    return name + GameMessages.T(" function does not support double or triple arrays");
                if (!isCharaRange && !varToken.Identifier.IsArray1D)
                    return name + GameMessages.T(" function: argument #1 is not an array variable");
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != arguments[0].GetOperandType())
                    return name + GameMessages.T(" function: argument #1 and argument #2 have different types");
                if ((arguments.Length >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 has an invalid type");
                if ((arguments.Length >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #4 has an invalid type");
                return null;
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                VariableTerm varTerm = arguments[0] as VariableTerm;
                Int64 start = (arguments.Length > 2 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
                Int64 end = (arguments.Length > 3 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLength());

                FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);
                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(start, end, "MATCH", 3L, 4L);
                    if (arguments[0].GetOperandType() == typeof(Int64))
                    {
                        Int64 targetValue = arguments[1].GetIntValue(exm);
                        return (exm.VEvaluator.GetMatch(p, targetValue, start, end));
                    }
                    else
                    {
                        string targetStr = arguments[1].GetStrValue(exm);
                        return (exm.VEvaluator.GetMatch(p, targetStr, start, end));
                    }
                }
                else
                {
                    Int64 charaNum = exm.VEvaluator.CHARANUM;
                    if (start >= charaNum || start < 0 || end > charaNum || end < 0)
                        throw new CodeEE(GameMessages.T("CMATCH function: range exceeds the character array bounds (") + start.ToString() + GameMessages.T(" to ") + end.ToString() + ")");
                    if (arguments[0].GetOperandType() == typeof(Int64))
                    {
                        Int64 targetValue = arguments[1].GetIntValue(exm);
                        return (exm.VEvaluator.GetMatchChara(p, targetValue, start, end));
                    }
                    else
                    {
                        string targetStr = arguments[1].GetStrValue(exm);
                        return (exm.VEvaluator.GetMatchChara(p, targetStr, start, end));
                    }
                }
            }

            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                arguments[0].Restructure(exm);
                for (int i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        continue;
                    arguments[i] = arguments[i].Restructure(exm);
                }
                return false;
            }
        }

        private sealed class GroupMatchMethod : FunctionMethod
        {
            public GroupMatchMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                Type baseType = arguments[0].GetOperandType();
                for (int i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" cannot be omitted");
                    if (arguments[i].GetOperandType() != baseType)
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" has an invalid type");
                }
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 ret = 0;
                if (arguments[0].GetOperandType() == typeof(Int64))
                {
                    Int64 baseValue = arguments[0].GetIntValue(exm);
                    for (int i = 1; i < arguments.Length; i++)
                    {
                        if (baseValue == arguments[i].GetIntValue(exm))
                            ret += 1;
                    }
                }
                else
                {
                    string baseString = arguments[0].GetStrValue(exm);
                    for (int i = 1; i < arguments.Length; i++)
                    {
                        if (baseString == arguments[i].GetStrValue(exm))
                            ret += 1;
                    }
                }
                return (ret);
            }
        }

        private sealed class NosamesMethod : FunctionMethod
        {
            public NosamesMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                Type baseType = arguments[0].GetOperandType();
                for (int i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" cannot be omitted");
                    if (arguments[i].GetOperandType() != baseType)
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" has an invalid type");
                }
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (arguments[0].GetOperandType() == typeof(Int64))
                {
                    Int64 baseValue = arguments[0].GetIntValue(exm);
                    for (int i = 1; i < arguments.Length; i++)
                    {
                        if (baseValue == arguments[i].GetIntValue(exm))
                            return 0L;
                    }
                }
                else
                {
                    string baseValue = arguments[0].GetStrValue(exm);
                    for (int i = 1; i < arguments.Length; i++)
                    {
                        if (baseValue == arguments[i].GetStrValue(exm))
                            return 0L;
                    }
                }
                return 1L;
            }
        }

        private sealed class AllsamesMethod : FunctionMethod
        {
            public AllsamesMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                Type baseType = arguments[0].GetOperandType();
                for (int i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" cannot be omitted");
                    if (arguments[i].GetOperandType() != baseType)
                        return name + GameMessages.T(" function: argument #") + (i + 1).ToString() + GameMessages.T(" has an invalid type");
                }
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                if (arguments[0].GetOperandType() == typeof(Int64))
                {
                    Int64 baseValue = arguments[0].GetIntValue(exm);
                    for (int i = 1; i < arguments.Length; i++)
                    {
                        if (baseValue != arguments[i].GetIntValue(exm))
                            return 0L;
                    }
                }
                else
                {
                    string baseValue = arguments[0].GetStrValue(exm);
                    for (int i = 1; i < arguments.Length; i++)
                    {
                        if (baseValue != arguments[i].GetStrValue(exm))
                            return 0L;
                    }
                }
                return 1L;
            }
        }

        private sealed class MaxArrayMethod : FunctionMethod
        {
            readonly bool isCharaRange;
            readonly bool isMax;
            readonly string funcName;
            public MaxArrayMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = false;
                isMax = true;
                funcName = "MAXARRAY";
                CanRestructure = false;
            }
            public MaxArrayMethod(bool isChara)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = isChara;
                isMax = true;
                if (isCharaRange)
                    funcName = "MAXCARRAY";
                else
                    funcName = "MAXARRAY";
                CanRestructure = false;
            }
            public MaxArrayMethod(bool isChara, bool isMaxFunc)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = isChara;
                isMax = isMaxFunc;
                funcName = (isMax ? "MAX" : "MIN") + (isCharaRange ? "C" : "") + "ARRAY";
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 3)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm))
                    return name + GameMessages.T(" function: argument #1 is not a variable");
                VariableTerm varToken = (VariableTerm)arguments[0];
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + GameMessages.T(" function: argument #1 is not a character variable");
                if (!varToken.IsInteger)
                    return name + GameMessages.T(" function: argument #1 is not a numeric variable");
                if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
                    return name + GameMessages.T(" function does not support double or triple arrays");
                if (!varToken.Identifier.IsArray1D)
                    return name + GameMessages.T(" function: argument #1 is not an array variable");
                if ((arguments.Length >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                if ((arguments.Length >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 has an invalid type");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                VariableTerm vTerm = (VariableTerm)arguments[0];
                Int64 start = (arguments.Length > 1 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
                Int64 end = (arguments.Length > 2 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : vTerm.GetLength());
                FixedVariableTerm p = vTerm.GetFixedVariableTerm(exm);
                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(start, end, funcName, 2L, 3L);
                    return (exm.VEvaluator.GetMaxArray(p, start, end, isMax));
                }
                else
                {
                    Int64 charaNum = exm.VEvaluator.CHARANUM;
                    if (start >= charaNum || start < 0 || end > charaNum || end < 0)
                        throw new CodeEE(funcName + GameMessages.T(" function: range exceeds the character array bounds (") + start.ToString() + GameMessages.T(" to ") + end.ToString() + ")");
                    return (exm.VEvaluator.GetMaxArrayChara(p, start, end, isMax));
                }
            }
        }

        private sealed class GetbitMethod : FunctionMethod
        {
            public GetbitMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
                CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                string ret = base.CheckArgumentType(name, arguments);
                if (ret != null)
                    return ret;
                if (arguments[1] is SingleTerm)
                {
                    Int64 m = ((SingleTerm)arguments[1]).Int;
                    if (m < 0 || m > 63)
                        return GameMessages.T("GETBIT function: argument #2 (") + m.ToString() + GameMessages.T(") exceeds the range (0-63)");
                }
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 n = arguments[0].GetIntValue(exm);
                Int64 m = arguments[1].GetIntValue(exm);
                if ((m < 0) || (m > 63))
                    throw new CodeEE(GameMessages.T("GETBIT function: argument #2 (") + m.ToString() + GameMessages.T(") exceeds the range (0-63)"));
                int mi = (int)m;
                return ((n >> mi) & 1);
            }
        }

        private sealed class GetnumMethod : FunctionMethod
        {
            public GetnumMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = true;
                HasUniqueRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length != 2)
                    return name + GameMessages.T(" function requires 2 arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != typeof(string))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                VariableTerm vToken = (VariableTerm)arguments[0];
                VariableCode varCode = vToken.Identifier.Code;
                string key = arguments[1].GetStrValue(exm);
                if (exm.VEvaluator.Constant.TryKeywordToInteger(out int ret, varCode, key, -1))
                    return ret;
                else
                    return -1;
            }
            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                arguments[1] = arguments[1].Restructure(exm);
                return arguments[1] is SingleTerm;
            }
        }

		private sealed class GetnumBMethod : FunctionMethod
		{
			public GetnumBMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(string) };
				CanRestructure = true;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				string errStr = base.CheckArgumentType(name, arguments);
				if (errStr != null)
					return errStr;
				if (arguments[0] == null)
					return name + GameMessages.T(" function: argument #1 cannot be omitted");
				if (arguments[0] is SingleTerm)
				{
					string varName = ((SingleTerm)arguments[0]).Str;
					if (GlobalStatic.IdentifierDictionary.GetVariableToken(varName, null, true) == null)
						return name + GameMessages.T(" function: argument #1 is not a variable name");
				}
				return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				VariableToken var = GlobalStatic.IdentifierDictionary.GetVariableToken(arguments[0].GetStrValue(exm), null, true);
				if (var == null)
					throw new CodeEE(GameMessages.T("GETNUMB function: argument #1 (\"") + arguments[0].GetStrValue(exm) + GameMessages.T("\") is not a variable name"));
				string key = arguments[1].GetStrValue(exm);
                if (exm.VEvaluator.Constant.TryKeywordToInteger(out int ret, var.Code, key, -1))
                    return ret;
                else
                    return -1;
            }
		}

        private sealed class GetPalamLVMethod : FunctionMethod
        {
            public GetPalamLVMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                string errStr = base.CheckArgumentType(name, arguments);
                if (errStr != null)
                    return errStr;
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 value = arguments[0].GetIntValue(exm);
                Int64 maxLv = arguments[1].GetIntValue(exm);

                return (exm.VEvaluator.getPalamLv(value, maxLv));
            }
        }

        private sealed class GetExpLVMethod : FunctionMethod
        {
            public GetExpLVMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
                CanRestructure = false;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                string errStr = base.CheckArgumentType(name, arguments);
                if (errStr != null)
                    return errStr;
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 value = arguments[0].GetIntValue(exm);
                Int64 maxLv = arguments[1].GetIntValue(exm);

                return (exm.VEvaluator.getExpLv(value, maxLv));
            }
        }

        private sealed class FindElementMethod : FunctionMethod
        {
            public FindElementMethod(bool last)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = true; //Should be possible if all are constant terms
                HasUniqueRestructure = true;
                isLast = last;
                funcName = isLast ? "FINDLASTELEMENT" : "FINDELEMENT";
            }

            readonly bool isLast;
            readonly string funcName;
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments.Length > 5)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm varToken))
                    return name + GameMessages.T(" function: argument #1 is not a variable");
                if (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D)
                    return name + GameMessages.T(" function does not support double or triple arrays");
                if (!varToken.Identifier.IsArray1D)
                    return name + GameMessages.T(" function: argument #1 is not an array variable");
                Type baseType = arguments[0].GetOperandType();
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != baseType)
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                if ((arguments.Length >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 has an invalid type");
                if ((arguments.Length >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #4 has an invalid type");
                if ((arguments.Length >= 5) && (arguments[4] != null) && (arguments[4].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #5 has an invalid type");
                return null;
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                bool isExact = false;
                VariableTerm varTerm = (VariableTerm)arguments[0];

                Int64 start = (arguments.Length > 2 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
                Int64 end = (arguments.Length > 3 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : varTerm.GetLength();
                if (arguments.Length > 4 && arguments[4] != null)
                    isExact = (arguments[4].GetIntValue(exm) != 0);

                FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);
                p.IsArrayRangeValid(start, end, funcName, 3L, 4L);

                if (arguments[0].GetOperandType() == typeof(Int64))
                {
                    Int64 targetValue = arguments[1].GetIntValue(exm);
                    return exm.VEvaluator.FindElement(p, targetValue, start, end, isExact, isLast);
                }
                else
                {
                    Regex targetString;
                    try
                    {
                        targetString = new Regex(arguments[1].GetStrValue(exm));
                    }
                    catch (ArgumentException)
                    {
                        throw new CodeEE(GameMessages.T("argument #2 is an invalid regular expression"));
                    }
                    return exm.VEvaluator.FindElement(p, targetString, start, end, isExact, isLast);
                }
            }
            
            
            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                arguments[0].Restructure(exm);
                VariableTerm varToken = arguments[0] as VariableTerm;
                bool isConst = varToken.Identifier.IsConst;
                for (int i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        continue;
                    arguments[i] = arguments[i].Restructure(exm);
                    if (isConst && !(arguments[i] is SingleTerm))
                        isConst = false;
                }
                return isConst;
            }
        }

        private sealed class InRangeMethod : FunctionMethod
        {
            public InRangeMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 value = arguments[0].GetIntValue(exm);
                Int64 min = arguments[1].GetIntValue(exm);
                Int64 max = arguments[2].GetIntValue(exm);
                return ((value >= min) && (value <= max)) ? 1L : 0L;
            }
        }

        private sealed class InRangeArrayMethod : FunctionMethod
        {
            public InRangeArrayMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = false;
            }
            public InRangeArrayMethod(bool isChara)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                isCharaRange = isChara;
                CanRestructure = false;
            }
            private readonly bool isCharaRange = false;
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments.Length > 6)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm))
                    return name + GameMessages.T(" function: argument #1 is not a variable");
                VariableTerm varToken = (VariableTerm)arguments[0];
                if (isCharaRange && !varToken.Identifier.IsCharacterData)
                    return name + GameMessages.T(" function: argument #1 is not a character variable");
                if (!isCharaRange && (varToken.Identifier.IsArray2D || varToken.Identifier.IsArray3D))
                    return name + GameMessages.T(" function does not support double or triple arrays");
                if (!isCharaRange && !varToken.Identifier.IsArray1D)
                    return name + GameMessages.T(" function: argument #1 is not an array variable");
                if (!varToken.IsInteger)
                    return name + GameMessages.T(" function: argument #1 is not a numeric variable");
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != typeof(Int64))
                    return name + GameMessages.T(" function: argument #2 is not numeric");
                if (arguments[2] == null)
                    return name + GameMessages.T(" function: argument #3 cannot be omitted");
                if (arguments[2].GetOperandType() != typeof(Int64))
                    return name + GameMessages.T(" function: argument #3 is not numeric");
                if ((arguments.Length >= 4) && (arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #4 has an invalid type");
                if ((arguments.Length >= 5) && (arguments[4] != null) && (arguments[4].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #5 has an invalid type");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 min = arguments[1].GetIntValue(exm);
                Int64 max = arguments[2].GetIntValue(exm);

                VariableTerm varTerm = arguments[0] as VariableTerm;
                Int64 start = (arguments.Length > 3 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : 0;
                Int64 end = (arguments.Length > 4 && arguments[4] != null) ? arguments[4].GetIntValue(exm) : (isCharaRange ? exm.VEvaluator.CHARANUM : varTerm.GetLength());

                FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);

                if (!isCharaRange)
                {
                    p.IsArrayRangeValid(start, end, "INRANGEARRAY", 4L, 5L);
                    return (exm.VEvaluator.GetInRangeArray(p, min, max, start, end));
                }
                else
                {
                    Int64 charaNum = exm.VEvaluator.CHARANUM;
                    if (start >= charaNum || start < 0 || end > charaNum || end < 0)
                        throw new CodeEE(GameMessages.T("INRANGECARRAY function: range exceeds the character array bounds (") + start.ToString() + GameMessages.T(" to ") + end.ToString() + ")");
                    return (exm.VEvaluator.GetInRangeArrayChara(p, min, max, start, end));
                }
            }
        }

		private sealed class ArrayMultiSortMethod : FunctionMethod
		{
			public ArrayMultiSortMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
				HasUniqueRestructure = true;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 2)
					return string.Format(GameMessages.T("{0} function requires at least {1} arguments"), name, 2);
				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						return string.Format(GameMessages.T("{0} function: argument {1} cannot be omitted"), name, i + 1);
                    if (!(arguments[i] is VariableTerm varTerm) || varTerm.Identifier.IsCalc || varTerm.Identifier.IsConst)
                        return string.Format(GameMessages.T("{0} function: argument {1} is not a variable"), name, i + 1);
                    if (varTerm.Identifier.IsCharacterData)
						return string.Format(GameMessages.T("{0} function: argument {1} is a character variable"), name, i + 1);
					if (i == 0 && !varTerm.Identifier.IsArray1D)
						return string.Format(GameMessages.T("{0} function: argument {1} is not a 1D array"), name, i + 1);
					if (!varTerm.Identifier.IsArray1D && !varTerm.Identifier.IsArray2D && !varTerm.Identifier.IsArray2D)
						return string.Format(GameMessages.T("{0} function: argument {1} is not an array variable"), name, i + 1);
				}
				return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				VariableTerm varTerm = arguments[0] as VariableTerm;
				int[] sortedArray;
				if (varTerm.Identifier.IsInteger)
				{
					List<KeyValuePair<Int64, int>> sortList = new List<KeyValuePair<long, int>>();
					Int64[] array = (Int64[])varTerm.Identifier.GetArray();
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] == 0)
							break;
						if (array[i] < Int64.MinValue || array[i] > Int64.MaxValue)
							return 0;
						sortList.Add(new KeyValuePair<long, int>(array[i], i));
					}
                    //On its own it can only handle the int range, so a trick is needed
                    sortList.Sort((a, b) => { return Math.Sign(a.Key - b.Key); });
					sortedArray = new int[sortList.Count];
					for (int i = 0; i < sortedArray.Length; i++)
						sortedArray[i] = sortList[i].Value;
				}
				else
				{
					List<KeyValuePair<string, int>> sortList = new List<KeyValuePair<string, int>>();
					string[] array = (string[])varTerm.Identifier.GetArray();
					for (int i = 0; i < array.Length; i++)
					{
						if (string.IsNullOrEmpty(array[i]))
							return 0;
						sortList.Add(new KeyValuePair<string, int>(array[i], i));
					}
					sortList.Sort((a, b) => { return a.Key.CompareTo(b.Key); });
					sortedArray = new int[sortList.Count];
					for (int i = 0; i < sortedArray.Length; i++)
						sortedArray[i] = sortList[i].Value;
				}
				foreach (VariableTerm term in arguments)//Could there be a smarter way?
				{
					if (term.Identifier.IsArray1D)
					{
						if (term.IsInteger)
						{
							var array = (Int64[])term.Identifier.GetArray();
							var clone = (Int64[])array.Clone();
							if (array.Length < sortedArray.Length)
								return 0;
							for (int i = 0; i < sortedArray.Length; i++)
								array[i] = clone[sortedArray[i]];
						}
						else
						{
							var array = (string[])term.Identifier.GetArray();
							var clone = (string[])array.Clone();
							if (array.Length < sortedArray.Length)
								return 0;
							for (int i = 0; i < sortedArray.Length; i++)
								array[i] = clone[sortedArray[i]];
						}
					}
					else if (term.Identifier.IsArray2D)
					{
						if (term.IsInteger)
						{
							var array = (Int64[,])term.Identifier.GetArray();
							var clone = (Int64[,])array.Clone();
							if (array.GetLength(0) < sortedArray.Length)
								return 0;
							for (int i = 0; i < sortedArray.Length; i++)
								for (int x = 0; x < array.GetLength(1); x++)
									array[i, x] = clone[sortedArray[i], x];
						}
						else
						{
							var array = (string[,])term.Identifier.GetArray();
							var clone = (string[,])array.Clone();
							if (array.GetLength(0) < sortedArray.Length)
								return 0;
							for (int i = 0; i < sortedArray.Length; i++)
								for (int x = 0; x < array.GetLength(1); x++)
									array[i, x] = clone[sortedArray[i], x];
						}
					}
					else if (term.Identifier.IsArray3D)
					{
						if (term.IsInteger)
						{
							var array = (Int64[, ,])term.Identifier.GetArray();
							var clone = (Int64[, ,])array.Clone();
							if (array.GetLength(0) < sortedArray.Length)
								return 0;
							for (int i = 0; i < sortedArray.Length; i++)
								for (int x = 0; x < array.GetLength(1); x++)
									for (int y = 0; y < array.GetLength(2); y++)
										array[i, x, y] = clone[sortedArray[i], x, y];
						}
						else
						{
							var array = (string[, ,])term.Identifier.GetArray();
							var clone = (string[, ,])array.Clone();
							if (array.GetLength(0) < sortedArray.Length)
								return 0;
							for (int i = 0; i < sortedArray.Length; i++)
								for (int x = 0; x < array.GetLength(1); x++)
									for (int y = 0; y < array.GetLength(2); y++)
										array[i, x, y] = clone[sortedArray[i], x, y];
						}
					}
					else { throw new ExeEE(GameMessages.T("unexpected array")); }
				}
				return 1;
			}
			public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				for (int i = 0; i < arguments.Length; i++)
					arguments[i] = arguments[i].Restructure(exm);
				return false;
			}
		}
        #endregion

        #region String operations
        private sealed class StrlenMethod : FunctionMethod
        {
            public StrlenMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string str = arguments[0].GetStrValue(exm);
                return (LangManager.GetStrlenLang(str));
            }
        }

        private sealed class StrlenuMethod : FunctionMethod
        {
            public StrlenuMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string str = arguments[0].GetStrValue(exm);
                return (str.Length);
            }
        }

        private sealed class SubstringMethod : FunctionMethod
        {
            public SubstringMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 3, with 2 optional, so 1-3 arguments are required.
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 3)
                    return name + GameMessages.T(" function: too many arguments");

                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                //2 and 3 are optional
                if ((arguments.Length >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                if ((arguments.Length >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 has an invalid type");
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string str = arguments[0].GetStrValue(exm);
                int start = 0;
                int length = -1;
                if ((arguments.Length >= 2) && (arguments[1] != null))
                    start = (int)arguments[1].GetIntValue(exm);
                if ((arguments.Length >= 3) && (arguments[2] != null))
                    length = (int)arguments[2].GetIntValue(exm);

                return (LangManager.GetSubStringLang(str, start, length));
            }
        }

        private sealed class SubstringuMethod : FunctionMethod
        {
            public SubstringuMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 3, with 2 optional, so 1-3 arguments are required.
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 3)
                    return name + GameMessages.T(" function: too many arguments");

                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                //2 and 3 are optional
                if ((arguments.Length >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                if ((arguments.Length >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 has an invalid type");
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string str = arguments[0].GetStrValue(exm);
                int start = 0;
                int length = -1;
                if ((arguments.Length >= 2) && (arguments[1] != null))
                    start = (int)arguments[1].GetIntValue(exm);
                if ((arguments.Length >= 3) && (arguments[2] != null))
                    length = (int)arguments[2].GetIntValue(exm);
                if ((start >= str.Length) || (length == 0))
                    return ("");
                if ((length < 0) || (length > str.Length))
                    length = str.Length;
                if (start <= 0)
                {
                    if (length == str.Length)
                        return (str);
                    else
                        start = 0;
                }
                if ((start + length) > str.Length)
                    length = str.Length - start;

                return (str.Substring(start, length));
            }
        }

        private sealed class StrfindMethod : FunctionMethod
        {
            public StrfindMethod(bool unicode)
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = null;
                CanRestructure = true;
				this.unicode = unicode;
            }

            readonly bool unicode = false;
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 3, with 1 optional, so 2-3 arguments are required.
                if (arguments.Length < 2)
                    return name + GameMessages.T(" function requires at least 2 arguments");
                if (arguments.Length > 3)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                if (arguments[1] == null)
                    return name + GameMessages.T(" function: argument #2 cannot be omitted");
                if (arguments[1].GetOperandType() != typeof(string))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                //The 3rd is optional
                if ((arguments.Length >= 3) && (arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 has an invalid type");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {

                string target = arguments[0].GetStrValue(exm);
                string word = arguments[1].GetStrValue(exm);
                int UFTstart = 0;
				if ((arguments.Length >= 3) && (arguments[2] != null))
				{
					if (unicode)
					{
						UFTstart = (int)arguments[2].GetIntValue(exm);
					}
					else
					{
						UFTstart = LangManager.GetUFTIndex(target, (int)arguments[2].GetIntValue(exm));
					}
				}
                if (UFTstart < 0 || UFTstart >= target.Length)
                    return (-1);
                int index = target.IndexOf(word, UFTstart);
				if (index > 0 && !unicode)
                {
                    string subStr = target.Substring(0, index);
                    index = LangManager.GetStrlenLang(subStr);
                }
                return (index);
            }
        }

        private sealed class StrCountMethod : FunctionMethod
        {
            public StrCountMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string), typeof(string) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Regex reg;
                try
                {
                    reg = new Regex(arguments[1].GetStrValue(exm));
                }
                catch (ArgumentException e)
                {
                    throw new CodeEE(GameMessages.T("argument #2 is an invalid regular expression: ") + e.Message);
                }
                return (reg.Matches(arguments[0].GetStrValue(exm)).Count);
            }
        }

        private sealed class ToStrMethod : FunctionMethod
        {
            public ToStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                CanRestructure = true;
            }

            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 2, with 1 optional, so 1-2 arguments are required.
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (arguments[0].GetOperandType() != typeof(Int64))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                if ((arguments.Length >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(string)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 i = arguments[0].GetIntValue(exm);
                if ((arguments.Length < 2) || (arguments[1] == null))
                    return (i.ToString());
                string format = arguments[1].GetStrValue(exm);
                string ret;
                try
                {
                    ret = i.ToString(format);
                }
                catch (FormatException)
                {
                    throw new CodeEE(GameMessages.T("TOSTR function: format specifier is invalid"));
                }
                return (ret);
            }
        }

        private sealed class ToIntMethod : FunctionMethod
        {
            public ToIntMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = true;
            }

            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string str = arguments[0].GetStrValue(exm);
                if (str == null || str == "")
                    return (0);
                //Return 0 unconditionally if full-width characters are present
                if (str.Length < LangManager.GetStrlenLang(str))
                    return (0);
                StringStream st = new StringStream(str);
                if (!char.IsDigit(st.Current) && st.Current != '+' && st.Current != '-')
                    return (0);
                else if ((st.Current == '+' || st.Current == '-') && !char.IsDigit(st.Next))
                    return (0);
                Int64 ret = LexicalAnalyzer.ReadInt64(st, true);
                if (!st.EOS)
                {
                    if (st.Current == '.')
                    {
                        st.ShiftNext();
                        while (!st.EOS)
                        {
                            if (!char.IsDigit(st.Current))
                                return (0);
                            st.ShiftNext();
                        }
                    }
                    else
                        return (0);
                }
                return ret;
            }
        }

        //Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
        [global::System.Reflection.Obfuscation(Exclude = false)]
        //Enum to generalize processing such as TOUPPER
        enum StrFormType
        {
            Upper = 0,
            Lower = 1,
            Half = 2,
            Full = 3,
        };

        private sealed class StrChangeStyleMethod : FunctionMethod
        {
            readonly StrFormType strType;
            public StrChangeStyleMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string) };
                strType = StrFormType.Upper;
                CanRestructure = true;
            }
            public StrChangeStyleMethod(StrFormType type)
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string) };
                strType = type;
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string str = arguments[0].GetStrValue(exm);
                if (str == null || str == "")
                    return ("");
                switch (strType)
                {
                    case StrFormType.Upper:
                        return (str.ToUpper());
                    case StrFormType.Lower:
                        return (str.ToLower());
                    case StrFormType.Half:
                        return (Strings.StrConv(str, VbStrConv.Narrow, Config.Language));
                    case StrFormType.Full:
                        return (Strings.StrConv(str, VbStrConv.Wide, Config.Language));
                }
                return ("");
            }
        }

        private sealed class LineIsEmptyMethod : FunctionMethod
        {
            public LineIsEmptyMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { };
                CanRestructure = false;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return GlobalStatic.Console.EmptyLine ? 1L : 0L;
            }
        }

        private sealed class ReplaceMethod : FunctionMethod
        {
            public ReplaceMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) };
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string baseString = arguments[0].GetStrValue(exm);
                Regex reg;
                try
                {
                    reg = new Regex(arguments[1].GetStrValue(exm));
                }
                catch (ArgumentException e)
                {
                    throw new CodeEE(GameMessages.T("argument #2 is an invalid regular expression: ") + e.Message);
                }
                return (reg.Replace(baseString, arguments[2].GetStrValue(exm)));
            }
        }

        private sealed class UnicodeMethod : FunctionMethod
        {
            public UnicodeMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(Int64) };
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 i = arguments[0].GetIntValue(exm);
                if ((i < 0) || (i > 0xFFFF))
                    throw new CodeEE(GameMessages.T("UNICODE function: out-of-range value (") + i.ToString() + GameMessages.T(") was passed"));
                //Control characters other than line breaks are now treated as warnings
                //That said, intentionally passing control characters other than line breaks is a coding problem in itself, so Error would be fine too
                if ((i < 0x001F && i != 0x000A && i != 0x000D) || (i >= 0x007F && i <= 0x009F))
                {
                    //When code is running
                    if(GlobalStatic.Process.getCurrentLine != null)
                        GlobalStatic.Console.PrintSystemLine(GameMessages.T("Note: ") + GlobalStatic.Process.getCurrentLine.Position.Filename + GameMessages.T(" line ") + GlobalStatic.Process.getCurrentLine.Position.LineNo.ToString() + GameMessages.T(": control character value (0x") + String.Format("{0:X}", i) + GameMessages.T(") was passed"));
                    else
                        ParserMediator.Warn(GameMessages.T("UNICODE function: control character value (0x") + String.Format("{0:X}", i) + GameMessages.T(") was passed"), GlobalStatic.Process.GetScaningLine(), 1, false, false, null);

                    return "";
                }
                string s = new string(new char[] { (char)i });

                return (s);
            }
        }

        private sealed class UnicodeByteMethod : FunctionMethod
        {
            public UnicodeByteMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = true;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string target = arguments[0].GetStrValue(exm);
                int length = Encoding.UTF32.GetEncoder().GetByteCount(target.ToCharArray(), 0, target.Length, false);
                byte[] bytes = new byte[length];
                Encoding.UTF32.GetEncoder().GetBytes(target.ToCharArray(), 0, target.Length, bytes, 0, false);
                Int64 i = (Int64)BitConverter.ToInt32(bytes, 0);

                return (i);
            }
        }

        private sealed class ConvertIntMethod : FunctionMethod
        {
            public ConvertIntMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                Int64 toBase = arguments[1].GetIntValue(exm);
                if ((toBase != 2) && (toBase != 8) && (toBase != 10) && (toBase != 16))
                    throw new CodeEE(GameMessages.T("CONVERT function: argument #2 must be 2, 8, 10, or 16"));
                return Convert.ToString(arguments[0].GetIntValue(exm), (int)toBase);
            }
        }

        private sealed class IsNumericMethod : FunctionMethod
        {
            public IsNumericMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = true;
            }
            public override long GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string baseStr = arguments[0].GetStrValue(exm);

                //If it contains full-width characters, it is not numeric
                if (baseStr.Length < LangManager.GetStrlenLang(baseStr))
                    return (0);
                StringStream st = new StringStream(baseStr);
                if (!char.IsDigit(st.Current) && st.Current != '+' && st.Current != '-')
                    return (0);
                else if ((st.Current == '+' || st.Current == '-') && !char.IsDigit(st.Next))
                    return (0);
                _ = LexicalAnalyzer.ReadInt64(st, true);
                if (!st.EOS)
                {
                    if (st.Current == '.')
                    {
                        st.ShiftNext();
                        while (!st.EOS)
                        {
                            if (!char.IsDigit(st.Current))
                                return (0);
                            st.ShiftNext();
                        }
                    }
                    else
                        return (0);
                }
                return 1;
            }
        }

        private sealed class EscapeMethod : FunctionMethod
        {
            public EscapeMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                return Regex.Escape(arguments[0].GetStrValue(exm));
            }
        }

        private sealed class EncodeToUniMethod : FunctionMethod
        {
            public EncodeToUniMethod()
            {
                ReturnType = typeof(Int64);
                argumentTypeArray = new Type[] { null };
                CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                //Usually 2, with 1 optional, so 1-2 arguments are required.
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 2)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (arguments[0].GetOperandType() != typeof(string))
                    return name + GameMessages.T(" function: argument #1 has an invalid type");
                if ((arguments.Length >= 2) && (arguments[1] != null) && (arguments[1].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #2 has an invalid type");
                return null;
            }
            public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string baseStr = arguments[0].GetStrValue(exm);
                if (baseStr.Length == 0)
                    return -1;
                Int64 position = (arguments.Length > 1 && arguments[1] != null) ? arguments[1].GetIntValue(exm) : 0;
                if (position < 0)
                    throw new CodeEE(GameMessages.T("ENCOIDETOUNI function: argument #2 (") + position.ToString() + GameMessages.T(") is negative"));
                if (position >= baseStr.Length)
                    throw new CodeEE(GameMessages.T("ENCOIDETOUNI function: argument #2 (") + position.ToString() + GameMessages.T(") exceeds the length of argument #1 string (") + baseStr + GameMessages.T(")"));
                return char.ConvertToUtf32(baseStr, (int)position);
            }
        }

        public sealed class CharAtMethod : FunctionMethod
        {
            public CharAtMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string), typeof(Int64) };
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                string str = arguments[0].GetStrValue(exm);
                Int64 pos = arguments[1].GetIntValue(exm);
                if (pos < 0 || pos >= str.Length)
                    return "";
                return str[(int)pos].ToString();
            }
        }

        public sealed class GetLineStrMethod : FunctionMethod
        {
            public GetLineStrMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string) };
                CanRestructure = true;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
                string str = arguments[0].GetStrValue(exm);
				if (string.IsNullOrEmpty(str))
					throw new CodeEE(GameMessages.T("GETLINESTR function: argument is an empty string"));
                return exm.Console.getStBar(str);
            }
        }

		public sealed class StrFormMethod : FunctionMethod
		{
			public StrFormMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = new Type[] { typeof(string) };
                HasUniqueRestructure = true;
				CanRestructure = true;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string str = arguments[0].GetStrValue(exm);
                string destStr;
                try
				{
					StrFormWord wt = LexicalAnalyzer.AnalyseFormattedString(new StringStream(str), FormStrEndWith.EoL, false);
					StrForm strForm = StrForm.FromWordToken(wt);
					destStr = strForm.GetString(exm);
				}
				catch(CodeEE e)
				{
					throw new CodeEE(GameMessages.T("STRFORM function: string \"") + str + GameMessages.T("\" expansion error: ") + e.Message);
				}
				catch
				{
					throw new CodeEE(GameMessages.T("STRFORM function: string \"") + str+ GameMessages.T("\" an error occurred during expansion"));
				}
				return destStr;
			}
            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                arguments[0].Restructure(exm);
                //If the argument is a string expression or the like, we give up
                if (!(arguments[0] is SingleTerm) && !(arguments[0] is VariableTerm))
                    return false;
                //If the argument is a string variable without a definite value, it's unconditionally disallowed (since the result varies)
                if ((arguments[0] is VariableTerm) && !(((VariableTerm)arguments[0]).Identifier.IsConst))
                    return false;
                string str = arguments[0].GetStrValue(exm);
                try
                {
                    StrFormWord wt = LexicalAnalyzer.AnalyseFormattedString(new StringStream(str), FormStrEndWith.EoL, false);
                    StrForm strForm = StrForm.FromWordToken(wt);
                    if (!strForm.IsConst)
                        return false;
                }
                catch
                {
                    //If it can't be parsed, we can't tell whether there's an error here, so ignore it for now
                    return false;
                }
                return true;
            }
        }

        public sealed class JoinMethod : FunctionMethod
        {
            public JoinMethod()
            {
                ReturnType = typeof(string);
                argumentTypeArray = null;
                HasUniqueRestructure = true;
                CanRestructure = true;
            }
            public override string CheckArgumentType(string name, IOperandTerm[] arguments)
            {
                if (arguments.Length < 1)
                    return name + GameMessages.T(" function requires at least 1 argument");
                if (arguments.Length > 4)
                    return name + GameMessages.T(" function: too many arguments");
                if (arguments[0] == null)
                    return name + GameMessages.T(" function: argument #1 cannot be omitted");
                if (!(arguments[0] is VariableTerm))
                    return name + GameMessages.T(" function: argument #1 is not a variable");
                VariableTerm varToken = (VariableTerm)arguments[0];
                if (!varToken.Identifier.IsArray1D && !varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D)
                    return name + GameMessages.T(" function: argument #1 is not an array variable");
                if (arguments.Length == 1)
                    return null;
                if ((arguments[1] != null) && (arguments[1].GetOperandType() != typeof(string)))
                    return name + GameMessages.T(" function: argument #2 is not a string");
                if (arguments.Length == 2)
                    return null;
                if ((arguments[2] != null) && (arguments[2].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #3 is not a number");
                if (arguments.Length == 3)
                    return null;
                if ((arguments[3] != null) && (arguments[3].GetOperandType() != typeof(Int64)))
                    return name + GameMessages.T(" function: argument #4 is not a number");
                return null;
            }
            public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
            {
                VariableTerm varTerm = (VariableTerm)arguments[0];
                string delimiter = (arguments.Length >= 2 && arguments[1] != null) ? arguments[1].GetStrValue(exm) : ",";
                Int64 index1 = (arguments.Length >= 3 && arguments[2] != null) ? arguments[2].GetIntValue(exm) : 0;
                Int64 index2 = (arguments.Length == 4 && arguments[3] != null) ? arguments[3].GetIntValue(exm) : varTerm.GetLastLength() - index1;

                FixedVariableTerm p = varTerm.GetFixedVariableTerm(exm);

                if (index2 < 0)
                    throw new CodeEE(GameMessages.T("STRJOIN function: argument #4 (") + index2.ToString()+ GameMessages.T(") is negative"));

                p.IsArrayRangeValid(index1, index1 + index2, "STRJOIN", 2L, 3L);
                return (exm.VEvaluator.GetJoinedStr(p, delimiter, index1, index2));
            }
            public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
            {                
                //The 1st variable is a variable name, so a constant string variable would cause problems; handle specially
                VariableTerm varTerm = (VariableTerm)arguments[0];
                bool canRerstructure = varTerm.Identifier.IsConst;
                for (int i = 1; i < arguments.Length; i++)
                {
                    if (arguments[i] == null)
                        continue;
                    arguments[i] = arguments[i].Restructure(exm);
                    canRerstructure &= arguments[i] is SingleTerm;
                }
                return canRerstructure;
            }
        }
		
		public sealed class GetConfigMethod : FunctionMethod
		{
			public GetConfigMethod(bool typeisInt)
			{
				if(typeisInt)
				{
					funcname = "GETCONFIG";
					ReturnType = typeof(Int64);
				}
				else
				{
					funcname = "GETCONFIGS";
					ReturnType = typeof(string);
				}
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = true;
			}
			private readonly string funcname;
			private SingleTerm GetSingleTerm(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string str = arguments[0].GetStrValue(exm);
				if(str == null || str.Length == 0)
					throw new CodeEE(funcname + GameMessages.T(" function: an empty string was passed"));
				string errMes = null;
				SingleTerm term = ConfigData.Instance.GetConfigValueInERB(str, ref errMes);
				if(errMes != null)
					throw new CodeEE(funcname + GameMessages.T(" function: ") + errMes);
				return term;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if(ReturnType != typeof(Int64))
					throw new ExeEE(funcname + GameMessages.T(" function: invalid call"));
				SingleTerm term = GetSingleTerm(exm, arguments);
				if(term.GetOperandType() != typeof(Int64))
					throw new CodeEE(funcname + GameMessages.T(" function: type mismatch (use the GETCONFIGS function)"));
				return term.Int;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if(ReturnType != typeof(string))
					throw new ExeEE(funcname + GameMessages.T(" function: invalid call"));
				SingleTerm term = GetSingleTerm(exm, arguments);
				if (term.GetOperandType() != typeof(string))
					throw new CodeEE(funcname + GameMessages.T(" function: type mismatch (use the GETCONFIG function)"));
				return term.Str;
			}
		}
        #endregion

		#region HTML

		private sealed class HtmlGetPrintedStrMethod : FunctionMethod
		{
			public HtmlGetPrintedStrMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				//Usually 1; optional.
				if (arguments.Length > 1)
					return name + GameMessages.T(" function: too many arguments");
				if (arguments.Length == 0|| arguments[0] == null)
					return null;
				if (arguments[0].GetOperandType() != typeof(Int64))
					return name + GameMessages.T(" function: argument #1 has an invalid type");
				return null;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				Int64 lineNo = 0;
				if (arguments.Length > 0)
					lineNo = arguments[0].GetIntValue(exm);
				if (lineNo < 0)
					throw new CodeEE(GameMessages.T("argument cannot be less than 0"));
				ConsoleDisplayLine[] dispLines = exm.Console.GetDisplayLines(lineNo);
				if (dispLines == null)
					return "";
				return HtmlManager.DisplayLine2Html(dispLines, true);
			}
		}

		private sealed class HtmlPopPrintingStrMethod : FunctionMethod
		{
			public HtmlPopPrintingStrMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = new Type[] { };
				CanRestructure = false;
			}

			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				ConsoleDisplayLine[] dispLines = exm.Console.PopDisplayingLines();
				if (dispLines == null)
					return "";
				return HtmlManager.DisplayLine2Html(dispLines, false);
			}
		}

		private sealed class HtmlToPlainTextMethod : FunctionMethod
		{
			public HtmlToPlainTextMethod()
			{
				ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				return HtmlManager.Html2PlainText(arguments[0].GetStrValue(exm));
			}
		}
		private sealed class HtmlEscapeMethod : FunctionMethod
		{
			public HtmlEscapeMethod()
			{
				ReturnType = typeof(string);
                argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				return HtmlManager.Escape(arguments[0].GetStrValue(exm));
			}
		}

		/// <summary>
		/// HTML_STRINGLINES(str, width) - Returns number of display lines the HTML string occupies at the given half-width character width.
		/// </summary>
		private sealed class HtmlStringLinesMethod : FunctionMethod
		{
			public HtmlStringLinesMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string str = arguments[0].GetStrValue(exm);
				if (string.IsNullOrEmpty(str)) return 0;
				int width = (int)arguments[1].GetIntValue(exm);
				if (width <= 0) return 0;
				int lineCount = 0;
				string remaining = str;
				do
				{
					string[] parts = HtmlManager.HtmlSubString(remaining, width);
					if (parts == null || parts.Length < 2 || (string.IsNullOrEmpty(parts[0]) && string.IsNullOrEmpty(parts[1])))
						break;
					remaining = parts[1];
					lineCount++;
				} while (!string.IsNullOrEmpty(remaining));
				return Math.Max(1, lineCount);
			}
		}
		#endregion

		#region Image processing
		/// <summary>
		/// Reads the argNo-th argument as an integer value representing a GraphicsImage ID, and returns a GraphicsImage or null.
		/// </summary>
		private static GraphicsImage ReadGraphics(string Name, ExpressionMediator exm, IOperandTerm[] arguments, int argNo)
		{
			Int64 target = arguments[argNo].GetIntValue(exm);
			if (target < 0)//funcname + ": a negative GraphicsID (" + target.ToString() + ") was specified"
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGraphicsID0, Name, target));
			else if (target > int.MaxValue)//funcname + ": GraphicsID value (" + target.ToString() + ") is too large"
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGraphicsID1, Name, target));
            return AppContents.GetGraphics((int)target);
		}

		/// <summary>
		/// Reads the argNo-th argument as an integer value and returns it as a Color struct including the alpha value.
		/// </summary>
		private static Color ReadColor(string Name, ExpressionMediator exm, IOperandTerm[] arguments, int argNo)
		{
			Int64 c64 = arguments[argNo].GetIntValue(exm);
			if (c64 < 0 || c64 > 0xFFFFFFFF)
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodColorARGB0, Name, c64));
			return Color.FromArgb((int)(c64 >> 24) & 0xFF, (int)(c64 >> 16) & 0xFF, (int)(c64 >> 8) & 0xFF, (int)c64 & 0xFF);
		}

		/// <summary>
		/// Reads 2 arguments (including the argNo-th) as integer values and returns them in Point form.
		/// </summary>
		private static Point ReadPoint(string Name, ExpressionMediator exm, IOperandTerm[] arguments, int argNo)
		{
			Int64 x64 = arguments[argNo].GetIntValue(exm);
			if(x64<int.MinValue || x64>int.MaxValue)
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name,x64, argNo+1));
			Int64 y64 = arguments[argNo+1].GetIntValue(exm);
			if(y64<int.MinValue || y64>int.MaxValue)
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name,y64, argNo+1+1));
			return new Point((int)x64, (int)y64);
		}

		/// <summary>
		/// Reads 4 arguments (including the argNo-th) as integer values and returns them in Rectangle form.
		/// </summary>
		private static Rectangle ReadRectangle(string Name, ExpressionMediator exm, IOperandTerm[] arguments, int argNo)
		{
			Int64 x64 = arguments[argNo].GetIntValue(exm);
			if (x64 < int.MinValue || x64 > int.MaxValue)
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, x64, argNo + 1));
			Int64 y64 = arguments[argNo + 1].GetIntValue(exm);
			if (y64 < int.MinValue || y64 > int.MaxValue)
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, y64, argNo + 1 + 1));

			Int64 w64 = arguments[argNo + 2].GetIntValue(exm);
			if (w64 < int.MinValue || w64 > int.MaxValue || w64 == 0)
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, w64, argNo + 2 + 1));
			Int64 h64 = arguments[argNo + 3].GetIntValue(exm);
			if (h64 < int.MinValue || h64 > int.MaxValue || h64 == 0)
				throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, h64, argNo + 3 + 1));
			return new Rectangle((int)x64, (int)y64, (int)w64, (int)h64);
		}

		/// <summary>
		/// Reads the argNo-th argument as a 5x5 color-matrix array variable and returns it in 5x5 float[][] form.
		/// </summary>
		private static float[][] ReadColormatrix(string Name, ExpressionMediator exm, IOperandTerm[] arguments, int argNo)
		{
			//Should be a numeric array variable with 2 or more dimensions
			FixedVariableTerm p = ((VariableTerm)arguments[argNo]).GetFixedVariableTerm(exm);
			Int64 e1, e2;
			float[][] cm = new float[5][];
			if (p.Identifier.IsArray2D)
			{
				Int64[,] array;
				if (p.Identifier.IsCharacterData)
				{
					array = p.Identifier.GetArrayChara((int)p.Index1) as Int64[,];
					e1 = p.Index2;
					e2 = p.Index3;
				}
				else
				{
					array = p.Identifier.GetArray() as Int64[,];
					e1 = p.Index1;
					e2 = p.Index2;
				}
				if (e1 < 0 || e2 < 0 || e1 + 5 > array.GetLength(0) || e2 + 5 > array.GetLength(1))
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGColorMatrix0, Name, e1, e2));
				for (int x = 0; x < 5; x++)
				{
					cm[x] = new float[5];
					for (int y = 0; y < 5; y++)
					{
						cm[x][y] = ((float)array[e1+x, e2+y]) / 256f;
					}
				}
			}
			if(p.Identifier.IsArray3D)
			{
				Int64[, ,] array; Int64 e3;
				if (p.Identifier.IsCharacterData)
				{
					throw new NotImplCodeEE();
				}
				else
				{
					array = p.Identifier.GetArray() as Int64[,,];
					e1 = p.Index1;
					e2 = p.Index2;
					e3 = p.Index3;
				}
				if (e1 < 0 || e1 >= array.GetLength(0))
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGColorMatrix0, Name, e2, e3));
				if (e2 < 0 || e3 < 0 || e2 + 5 > array.GetLength(1) || e3 + 5 > array.GetLength(2))
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGColorMatrix0, Name, e2, e3));
				for (int x = 0; x < 5; x++)
				{
					cm[x] = new float[5];
					for (int y = 0; y < 5; y++)
					{
						cm[x][y] = ((float)array[e1,e2+x, e3+y]) / 256f;
					}
				}
			}
			return cm;
		}

		public sealed class GraphicsStateMethod : FunctionMethod
		{
			public GraphicsStateMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				switch (Name)
				{
					case "GCREATED":
						return 1;
					case "GWIDTH":
						return g.Width;
					case "GHEIGHT":
						return g.Height;
				}
				throw new ExeEE("GraphicsState:" + Name + GameMessages.T(": unexpected branch"));
			}
		}

		public sealed class GraphicsGetColorMethod : FunctionMethod
		{
			public GraphicsGetColorMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				//Return a negative value on failure. Different from the others, but nothing we can do.
				if (!g.IsCreated)
					return -1;
				Point p = ReadPoint(Name, exm, arguments, 1);
				if (p.X < 0 || p.X >= g.Width || p.X < 0 || p.Y >= g.Height)
					return -1;
				Color c = g.GGetColor(p.X,p.Y);
				//Color.ToArgb() can take negative Int32 values, which might not convert well to Int64? (I thought so, but it was my imagination
				return ((Int64)c.ToArgb()) & 0xFFFFFFFFL;
			}
		}

		public sealed class GraphicsSetColorMethod : FunctionMethod
		{
			public GraphicsSetColorMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				Color c = ReadColor(Name, exm, arguments, 1);
				Point p = ReadPoint(Name, exm, arguments, 2);
				if (p.X < 0 || p.X >= g.Width || p.X < 0 || p.Y >= g.Height)
					return 0;
				g.GSetColor(c, p.X, p.Y);
				return 1;
			}
		}
		
		public sealed class GraphicsSetBrushMethod : FunctionMethod
		{
			public GraphicsSetBrushMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				Color c = ReadColor(Name, exm, arguments, 1);
				g.GSetBrush(new SolidBrush(c));
				return 1;
			}
		}
		public sealed class GraphicsSetFontMethod : FunctionMethod
		{
			public GraphicsSetFontMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				string fontname = arguments[1].GetStrValue(exm);
				Int64 fontsize = arguments[2].GetIntValue(exm);

                Font styledFont;
                try
				{
					styledFont = new Font(fontname, fontsize, FontStyle.Regular, GraphicsUnit.Pixel);
				}
				catch
				{
					return 0;
				}
				g.GSetFont(styledFont);
				return 1;
			}
		}
		
		public sealed class GraphicsSetPenMethod : FunctionMethod
		{
			public GraphicsSetPenMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				Color c = ReadColor(Name, exm, arguments, 1);
				Int64 width = arguments[2].GetIntValue(exm);
				g.GSetPen(new Pen(c,width));
				return 1;
			}
		}

		public sealed class SpriteStateMethod : FunctionMethod
		{
			public SpriteStateMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string imgname = arguments[0].GetStrValue(exm);
				ASprite img = AppContents.GetSprite(imgname);
				if (img == null || !img.IsCreated)
					return 0;
				switch (Name)
				{
					case "SPRITECREATED":
					case "SPRITEEXIST":
						return 1;
					case "SPRITEWIDTH":
						return img.DestBaseSize.Width;
					case "SPRITEHEIGHT":
						return img.DestBaseSize.Height;
					case "SPRITEPOSX":
						return img.DestBasePosition.X;
					case "SPRITEPOSY":
						return img.DestBasePosition.Y;
				}
				throw new ExeEE("SpriteStateMethod:" + Name + GameMessages.T(": unexpected branch"));
			}
		}

		public sealed class SpriteSetPosMethod : FunctionMethod
		{
			public SpriteSetPosMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) , typeof(Int64),typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string imgname = arguments[0].GetStrValue(exm);
				ASprite img = AppContents.GetSprite(imgname);
				if (img == null || !img.IsCreated)
					return 0;
				Point p = ReadPoint(Name, exm, arguments, 1);
				switch (Name)
				{
					case "SPRITEMOVE":
						img.DestBasePosition.Offset(p);
						return 1;
					case "SPRITESETPOS":
						img.DestBasePosition = p;
						return 1;
				}
				throw new ExeEE("SpriteStateMethod:" + Name + GameMessages.T(": unexpected branch"));
			}
		}

		public sealed class SpriteGetColorMethod : FunctionMethod
		{
			public SpriteGetColorMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string imgname = arguments[0].GetStrValue(exm);
				ASprite img = AppContents.GetSprite(imgname);
				//Unlike the others, failure is a negative value, not 0
				if (img == null || !img.IsCreated)
					return -1;
				Point p = ReadPoint(Name, exm, arguments, 1);
				if (p.X < 0 || p.X >= img.DestBaseSize.Width)
					return -1;
				if (p.Y < 0 || p.Y >= img.DestBaseSize.Height)
					return -1;
				Color c = img.SpriteGetColor(p.X, p.Y);
				//Color.ToArgb() can take negative Int32 values, which might not convert well to Int64? (I thought so, but it was my imagination
				return c.ToArgb() & 0xFFFFFFFFL;
			}
		}

		public sealed class ClientSizeMethod : FunctionMethod
		{
			public ClientSizeMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] {};
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				switch (Name)
				{
					case "CLIENTWIDTH":
						return exm.Console.ClientWidth;
					case "CLIENTHEIGHT":
						return exm.Console.ClientHeight;
				}
				throw new ExeEE("ClientSize:" + Name + GameMessages.T(": unexpected branch"));
			}
		}

		public sealed class GraphicsCreateMethod : FunctionMethod
		{
			public GraphicsCreateMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (g.IsCreated)
					return 0;

				Point p = ReadPoint(Name, exm, arguments, 1);
				int width = p.X; int height = p.Y;
				if (width <= 0)//{0}: Graphics width of 0 or less ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth0, Name, width));
				else if (width > AbstractImage.MAX_IMAGESIZE)//{0}: Graphics width of {2} or more ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth1, Name, width, AbstractImage.MAX_IMAGESIZE));
				if (height <= 0)//{0}: Graphics height of 0 or less ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight0, Name, height));
				else if (height > AbstractImage.MAX_IMAGESIZE)//{0}: Graphics height of {2} or more ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight1, Name, height, AbstractImage.MAX_IMAGESIZE));

				g.GCreate(width, height, false);
				return 1;

			}
		}

		public sealed class GraphicsCreateFromFileMethod : FunctionMethod
		{
			public GraphicsCreateFromFileMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (g.IsCreated)
					return 0;

				string filename = arguments[1].GetStrValue(exm);
                BitmapTexture bmp = null;
				try
				{
					string filepath = filename;
					if(!System.IO.Path.IsPathRooted(filepath))
						filepath = Program.ContentDir + filename;
					if (!System.IO.File.Exists(filepath))
						return 0;
					bmp = new BitmapTexture(filepath);
					if (bmp.Width > AbstractImage.MAX_IMAGESIZE || bmp.Height > AbstractImage.MAX_IMAGESIZE)
						return 0;
					g.GCreateFromF(bmp, (Config.TextDrawingMode == TextDrawingMode.WINAPI));
				}
				catch (Exception e)
				{
					if (e is CodeEE)
						throw;
				}
				finally
				{
					if (bmp != null)
						bmp.Dispose();
				}
				//Failure caused by e.g. the file not being an image file
				if (!g.IsCreated)
					return 0;
				return 1;
			}
		}

		public sealed class GraphicsDisposeMethod : FunctionMethod
		{
			public GraphicsDisposeMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				g.GDispose();
				return 1;
			}
		}
		/// <summary>
		/// SPRITECREATE(str imgName, int gID, int x, int y, int width, int height)
		/// SPRITECREATE(str imgName, int gID)
		/// </summary>
		public sealed class SpriteCreateMethod : FunctionMethod
		{
			public SpriteCreateMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{

				if (arguments.Length < 2)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
				if (arguments.Length > 6)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				if (arguments[0] == null)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, 0 + 1);
				if (arguments[1] == null)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, 1 + 1);
				if (arguments[0].GetOperandType() != typeof(string))
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, 0 + 1);
				if (arguments[1].GetOperandType() != typeof(Int64))
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, 1 + 1);
				if (arguments.Length == 2)
					return null;
				if (arguments.Length != 6)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);
				for (int i = 2; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
					if (arguments[i].GetOperandType() != typeof(Int64))
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
				}
				return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				string imgname = arguments[0].GetStrValue(exm);
				if (string.IsNullOrEmpty(imgname))
					return 0;
				ASprite img = AppContents.GetSprite(imgname);
				if (img != null && img.IsCreated)
					return 0;
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 1);
				if (!g.IsCreated)
					return 0;

				Rectangle rect = new Rectangle(0, 0, g.Width, g.Height);
				if(arguments.Length == 6)
				{//The rectangle may be positive or negative, but must not point outside the parent image
					rect = ReadRectangle(Name, exm, arguments, 2);
					if (rect.X + rect.Width < 0 || rect.X + rect.Width > g.Width || rect.Y + rect.Height < 0 || rect.Y + rect.Height > g.Height)
						throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodCIMGCreateOutOfRange0, Name));
				}
				AppContents.CreateSpriteG(imgname, g, rect);
				return 1;
			}
		}

		public sealed class SpriteDisposeMethod : FunctionMethod
		{
			public SpriteDisposeMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string imgname = arguments[0].GetStrValue(exm);
				ASprite img = AppContents.GetSprite(imgname);
				if (img == null || !img.IsCreated)
					return 0;
				AppContents.SpriteDispose(imgname);
				return 1;
			}
		}

		public sealed class SpriteDisposeAllMethod : FunctionMethod
		{
			public SpriteDisposeAllMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				bool includeG = arguments[0].GetIntValue(exm) != 0;
				return AppContents.SpriteDisposeAll(includeG);
			}
		}


		/// <summary>
		/// GCLEAR(int ID, int cARGB)
		/// </summary>
		public sealed class GraphicsClearMethod : FunctionMethod
		{
			public GraphicsClearMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				Color c = ReadColor(Name, exm, arguments, 1);
				if (!g.IsCreated)
					return 0;
				g.GClear(c);
				return 1;
			}
		}

		/// <summary>
		/// GFILLRECTANGLE(int ID, int cARGB, int x, int y, int width, int height)
		/// </summary>
		public sealed class GraphicsFillRectangleMethod : FunctionMethod
		{
			public GraphicsFillRectangleMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				Rectangle rect = ReadRectangle(Name, exm, arguments, 1);
				g.GFillRectangle(rect);
				return 1;
			}
		}

		public sealed class GraphicsDrawLineMethod : FunctionMethod
		{
			public GraphicsDrawLineMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				int x1 = (int)arguments[1].GetIntValue(exm);
				int y1 = (int)arguments[2].GetIntValue(exm);
				int x2 = (int)arguments[3].GetIntValue(exm);
				int y2 = (int)arguments[4].GetIntValue(exm);
				g.GDrawLine(x1, y1, x2, y2);
				return 1;
			}
		}

		/// <summary>
		/// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight)
		/// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight, var CM)
		/// </summary>
		public sealed class GraphicsDrawGMethod : FunctionMethod
		{
			public GraphicsDrawGMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
				HasUniqueRestructure = true;
			}
			
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 10)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 10);
				if (arguments.Length > 11)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				for (int i = 0; i < 10; i++)
				{
					if (arguments[i] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
					if (typeof(Int64) != arguments[i].GetOperandType())
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
				}
				if (arguments.Length == 10)
					return null;
                if (!(arguments[10] is VariableTerm varToken) || !varToken.IsInteger || (!varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D))
                    return string.Format(Properties.Resources.SyntaxErrMesMethodGraphicsColorMatrix0, name);
                return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage dest = ReadGraphics(Name, exm, arguments, 0);
				if (!dest.IsCreated)
					return 0;
				GraphicsImage src = ReadGraphics(Name, exm, arguments, 1);
				if (!src.IsCreated)
					return 0;
				Rectangle destRect = ReadRectangle(Name, exm, arguments, 2);
				Rectangle srcRect = ReadRectangle(Name, exm, arguments, 6);
				if (arguments.Length == 10 || arguments[10] == null)
				{
					dest.GDrawG(src, destRect, srcRect);
					return 1;
				}
				float[][] cm = ReadColormatrix(Name, exm, arguments, 10);
				dest.GDrawG(src, destRect, srcRect, cm);
				return 1;
			}

			public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						continue;
					//The 11th argument points to a ColorMatrix array, so it must not be const-ified
					if (i == 10)
						arguments[i].Restructure(exm);
					else
						arguments[i] = arguments[i].Restructure(exm);
				}
				return false;
			}
		}
		
		/// <summary>
		/// GDRAWGWITHMASK(int ID, int srcID, int maskID, int destX, int destY)
		/// </summary>
		public sealed class GraphicsDrawGWithMaskMethod : FunctionMethod
		{
			public GraphicsDrawGWithMaskMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage dest = ReadGraphics(Name, exm, arguments, 0);
				if (!dest.IsCreated)
					return 0;
				GraphicsImage src = ReadGraphics(Name, exm, arguments, 1);
				if (!src.IsCreated)
					return 0;
				GraphicsImage mask = ReadGraphics(Name, exm, arguments, 2);
				if (!mask.IsCreated)
					return 0;
				if (src.Width != mask.Width || src.Height != mask.Height)
					return 0;
				Point destPoint = ReadPoint(Name, exm, arguments, 3);
				if (destPoint.X + src.Width > dest.Width || destPoint.Y + src.Height > dest.Height)
					return 0;
				dest.GDrawGWithMask(src, mask, destPoint);
				return 1;
			}


		}

		/// <summary>
		/// GDRAWCIMG(int ID, str imgName)
		/// GDRAWCIMG(int ID, str imgName, int destX, int destY)
		/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight)
		/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight, var CM)
		/// </summary>
		public sealed class GraphicsDrawSpriteMethod : FunctionMethod
		{
			public GraphicsDrawSpriteMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
				HasUniqueRestructure = true;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 2)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
				if (arguments.Length > 7)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				if (arguments.Length != 2 && arguments.Length != 4 && arguments.Length != 6 && arguments.Length != 7)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);

				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
					
					if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
				}
				if (arguments.Length <= 6)
					return null;
                if (!(arguments[6] is VariableTerm varToken) || !varToken.IsInteger || (!varToken.Identifier.IsArray2D && !varToken.Identifier.IsArray3D))
                    return string.Format(Properties.Resources.SyntaxErrMesMethodGraphicsColorMatrix0, name);
                return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage dest = ReadGraphics(Name, exm, arguments, 0);
				if (!dest.IsCreated)
					return 0;

				string imgname = arguments[1].GetStrValue(exm);
				ASprite img = AppContents.GetSprite(imgname);
				if (img == null || !img.IsCreated)
					return 0;

				Rectangle destRect = new Rectangle(0, 0, img.DestBaseSize.Width, img.DestBaseSize.Height);
				if (arguments.Length == 2)
				{
					dest.GDrawCImg(img, destRect);
					return 1;
				}
				if (arguments.Length == 4)
				{
					Point p = ReadPoint(Name, exm, arguments, 2);
					destRect.X = p.X;
					destRect.Y = p.Y;
					dest.GDrawCImg(img, destRect);
					return 1;
				}
				if (arguments.Length == 6)
				{
					destRect = ReadRectangle(Name, exm, arguments, 2);
					dest.GDrawCImg(img, destRect);
					return 1;
				}
				//if (arguments.Length == 7)
				destRect = ReadRectangle(Name, exm, arguments, 2);
				float[][] cm = ReadColormatrix(Name, exm, arguments, 6);
				dest.GDrawCImg(img, destRect, cm);
				return 1;
			}

			public override bool UniqueRestructure(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						continue;
					//The 7th argument points to a ColorMatrix array, so it must not be const-ified
					if (i == 6)
						arguments[i].Restructure(exm);
					else
						arguments[i] = arguments[i].Restructure(exm);
				}
				return false;
			}
		}

		/// <summary>
		/// int SPRITEANIMECREATE (string name, int width, int height)
		/// </summary>
		public sealed class SpriteAnimeCreateMethod : FunctionMethod
		{
			public SpriteAnimeCreateMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				string imgname = arguments[0].GetStrValue(exm);
				if (string.IsNullOrEmpty(imgname))
					return 0;
				//Resource check: fail if it already exists
				ASprite img = AppContents.GetSprite(imgname);
				if (img != null && img.IsCreated)
					return 0;
				Point pos = ReadPoint(Name, exm, arguments, 1);
				if (pos.X <= 0)//{0}: Graphics width of 0 or less ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth0, Name, pos.X));
				else if (pos.X > AbstractImage.MAX_IMAGESIZE)//{0}: Graphics width of {2} or more ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGWidth1, Name, pos.X, AbstractImage.MAX_IMAGESIZE));
				if (pos.Y <= 0)//{0}: Graphics height of 0 or less ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight0, Name, pos.Y));
				else if (pos.Y > AbstractImage.MAX_IMAGESIZE)//{0}: Graphics height of {2} or more ({1}) was specified
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGHeight1, Name, pos.Y, AbstractImage.MAX_IMAGESIZE));
				AppContents.CreateSpriteAnime(imgname, pos.X, pos.Y);
				return 1;
			}
		}


		/// <summary>
		/// SPRITEANIMEADDFRAME (string name, int graphID, int x, int y, int width, int height, int offsetx, int offsety, int delay)
		/// </summary>
		public sealed class SpriteAnimeAddFrameMethod : FunctionMethod
		{
			public SpriteAnimeAddFrameMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				string imgname = arguments[0].GetStrValue(exm);
				if (string.IsNullOrEmpty(imgname))
					return 0;
				SpriteAnime img = AppContents.GetSprite(imgname) as SpriteAnime;
				if (img == null && !img.IsCreated)
					return 0;
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 1);
				if (!g.IsCreated)
					return 0;
				Rectangle rect = ReadRectangle(Name, exm, arguments, 2);
				//The rectangle must be positive and must not point outside the parent image
				if (rect.Width <= 0 || rect.Height <= 0 ||
					rect.X < 0 || rect.X + rect.Width > g.Width || rect.Y < 0 || rect.Y + rect.Height > g.Height)
					return 0;
					//throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodCIMGCreateOutOfRange0, Name));
				Point offset = ReadPoint(Name, exm, arguments, 6);
				Int64 delay = arguments[8].GetIntValue(exm);
				if (delay <= 0 || delay > int.MaxValue)
					return 0;
				img.AddFrame(g, rect, offset, (int)delay);
				return 1;
			}
		}


		/// <summary>
		/// CBGCLEAR
		/// </summary>
		public sealed class CBGClearMethod : FunctionMethod
		{
			public CBGClearMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] {};
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				exm.Console.CBG_Clear();
				return 1;
			}
		}

		/// <summary>
		/// CBGREMOVERANGE(int zmin, int zmax)
		/// </summary>
		public sealed class CBGRemoveRangeMethod : FunctionMethod
		{
			public CBGRemoveRangeMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{

				Int64 x64 = arguments[0].GetIntValue(exm);
				Int64 y64 = arguments[1].GetIntValue(exm);
				unchecked
				{
					exm.Console.CBG_ClearRange((int)x64, (int)y64);
				}
				return 1;
			}
		}
		/// <summary>
		/// CBGCLEARBUTTON
		/// </summary>
		public sealed class CBGClearButtonMethod : FunctionMethod
		{
			public CBGClearButtonMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				exm.Console.CBG_ClearButton();
				return 1;
			}
		}
		/// <summary>
		/// CBGREMOVEBMAP
		/// </summary>
		public sealed class CBGRemoveBMapMethod : FunctionMethod
		{
			public CBGRemoveBMapMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				exm.Console.CBG_ClearBMap();
				return 1;
			}
		}
		/// <summary>
		/// CBGSETG(int ID, int x, int y, int zdepth)
		/// </summary>
		public sealed class CBGSetGraphicsMethod : FunctionMethod
		{
			public CBGSetGraphicsMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));

				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				Point p = ReadPoint(Name, exm, arguments, 1);
				Int64 z64 = arguments[3].GetIntValue(exm);
				if (z64 < int.MinValue || z64 > int.MaxValue || z64 == 0)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, z64, 3 + 1));
				exm.Console.CBG_SetGraphics(g, p.X, p.Y, (int)z64);
				return 1;

			}
		}

		/// <summary>
		/// CBGSETBMAPG(int ID, int x, int y, int zdepth)
		/// </summary>
		public sealed class CBGSetBMapGMethod : FunctionMethod
		{
			public CBGSetBMapGMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64)};
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));

				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;
				exm.Console.CBG_SetButtonMap(g);
				return 1;

			}
		}

		/// <summary>
		/// CBGSETCIMG(str imgName, int x, int y, int zdepth)
		/// </summary>
		public sealed class CBGSetCIMGMethod : FunctionMethod
		{
			public CBGSetCIMGMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				//if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
				//	throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));

				string imgname = arguments[0].GetStrValue(exm);
				ASprite img = AppContents.GetSprite(imgname);
				if (img == null || !img.IsCreated)
					return 0;
				Point p = ReadPoint(Name, exm, arguments, 1);
				Int64 z64 = arguments[3].GetIntValue(exm);
				if (z64 < int.MinValue || z64 > int.MaxValue || z64 == 0)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, z64, 3 + 1));
				if (!exm.Console.CBG_SetImage(img, p.X,p.Y, (int)z64))
					return 0;
				return 1;

			}
		}

		/// <summary>
		/// CBGSETBUTTONCIMG(int button, str imgName, str imgName, int x, int y,int zdepth str tooltipmes)
		/// </summary>
		public sealed class CBGSETButtonSpriteMethod : FunctionMethod
		{
			public CBGSETButtonSpriteMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(string), typeof(string), typeof(Int64), typeof(Int64), typeof(Int64), typeof(string) };
				CanRestructure = false;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{

				if (arguments.Length < 6)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 6);
				if (arguments.Length > 7)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				if (arguments.Length != 6 && arguments.Length != 7)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum0, name);

				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);

					if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
				}
				return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));

				Int64 b64 = arguments[0].GetIntValue(exm);
				if (b64 < 0 || b64 > 0xFFFFFF)
					return 0;
				string imgnameN = arguments[1].GetStrValue(exm);
				ASprite imgN = AppContents.GetSprite(imgnameN);
				string imgnameB = arguments[2].GetStrValue(exm);
				ASprite imgB = AppContents.GetSprite(imgnameB);

				Point p = ReadPoint(Name, exm, arguments, 3);
				Int64 z64 = arguments[5].GetIntValue(exm);
				if (z64 < int.MinValue || z64 > int.MaxValue || z64 == 0)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, z64, 5 + 1));
				string tooltip = null;
				if(arguments.Length > 6)
					tooltip = arguments[6].GetStrValue(exm);
				if (!exm.Console.CBG_SetButtonImage((int)b64, imgN, imgB, p.X, p.Y, (int)z64, tooltip))
					return 0;
				return 1;

			}
		}

		static readonly short[] keytoggle = new short[256];
		private sealed class GetKeyStateMethod : FunctionMethod
		{
			public GetKeyStateMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (!exm.Console.IsActive)//Skip if not active
					return 0;
				Int64 keycode = arguments[0].GetIntValue(exm);
				if (keycode < 0 || keycode > 255)
					return 0;
				short s = WinInput.GetKeyState((int)keycode);
				short toggle = keytoggle[keycode];
				keytoggle[keycode] = (short)((s & 1) + 1);//Initial value 0; assign 1 or 2 depending on the toggle state.
				switch(Name)
				{
					case "GETKEY": return (s < 0) ? 1 : 0;
					case "GETKEYTRIGGERED": return (s < 0) && (toggle != keytoggle[keycode]) ? 1 : 0;//true on the first press; afterwards 1 only if the toggle state differs from the previous one
				}
				throw new ExeEE(GameMessages.T("unexpected branch"));
			}
		}

		private sealed class MousePosMethod : FunctionMethod
		{
			public MousePosMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				switch(Name)
				{
					case "MOUSEX": return exm.Console.GetMousePosition().X;
					case "MOUSEY": return exm.Console.GetMousePosition().Y;
				}
				throw new ExeEE(GameMessages.T("unexpected name"));
			}
		}

		private sealed class MouseButtonMethod : FunctionMethod
		{
			public MouseButtonMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = new Type[] { };
				CanRestructure = false;
			}

			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				ConsoleButtonString pointing = exm.Console.PointingString;
				if (pointing == null || !pointing.IsButton)
					return "";
				return pointing.IsInteger ? pointing.Input.ToString() : pointing.Inputs;
			}
		}


		private sealed class IsActiveMethod : FunctionMethod
		{
			public IsActiveMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				return exm.Console.IsActive ? 1 : 0;
			}
		}

		private sealed class SetAnimeTimerMethod : FunctionMethod
		{
			public SetAnimeTimerMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] {typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				Int64 i64 = arguments[0].GetIntValue(exm);
				if (i64 < int.MinValue || i64 > short.MaxValue)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodDefaultArgumentOutOfRange0, Name, i64, 1));
				exm.Console.setRedrawTimer((int)i64);
				return 1;
			}
		}

		/// <summary>
		/// int SAVETEXT str text, int fileNo{, int force_savdir, int force_UTF8}
		/// </summary>
		private sealed class SaveTextMethod : FunctionMethod
		{
			public SaveTextMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) ,typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{

				if (arguments.Length < 2)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 2);
				if (arguments.Length > 4)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);

					if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
				}
				return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string savText = arguments[0].GetStrValue(exm);
				Int64 i64 = arguments[1].GetIntValue(exm);
				if (i64 < 0 || i64 > int.MaxValue)
					return 0;
				bool forceSavdir = arguments.Length > 2 && (arguments[2].GetIntValue(exm) != 0);
				bool forceUTF8 = arguments.Length > 3 && (arguments[3].GetIntValue(exm) != 0);
				int fileIndex = (int)i64;
				string filepath = forceSavdir ?
					GetSaveDataPathText(fileIndex, Config.ForceSavDir) :
					GetSaveDataPathText(fileIndex, Config.SavDir);
				Encoding encoding = forceUTF8 ?
					Encoding.GetEncoding("UTF-8") :
					Config.SaveEncode;
				try
				{
					if (forceSavdir)
						Config.ForceCreateSavDir();
					else
						Config.CreateSavDir();
					System.IO.File.WriteAllText(filepath, savText, encoding);
				}
				catch { return 0; }
				return 1;
			}
		}
		/// <summary>
		/// str LOADTEXT int fileNo{, int force_savdir, int force_UTF8}
		/// </summary>
		private sealed class LoadTextMethod : FunctionMethod
		{
			public LoadTextMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{

				if (arguments.Length < 1)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 1);
				if (arguments.Length > 3)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
					if (i < argumentTypeArray.Length && argumentTypeArray[i] != arguments[i].GetOperandType())
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
				}
				return null;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
                Int64 i64 = arguments[0].GetIntValue(exm);
                if (i64 < 0 || i64 > int.MaxValue)
					return "";
				bool forceSavdir = arguments.Length > 1 && (arguments[1].GetIntValue(exm) != 0);
				bool forceUTF8 = arguments.Length > 2 && (arguments[2].GetIntValue(exm) != 0);
				int fileIndex = (int)i64;
				string filepath = forceSavdir ?
					GetSaveDataPathText(fileIndex, Config.ForceSavDir) :
					GetSaveDataPathText(fileIndex, Config.SavDir);
				Encoding encoding = forceUTF8 ?
					Encoding.GetEncoding("UTF-8") :
					Config.SaveEncode;
				if (!System.IO.File.Exists(filepath))
					return "";
                string ret;
                try
                {
                    ret = System.IO.File.ReadAllText(filepath, encoding);
                }
                catch { return ""; }
                //For consistency's sake, \r has to go
                return ret.Replace("\r","");
			}
		}



		private static string GetSaveDataPathText(int index, string dir) { return string.Format("{0}txt{1:00}.txt", dir, index); }
		private static string GetSaveDataPathGraphics(int index) { return string.Format("{0}img{1:0000}.png", Config.SavDir, index); }

		/// <summary>
		/// int GSAVE int ID, int fileNo
		/// </summary>
		public sealed class GraphicsSaveMethod : FunctionMethod
		{
			public GraphicsSaveMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (!g.IsCreated)
					return 0;

				Int64 i64 = arguments[1].GetIntValue(exm);
				if (i64 < 0 || i64 > int.MaxValue)
					return 0;

				string filepath = GetSaveDataPathGraphics((int)i64);
				try
				{
					Config.CreateSavDir();
					g.Bitmap.Save(filepath);
				}
				catch
				{
					return 0;
				}
				return 1;
			}
		}
		/// <summary>
		/// int GLOAD int ID, int fileNo
		/// </summary>
		public sealed class GraphicsLoadMethod : FunctionMethod
		{
			public GraphicsLoadMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				if (Config.TextDrawingMode == TextDrawingMode.WINAPI)
					throw new CodeEE(string.Format(Properties.Resources.RuntimeErrMesMethodGDIPLUSOnly, Name));
				GraphicsImage g = ReadGraphics(Name, exm, arguments, 0);
				if (g.IsCreated)
					return 0;

				Int64 i64 = arguments[1].GetIntValue(exm);
				if (i64 < 0 || i64 > int.MaxValue)
					return 0;

				string filepath = GetSaveDataPathGraphics((int)i64);
				Bitmap bmp = null;
				try
				{
					if (!System.IO.File.Exists(filepath))
						return 0;
					bmp = new Bitmap(filepath);
					if (bmp.Width > AbstractImage.MAX_IMAGESIZE || bmp.Height > AbstractImage.MAX_IMAGESIZE)
						return 0;
					g.GCreateFromF(bmp, (Config.TextDrawingMode == TextDrawingMode.WINAPI));
				}
				catch (Exception e)
				{
					if (e is CodeEE)
						throw;
				}
				finally
				{
					if (bmp != null)
						bmp.Dispose();
				}
				if (!g.IsCreated)
					return 0;
				return 1;
			}
		}

		#endregion

		#region Emuera EM/EE Extensions

		/// <summary>
		/// EXISTFUNCTION method - checks if a function exists
		/// Returns: 0 if not found, 1 if normal function, 2 if integer #FUNCTION, 3 if string #FUNCTIONS
		/// </summary>
		private sealed class ExistFunctionMethod : FunctionMethod
		{
			public ExistFunctionMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
				{
					string funcName = arguments[0].GetStrValue(exm);
					if (string.IsNullOrEmpty(funcName))
						return 0;

					// Convert to uppercase if case-insensitive function names is enabled
					if (Config.ICFunction)
						funcName = funcName.ToUpper();

					return FunctionResolver.ExistFunctionValue(funcName);
				}
		}

		/// <summary>
		/// EXISTSOUND method - checks if a sound file exists
		/// Returns: 1 if file exists, 0 otherwise
		/// </summary>
		private sealed class ExistSoundMethod : FunctionMethod
		{
			public ExistSoundMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string filename = arguments[0].GetStrValue(exm);
				return MinorShift.Emuera.Content.AudioManager.Instance.ExistSound(filename);
			}
		}

		/// <summary>
		/// FLOOR method - rounds a number down to the nearest integer
		/// </summary>
		private sealed class FloorMethod : FunctionMethod
		{
			public FloorMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64) };
				CanRestructure = true;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				// In Emuera, all numbers are integers, so FLOOR just returns the input
				// This is provided for compatibility with scripts that use FLOOR
				return arguments[0].GetIntValue(exm);
			}
		}

		/// <summary>
		/// CEILING method - rounds a number up to the nearest integer
		/// </summary>
		private sealed class CeilingMethod : FunctionMethod
		{
			public CeilingMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64) };
				CanRestructure = true;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				// In Emuera, all numbers are integers, so CEILING just returns the input
				// This is provided for compatibility with scripts that use CEILING
				return arguments[0].GetIntValue(exm);
			}
		}

		/// <summary>
		/// ROUND method - rounds a number to the nearest integer
		/// </summary>
		private sealed class RoundMethod : FunctionMethod
		{
			public RoundMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64) };
				CanRestructure = true;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				// In Emuera, all numbers are integers, so ROUND just returns the input
				// This is provided for compatibility with scripts that use ROUND
				return arguments[0].GetIntValue(exm);
			}
		}

		#endregion

		#region XML commands

		/// <summary>XML_DOCUMENT / XML_EXIST / XML_RELEASE</summary>
		private sealed class XmlDocumentMethod : FunctionMethod
		{
			public enum Operation { Create, Check, Release }
			readonly Operation op;

			public XmlDocumentMethod(Operation op)
			{
				this.op = op;
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				int expected = op == Operation.Create ? 2 : 1;
				if (arguments.Length != expected)
					return name + GameMessages.T(" function: wrong number of arguments");
				if (arguments[0] == null ||
					(arguments[0].GetOperandType() != typeof(string) && arguments[0].GetOperandType() != typeof(Int64)))
					return name + GameMessages.T(" function: argument #1 has an invalid type");
				if (op == Operation.Create && (arguments[1] == null || arguments[1].GetOperandType() != typeof(string)))
					return name + GameMessages.T(" function: XML must be a string");
				return null;
			}

			static XmlDocument LoadXml(string text)
			{
				XmlDocument document = new XmlDocument { XmlResolver = null };
				XmlReaderSettings settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersFromEntities = 0
				};
				using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(text), settings))
					document.Load(reader);
				return document;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string key = arguments[0].GetOperandType() == typeof(string)
					? arguments[0].GetStrValue(exm)
					: arguments[0].GetIntValue(exm).ToString();
				var documents = exm.VEvaluator.VariableData.DataXmlDocuments;
				if (op == Operation.Create)
				{
					if (documents.ContainsKey(key)) return 0;
					XmlDocument document;
					try
					{
						document = LoadXml(arguments[1].GetStrValue(exm));
					}
					catch (XmlException)
					{
						return 0;
					}
					documents.Add(key, document);
					return 1;
				}
			if (!documents.ContainsKey(key)) return 0;
			if (op == Operation.Check) return 1;
			documents.Remove(key);
			return 1;
			}
		}

		private sealed class XmlToStrMethod : FunctionMethod
		{
			public XmlToStrMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length != 1 || arguments[0] == null ||
					(arguments[0].GetOperandType() != typeof(string) && arguments[0].GetOperandType() != typeof(Int64)))
					return name + GameMessages.T(" function: invalid argument type");
				return null;
			}

			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string key = arguments[0].GetOperandType() == typeof(string)
					? arguments[0].GetStrValue(exm)
					: arguments[0].GetIntValue(exm).ToString();
				var documents = exm.VEvaluator.VariableData.DataXmlDocuments;
				return documents.TryGetValue(key, out XmlDocument document) ? document.OuterXml : "";
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) { return 0; }
		}

		/// <summary>XML_GET(name, xpath) → returns text at XPath node</summary>
		private sealed class XmlGetMethod : FunctionMethod
		{
			readonly bool byName;

			public XmlGetMethod(bool byName = false)
			{
				this.byName = byName;
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 2 || arguments.Length > 4)
					return name + GameMessages.T(" function: wrong number of arguments (2-4 expected)");
				if (arguments[0] == null)
					return name + GameMessages.T(" function: argument #1 cannot be omitted");
				if (byName && arguments[0].GetOperandType() != typeof(string))
					return name + GameMessages.T(" function: argument #1 must be a string");
				if (arguments[1] == null || arguments[1].GetOperandType() != typeof(string))
					return name + GameMessages.T(" function: argument #2 must be a string");
				if (arguments.Length == 3)
				{
					if (arguments[2] == null ||
						(arguments[2].GetOperandType() != typeof(Int64) && !IsStringArrayReference(arguments[2])))
						return name + GameMessages.T(" function: argument #3 has an invalid type");
				}
				if (arguments.Length == 4)
				{
					if (arguments[2] == null ||
						(arguments[2].GetOperandType() != typeof(Int64) && !IsStringArrayReference(arguments[2])))
						return name + GameMessages.T(" function: argument #3 has an invalid type");
					if (arguments[3] == null || arguments[3].GetOperandType() != typeof(Int64))
						return name + GameMessages.T(" function: argument #4 must be an integer");
				}
				return null;
			}

			static bool IsStringArrayReference(IOperandTerm term)
			{
				VariableTerm variable = term as VariableTerm;
				return variable != null && !variable.Identifier.IsConst && !variable.Identifier.IsCalc &&
					variable.Identifier.IsArray1D && !variable.IsInteger;
			}

			static XmlDocument LoadXml(string text)
			{
				XmlDocument document = new XmlDocument { XmlResolver = null };
				XmlReaderSettings settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersFromEntities = 0
				};
				using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(text), settings))
					document.Load(reader);
				return document;
			}

			static string NodeValue(XmlNode node, int outputType)
			{
				switch (outputType)
				{
					case 1: return node.InnerText;
					case 2: return node.InnerXml;
					case 3: return node.OuterXml;
					case 4: return node.Name;
					default: return node.Value ?? "";
				}
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				XmlDocument document;
				try
				{
					if (byName || arguments[0].GetOperandType() == typeof(Int64))
					{
						string key = byName
							? arguments[0].GetStrValue(exm)
							: arguments[0].GetIntValue(exm).ToString();
						if (!exm.VEvaluator.VariableData.DataXmlDocuments.TryGetValue(key, out document)) return -1;
					}
					else
						document = LoadXml(arguments[0].GetStrValue(exm));

					XmlNodeList nodes = document.SelectNodes(arguments[1].GetStrValue(exm));
					if (nodes == null) return 0;
					VariableTerm outputVariable = null;
					int doOutput = 0;
					int outputType = 0;
					if (arguments.Length >= 3)
					{
						outputVariable = arguments[2] as VariableTerm;
						if (outputVariable == null) doOutput = (int)arguments[2].GetIntValue(exm);
					}
					if (arguments.Length == 4) outputType = (int)arguments[3].GetIntValue(exm);

					string[] output = outputVariable == null
						? exm.VEvaluator.RESULTS_ARRAY
						: (string[])outputVariable.Identifier.GetArray();
					if (outputVariable != null || doOutput != 0)
					{
						int limit = Math.Min(nodes.Count, output.Length);
						for (int i = 0; i < limit; i++) output[i] = NodeValue(nodes[i], outputType);
					}
					return nodes.Count;
				}
				catch (XmlException)
				{
					return 0;
				}
				catch (System.Xml.XPath.XPathException)
				{
					return 0;
				}
			}
		}

		/// <summary>XML_SET(name, xpath, value) → sets text at XPath node</summary>
		private sealed class XmlSetMethod : FunctionMethod
		{
			readonly bool byName;

			public XmlSetMethod(bool byName = false)
			{
				this.byName = byName;
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 3 || arguments.Length > 5)
					return name + GameMessages.T(" function: wrong number of arguments (3-5 expected)");
				if (arguments[0] == null ||
					(byName && arguments[0].GetOperandType() != typeof(string)) ||
					(!byName && arguments[0].GetOperandType() != typeof(Int64) && !IsMutableXmlReference(arguments[0])))
					return name + GameMessages.T(" function: argument #1 has an invalid type");
				if (arguments[1] == null || arguments[1].GetOperandType() != typeof(string) ||
					(arguments[2] == null || arguments[2].GetOperandType() != typeof(string)))
					return name + GameMessages.T(" function: XPath and value must be strings");
			for (int i = 3; i < arguments.Length; i++)
				if (arguments[i] == null || arguments[i].GetOperandType() != typeof(Int64))
					return name + GameMessages.T(" function: the additional argument must be an integer");
			return null;
			}

			static bool IsMutableXmlReference(IOperandTerm term)
			{
				VariableTerm variable = term as VariableTerm;
				return variable != null && !variable.Identifier.IsConst && !variable.Identifier.IsCalc &&
					!variable.Identifier.IsArray1D && !variable.Identifier.IsArray2D && !variable.Identifier.IsArray3D &&
					!variable.IsInteger;
			}

			static XmlDocument LoadXml(string text)
			{
				XmlDocument document = new XmlDocument { XmlResolver = null };
				XmlReaderSettings settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersFromEntities = 0
				};
				using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(text), settings))
					document.Load(reader);
				return document;
			}

			static void SetNodeValue(XmlNode node, string value, int outputType)
			{
				switch (outputType)
				{
					case 1: node.InnerText = value; break;
					case 2: node.InnerXml = value; break;
					default: node.Value = value; break;
				}
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				XmlDocument document;
				VariableTerm sourceVariable = null;
				try
				{
					if (byName || arguments[0].GetOperandType() == typeof(Int64))
					{
						string key = byName
							? arguments[0].GetStrValue(exm)
							: arguments[0].GetIntValue(exm).ToString();
						if (!exm.VEvaluator.VariableData.DataXmlDocuments.TryGetValue(key, out document)) return -1;
					}
					else
					{
						sourceVariable = (VariableTerm)arguments[0];
						document = LoadXml(sourceVariable.GetStrValue(exm));
					}

					XmlNodeList nodes = document.SelectNodes(arguments[1].GetStrValue(exm));
					if (nodes == null || nodes.Count == 0) return 0;
					int doSetAll = arguments.Length >= 4 ? (int)arguments[3].GetIntValue(exm) : 0;
					int outputType = arguments.Length == 5 ? (int)arguments[4].GetIntValue(exm) : 0;
					if (nodes.Count != 1 && doSetAll == 0) return nodes.Count;
					string value = arguments[2].GetStrValue(exm);
					for (int i = 0; i < nodes.Count; i++) SetNodeValue(nodes[i], value, outputType);
					if (sourceVariable != null) sourceVariable.SetValue(document.OuterXml, exm);
					return nodes.Count;
				}
				catch (XmlException)
				{
					return 0;
				}
				catch (System.Xml.XPath.XPathException)
				{
					return 0;
				}
			}
		}

		/// <summary>XML_ADDNODE / XML_ADDATTRIBUTE</summary>
		private sealed class XmlAddNodeMethod : FunctionMethod
		{
			public enum Operation { Node, Attribute }
			readonly Operation op;
			readonly bool byName;

			public XmlAddNodeMethod(Operation op, bool byName = false)
			{
				this.op = op;
				this.byName = byName;
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				int min = op == Operation.Node ? 3 : 4;
				int max = op == Operation.Node ? 5 : 6;
				if (arguments.Length < min || arguments.Length > max)
					return name + GameMessages.T(" function: wrong number of arguments");
				if (arguments[0] == null ||
					(byName && arguments[0].GetOperandType() != typeof(string)) ||
					(!byName && arguments[0].GetOperandType() != typeof(Int64) && !IsMutableReference(arguments[0])))
					return name + GameMessages.T(" function: argument #1 has an invalid type");
				if (arguments[1] == null || arguments[1].GetOperandType() != typeof(string) ||
					(arguments[2] == null || arguments[2].GetOperandType() != typeof(string)))
					return name + GameMessages.T(" function: XPath and content must be strings");
				int firstInteger = op == Operation.Node ? 3 : 4;
				for (int i = firstInteger; i < arguments.Length; i++)
					if (arguments[i] == null || arguments[i].GetOperandType() != typeof(Int64))
						return name + GameMessages.T(" function: the additional argument must be an integer");
				return null;
			}

			static bool IsMutableReference(IOperandTerm term)
			{
				VariableTerm variable = term as VariableTerm;
				return variable != null && !variable.Identifier.IsConst && !variable.Identifier.IsCalc &&
					!variable.Identifier.IsArray1D && !variable.Identifier.IsArray2D && !variable.Identifier.IsArray3D &&
					!variable.IsInteger;
			}

			static XmlDocument LoadXml(string text)
			{
				XmlDocument document = new XmlDocument { XmlResolver = null };
				XmlReaderSettings settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersFromEntities = 0
				};
				using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(text), settings))
					document.Load(reader);
				return document;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				XmlDocument document;
				VariableTerm sourceVariable = null;
				try
				{
					if (byName || arguments[0].GetOperandType() == typeof(Int64))
					{
						string key = byName
							? arguments[0].GetStrValue(exm)
							: arguments[0].GetIntValue(exm).ToString();
						if (!exm.VEvaluator.VariableData.DataXmlDocuments.TryGetValue(key, out document)) return -1;
					}
					else
					{
						sourceVariable = (VariableTerm)arguments[0];
						document = LoadXml(sourceVariable.GetStrValue(exm));
					}

					XmlNodeList nodes = document.SelectNodes(arguments[1].GetStrValue(exm));
					if (nodes == null || nodes.Count == 0) return 0;
					int methodPosition = op == Operation.Node ? 3 : 4;
					int setAllPosition = op == Operation.Node ? 4 : 5;
					int methodType = arguments.Length > methodPosition ? (int)arguments[methodPosition].GetIntValue(exm) : 0;
					int doSetAll = arguments.Length > setAllPosition ? (int)arguments[setAllPosition].GetIntValue(exm) : 0;
					if (methodType < 0 || methodType > 2) methodType = 0;
					if (nodes.Count != 1 && doSetAll == 0) return nodes.Count;

					if (op == Operation.Node)
					{
						XmlDocument fragment = LoadXml(arguments[2].GetStrValue(exm));
						if (fragment.DocumentElement == null) return 0;
						for (int i = 0; i < nodes.Count; i++)
						{
							XmlNode target = nodes[i];
							XmlNode imported = document.ImportNode(fragment.DocumentElement, true);
							if (methodType == 1 && target.ParentNode != null && target != document.DocumentElement)
								target.ParentNode.InsertBefore(imported, target);
							else if (methodType == 2 && target.ParentNode != null && target != document.DocumentElement)
								target.ParentNode.InsertAfter(imported, target);
							else if (methodType == 0)
								target.AppendChild(imported);
						}
					}
					else
					{
						string attributeName = arguments[2].GetStrValue(exm);
						string attributeValue = arguments[3].GetStrValue(exm);
						for (int i = 0; i < nodes.Count; i++)
						{
							if (nodes[i] is XmlElement element && methodType == 0)
								element.SetAttribute(attributeName, attributeValue);
							else if (nodes[i] is XmlAttribute target && target.OwnerElement != null)
							{
								XmlAttribute inserted = document.CreateAttribute(attributeName);
								inserted.Value = attributeValue;
								if (methodType == 1) target.OwnerElement.Attributes.InsertBefore(inserted, target);
								else if (methodType == 2) target.OwnerElement.Attributes.InsertAfter(inserted, target);
							}
						}
					}
					if (sourceVariable != null) sourceVariable.SetValue(document.OuterXml, exm);
					return nodes.Count;
				}
				catch (XmlException)
				{
					return 0;
				}
				catch (System.Xml.XPath.XPathException)
				{
					return 0;
				}
			}
		}

		/// <summary>XML_REMOVENODE / XML_REMOVEATTRIBUTE</summary>
		private sealed class XmlRemoveNodeMethod : FunctionMethod
		{
			public enum Operation { Node, Attribute }
			readonly Operation op;
			readonly bool byName;

			public XmlRemoveNodeMethod(Operation op, bool byName = false)
			{
				this.op = op;
				this.byName = byName;
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 2 || arguments.Length > 3)
					return name + GameMessages.T(" function: wrong number of arguments");
				if (arguments[0] == null ||
					(byName && arguments[0].GetOperandType() != typeof(string)) ||
					(!byName && arguments[0].GetOperandType() != typeof(Int64) && !IsMutableReference(arguments[0])))
					return name + GameMessages.T(" function: argument #1 has an invalid type");
				if (arguments[1] == null || arguments[1].GetOperandType() != typeof(string))
					return name + GameMessages.T(" function: XPath must be a string");
				if (arguments.Length == 3 && (arguments[2] == null || arguments[2].GetOperandType() != typeof(Int64)))
					return name + GameMessages.T(" function: argument #3 must be an integer");
				return null;
			}

			static bool IsMutableReference(IOperandTerm term)
			{
				VariableTerm variable = term as VariableTerm;
				return variable != null && !variable.Identifier.IsConst && !variable.Identifier.IsCalc &&
					!variable.Identifier.IsArray1D && !variable.Identifier.IsArray2D && !variable.Identifier.IsArray3D &&
					!variable.IsInteger;
			}

			static XmlDocument LoadXml(string text)
			{
				XmlDocument document = new XmlDocument { XmlResolver = null };
				XmlReaderSettings settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersFromEntities = 0
				};
				using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(text), settings))
					document.Load(reader);
				return document;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				XmlDocument document;
				VariableTerm sourceVariable = null;
				try
				{
					if (byName || arguments[0].GetOperandType() == typeof(Int64))
					{
						string key = byName
							? arguments[0].GetStrValue(exm)
							: arguments[0].GetIntValue(exm).ToString();
						if (!exm.VEvaluator.VariableData.DataXmlDocuments.TryGetValue(key, out document)) return -1;
					}
					else
					{
						sourceVariable = (VariableTerm)arguments[0];
						document = LoadXml(sourceVariable.GetStrValue(exm));
					}
					XmlNodeList nodes = document.SelectNodes(arguments[1].GetStrValue(exm));
					if (nodes == null || nodes.Count == 0) return 0;
					bool removeAll = arguments.Length == 3 && arguments[2].GetIntValue(exm) != 0;
					int limit = nodes.Count == 1 || removeAll ? nodes.Count : 0;
					for (int i = 0; i < limit; i++)
					{
						if (op == Operation.Attribute)
						{
							XmlAttribute attribute = nodes[i] as XmlAttribute;
							if (attribute != null && attribute.OwnerElement != null)
							{
								attribute.OwnerElement.Attributes.Remove(attribute);

							}
						}
						else if (nodes[i] != document.DocumentElement && nodes[i].ParentNode != null)
						{
							nodes[i].ParentNode.RemoveChild(nodes[i]);

						}
					}
					if (sourceVariable != null) sourceVariable.SetValue(document.OuterXml, exm);
					return nodes.Count;
				}
				catch (XmlException)
				{
					return 0;
				}
				catch (System.Xml.XPath.XPathException)
				{
					return 0;
				}
			}
		}

		/// <summary>XML_REPLACE(name, xpath, newValue) → replaces inner text at node</summary>
		private sealed class XmlReplaceMethod : FunctionMethod
		{
			readonly bool byName;

			public XmlReplaceMethod(bool byName = false)
			{
				this.byName = byName;
				ReturnType = typeof(Int64);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 2 || arguments.Length > 4)
					return name + GameMessages.T(" function: wrong number of arguments (2-4 expected)");
				if (arguments[0] == null ||
					(byName && arguments[0].GetOperandType() != typeof(string)) ||
					(!byName && arguments.Length > 2 && arguments[0].GetOperandType() != typeof(Int64) && !IsMutableReference(arguments[0])))
					return name + GameMessages.T(" function: argument #1 has an invalid type");
				if (arguments.Length == 2)
				{
					if (arguments[1] == null || arguments[1].GetOperandType() != typeof(string))
						return name + GameMessages.T(" function: XML must be a string");
				}
				else
				{
					if (arguments[1] == null || arguments[1].GetOperandType() != typeof(string) ||
						arguments[2] == null || arguments[2].GetOperandType() != typeof(string))
						return name + GameMessages.T(" function: XPath and XML must be strings");
					if (arguments.Length == 4 && arguments[3].GetOperandType() != typeof(Int64))
						return name + GameMessages.T(" function: argument #4 must be an integer");
				}
				return null;
			}

			static bool IsMutableReference(IOperandTerm term)
			{
				VariableTerm variable = term as VariableTerm;
				return variable != null && !variable.Identifier.IsConst && !variable.Identifier.IsCalc &&
					!variable.Identifier.IsArray1D && !variable.Identifier.IsArray2D && !variable.Identifier.IsArray3D &&
					!variable.IsInteger;
			}

			static XmlDocument LoadXml(string text)
			{
				XmlDocument document = new XmlDocument { XmlResolver = null };
				XmlReaderSettings settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersFromEntities = 0
				};
				using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(text), settings))
					document.Load(reader);
				return document;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				XmlDocument replacement;
				try
				{
					replacement = LoadXml(arguments.Length == 2
						? arguments[1].GetStrValue(exm)
						: arguments[2].GetStrValue(exm));
				}
				catch (XmlException)
				{
					return 0;
				}

				XmlDocument document;
				VariableTerm sourceVariable = null;
				try
				{
					bool stored = byName || arguments[0].GetOperandType() == typeof(Int64) || arguments.Length == 2;
					if (stored)
					{
						string key = arguments[0].GetOperandType() == typeof(Int64)
							? arguments[0].GetIntValue(exm).ToString()
							: arguments[0].GetStrValue(exm);
						if (!exm.VEvaluator.VariableData.DataXmlDocuments.ContainsKey(key)) return -1;
						if (arguments.Length == 2)
						{
							exm.VEvaluator.VariableData.DataXmlDocuments[key] = replacement;
							return 1;
						}
						document = exm.VEvaluator.VariableData.DataXmlDocuments[key];
					}
					else
					{
						sourceVariable = (VariableTerm)arguments[0];
						document = LoadXml(sourceVariable.GetStrValue(exm));
					}

					XmlNodeList nodes = document.SelectNodes(arguments[1].GetStrValue(exm));
					if (nodes == null || nodes.Count == 0 || replacement.DocumentElement == null) return 0;
					bool setAll = arguments.Length == 4 && arguments[3].GetIntValue(exm) != 0;
					if (nodes.Count == 1 || setAll)
					{
						int limit = nodes.Count == 1 ? 1 : nodes.Count;
						for (int i = 0; i < limit; i++)
							if (nodes[i].ParentNode != null)
								nodes[i].ParentNode.ReplaceChild(document.ImportNode(replacement.DocumentElement, true), nodes[i]);
					}
					if (sourceVariable != null) sourceVariable.SetValue(document.OuterXml, exm);
					return nodes.Count;
				}
				catch (XmlException)
				{
					return 0;
				}
				catch (System.Xml.XPath.XPathException)
				{
					return 0;
				}
			}
		}

		#endregion

		#region MAP commands

		private sealed class MapCreateMethod : FunctionMethod
		{
			public MapCreateMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (maps.ContainsKey(name)) return 0;
				maps[name] = new Dictionary<string, string>();
				return 1;
			}
		}

		private sealed class MapExistMethod : FunctionMethod
		{
			public MapExistMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				return exm.VEvaluator.VariableData.DataMaps.ContainsKey(name) ? 1 : 0;
			}
		}

		private sealed class MapReleaseMethod : FunctionMethod
		{
			public MapReleaseMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				exm.VEvaluator.VariableData.DataMaps.Remove(name);
				return 1;
			}
		}

		private sealed class MapGetMethod : FunctionMethod
		{
			public MapGetMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = new Type[] { typeof(string), typeof(string) };
				CanRestructure = false;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				string key = arguments[1].GetStrValue(exm);
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (maps.TryGetValue(name, out var map) && map.TryGetValue(key, out var val))
					return val;
				return "";
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string s = GetStrValue(exm, arguments);
				if (Int64.TryParse(s, out long v)) return v;
				return 0;
			}
		}

		private sealed class MapHasMethod : FunctionMethod
		{
			public MapHasMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				string key = arguments[1].GetStrValue(exm);
				var maps = exm.VEvaluator.VariableData.DataMaps;
				return (maps.TryGetValue(name, out var map) && map.ContainsKey(key)) ? 1 : 0;
			}
		}

		private sealed class MapSetMethod : FunctionMethod
		{
			public MapSetMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				string key = arguments[1].GetStrValue(exm);
				string val = arguments[2].GetStrValue(exm);
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (!maps.TryGetValue(name, out var map)) return -1;
				map[key] = val;
				return 1;
			}
		}

		private sealed class MapRemoveMethod : FunctionMethod
		{
			public MapRemoveMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				string key = arguments[1].GetStrValue(exm);
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (!maps.TryGetValue(name, out var map)) return -1;
				map.Remove(key);
				return 1;
			}
		}

		private sealed class MapClearMethod : FunctionMethod
		{
			public MapClearMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (!maps.TryGetValue(name, out var map)) return -1;
				map.Clear();
				return 1;
			}
		}

		private sealed class MapSizeMethod : FunctionMethod
		{
			public MapSizeMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (maps.TryGetValue(name, out var map)) return map.Count;
				return 0;
			}
		}

		/// <summary>
		/// MAP_GETKEYS(name) or MAP_GETKEYS(name, max) — fills RESULTS string array with keys, returns count.
		/// </summary>
		private sealed class MapGetKeysMethod : FunctionMethod
		{
			public MapGetKeysMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = null;
				CanRestructure = false;
			}

			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 1 || arguments.Length > 3)
					return name + GameMessages.T(" function: wrong number of arguments (1-3 expected)");
				if (arguments[0] == null || arguments[0].GetOperandType() != typeof(string))
					return name + GameMessages.T(" function: argument #1 must be a string");
				if (arguments.Length == 2)
				{
					if (arguments[1] == null || arguments[1].GetOperandType() != typeof(Int64))
						return name + GameMessages.T(" function: argument #2 must be an integer");
				}
				else if (arguments.Length == 3)
				{
					VariableTerm output = arguments[1] as VariableTerm;
					if (output == null || output.Identifier.IsConst || output.Identifier.IsCalc ||
						!output.Identifier.IsArray1D || output.IsInteger)
						return name + GameMessages.T(" function: argument #2 must be a 1D string array");
					if (arguments[2] == null || arguments[2].GetOperandType() != typeof(Int64))
						return name + GameMessages.T(" function: argument #3 must be an integer");
				}
				return null;
			}

			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (!maps.TryGetValue(name, out var map)) return "";

				if (arguments.Length > 1)
				{
					if (arguments.Length == 2 && arguments[1].GetIntValue(exm) == 0) return "";
					if (arguments.Length == 3 && arguments[2].GetIntValue(exm) == 0) return "";
					string[] output = arguments.Length == 2
						? exm.VEvaluator.RESULTS_ARRAY
						: (string[])((VariableTerm)arguments[1]).Identifier.GetArray();
					int count = 0;
					foreach (string key in map.Keys)
					{
						if (count >= output.Length) break;
						output[count++] = key;
					}
					exm.VEvaluator.RESULT = map.Count;
					return arguments.Length == 2 ? exm.VEvaluator.RESULTS : "";
				}

				return string.Join(",", map.Keys);
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				return 0;
			}
		}

		private sealed class MapToXmlMethod : FunctionMethod
		{
			public MapToXmlMethod()
			{
				ReturnType = typeof(string);
				argumentTypeArray = new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				var maps = exm.VEvaluator.VariableData.DataMaps;
				if (!maps.TryGetValue(name, out var map)) return "";
				var sb = new System.Text.StringBuilder();
				using (var writer = System.Xml.XmlWriter.Create(sb, new System.Xml.XmlWriterSettings
				{
					OmitXmlDeclaration = true,
					ConformanceLevel = System.Xml.ConformanceLevel.Fragment
				}))
				{
					writer.WriteStartElement("map");
					foreach (var pair in map)
					{
						writer.WriteStartElement("p");
						writer.WriteElementString("k", pair.Key ?? "");
						writer.WriteElementString("v", pair.Value ?? "");
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
			return sb.ToString();
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) => 0;
		}

		private sealed class MapFromXmlMethod : FunctionMethod
		{
			public MapFromXmlMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(string), typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				string xml = arguments[1].GetStrValue(exm);
				try
				{
					var doc = new System.Xml.XmlDocument();
					doc.XmlResolver = null;
					var settings = new System.Xml.XmlReaderSettings
					{
						DtdProcessing = System.Xml.DtdProcessing.Prohibit,
						XmlResolver = null,
						MaxCharactersFromEntities = 0
					};
					using (var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(xml), settings))
						doc.Load(reader);
					var root = doc.DocumentElement;
					if (root == null || !string.Equals(root.Name, "map", StringComparison.Ordinal)) return 0;
					var maps = exm.VEvaluator.VariableData.DataMaps;
					if (!maps.TryGetValue(name, out var map)) return 0;
					foreach (System.Xml.XmlNode node in root.SelectNodes("./p"))
					{
						System.Xml.XmlNode key = node.SelectSingleNode("./k");
						System.Xml.XmlNode value = node.SelectSingleNode("./v");
						if (key != null && value != null)
							map[key.InnerText] = value.InnerText;
					}
					return 1;
				}
				catch (System.Xml.XmlException ex)
				{
					throw new CodeEE("MAP_FROMXML: invalid XML: " + ex.Message);
				}
			}
		}

	#endregion

	#region FLOWINPUT / FLOWINPUTS

		public sealed class FlowInputMethod : FunctionMethod
		{
			public FlowInputMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(Int64), typeof(Int64), typeof(Int64) };
				CanRestructure = false;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 1)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 1);
				if (arguments.Length > 4)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				for (int i = 0; i < arguments.Length; i++)
				{
					if (arguments[i] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
					if (arguments[i].GetOperandType() != typeof(Int64))
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
				}
				return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				Process proc = exm.Process;
				proc.flowinputDef = arguments[0].GetIntValue(exm);
				if (arguments.Length > 1) proc.flowinput = arguments[1].GetIntValue(exm) != 0;
				if (arguments.Length > 2) proc.flowinputCanSkip = arguments[2].GetIntValue(exm) != 0;
				if (arguments.Length > 3) proc.flowinputForceSkip = arguments[3].GetIntValue(exm) != 0;
				return 0;
			}
		}

		public sealed class FlowInputsMethod : FunctionMethod
		{
			public FlowInputsMethod()
			{
				ReturnType = typeof(Int64);
				argumentTypeArray = new Type[] { typeof(Int64), typeof(string) };
				CanRestructure = false;
			}
			public override string CheckArgumentType(string name, IOperandTerm[] arguments)
			{
				if (arguments.Length < 1)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 1);
				if (arguments.Length > 2)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
				if (arguments[0] == null)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, 1);
				if (arguments[0].GetOperandType() != typeof(Int64))
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, 1);
				if (arguments.Length > 1)
				{
					if (arguments[1] == null)
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, 2);
					if (arguments[1].GetOperandType() != typeof(string))
						return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, 2);
				}
				return null;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				Process proc = exm.Process;
				proc.flowinputString = arguments[0].GetIntValue(exm) != 0;
				if (arguments.Length > 1) proc.flowinputDefString = arguments[1].GetStrValue(exm);
				return 0;
			}
		}

	#endregion

	#region DT (DataTable) commands

		/// <summary>Handles DT_CREATE, DT_EXIST, DT_RELEASE, DT_NOCASE, DT_CLEAR, DT_ROW_COUNT, DT_ROW_ADD.</summary>
		private sealed class DtManageMethod : FunctionMethod
		{
			public enum Op { Create, Check, Release, Case, Clear, RowCount, RowAdd }
			readonly Op op;
			public DtManageMethod(Op op)
			{
				this.op = op;
				ReturnType = typeof(Int64);
				argumentTypeArray = op == Op.Case
					? new Type[] { typeof(string), typeof(Int64) }
					: new Type[] { typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm);
				var dict = exm.VEvaluator.VariableData.DataDataTables;
				EraDataTable dt = null;
				switch (op)
				{
					case Op.Create:
						if (dict.ContainsKey(name)) return 0L;
						dict[name] = new EraDataTable();
						return 1L;
					case Op.Check:
						return dict.ContainsKey(name) ? 1L : 0L;
					case Op.Release:
						dict.Remove(name);
						return 1L;
					case Op.Case:
						if (!dict.TryGetValue(name, out dt)) return -1L;
						dt.CaseSensitive = arguments[1].GetIntValue(exm) == 0;
						return 1L;
					case Op.Clear:
						if (!dict.TryGetValue(name, out dt)) return -1L;
						dt.Clear();
						return 1L;
					case Op.RowCount:
						return dict.TryGetValue(name, out dt) ? (Int64)dt.RowCount : -1L;
					case Op.RowAdd:
						return dict.TryGetValue(name, out dt) ? dt.AddRow() : -1L;
				}
				return 0L;
			}
		}

		/// <summary>Handles DT_COLUMN_ADD, DT_COLUMN_NAMES, DT_COLUMN_EXIST, DT_COLUMN_REMOVE.</summary>
		private sealed class DtColMethod : FunctionMethod
		{
			public enum Op { Add, Names, Check, Remove }
			readonly Op op;
			public DtColMethod(Op op)
			{
				this.op = op;
				ReturnType = typeof(Int64);
				if (op == Op.Add)
					argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(Int64), typeof(Int64) };
				else if (op == Op.Names)
					argumentTypeArray = new Type[] { typeof(string) };
				else
					argumentTypeArray = new Type[] { typeof(string), typeof(string) };
				CanRestructure = false;
			}
			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				var dict = exm.VEvaluator.VariableData.DataDataTables;
				EraDataTable dt;
				if (!dict.TryGetValue(name, out dt)) return 0;
				switch (op)
				{
					case Op.Add:
					{
						int typeInt = (int)arguments[2].GetIntValue(exm);
						EraDataTable.ColType ct = typeInt == 1 ? EraDataTable.ColType.Int
						                        : typeInt == 2 ? EraDataTable.ColType.Float
						                        : EraDataTable.ColType.Str;
						return dt.AddCol(arguments[1].GetStrValue(exm), ct) ? 1L : 0L;
					}
					case Op.Names:
					{
						string[] names = dt.ColNames();
						string[] results = exm.VEvaluator.VariableData.DataStringArray[(int)(VariableCode.RESULTS & VariableCode.__LOWERCASE__)];
						int cnt = Math.Min(names.Length, results.Length);
						for (int i = 0; i < cnt; i++) results[i] = names[i];
						return (Int64)names.Length;
					}
					case Op.Check:
						return (Int64)dt.ColExist(arguments[1].GetStrValue(exm));
					case Op.Remove:
						return dt.RemoveCol(arguments[1].GetStrValue(exm)) ? 1L : 0L;
				}
				return 0;
			}
		}

		/// <summary>Handles DT_ROW_REMOVE, DT_GET, DT_GETINT, DT_SET, DT_SETINT, DT_FIND, DT_SORT, DT_TOCSV, DT_TOXML.</summary>
		private sealed class DtRowOpMethod : FunctionMethod
		{
			public enum Op { Remove, GetStr, GetInt, SetStr, SetInt, Find, Sort, ToCsv, ToXml }
			readonly Op op;
			public DtRowOpMethod(Op op)
			{
				this.op = op;
				ReturnType = (op == Op.GetStr || op == Op.ToCsv || op == Op.ToXml)
					? typeof(string) : typeof(Int64);
				switch (op)
				{
					case Op.Remove:
						argumentTypeArray = new Type[] { typeof(string), typeof(Int64) };
						break;
					case Op.GetStr:
					case Op.GetInt:
						argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(string) };
						break;
					case Op.SetStr:
						argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(string), typeof(string) };
						break;
					case Op.SetInt:
						argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(string), typeof(Int64) };
						break;
					case Op.Find:
						// DT_FIND(name, colName, value) — all strings
						argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) };
						break;
					case Op.Sort:
						argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(Int64) };
						break;
					default: // ToCsv, ToXml
						argumentTypeArray = new Type[] { typeof(string) };
						break;
				}
				CanRestructure = false;
			}

			EraDataTable GetTable(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
				EraDataTable dt;
				exm.VEvaluator.VariableData.DataDataTables.TryGetValue(name, out dt);
				return dt;
			}

			public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				EraDataTable dt = GetTable(exm, arguments);
				if (dt == null) return -1L;
				switch (op)
				{
					case Op.Remove:
						return dt.RemoveRow((int)arguments[1].GetIntValue(exm)) ? 1L : 0L;
					case Op.GetInt:
						return dt.GetInt((int)arguments[1].GetIntValue(exm), arguments[2].GetStrValue(exm));
					case Op.SetStr:
						dt.SetStr((int)arguments[1].GetIntValue(exm), arguments[2].GetStrValue(exm), arguments[3].GetStrValue(exm));
						return 0;
					case Op.SetInt:
						dt.SetInt((int)arguments[1].GetIntValue(exm), arguments[2].GetStrValue(exm), arguments[3].GetIntValue(exm));
						return 0;
					case Op.Find:
						return (Int64)dt.Find(arguments[1].GetStrValue(exm), arguments[2].GetStrValue(exm));
					case Op.Sort:
						dt.Sort(arguments[1].GetStrValue(exm), arguments[2].GetIntValue(exm) != 0);
						return 0;
				}
				return 0;
			}

			public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
			{
				EraDataTable dt = GetTable(exm, arguments);
				if (dt == null) return "";
				switch (op)
				{
					case Op.GetStr:
						return dt.GetStr((int)arguments[1].GetIntValue(exm), arguments[2].GetStrValue(exm));
					case Op.ToCsv:
						return dt.ToCsv();
					case Op.ToXml:
						return dt.ToXml();
				}
				return "";
			}
		}

	#endregion

	#region ERDNAME

	/// <summary>
	/// ERDNAME(varname, index) — returns the keyword name for an integer index in a named variable's keyword dictionary.
	/// varname is a string like "ABL" or "ABLNAME"; index is the integer value to look up.
	/// Equivalent to: ABLNAME:index (but works for any variable name at runtime).
	/// </summary>
	private sealed class ErdNameMethod : FunctionMethod
	{
		public ErdNameMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = new Type[] { typeof(string), typeof(Int64) };
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string varname = arguments[0].GetStrValue(exm);
			long index = arguments[1].GetIntValue(exm);
			if (exm.VEvaluator.Constant.TryIntegerToKeyword(out string ret, index, varname))
				return ret;
			return "";
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) => 0;
	}

	#endregion

	#region CLEARMEMORY / EXISTFILE / EXISTVAR / ENUMFILES / DT extensions

	private sealed class ClearMemoryMethod : FunctionMethod
	{
		public ClearMemoryMethod() { ReturnType = typeof(Int64); argumentTypeArray = new Type[0]; CanRestructure = false; }
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			long before = GC.GetTotalMemory(false);
			GC.Collect();
			long after = GC.GetTotalMemory(true);
			return Math.Max(0L, before - after);
		}
	}

	private sealed class ExistFileMethod : FunctionMethod
	{
		public ExistFileMethod() { ReturnType = typeof(Int64); argumentTypeArray = new Type[] { typeof(string) }; CanRestructure = false; }
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string rel = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(rel)) return 0;
			try
			{
				return new GameVirtualFileSystem(Program.ExeDir).Exists(rel) ? 1L : 0L;
			}
			catch (Exception)
			{
				return 0;
			}
		}
	}

	private sealed class ExistVarMethod : FunctionMethod
	{
		public ExistVarMethod() { ReturnType = typeof(Int64); argumentTypeArray = new Type[] { typeof(string) }; CanRestructure = false; }
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string varname = arguments[0].GetStrValue(exm);
			var token = GlobalStatic.IdentifierDictionary?.GetVariableToken(varname, null, true);
			if (token == null) return 0;
			long res = 0;
			if (token.IsInteger) res |= 1;
			if (token.IsString)  res |= 2;
			if (token.IsConst)   res |= 4;
			if (token.IsArray2D) res |= 8;
			if (token.IsArray3D) res |= 16;
			return res;
		}
	}

	private sealed class EnumFilesMethod : FunctionMethod
	{
		public EnumFilesMethod()
		{
			ReturnType = typeof(Int64);
			argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(Int64) };
			CanRestructure = false;
		}
		public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		{
			if (arguments.Length < 1)
				return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum1, name, 1);
			if (arguments.Length > 3)
				return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNum2, name);
			Type[] types = new Type[] { typeof(string), typeof(string), typeof(Int64) };
			for (int i = 0; i < arguments.Length; i++)
			{
				if (arguments[i] == null)
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentNotNullable0, name, i + 1);
				if (types[i] != arguments[i].GetOperandType())
					return string.Format(Properties.Resources.SyntaxErrMesMethodDefaultArgumentType0, name, i + 1);
			}
			return null;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string dir = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(dir)) return -1;
			string pattern = arguments.Length > 1 ? arguments[1].GetStrValue(exm) : "*";
			bool recursive = arguments.Length > 2 && arguments[2].GetIntValue(exm) != 0;
			try
			{
				GameVirtualFileSystem vfs = new GameVirtualFileSystem(Program.ExeDir);
				string[] files = vfs.EnumerateFiles(dir, pattern, recursive);
				string[] results = exm.VEvaluator.VariableData.DataStringArray[(int)(VariableCode.RESULTS & VariableCode.__LOWERCASE__)];
				int cnt = Math.Min(files.Length, results.Length);
				Array.Copy(files, results, cnt);
				return files.Length;
			}
			catch (Exception)
			{
				return -1;
			}
		}
	}

	private sealed class DtRowLengthMethod : FunctionMethod
	{
		public DtRowLengthMethod() { ReturnType = typeof(Int64); argumentTypeArray = new Type[] { typeof(string) }; CanRestructure = false; }
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
			if (!exm.VEvaluator.VariableData.DataDataTables.TryGetValue(name, out EraDataTable dt)) return -1;
			return dt.RowCount;
		}
	}

	private sealed class DtCellGetMethod : FunctionMethod
	{
		public enum Op { GetInt, GetStr, IsNull }
		readonly Op op;
		public DtCellGetMethod(Op op)
		{
			this.op = op;
			ReturnType = op == Op.GetStr ? typeof(string) : typeof(Int64);
			argumentTypeArray = new Type[] { typeof(string), typeof(Int64), typeof(string) };
			CanRestructure = false;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
			if (!exm.VEvaluator.VariableData.DataDataTables.TryGetValue(name, out EraDataTable dt))
				return op == Op.IsNull ? -2L : 0L;
			int idx = (int)arguments[1].GetIntValue(exm);
			string col = arguments[2].GetStrValue(exm);
			if (idx < 0 || idx >= dt.RowCount) return op == Op.IsNull ? -1L : 0L;
			if (dt.ColExist(col) == 0) return op == Op.IsNull ? -1L : 0L;
			if (op == Op.IsNull) return string.IsNullOrEmpty(dt.GetStr(idx, col)) ? 1L : 0L;
			return dt.GetInt(idx, col);
		}
		public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
			if (!exm.VEvaluator.VariableData.DataDataTables.TryGetValue(name, out EraDataTable dt)) return "";
			int idx = (int)arguments[1].GetIntValue(exm);
			string col = arguments[2].GetStrValue(exm);
			if (idx < 0 || idx >= dt.RowCount || dt.ColExist(col) == 0) return "";
			return dt.GetStr(idx, col);
		}
	}

	private sealed class DtSelectMethod : FunctionMethod
	{
		public DtSelectMethod() { ReturnType = typeof(Int64); argumentTypeArray = new Type[] { typeof(string), typeof(string), typeof(string) }; CanRestructure = false; }
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string name = arguments[0].GetStrValue(exm).ToUpperInvariant();
			if (!exm.VEvaluator.VariableData.DataDataTables.TryGetValue(name, out EraDataTable dt)) return 0;
			string col = arguments[1].GetStrValue(exm);
			string val = arguments[2].GetStrValue(exm);
			var matches = new List<int>();
			for (int i = 0; i < dt.RowCount; i++)
				if (string.Equals(dt.GetStr(i, col), val, StringComparison.OrdinalIgnoreCase))
					matches.Add(i);
			Int64[] result = exm.VEvaluator.VariableData.DataIntegerArray[(int)(VariableCode.RESULT & VariableCode.__LOWERCASE__)];
			int cnt = Math.Min(matches.Count, result.Length);
			for (int i = 0; i < cnt; i++) result[i] = matches[i];
			return matches.Count;
		}
	}

	private sealed class GetDoingFunctionMethod : FunctionMethod
	{
		public GetDoingFunctionMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = new Type[0];
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			var line = exm.Process.getCurrentLine;
			if (line?.ParentLabelLine == null) return "";
			return line.ParentLabelLine.LabelName;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) => 0;
	}

	private sealed class HtmlStringLenMethod : FunctionMethod
	{
		public HtmlStringLenMethod()
		{
			ReturnType = typeof(Int64);
			argumentTypeArray = new Type[] { typeof(string), typeof(Int64) };
			CanRestructure = false;
		}
		public override string CheckArgumentType(string name, IOperandTerm[] arguments)
		{
			if (arguments.Length < 1 || arguments.Length > 2)
				return name + GameMessages.T(" function: wrong number of arguments");
			if (arguments[0] == null || arguments[0].GetOperandType() != typeof(string))
				return name + GameMessages.T(" function: argument #1 must be a string");
			if (arguments.Length == 2 && arguments[1] != null && arguments[1].GetOperandType() != typeof(Int64))
				return name + GameMessages.T(" function: argument #2 must be an integer");
			return null;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string html = arguments[0].GetStrValue(exm);
			int len = HtmlManager.HtmlLength(html);
			bool rawPx = arguments.Length >= 2 && arguments[1] != null && arguments[1].GetIntValue(exm) != 0;
			if (rawPx) return len;
			int fs = Config.FontSize;
			if (fs <= 0) return 0;
			int half = fs / 2;
			if (half <= 0) return len;
			long result = len / half;
			if (len % half != 0) result += (len >= 0 ? 1 : -1);
			return result;
		}
	}

	private sealed class GetVarMethod : FunctionMethod
	{
		public GetVarMethod()
		{
			ReturnType = typeof(Int64);
			argumentTypeArray = new Type[] { typeof(string) };
			CanRestructure = false;
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string expr = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(expr)) return 0;
			try
			{
				var st = new StringStream(expr);
				var wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
				var term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
				if (term == null || term.GetOperandType() != typeof(Int64)) return 0;
				return term.GetIntValue(exm);
			}
			catch { return 0; }
		}
	}

	private sealed class GetVarsMethod : FunctionMethod
	{
		public GetVarsMethod()
		{
			ReturnType = typeof(string);
			argumentTypeArray = new Type[] { typeof(string) };
			CanRestructure = false;
		}
		public override string GetStrValue(ExpressionMediator exm, IOperandTerm[] arguments)
		{
			string expr = arguments[0].GetStrValue(exm);
			if (string.IsNullOrEmpty(expr)) return "";
			try
			{
				var st = new StringStream(expr);
				var wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.None);
				var term = ExpressionParser.ReduceExpressionTerm(wc, TermEndWith.EoL);
				if (term == null || term.GetOperandType() != typeof(string)) return "";
				return term.GetStrValue(exm);
			}
			catch { return ""; }
		}
		public override Int64 GetIntValue(ExpressionMediator exm, IOperandTerm[] arguments) => 0;
	}

	#endregion
}
}
