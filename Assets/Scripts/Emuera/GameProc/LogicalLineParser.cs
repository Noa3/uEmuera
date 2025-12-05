using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameProc.Function;

namespace MinorShift.Emuera.GameProc
{
	internal static class LogicalLineParser
	{
		public static bool ParseSharpLine(FunctionLabelLine label, StringStream st, ScriptPosition position, List<string> OnlyLabel)
		{
			st.ShiftNext();//'#'を飛ばす
			string token = LexicalAnalyzer.ReadSingleIdentifier(st);//#～自体にはマクロ非適用
			if (Config.ICFunction)
				token = token.ToUpper();
            // 先に存在しない#～は弾いてしまう
            if (token == null || (token != "SINGLE" && token != "LATER" && token != "PRI" && token != "ONLY" && token != "FUNCTION" && token != "FUNCTIONS" 
                && token != "LOCALSIZE" && token != "LOCALSSIZE" && token != "DIM" && token != "DIMS"))
            {
                ParserMediator.Warn(LocalizationHelper.GetMessageString("Parse_InvalidSharpLine"), position, 1);
                return false;
            }
			try
			{
				WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
				switch (token)
				{
					case "SINGLE":
						if (label.IsMethod)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Single_NotAllowed_Method"), position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Single_NotAllowed_NonEvent"), position, 1);
							break;
						}
						else if (label.IsSingle)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Single_Duplicated"), position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Single_Ignored_OnOnly"), position, 1);
							break;
						}
						label.IsSingle = true;
						break;
					case "LATER":
						if (label.IsMethod)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Later_NotAllowed_Method"), position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Later_NotAllowed_NonEvent"), position, 1);
							break;
						}
						else if (label.IsLater)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Later_Duplicated"), position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Later_Ignored_OnOnly"), position, 1);
							break;
						}
						else if (label.IsPri)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_PriLater_Duplicated"), position, 1);
						}
						label.IsLater = true;
						break;
					case "PRI":
						if (label.IsMethod)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Pri_NotAllowed_Method"), position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Pri_NotAllowed_NonEvent"), position, 1);
							break;
						}
						else if (label.IsPri)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Pri_Duplicated"), position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Pri_Ignored_OnOnly"), position, 1);
							break;
						}
						else if (label.IsLater)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_PriLater_Duplicated"), position, 1);
						}
						label.IsPri = true;
						break;
					case "ONLY":
						if (label.IsMethod)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Only_NotAllowed_Method"), position, 1);
							break;
						}
						else if (!label.IsEvent)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Only_NotAllowed_NonEvent"), position, 1);
							break;
						}
						else if (label.IsOnly)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Only_Duplicated"), position, 1);
							break;
						}
						else if (OnlyLabel.Contains(label.LabelName))
						{
							ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_Only_AlreadyDeclared"), label.LabelName), position, 1);
						}
						OnlyLabel.Add(label.LabelName);
						label.IsOnly = true;
						if (label.IsPri)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Pri_Ignored_OnOnly"), position, 1);
							label.IsPri = false;
						}
						if (label.IsLater)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Later_Ignored_OnOnly"), position, 1);
							label.IsLater = false;
						}
						if (label.IsSingle)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Single_Ignored_OnOnly"), position, 1);
							label.IsSingle = false;
						}
						break;
					case "FUNCTION":
					case "FUNCTIONS":
						if (!string.IsNullOrEmpty(label.LabelName) && char.IsDigit(label.LabelName[0]))
						{
							ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_Function_NotAllowed_DigitStart"), token), position, 1);
							label.IsError = true;
							label.ErrMes = LocalizationHelper.GetMessageString("Sharp_Function_Error_DigitStart");
							break;
						}
						if (label.IsMethod)
						{
							if ((label.MethodType == typeof(Int64) && token == "FUNCTION") || (label.MethodType == typeof(string) && token == "FUNCTIONS"))
							{
								ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_Function_AlreadyDeclared_Ignore"), label.LabelName, token), position, 1);
								return false;
							}
							if (label.MethodType == typeof(Int64) && token == "FUNCTIONS")
							{
								ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_Function_AlreadyDeclared_Function"), label.LabelName), position, 2);
							}
							else if (label.MethodType == typeof(string) && token == "FUNCTION")
							{
								ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_Function_AlreadyDeclared_FunctionS"), label.LabelName), position, 2);
							}
							return false;
						}
						if (label.Depth == 0)
						{
							ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_Function_NotAllowed_System"), token), position, 2);
							return false;
						}
						label.IsMethod = true;
						label.Depth = 0;
						label.MethodType = (token == "FUNCTIONS") ? typeof(string) : typeof(Int64);
						if (label.IsPri)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Pri_NotAllowed_Method"), position, 1);
							label.IsPri = false;
						}
						if (label.IsLater)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Later_NotAllowed_Method"), position, 1);
							label.IsLater = false;
						}
						if (label.IsSingle)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Single_NotAllowed_Method"), position, 1);
							label.IsSingle = false;
						}
						if (label.IsOnly)
						{
							ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_Only_NotAllowed_Method"), position, 1);
							label.IsOnly = false;
						}
						break;
					case "LOCALSIZE":
					case "LOCALSSIZE":
						{
							if (wc.EOL)
							{
								ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_LocalSize_MissingValue"), token), position, 2);
								break;
							}
                            // イベント関数では指定しても無視される
                            if (label.IsEvent)
                            {
                                ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_LocalSize_Ignored_OnEvent"), token, token.Substring(0, token.Length - 4)), position, 1);
                                break;
                            }
							IOperandTerm arg = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL);
                            if ((!(arg.Restructure(null) is SingleTerm sizeTerm)) || (sizeTerm.GetOperandType() != typeof(Int64)))
                            {
                                ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_LocalSize_InvalidConst"), token), position, 2);
                                break;
                            }
                            if (sizeTerm.Int <= 0)
							{
								ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_LocalSize_ValueLEZero"), token, sizeTerm.Int.ToString()), position, 1);
								break;
							}
							if (sizeTerm.Int >= Int32.MaxValue)
							{
								ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_LocalSize_ValueTooLarge"), token, sizeTerm.Int.ToString()), position, 1);
								break;
							}
							int size = (int)sizeTerm.Int;
							if (token == "LOCALSIZE")
							{
								if (GlobalStatic.IdentifierDictionary.getLocalIsForbid("LOCAL"))
								{
									ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_LocalSize_Local_Forbidden"), token), position, 2);
									break;
								}
								if (label.LocalLength > 0)
								{
									ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_LocalSize_Local_AlreadyDefined"), position, 1);
								}
								label.LocalLength = size;
							}
							else
							{
								if (GlobalStatic.IdentifierDictionary.getLocalIsForbid("LOCALS"))
								{
									ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_LocalSize_Locals_Forbidden"), token), position, 2);
									break;
								}
								if (label.LocalsLength > 0)
								{
									ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_LocalSize_Locals_AlreadyDefined"), position, 1);
								}
								label.LocalsLength = size;
							}
						}
						break;
					case "DIM":
					case "DIMS":
						{
							UserDefinedVariableData data = UserDefinedVariableData.Create(wc, token == "DIMS", true, position);
							if (!label.AddPrivateVariable(data))
							{
								ParserMediator.Warn(string.Format(LocalizationHelper.GetMessageString("Sharp_Dim_VarAlreadyUsed"), data.Name), position, 2);
								return false;
							}
							break;
						}
					default:
						ParserMediator.Warn(LocalizationHelper.GetMessageString("Parse_InvalidSharpLine"), position, 1);
						break;
				}
				if (!wc.EOL)
				{
					ParserMediator.Warn(LocalizationHelper.GetMessageString("Sharp_ExtraCharsAfterIdentifier"), position, 1);
				}
			}
			catch (Exception e)
			{
				ParserMediator.Warn(e.Message, position, 2);
				goto err;
			}
			return true;
		err:
			return false;
		}
		
		public static LogicalLine ParseLine(string str, EmueraConsole console)
		{
			ScriptPosition position = new ScriptPosition();
			StringStream stream = new StringStream(str);
			return ParseLine(stream, position, console);
		}

		public static LogicalLine ParseLabelLine(StringStream stream, ScriptPosition position, EmueraConsole console)
		{
			bool isFunction = (stream.Current == '@');
			string labelName = "";
			string errMes = "";
			try
			{
				int warnLevel = -1;
                stream.ShiftNext();//@か$を除去
				WordCollection wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
				if (wc.EOL || !(wc.Current is IdentifierWord))
				{
					errMes = LocalizationHelper.GetMessageString("Label_InvalidOrMissingName");
					goto err;
				}
				labelName = ((IdentifierWord)wc.Current).Code;
				wc.ShiftNext();
				if (Config.ICVariable)
					labelName = labelName.ToUpper();
				GlobalStatic.IdentifierDictionary.CheckUserLabelName(ref errMes, ref warnLevel, isFunction, labelName);
				if (warnLevel >= 0)
				{
					if (warnLevel >= 2)
						goto err;
					ParserMediator.Warn(errMes, position, warnLevel);
				}
				if (!isFunction)//$ならこの時点で終了
				{
					if (!wc.EOL)
					{
						ParserMediator.Warn(LocalizationHelper.GetMessageString("Label_ArgumentProvidedForGoto"), position, 1);
					}
					return new GotoLabelLine(position, labelName);
				}

				if (Program.AnalysisMode)
					console.PrintC("@" + labelName, false);
				FunctionLabelLine funclabelLine = new FunctionLabelLine(position, labelName, wc);
				if (IdentifierDictionary.IsEventLabelName(labelName))
				{
					funclabelLine.IsEvent = true;
					funclabelLine.IsSystem = true;
					funclabelLine.Depth = 0;
				}
				else if (IdentifierDictionary.IsSystemLabelName(labelName))
				{
					funclabelLine.IsSystem = true;
					funclabelLine.Depth = 0;
				}
				return funclabelLine;
			}
			catch (CodeEE e)
			{
				errMes = e.Message;
			}
		err:
			uEmuera.Media.SystemSounds.Hand.Play();
			if (isFunction)
			{
				if(labelName.Length == 0)
					labelName = "<Error>";
				return new InvalidLabelLine(position, labelName, errMes);
			}
			return new InvalidLine(position, errMes);
		}
		
		public static LogicalLine ParseLine(StringStream stream, ScriptPosition position, EmueraConsole console)
		{
			string errMes;
			LexicalAnalyzer.SkipWhiteSpace(stream);//先頭のホワイトスペースを読み飛ばす
			if (stream.EOS)
				return null;
			try
			{
				// 前置インクリメント、デクリメント行
				if (stream.Current == '+' || stream.Current == '-')
				{
					char op = stream.Current;
					WordCollection wc = LexicalAnalyzer.Analyse(stream, LexEndWith.EoL, LexAnalyzeFlag.None);
                    if ((!(wc.Current is OperatorWord opWT)) || ((opWT.Code != OperatorCode.Increment) && (opWT.Code != OperatorCode.Decrement)))
                    {
                        errMes = (op == '+') ? LocalizationHelper.GetMessageString("Line_StartsWithPlus_NotIncrement") : LocalizationHelper.GetMessageString("Line_StartsWithMinus_NotDecrement");
                        ParserMediator.Warn(errMes + " (" + LocalizationHelper.GetMessageString("Line_Content") + ": " + stream.ToString() + ")", position, 1);
                        return new NullLine();
                    }
                    wc.ShiftNext();
					return new InstructionLine(position, FunctionIdentifier.SETFunction, opWT.Code, wc, null);
				}
				IdentifierWord idWT = LexicalAnalyzer.ReadFirstIdentifierWord(stream);
				if (idWT != null)
				{
					FunctionIdentifier func = GlobalStatic.IdentifierDictionary.GetFunctionIdentifier(idWT.Code);
					if (func != null)
					{
						if (stream.EOS)
							return new InstructionLine(position, func, stream);
						if ((stream.Current != ';') && (stream.Current != ' ') && (stream.Current != '\t') && (!Config.SystemAllowFullSpace || (stream.Current != '　')))
						{
							if (stream.Current == '　')
							{
								errMes = string.Format(LocalizationHelper.GetMessageString("Command_InvalidCharAfterCommand_FullWidth"), Config.GetConfigName(ConfigCode.SystemAllowFullSpace));
							}
							else
							{
								errMes = LocalizationHelper.GetMessageString("Command_InvalidCharAfterCommand");
							}
							ParserMediator.Warn(errMes + " (" + LocalizationHelper.GetMessageString("Line_Content") + ": " + stream.ToString() + ", " + LocalizationHelper.GetMessageString("Following_Char") + ": '" + stream.Current + "' [0x" + ((int)stream.Current).ToString("X4") + "])", position, 1);
                            return new NullLine();
						}
						stream.ShiftNext();
						return new InstructionLine(position, func, stream);
					}
				}
				LexicalAnalyzer.SkipWhiteSpace(stream);
				if (stream.EOS)
				{
					errMes = LocalizationHelper.GetMessageString("Line_CannotInterpret");
					ParserMediator.Warn(errMes + " (" + LocalizationHelper.GetMessageString("EmptyOrNoContentAfterParse") + " " + LocalizationHelper.GetMessageString("Original_Line_Content") + ": " + stream.ToString() + ")", position, 1);
                    return new NullLine();
				}
				stream.Seek(0, System.IO.SeekOrigin.Begin);
				OperatorCode assignOP = OperatorCode.NULL;
				WordCollection wc1 = LexicalAnalyzer.Analyse(stream, LexEndWith.Operator, LexAnalyzeFlag.None);
				try
				{
					assignOP = LexicalAnalyzer.ReadAssignmentOperator(stream);
				}
				catch(CodeEE)
				{
					errMes = LocalizationHelper.GetMessageString("Line_CannotInterpret");
					ParserMediator.Warn(errMes + " (" + LocalizationHelper.GetMessageString("AssignmentOperator_NotFound") + " " + LocalizationHelper.GetMessageString("Line_Content") + ": " + stream.ToString() + ")", position, 1);
                    return new NullLine();
				}

				if (assignOP == OperatorCode.Equal)
				{
					if (console != null)
					{
						ParserMediator.Warn(LocalizationHelper.GetMessageString("Assignment_UsesDoubleEqual"), position, 0);
					}
					assignOP = OperatorCode.Assignment;
				}
				return new InstructionLine(position, FunctionIdentifier.SETFunction, assignOP, wc1, stream);
			}
			catch (CodeEE e)
			{
				uEmuera.Media.SystemSounds.Hand.Play();
				ParserMediator.Warn(LocalizationHelper.GetMessageString("Error_OccurredDuringParsing") + ": " + e.Message + " (" + LocalizationHelper.GetMessageString("Line_Content") + ": " + stream.ToString() + ")", position, 1);
                return new NullLine();
			}
		}
	}
}
