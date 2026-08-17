using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData;
using MinorShift._Library;
using MinorShift.Emuera.GameData.Function;
//using System.Drawing;
using System.IO;
using uEmuera.Drawing;

namespace MinorShift.Emuera.GameProc.Function
{
	internal sealed partial class FunctionIdentifier
	{
		#region normalFunction
		private sealed class PRINT_Instruction : AbstractInstruction
		{
			public PRINT_Instruction(string name)
			{
			//PRINT(|V|S|FORM|FORMS)(|K)(|D)(|L|W) and these
			//PRINTSINGLE(|V|S|FORM|FORMS)(|K)(|D) and these
			//PRINT(|FORM)(C|LC)(|K)(|D) and these
			//PRINTDATA(|K)(|D)(|L|W) <- handled in a separate class
				flag = IS_PRINT;
				StringStream st = new StringStream(name);
				st.Jump(5);//PRINT
				if (st.CurrentEqualTo("SINGLE"))
				{
					flag |= PRINT_SINGLE | EXTENDED;
					st.Jump(6);
				}

				if (st.CurrentEqualTo("V"))
				{
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_PRINTV);
					isPrintV = true;
					st.Jump(1);
				}
				else if (st.CurrentEqualTo("S"))
				{
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
					st.Jump(1);
				}
				else if (st.CurrentEqualTo("FORMS"))
				{
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
					isForms = true;
					st.Jump(5);
				}
				else if (st.CurrentEqualTo("FORM"))
				{
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
					st.Jump(4);
				}
				else
				{
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_NULLABLE);
				}
				if (st.CurrentEqualTo("LC"))
				{
					flag |= EXTENDED;
					isLC = true;
					st.Jump(2);
				}
				else if (st.CurrentEqualTo("C"))
				{
					if (name == "PRINTFORMC")
						flag |= EXTENDED;
					isC = true;
					st.Jump(1);
				}
				if (st.CurrentEqualTo("K"))
				{
					flag |= ISPRINTKFUNC | EXTENDED;
					st.Jump(1);
				}
				if (st.CurrentEqualTo("D"))
				{
					flag |= ISPRINTDFUNC | EXTENDED;
					st.Jump(1);
				}
				if (st.CurrentEqualTo("L"))
				{
					flag |= PRINT_NEWLINE;
					flag |= METHOD_SAFE;
					st.Jump(1);
				}
				else if (st.CurrentEqualTo("W"))
				{
					flag |= PRINT_NEWLINE | PRINT_WAITINPUT;
					st.Jump(1);
				}
				else
				{
					flag |= METHOD_SAFE;
				}
				if ((ArgBuilder == null) || (!st.EOS))
					throw new ExeEE(GameMessages.T("PRINT instruction error"));
			}

			readonly bool isPrintV;
			readonly bool isLC;
			readonly bool isC;
			readonly bool isForms;
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
                if (GlobalStatic.Process.SkipPrint)
                    return;
				exm.Console.UseUserStyle = true;
				exm.Console.UseSetColorStyle = !func.Function.IsPrintDFunction();
				string str;
				if (func.Argument.IsConst)
					str = func.Argument.ConstStr;
				else if (isPrintV)
				{
					StringBuilder builder = new StringBuilder();
					IOperandTerm[] terms = ((SpPrintVArgument)func.Argument).Terms;
					foreach (IOperandTerm termV in terms)
					{
						if (termV.GetOperandType() == typeof(Int64))
							builder.Append(termV.GetIntValue(exm).ToString());
						else
							builder.Append(termV.GetStrValue(exm));
					}
					str = builder.ToString();
				}
				else
				{
					str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
					if (isForms)
					{
						str = exm.CheckEscape(str);
						StrFormWord wt = LexicalAnalyzer.AnalyseFormattedString(new StringStream(str), FormStrEndWith.EoL, false);
						StrForm strForm = StrForm.FromWordToken(wt);
						str = strForm.GetString(exm);
					}
				}
				if (func.Function.IsPrintKFunction())
					str = exm.ConvertStringType(str);
				if (isC)
					exm.Console.PrintC(str, true);
				else if (isLC)
					exm.Console.PrintC(str, false);
				else
					exm.OutputToConsole(str, func.Function);
				exm.Console.UseSetColorStyle = true;
			}
		}

		private sealed class PRINT_DATA_Instruction : AbstractInstruction
		{
			public PRINT_DATA_Instruction(string name)
			{
				//PRINTDATA(|K)(|D)(|L|W)
				flag = EXTENDED | IS_PRINT | IS_PRINTDATA | PARTIAL;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VAR_INT);
				StringStream st = new StringStream(name);
				st.Jump(9);//PRINTDATA
				if (st.CurrentEqualTo("K"))
				{
					flag |= ISPRINTKFUNC | EXTENDED;
					st.Jump(1);
				}
				if (st.CurrentEqualTo("D"))
				{
					flag |= ISPRINTDFUNC | EXTENDED;
					st.Jump(1);
				}
				if (st.CurrentEqualTo("L"))
				{
					flag |= PRINT_NEWLINE;
					flag |= METHOD_SAFE;
					st.Jump(1);
				}
				else if (st.CurrentEqualTo("W"))
				{
					flag |= PRINT_NEWLINE | PRINT_WAITINPUT;
					st.Jump(1);
				}
				else
				{
					flag |= METHOD_SAFE;
				}
				if ((ArgBuilder == null) || (!st.EOS))
					throw new ExeEE(GameMessages.T("PRINTDATA instruction error"));
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
                if (GlobalStatic.Process.SkipPrint)
                    return;
                exm.Console.UseUserStyle = true;
				exm.Console.UseSetColorStyle = !func.Function.IsPrintDFunction();
				// If display data is empty, skip without doing anything
				if (func.dataList.Count == 0)
				{
					state.JumpTo(func.JumpTo);
					return;
				}
				int count = func.dataList.Count;
				int choice = (int)exm.VEvaluator.GetNextRand(count);
				VariableTerm iTerm = ((PrintDataArgument)func.Argument).Var;
				if (iTerm != null)
				{
					iTerm.SetValue(choice, exm);
				}
				List<InstructionLine> iList = func.dataList[choice];
				int i = 0;
				IOperandTerm term;
				string str;
				foreach (InstructionLine selectedLine in iList)
				{
					state.CurrentLine = selectedLine;
					if (selectedLine.Argument == null)
						ArgumentParser.SetArgumentTo(selectedLine);
					term = ((ExpressionArgument)selectedLine.Argument).Term;
					str = term.GetStrValue(exm);
					if (func.Function.IsPrintKFunction())
						str = exm.ConvertStringType(str);
					exm.Console.Print(str);
					if (++i < (int)iList.Count)
						exm.Console.NewLine();
				}
				if (func.Function.IsNewLine() || func.Function.IsWaitInput())
				{
					exm.Console.NewLine();
					if (func.Function.IsWaitInput())
						exm.Console.ReadAnyKey();
				}
				exm.Console.UseSetColorStyle = true;
				// Jump, but ensure the flow is continuous.
				state.JumpTo(func.JumpTo);
				//state.RunningLine = null;
			}
		}
		
		private sealed class HTML_PRINT_Instruction : AbstractInstruction
		{
			public HTML_PRINT_Instruction()
			{
				flag = EXTENDED | METHOD_SAFE;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTML_PRINT);
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
                if (GlobalStatic.Process.SkipPrint)
                    return;
                var arg = (SpHtmlPrintArgument)func.Argument;
                string str = arg.HtmlStr.GetStrValue(exm);
                bool toPrintBuffer = arg.OptTerm != null && arg.OptTerm.GetIntValue(exm) != 0;
                exm.Console.PrintHtml(str, toPrintBuffer);
			}
		}

		private sealed class HTML_TAGSPLIT_Instruction : AbstractInstruction
		{
			public HTML_TAGSPLIT_Instruction()
			{
				flag = EXTENDED | METHOD_SAFE;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTMLSPLIT);
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpHtmlSplitArgument spSplitArg = (SpHtmlSplitArgument)func.Argument;
				string str = spSplitArg.TargetStr.GetStrValue(exm);
				string[] strs = MinorShift.Emuera.GameView.HtmlManager.HtmlTagSplit(str);
				
				if (strs == null)
				{
					spSplitArg.Num.SetValue(-1, exm);
					return;
				}
				
				spSplitArg.Num.SetValue(strs.Length, exm);
				string[] output = (string[])spSplitArg.Var.GetArray();
				int outputlength = Math.Min(output.Length, strs.Length);
				Array.Copy(strs, output, outputlength);
			}
		}

		private sealed class HTML_PRINT_ISLAND_Instruction : AbstractInstruction
		{
			public HTML_PRINT_ISLAND_Instruction()
			{
				flag = EXTENDED | METHOD_SAFE;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_HTML_PRINT);
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				if (GlobalStatic.Process.SkipPrint) return;
				var arg = (SpHtmlPrintArgument)func.Argument;
				string str = arg.HtmlStr.GetStrValue(exm);
				exm.Console.PrintHTMLIsland(str);
			}
		}

		private sealed class HTML_PRINT_ISLAND_CLEAR_Instruction : AbstractInstruction
		{
			public HTML_PRINT_ISLAND_CLEAR_Instruction()
			{
				flag = EXTENDED | METHOD_SAFE;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.ClearHTMLIsland();
			}
		}
		
		
		private sealed class PRINT_IMG_Instruction : AbstractInstruction
		{
			public PRINT_IMG_Instruction()
			{
				flag = EXTENDED | METHOD_SAFE;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
                if (GlobalStatic.Process.SkipPrint)
                    return;
                string str;
				if (func.Argument.IsConst)
					str = func.Argument.ConstStr;
				else
					str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
				exm.Console.PrintImg(str);
			}
		}

		private sealed class PRINT_RECT_Instruction : AbstractInstruction
		{
			public PRINT_RECT_Instruction()
			{
				flag = EXTENDED | METHOD_SAFE;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
                if (GlobalStatic.Process.SkipPrint)
                    return;
                ExpressionArrayArgument intExpArg = (ExpressionArrayArgument)func.Argument;
				int[] param = new int[intExpArg.TermList.Length];
				for (int i = 0; i < intExpArg.TermList.Length; i++)
					param[i] = FunctionIdentifier.toUInt32inArg(intExpArg.TermList[i].GetIntValue(exm), "PRINT_RECT", i + 1);

				exm.Console.PrintShape("rect", param);
			}
		}

		private sealed class PRINT_SPACE_Instruction : AbstractInstruction
		{
			public PRINT_SPACE_Instruction()
			{
				flag = EXTENDED | METHOD_SAFE;
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
                if (GlobalStatic.Process.SkipPrint)
                    return;
                Int64 param;
				if (func.Argument.IsConst)
					param = func.Argument.ConstInt;
				else
					param = ((ExpressionArgument)func.Argument).Term.GetIntValue(exm);
				int param32 = FunctionIdentifier.toUInt32inArg(param, "PRINT_SPACE", 1);
				exm.Console.PrintShape("space", new int[] { param32 });
			}
		}

		private sealed class DEBUGPRINT_Instruction : AbstractInstruction
		{
			public DEBUGPRINT_Instruction(bool form, bool newline)
			{
				if (form)
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
				else
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_NULLABLE);
				flag = METHOD_SAFE | EXTENDED | DEBUG_FUNC;
				if (newline)
					flag |= PRINT_NEWLINE;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				string str;
				if (func.Argument.IsConst)
					str = func.Argument.ConstStr;
				else
					str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
				exm.Console.DebugPrint(str);
				if (func.Function.IsNewLine())
					exm.Console.DebugNewLine();
			}
		}

		private sealed class DEBUGCLEAR_Instruction : AbstractInstruction
		{
			public DEBUGCLEAR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED | DEBUG_FUNC;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.DebugClear();
			}
		}

		private sealed class METHOD_Instruction : AbstractInstruction
		{
			public METHOD_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.METHOD);
				flag = METHOD_SAFE | EXTENDED;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				IOperandTerm term = ((MethodArgument)func.Argument).MethodTerm;
				//Type type = term.GetOperandType();
				if (term.GetOperandType() == typeof(Int64))
					exm.VEvaluator.RESULT = term.GetIntValue(exm);
				else// if (func.Argument.MethodTerm.GetOperandType() == typeof(string))
					exm.VEvaluator.RESULTS = term.GetStrValue(exm);
				//these are the only types that currently exist
				//else
				//	throw new ExeEE(func.Function.Name + " instruction type is unknown");
			}
		}

		/// <summary>
		/// Assignment statement
		/// </summary>
		private sealed class SET_Instruction : AbstractInstruction
		{
			public SET_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SET);
				flag = METHOD_SAFE;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				if (func.Argument is SpSetArrayArgument)
				{
					SpSetArrayArgument arg = (SpSetArrayArgument)func.Argument;
					if (arg.VariableDest.IsInteger)
					{
						if (arg.IsConst)
							arg.VariableDest.SetValue(arg.ConstIntList, exm);
						else
						{
							Int64[] values = new Int64[arg.TermList.Length];
							for (int i = 0; i < values.Length; i++)
							{
								values[i] = arg.TermList[i].GetIntValue(exm);
							}
							arg.VariableDest.SetValue(values, exm);
						}
					}
					else
					{
						if (arg.IsConst)
							arg.VariableDest.SetValue(arg.ConstStrList, exm);
						else
						{
							string[] values = new string[arg.TermList.Length];
							for (int i = 0; i < values.Length; i++)
							{
								values[i] = arg.TermList[i].GetStrValue(exm);
							}
							arg.VariableDest.SetValue(values, exm);
						}
					}
					return;
				}
				SpSetArgument spsetarg = (SpSetArgument)func.Argument;
				if (spsetarg.VariableDest.IsInteger)
				{
					Int64 src = spsetarg.IsConst ? spsetarg.ConstInt : spsetarg.Term.GetIntValue(exm);
					if (spsetarg.AddConst)
						spsetarg.VariableDest.PlusValue(src, exm);
					else
						spsetarg.VariableDest.SetValue(src, exm);
				}
				else
				{
					string src = spsetarg.IsConst ? spsetarg.ConstStr : spsetarg.Term.GetStrValue(exm);
					spsetarg.VariableDest.SetValue(src, exm);
				}
			}
		}

		private sealed class REUSELASTLINE_Instruction : AbstractInstruction
		{
			public REUSELASTLINE_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
				flag = METHOD_SAFE | EXTENDED | IS_PRINT;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				IOperandTerm term = ((ExpressionArgument)func.Argument).Term;
				string str = term.GetStrValue(exm);
				exm.Console.PrintTemporaryLine(str);
			}
		}

		private sealed class CLEARLINE_Instruction : AbstractInstruction
		{
			public CLEARLINE_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED | IS_PRINT;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument intExpArg = (ExpressionArgument)func.Argument;
				Int32 delNum = (Int32)intExpArg.Term.GetIntValue(exm);
				exm.Console.deleteLine(delNum);
				exm.Console.RefreshStrings(false);
			}
		}

		private sealed class STRLEN_Instruction : AbstractInstruction
		{
			public STRLEN_Instruction(bool argisform, bool unicode)
			{
				if (argisform)
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_NULLABLE);
				else
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_NULLABLE);
				flag = METHOD_SAFE | EXTENDED;
				this.unicode = unicode;
			}
			bool unicode;
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				string str;
				if (func.Argument.IsConst)
					str = func.Argument.ConstStr;
				else
					str = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
				if (unicode)
					exm.VEvaluator.RESULT = str.Length;
				else
					exm.VEvaluator.RESULT = LangManager.GetStrlenLang(str);
			}
		}

		private sealed class SETBIT_Instruction : AbstractInstruction
		{
			public SETBIT_Instruction(int op)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.BIT_ARG);
				flag = METHOD_SAFE | EXTENDED;
				this.op = op;
			}
			int op;
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				BitArgument spsetarg = (BitArgument)func.Argument;
				VariableTerm varTerm = spsetarg.VariableDest;
				IOperandTerm[] terms = spsetarg.Term;
				for (int i = 0; i < terms.Length; i++)
				{
					Int64 x = terms[i].GetIntValue(exm);
					if ((x < 0) || (x > 63))
						throw new CodeEE(GameMessages.T("The 2nd argument exceeds the bit range (0 to 63)"));
					Int64 baseValue = varTerm.GetIntValue(exm);
					Int64 shift = 1L << (int)x;
					if (op == 1)
						baseValue |= shift;
					else if (op == 0)
						baseValue &= ~shift;
					else
						baseValue ^= shift;
					varTerm.SetValue(baseValue, exm);
				}
			}
		}

		private sealed class WAIT_Instruction : AbstractInstruction
		{
			public WAIT_Instruction(bool force)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = IS_PRINT;
				isForce = force;
			}
			bool isForce;
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				if (isForce)
					exm.Console.ReadAnyKey(false, true);
				else
					exm.Console.ReadAnyKey();
			}
		}

		private sealed class WAITANYKEY_Instruction : AbstractInstruction
		{
			public WAITANYKEY_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = IS_PRINT;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.ReadAnyKey(true,false);
			}
		}

		private sealed class TWAIT_Instruction : AbstractInstruction
		{
			public TWAIT_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
				flag = IS_PRINT | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.ReadAnyKey();
				SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
				Int64 time = arg.X.GetIntValue(exm);
				Int64 flag = arg.Y.GetIntValue(exm);
				InputRequest req = new InputRequest();
				req.InputType = InputType.EnterKey;
				if (flag != 0)
					req.InputType = InputType.Void;
				req.Timelimit = time;
				exm.Console.WaitInput(req);
			}
		}

		private sealed class INPUT_Instruction : AbstractInstruction
		{
			public INPUT_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
				flag = IS_PRINT | IS_INPUT;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				InputRequest req = new InputRequest();
				req.InputType = InputType.IntValue;
				if (arg.Term != null)
				{
					Int64 def;
					if (arg.IsConst)
						def = arg.ConstInt;
					else
						def = arg.Term.GetIntValue(exm);
					req.HasDefValue = true;
					req.DefIntValue = def;
				}
				exm.Console.WaitInput(req);
			}
		}
		private sealed class INPUTS_Instruction : AbstractInstruction
		{
			public INPUTS_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
				flag = IS_PRINT | IS_INPUT;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				InputRequest req = new InputRequest();
				req.InputType = InputType.StrValue;
				if (arg.Term != null)
				{
					string def;
					if (arg.IsConst)
						def = arg.ConstStr;
					else
						def = arg.Term.GetStrValue(exm);
					req.HasDefValue = true;
					req.DefStrValue = def;
				}
				exm.Console.WaitInput(req);
			}
		}

		private sealed class ONEINPUT_Instruction : AbstractInstruction
		{
			public ONEINPUT_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
				flag = IS_PRINT | IS_INPUT | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				InputRequest req = new InputRequest();
				req.InputType = InputType.IntValue;
				req.OneInput = true;
				if (arg.Term != null)
				{
					// Unify default value handling with ONETINPUT for consistency
					Int64 def;
					if (arg.IsConst)
						def = arg.ConstInt;
					else
						def = arg.Term.GetIntValue(exm);
					// Extract first digit from multi-digit numbers (simpler and more reliable than Log10)
					while (def >= 10)
						def /= 10;
					if (def >= 0)
					{
						req.HasDefValue = true;
						req.DefIntValue = def;
					}
				}
				exm.Console.WaitInput(req);
			}
		}

		private sealed class ONEINPUTS_Instruction : AbstractInstruction
		{
			public ONEINPUTS_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
				flag = IS_PRINT | IS_INPUT | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				InputRequest req = new InputRequest();
				req.InputType = InputType.StrValue;
				req.OneInput = true;
				if (arg.Term != null)
				{
					string def;
					if (arg.IsConst)
						def = arg.ConstStr;
					else
						def = arg.Term.GetStrValue(exm);
					if (def.Length > 1)
						def = def.Remove(1);
					if (def.Length > 0)
					{
						req.HasDefValue = true;
						req.DefStrValue = def;
					}
				}
				exm.Console.WaitInput(req);
			}
		}

		private sealed class TINPUT_Instruction : AbstractInstruction
		{
			public TINPUT_Instruction(bool oneInput)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_TINPUT);
				flag = IS_PRINT | IS_INPUT | EXTENDED;
				this.isOne = oneInput;
			}
			bool isOne;
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpTInputsArgument tinputarg = (SpTInputsArgument)func.Argument;

				InputRequest req = new InputRequest();
				req.InputType = InputType.IntValue;
				req.HasDefValue = true;
				req.OneInput = isOne;
				Int64 x = tinputarg.Time.GetIntValue(exm);
				Int64 y = tinputarg.Def.GetIntValue(exm);
				// Unified default value handling with ONEINPUT
				if (isOne)
				{
					if (y < 0)
						y = Math.Abs(y);
					// Extract first digit from multi-digit numbers (simpler and more reliable than Log10)
					while (y >= 10)
						y /= 10;
				}
				Int64 z = (tinputarg.Disp != null) ? tinputarg.Disp.GetIntValue(exm) : 1;
				req.Timelimit = x;
				req.DefIntValue = y;
				req.DisplayTime = z != 0;
				req.TimeUpMes = (tinputarg.Timeout != null) ? tinputarg.Timeout.GetStrValue(exm) : Config.TimeupLabel;
				exm.Console.WaitInput(req);
			}
		}

		private sealed class TINPUTS_Instruction : AbstractInstruction
		{
			public TINPUTS_Instruction(bool oneInput)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_TINPUTS);
				flag = IS_PRINT | IS_INPUT | EXTENDED;
				this.isOne = oneInput;
			}
			bool isOne;
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpTInputsArgument tinputarg = (SpTInputsArgument)func.Argument;
				InputRequest req = new InputRequest();
				req.InputType = InputType.StrValue;
				req.HasDefValue = true;
				req.OneInput = isOne;
				Int64 x = tinputarg.Time.GetIntValue(exm);
				string strs = tinputarg.Def.GetStrValue(exm);
				if (isOne && strs.Length > 1)
					strs = strs.Remove(1);
				Int64 z = (tinputarg.Disp != null) ? tinputarg.Disp.GetIntValue(exm) : 1;
				req.Timelimit = x;
				req.DefStrValue = strs;
				req.DisplayTime = z != 0;
				req.TimeUpMes = (tinputarg.Timeout != null) ? tinputarg.Timeout.GetStrValue(exm) : Config.TimeupLabel;
				exm.Console.WaitInput(req);
			}
		}

		private sealed class CALLF_Instruction : AbstractInstruction
		{
			public CALLF_Instruction(bool form)
			{
				if (form)
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORMF);
				else
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLF);
				flag = EXTENDED | METHOD_SAFE | FORCE_SETARG;
			}

			public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
			{
				if (!func.Argument.IsConst)
				{
					useCallForm = true;
					return;
				}
				SpCallFArgment callfArg = (SpCallFArgment)func.Argument;
				if (Config.ICFunction)
					callfArg.ConstStr = callfArg.ConstStr.ToUpper();
				try
				{
					callfArg.FuncTerm = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, callfArg.ConstStr, callfArg.RowArgs, true);
				}
				catch (CodeEE e)
				{
					ParserMediator.Warn(e.Message, func, 2, true, false);
					return;
				}
				if (callfArg.FuncTerm == null)
				{
					if (!Program.AnalysisMode)
						ParserMediator.Warn(string.Format(GameMessages.T("The specified function name \"@{0}\" does not exist"), callfArg.ConstStr), func, 2, true, false);
					else
						ParserMediator.Warn(callfArg.ConstStr, func, 2, true, false);
					return;
				}
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				IOperandTerm mToken;
				string labelName;
				if ((!func.Argument.IsConst) || (exm.Console.RunERBFromMemory))
				{
					SpCallFArgment spCallformArg = (SpCallFArgment)func.Argument;
					labelName = spCallformArg.FuncnameTerm.GetStrValue(exm);
					mToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, labelName, spCallformArg.RowArgs, true);
				}
				else
				{
					labelName = func.Argument.ConstStr;
					mToken = ((SpCallFArgment)func.Argument).FuncTerm;
				}
				if (mToken == null)
					throw new CodeEE(string.Format(GameMessages.T("Inline function \"@{0}\" not found"), labelName));
				mToken.GetValue(exm);
			}
		}

		private sealed class BAR_Instruction : AbstractInstruction
		{
			public BAR_Instruction(bool newline)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_BAR);
				flag = IS_PRINT | METHOD_SAFE | EXTENDED;
				this.newline = newline;
			}
			bool newline;

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpBarArgument barArg = (SpBarArgument)func.Argument;
				Int64 var = barArg.Terms[0].GetIntValue(exm);
				Int64 max = barArg.Terms[1].GetIntValue(exm);
				Int64 length = barArg.Terms[2].GetIntValue(exm);
				exm.Console.Print(exm.CreateBar(var, max, length));
				if (newline)
					exm.Console.NewLine();
			}
		}
		
		private sealed class TIMES_Instruction : AbstractInstruction
		{
			public TIMES_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_TIMES);
				flag = METHOD_SAFE;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpTimesArgument timesArg = (SpTimesArgument)func.Argument;
				VariableTerm var = timesArg.VariableDest;
                if (Config.TimesNotRigorousCalculation)
                {
                    double d = (double)var.GetIntValue(exm) * timesArg.DoubleValue;
                    unchecked
                    {
                        var.SetValue((Int64)d, exm);
                    }
                }
                else
                {
                    decimal d = var.GetIntValue(exm) * (decimal)timesArg.DoubleValue;
                    unchecked
                    {
                        //decimal type forcibly throws OverFlowException, so we need a workaround
                        //In case of OverFlow, approximate the old behavior
                        if (d <= Int64.MaxValue && d >= Int64.MinValue)
                            var.SetValue((Int64)d, exm);
                        else
                            var.SetValue((Int64)((double)d), exm);
                    }
                }
			}
		}


		private sealed class ADDCHARA_Instruction : AbstractInstruction
		{
			public ADDCHARA_Instruction(bool flagSp, bool flagDel)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
				flag = METHOD_SAFE;
				isDel = flagDel;
				isSp = flagSp;
			}
			bool isDel;
			bool isSp;

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
			if(!Config.CompatiSPChara && isSp)
				throw new CodeEE(GameMessages.T("SP character features are not available by default (enable the compatibility option 'Use SP character')"));
				ExpressionArrayArgument intExpArg = (ExpressionArrayArgument)func.Argument;
				Int64 integer;
				Int64[] charaNoList = new Int64[intExpArg.TermList.Length];
				int i = 0;
				foreach (IOperandTerm int64Term in intExpArg.TermList)
				{
					integer = int64Term.GetIntValue(exm);
					if (isDel)
					{
						charaNoList[i] = integer;
						i++;
					}
					else
					{
						if(Config.CompatiSPChara)
							exm.VEvaluator.AddCharacter_UseSp(integer, isSp);
						else
							exm.VEvaluator.AddCharacter(integer);
					}
				}
				if (isDel)
				{
					if(charaNoList.Length == 1)
						exm.VEvaluator.DelCharacter(charaNoList[0]);
					else
						exm.VEvaluator.DelCharacter(charaNoList);
				}
			}
		}

		private sealed class ADDVOIDCHARA_Instruction : AbstractInstruction
		{
			public ADDVOIDCHARA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.VEvaluator.AddPseudoCharacter();
			}
		}

		private sealed class SWAPCHARA_Instruction : AbstractInstruction
		{
			public SWAPCHARA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
				long x = arg.X.GetIntValue(exm);
				long y = arg.Y.GetIntValue(exm);
				exm.VEvaluator.SwapChara(x, y);
			}
		}
		private sealed class COPYCHARA_Instruction : AbstractInstruction
		{
			public COPYCHARA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
				long x = arg.X.GetIntValue(exm);
				long y = arg.Y.GetIntValue(exm);
				exm.VEvaluator.CopyChara(x, y);
			}
		}

		private sealed class ADDCOPYCHARA_Instruction : AbstractInstruction
		{
			public ADDCOPYCHARA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
				flag = METHOD_SAFE;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArrayArgument intExpArg = (ExpressionArrayArgument)func.Argument;
				foreach (IOperandTerm int64Term in intExpArg.TermList)
					exm.VEvaluator.AddCopyChara(int64Term.GetIntValue(exm));
			}
		}

		private sealed class SORTCHARA_Instruction : AbstractInstruction
		{
			public SORTCHARA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SORTCHARA);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpSortcharaArgument spSortArg = (SpSortcharaArgument)func.Argument;
				Int64 elem = 0;
				VariableTerm sortKey = spSortArg.SortKey;
				if (sortKey.Identifier.IsArray1D)
					elem = sortKey.GetElementInt(1, exm);
				else if (sortKey.Identifier.IsArray2D)
				{
					elem = sortKey.GetElementInt(1, exm) << 32;
					elem += sortKey.GetElementInt(2, exm);
				}

				exm.VEvaluator.SortChara(sortKey.Identifier, elem, spSortArg.SortOrder, true);
			}
		}

		private sealed class RESETCOLOR_Instruction : AbstractInstruction
		{
			public RESETCOLOR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.SetStringStyle(Config.ForeColor);
			}
		}

		private sealed class RESETBGCOLOR_Instruction : AbstractInstruction
		{
			public RESETBGCOLOR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.SetBgColor(Config.BackColor);
			}
		}

		private sealed class FONTBOLD_Instruction : AbstractInstruction
		{
			public FONTBOLD_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.SetStringStyle(exm.Console.StringStyle.FontStyle | FontStyle.Bold);
			}
		}
		private sealed class FONTITALIC_Instruction : AbstractInstruction
		{
			public FONTITALIC_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.SetStringStyle(exm.Console.StringStyle.FontStyle | FontStyle.Italic);
			}
		}
		private sealed class FONTREGULAR_Instruction : AbstractInstruction
		{
			public FONTREGULAR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.Console.SetStringStyle(FontStyle.Regular);
			}
		}

		private sealed class VARSET_Instruction : AbstractInstruction
		{
			public VARSET_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_VAR_SET);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{

				SpVarSetArgument spvarsetarg = (SpVarSetArgument)func.Argument;
				VariableTerm var = spvarsetarg.VariableDest;
				FixedVariableTerm p = var.GetFixedVariableTerm(exm);
				int start = 0;
				int end = 0;
				// Fetch end first to change the evaluation logic
				if (spvarsetarg.End != null)
					end = (int)spvarsetarg.End.GetIntValue(exm);
				else if (var.Identifier.IsArray1D)
					end = (int)var.GetLength();
				if (spvarsetarg.Start != null)
				{
					start = (int)spvarsetarg.Start.GetIntValue(exm);
					if (start > end)
					{
						int temp = start;
						start = end;
						end = temp;
					}
				}
				if (var.IsString)
				{
					string src = spvarsetarg.Term.GetStrValue(exm);
					exm.VEvaluator.SetValueAll(p, src, start, end);
				}
				else
				{
					long src = spvarsetarg.Term.GetIntValue(exm);
					exm.VEvaluator.SetValueAll(p, src, start, end);
				}
			}
		}

		private sealed class CVARSET_Instruction : AbstractInstruction
		{
			public CVARSET_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CVAR_SET);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpCVarSetArgument spvarsetarg = (SpCVarSetArgument)func.Argument;
				FixedVariableTerm p = spvarsetarg.VariableDest.GetFixedVariableTerm(exm);
				SingleTerm index = spvarsetarg.Index.GetValue(exm);
				int charaNum = (int)exm.VEvaluator.CHARANUM;
				int start = 0;
				if (spvarsetarg.Start != null)
				{
					start = (int)spvarsetarg.Start.GetIntValue(exm);
					if (start < 0 || start >= charaNum)
						throw new CodeEE(string.Format(GameMessages.T("The 4th argument of CVARSET ({0}) is out of character range"), start.ToString()));
				}
				int end;
				if (spvarsetarg.End != null)
				{
					end = (int)spvarsetarg.End.GetIntValue(exm);
					if (end < 0 || end > charaNum)
						throw new CodeEE(string.Format(GameMessages.T("The 5th argument of CVARSET ({0}) is out of character range"), end.ToString()));
				}
				else
					end = charaNum;
				if (start > end)
				{
					int temp = start;
					start = end;
					end = temp;
				}
				if (!p.Identifier.IsCharacterData)
					throw new CodeEE(string.Format(GameMessages.T("A non-character variable '{0}' was passed to CVARSET"), p.Identifier.Name));
				if (index.GetOperandType() == typeof(string) && p.Identifier.IsArray1D)
				{
					if (!GlobalStatic.ConstantData.isDefined(p.Identifier.Code, index.Str))
						throw new CodeEE(string.Format(GameMessages.T("String '{0}' is not an element of array variable '{1}'"), index.Str, p.Identifier.Name));
				}
				if (p.Identifier.IsString)
				{
					string src = spvarsetarg.Term.GetStrValue(exm);
					exm.VEvaluator.SetValueAllEachChara(p, index, src, start, end);
				}
				else
				{
					long src = spvarsetarg.Term.GetIntValue(exm);
					exm.VEvaluator.SetValueAllEachChara(p, index, src, start, end);
				}
			}
		}

		private sealed class RANDOMIZE_Instruction : AbstractInstruction
		{
			public RANDOMIZE_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION_NULLABLE);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				Int64 iValue;
				if (func.Argument.IsConst)
					iValue = func.Argument.ConstInt;
				else
					iValue = ((ExpressionArgument)func.Argument).Term.GetIntValue(exm);
				exm.VEvaluator.Randomize(iValue);
			}
		}
		private sealed class INITRAND_Instruction : AbstractInstruction
		{
			public INITRAND_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.VEvaluator.InitRanddata();
			}
		}

		private sealed class DUMPRAND_Instruction : AbstractInstruction
		{
			public DUMPRAND_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.VEvaluator.DumpRanddata();
			}
		}


		private sealed class SAVEGLOBAL_Instruction : AbstractInstruction
		{
			public SAVEGLOBAL_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.VEvaluator.SaveGlobal();
			}
		}

		private sealed class LOADGLOBAL_Instruction : AbstractInstruction
		{
			public LOADGLOBAL_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				if (exm.VEvaluator.LoadGlobal())
					exm.VEvaluator.RESULT = 1;
				else
					exm.VEvaluator.RESULT = 0;
			}
		}

		private sealed class RESETDATA_Instruction : AbstractInstruction
		{
			public RESETDATA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.VEvaluator.ResetData();
				exm.Console.ResetStyle();
			}
		}

		private sealed class RESETGLOBAL_Instruction : AbstractInstruction
		{
			public RESETGLOBAL_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				exm.VEvaluator.ResetGlobalData();
			}
		}

		private static int toUInt32inArg(Int64 value, string funcName, int argnum)
		{
			if (value < 0)
				throw new CodeEE(string.Format(GameMessages.T("{0}: a negative value ({2}) was specified for argument {1}"), funcName, argnum.ToString(), value.ToString()));
			else if (value > Int32.MaxValue)
				throw new CodeEE(string.Format(GameMessages.T("{0}: the value ({2}) of argument {1} is too large"), funcName, argnum.ToString(), value.ToString()));

			return (int)value;
		}

		private sealed class SAVECHARA_Instruction : AbstractInstruction
		{
			public SAVECHARA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SAVECHARA);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArrayArgument arg = (ExpressionArrayArgument)func.Argument;
				IOperandTerm[] terms = arg.TermList;
				string datFilename = terms[0].GetStrValue(exm);
				string savMes = terms[1].GetStrValue(exm);
				int[] savCharaList = new int[terms.Length - 2];
				int charanum = (int)exm.VEvaluator.CHARANUM;
				for (int i = 0; i < savCharaList.Length; i++)
				{
					Int64 v = terms[i + 2].GetIntValue(exm);
					savCharaList[i] = FunctionIdentifier.toUInt32inArg(v, "SAVECHARA", i + 3);
					if (savCharaList[i] >= charanum)
						throw new CodeEE(string.Format(GameMessages.T("The value of SAVECHARA argument {0} exceeds the character registration number range"), (i + 3).ToString()));
					for (int j = 0; j < i; j++)
					{
						if (savCharaList[i] == savCharaList[j])
							throw new CodeEE(string.Format(GameMessages.T("The same character registration number ({0}) was specified multiple times"), (savCharaList[i]).ToString()));
					}
				}
				exm.VEvaluator.SaveChara(datFilename, savMes, savCharaList);
			}
		}

		private sealed class LOADCHARA_Instruction : AbstractInstruction
		{
			public LOADCHARA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				string datFilename;
				if (arg.IsConst)
					datFilename = arg.ConstStr;
				else
					datFilename = arg.Term.GetStrValue(exm);
				exm.VEvaluator.LoadChara(datFilename);
			}
		}


		private sealed class SAVEVAR_Instruction : AbstractInstruction
		{
			public SAVEVAR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SAVEVAR);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpSaveVarArgument arg = (SpSaveVarArgument)func.Argument;
				VariableToken[] vars = arg.VarTokens;
				string datFilename = arg.Term.GetStrValue(exm);
				string savMes = arg.SavMes.GetStrValue(exm);
				exm.VEvaluator.SaveVariable(datFilename, savMes, vars);
			}
		}
		private sealed class LOADVAR_Instruction : AbstractInstruction
		{
			public LOADVAR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				string datFilename;
				if (arg.IsConst)
					datFilename = arg.ConstStr;
				else
					datFilename = arg.Term.GetStrValue(exm);
				exm.VEvaluator.LoadVariable(datFilename);
			}
		}

		private sealed class DELDATA_Instruction : AbstractInstruction
		{
			public DELDATA_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				Int64 target;
				if (func.Argument.IsConst)
					target = func.Argument.ConstInt;
				else
					target = ((ExpressionArgument)func.Argument).Term.GetIntValue(exm);

				int target32 = FunctionIdentifier.toUInt32inArg(target, "DELDATA", 1);
				exm.VEvaluator.DelData(target32);
			}
		}

		private sealed class DO_NOTHING_Instruction : AbstractInstruction
		{
			public DO_NOTHING_Instruction()
			{
				// Effectively a non-flow-control version of ENDIF
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED | PARTIAL;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				// Do nothing
			}
		}

		private sealed class REF_Instruction : AbstractInstruction
		{
			public REF_Instruction(bool byname)
			{
				this.byname = byname;
				if (byname)
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_REFBYNAME);
				else
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_REF);

				flag = METHOD_SAFE | EXTENDED;
			}
			bool byname;

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				throw new NotImplCodeEE();

#pragma warning disable CS0162 // Unreachable code detected
				RefArgument arg = (RefArgument)func.Argument;
#pragma warning restore CS0162 // Unreachable code detected
				string str = null;
				if (arg.SrcTerm != null)
					str = arg.SrcTerm.GetStrValue(exm);
				if (arg.RefMethodToken != null)
				{
					UserDefinedRefMethod srcRef = arg.SrcRefMethodToken;
					CalledFunction call = arg.SrcCalledFunction;
					if (str != null)//REFBYNAME and the second argument is not a constant
					{
						srcRef = GlobalStatic.IdentifierDictionary.GetRefMethod(str);
						if (srcRef == null)
						{
							FunctionLabelLine label = FunctionResolver.ResolveNormalLabel(GlobalStatic.LabelDictionary, str);
							//if (label == null)
						//    throw new CodeEE("Inline function " + str + " not found");
						//if (!label.IsMethod)
						//    throw new CodeEE("Function " + str + " without #FUNCTION(S) attribute cannot be referenced");
							if (label != null && label.IsMethod)
								call = CalledFunction.CreateCalledFunctionMethod(label, str);
						}
					}
					else if (srcRef != null)
						call = srcRef.CalledFunction;//second argument is a function reference; if call is null it's an error
					if (call == null || !arg.RefMethodToken.MatchType(call))
					{
						arg.RefMethodToken.SetReference(null);
						exm.VEvaluator.RESULT = 0;
					}
					else
					{
						arg.RefMethodToken.SetReference(call);
						exm.VEvaluator.RESULT = 1;
					}
					return;
				}

				ReferenceToken refVar = arg.RefVarToken;
				VariableToken srcVar = arg.SrcVarToken;
				string errmes;
				if (str != null)
				{
					srcVar = GlobalStatic.IdentifierDictionary.GetVariableToken(str, null, true);

					//if (srcVar == null)
					//    throw new CodeEE("Variable " + str + " not found");
				}
				if (srcVar == null || !refVar.MatchType(srcVar, false, out errmes))
				{
					refVar.SetRef(null);
					exm.VEvaluator.RESULT = 0;
				}
				else
				{
					refVar.SetRef((Array)srcVar.GetArray());
					exm.VEvaluator.RESULT = 1;
				}
				return;
			}
		}

		private sealed class TOOLTIP_SETCOLOR_Instruction : AbstractInstruction
		{
			public TOOLTIP_SETCOLOR_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_SWAP);
				flag = METHOD_SAFE | EXTENDED;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpSwapCharaArgument arg = (SpSwapCharaArgument)func.Argument;
				long foreColor = arg.X.GetIntValue(exm);
				long backColor = arg.Y.GetIntValue(exm);
			if (foreColor < 0 || foreColor > 0xFFFFFF)
				throw new CodeEE(GameMessages.T("The 1st argument is out of range for a color integer"));
			if (backColor < 0 || backColor > 0xFFFFFF)
				throw new CodeEE(GameMessages.T("The 2nd argument is out of range for a color integer"));
				Color fc = Color.FromArgb((int)foreColor >>16, (int)foreColor>>8 &0xFF,(int)foreColor &0xFF);
				Color bc = Color.FromArgb((int)backColor >>16, (int)backColor>>8 &0xFF,(int)backColor &0xFF);
				exm.Console.SetToolTipColor(fc, bc);
				return;
			}
		}

		private sealed class TOOLTIP_SETDELAY_Instruction : AbstractInstruction
		{
			public TOOLTIP_SETDELAY_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				long delay;
				if(arg.IsConst)
					delay = arg.ConstInt;
				else
					delay = arg.Term.GetIntValue(exm);
			if (delay < 0 || delay > int.MaxValue)
				throw new CodeEE(GameMessages.T("The argument value is out of valid range"));
			exm.Console.SetToolTipDelay((int)delay);
				return;
			}
		}

        private sealed class TOOLTIP_SETDURATION_Instruction : AbstractInstruction
        {
            public TOOLTIP_SETDURATION_Instruction()
            {
                ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
                flag = METHOD_SAFE | EXTENDED;
            }
            public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
            {
                ExpressionArgument arg = (ExpressionArgument)func.Argument;
                long duration;
                if (arg.IsConst)
                    duration = arg.ConstInt;
                else
                    duration = arg.Term.GetIntValue(exm);
                if (duration < 0 || duration > int.MaxValue)
                    throw new CodeEE(GameMessages.T("The argument value is out of valid range"));
                if (duration > short.MaxValue)
                    duration = short.MaxValue;
                exm.Console.SetToolTipDuration((int)duration);
                return;
            }
        }
		
		private sealed class INPUTMOUSEKEY_Instruction : AbstractInstruction
		{
			public INPUTMOUSEKEY_Instruction()
			{
			ArgBuilder = ArgumentParser.GetNormalArgumentBuilder("I", 0);
			// Cannot be skipped
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
				flag =EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionsArgument arg = (ExpressionsArgument)func.Argument;
				Int64 time = 0;
				if (arg.ArgumentArray.Length > 0)
					time = arg.ArgumentArray[0].GetIntValue(exm);
				InputRequest req = new InputRequest();
				req.InputType = InputType.PrimitiveMouseKey; 
				if (time > 0)
					req.Timelimit = (int)time;
				exm.Console.WaitInput(req);
			}
		}
		
		private sealed class AWAIT_Instruction : AbstractInstruction
		{
			public AWAIT_Instruction()
			{
			ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.EXPRESSION_NULLABLE);
			// Cannot be skipped
			//flag = IS_PRINT | IS_INPUT | EXTENDED;
				flag = EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				Int64 waittime = -1;
				ExpressionArgument arg = func.Argument as ExpressionArgument;
				if (arg != null && arg.Term != null)
				{
					waittime = arg.Term.GetIntValue(exm);
					if (waittime < 0)
					throw new CodeEE(string.Format(GameMessages.T("AWAIT: a negative value ({0}) was specified"), waittime.ToString()));
				if (waittime > 10000)
					throw new CodeEE(string.Format(GameMessages.T("AWAIT: a wait time of 10 seconds or more ({0} ms) was specified"), waittime.ToString()));
				}

				exm.Console.Await((int)waittime);
			}
		}

		// Emuera EM/EE Extensions - BINPUT (Button-only input)
		private sealed class BINPUT_Instruction : AbstractInstruction
		{
			public BINPUT_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUT);
				flag = IS_PRINT | IS_INPUT | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				InputRequest req = new InputRequest();
				req.InputType = InputType.BIntValue;  // Button-only integer input
				if (arg.Term != null)
				{
					Int64 def;
					if (arg.IsConst)
						def = arg.ConstInt;
					else
						def = arg.Term.GetIntValue(exm);
					req.HasDefValue = true;
					req.DefIntValue = def;
				}
				exm.Console.WaitInput(req);
			}
		}

		private sealed class BINPUTS_Instruction : AbstractInstruction
		{
			public BINPUTS_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_INPUTS);
				flag = IS_PRINT | IS_INPUT | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				InputRequest req = new InputRequest();
				req.InputType = InputType.BStrValue;  // Button-only string input
				if (arg.Term != null)
				{
					string def;
					if (arg.IsConst)
						def = arg.ConstStr;
					else
						def = arg.Term.GetStrValue(exm);
					req.HasDefValue = true;
					req.DefStrValue = def;
				}
				exm.Console.WaitInput(req);
			}
		}

		// Emuera EM/EE Extensions - TRYCALLF/TRYCALLFORMF
		private sealed class TRYCALLF_Instruction : AbstractInstruction
		{
			public TRYCALLF_Instruction(bool form)
			{
				if (form)
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORMF);
				else
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLF);
				flag = EXTENDED | METHOD_SAFE | FORCE_SETARG | IS_TRY;
			}

			public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
			{
				if (!func.Argument.IsConst)
				{
					useCallForm = true;
					return;
				}
				SpCallFArgment callfArg = (SpCallFArgment)func.Argument;
				if (Config.ICFunction)
					callfArg.ConstStr = callfArg.ConstStr.ToUpper();
				try
				{
					callfArg.FuncTerm = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, callfArg.ConstStr, callfArg.RowArgs, true);
				}
				catch (CodeEE)
				{
					// For TRYCALLF, we don't warn - just continue with null
					if (!Program.AnalysisMode)
						callfArg.FuncTerm = null;
					return;
				}
				// For TRYCALLF, we don't warn if function not found - just continue with null
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				IOperandTerm mToken;
				string labelName;
				if ((!func.Argument.IsConst) || (exm.Console.RunERBFromMemory))
				{
					SpCallFArgment spCallformArg = (SpCallFArgment)func.Argument;
					labelName = spCallformArg.FuncnameTerm.GetStrValue(exm);
					try
					{
						mToken = GlobalStatic.IdentifierDictionary.GetFunctionMethod(GlobalStatic.LabelDictionary, labelName, spCallformArg.RowArgs, true);
					}
					catch (CodeEE)
					{
						mToken = null;
					}
				}
				else
				{
					labelName = func.Argument.ConstStr;
					mToken = ((SpCallFArgment)func.Argument).FuncTerm;
				}
				if (mToken == null)
				{
					// Function not found - set RESULT to 0 and return
					exm.VEvaluator.RESULT = 0;
					return;
				}
				mToken.GetValue(exm);
				exm.VEvaluator.RESULT = 1;
			}
		}

		// Emuera EM/EE Extensions - Sound commands
		private sealed class PLAYSOUND_Instruction : AbstractInstruction
		{
			public PLAYSOUND_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				string filename;
				if (func.Argument.IsConst)
					filename = func.Argument.ConstStr;
				else
					filename = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
				
				Content.AudioManager.Instance.PlaySound(filename);
			}
		}

		private sealed class STOPSOUND_Instruction : AbstractInstruction
		{
			public STOPSOUND_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				Content.AudioManager.Instance.StopSound();
			}
		}

		private sealed class PLAYBGM_Instruction : AbstractInstruction
		{
			public PLAYBGM_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				string filename;
				if (func.Argument.IsConst)
					filename = func.Argument.ConstStr;
				else
					filename = ((ExpressionArgument)func.Argument).Term.GetStrValue(exm);
				
				Content.AudioManager.Instance.PlayBGM(filename);
			}
		}

		private sealed class STOPBGM_Instruction : AbstractInstruction
		{
			public STOPBGM_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				Content.AudioManager.Instance.StopBGM();
			}
		}

		private sealed class SETSOUNDVOLUME_Instruction : AbstractInstruction
		{
			public SETSOUNDVOLUME_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				int volume = (int)arg.Term.GetIntValue(exm);
				Content.AudioManager.Instance.SetSoundVolume(volume);
			}
		}

		private sealed class SETBGMVOLUME_Instruction : AbstractInstruction
		{
			public SETBGMVOLUME_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument arg = (ExpressionArgument)func.Argument;
				int volume = (int)arg.Term.GetIntValue(exm);
				Content.AudioManager.Instance.SetBGMVolume(volume);
			}
		}
        #endregion

        #region flowControlFunction

        private sealed class BEGIN_Instruction : AbstractInstruction
		{
			public BEGIN_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR);
				flag = FLOW_CONTROL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				string keyword = func.Argument.ConstStr;
				if (Config.ICFunction)//1756 BEGIN keyword is apparently treated as a function name
					keyword = keyword.ToUpper();
				state.SetBegin(keyword);
				state.Return(0);
				exm.Console.ResetStyle();
			}
		}

		private sealed class SAVELOADGAME_Instruction : AbstractInstruction
		{
			public SAVELOADGAME_Instruction(bool isSave)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = FLOW_CONTROL;
				this.isSave = isSave;
			}
			readonly bool isSave;
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				if ((state.SystemState & SystemStateCode.__CAN_SAVE__) != SystemStateCode.__CAN_SAVE__)
				{
					string funcName = state.Scope;
					if (funcName == null)
						funcName = "";
				throw new CodeEE(string.Format(GameMessages.T("SAVEGAME/LOADGAME cannot be executed inside @{0}"), funcName));
			}
			GlobalStatic.Process.saveCurrentState(true);
			// The old ProcessState stored in the backup is referenced here, so state cannot be used
				GlobalStatic.Process.getCurrentState.SaveLoadData(isSave);
			}
		}

		private sealed class REPEAT_Instruction : AbstractInstruction
		{
			public REPEAT_Instruction(bool fornext)
			{
				flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL;
				if (fornext)
				{
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_FOR_NEXT);
					flag |= EXTENDED;
				}
				else
				{
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				}
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpForNextArgment forArg = (SpForNextArgment)func.Argument;
				func.LoopCounter = forArg.Cnt;
				//1.725 Order changed. Following REPEAT behavior.
				func.LoopCounter.SetValue(forArg.Start.GetIntValue(exm), exm);
				func.LoopEnd = forArg.End.GetIntValue(exm);
				func.LoopStep = forArg.Step.GetIntValue(exm);
			if ((func.LoopStep > 0) && (func.LoopEnd > func.LoopCounter.GetIntValue(exm)))//if there are remaining iterations,
				return;//proceed to the next line as-is
			else if ((func.LoopStep < 0) && (func.LoopEnd < func.LoopCounter.GetIntValue(exm)))//if there are remaining iterations,
				return;//proceed to the next line as-is
				state.JumpTo(func.JumpTo);
			}
		}

		private sealed class WHILE_Instruction : AbstractInstruction
		{
			public WHILE_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument expArg = (ExpressionArgument)func.Argument;
			if (expArg.Term.GetIntValue(exm) != 0)//if the expression is true,
				return;//proceed into the block as-is
				state.JumpTo(func.JumpTo);
			}
		}

		private sealed class SIF_Instruction : AbstractInstruction
		{
			public SIF_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
			}

			public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
			{
				LogicalLine jumpto = func.NextLine;
				if ((jumpto == null) || (jumpto.NextLine == null) ||
					(jumpto is FunctionLabelLine) || (jumpto is NullLine))
				{
					ParserMediator.Warn(GameMessages.T("There is no next line after the SIF statement"), func, 2, true, false);
					return;
				}
				else if (jumpto is InstructionLine)
				{
					InstructionLine sifFunc = (InstructionLine)jumpto;
					if (sifFunc.Function.IsPartial())
						ParserMediator.Warn(string.Format(GameMessages.T("The next line of SIF cannot be a {0} statement"), sifFunc.Function.Name), func, 2, true, false);
					else
						func.JumpTo = func.NextLine.NextLine;
				}
				else if (jumpto is GotoLabelLine)
					ParserMediator.Warn(GameMessages.T("The next line of SIF cannot be a label line"), func, 2, true, false);
				else
					func.JumpTo = func.NextLine.NextLine;

				if ((func.JumpTo != null) && (func.Position.LineNo + 1 != func.NextLine.Position.LineNo))
					ParserMediator.Warn(GameMessages.T("The next line of SIF is a blank or comment line (eramaker: SIF has no effect)"), func, 0, false, true);
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				ExpressionArgument expArg = (ExpressionArgument)func.Argument;
			if (expArg.Term.GetIntValue(exm) == 0)//if the expression is true, fall through as-is
				state.ShiftNextLine();//if false, skip one line; treat it the same as arriving in order
			}
		}

		private sealed class ELSEIF_Instruction : AbstractInstruction
		{
			public ELSEIF_Instruction(FunctionArgType argtype)
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(argtype);
				flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				//if (iFuncCode == FunctionCode.ELSE || iFuncCode == FunctionCode.ELSEIF
				//	|| iFuncCode == FunctionCode.CASE || iFuncCode == FunctionCode.CASEELSE)
			// pre-checked
			//if (func.JumpTo == null)
			//	throw new ExeEE(func.Function.Name + " jump target has not been set");
				state.JumpTo(func.JumpTo);
			}
		}
		private sealed class ENDIF_Instruction : AbstractInstruction
		{
			public ENDIF_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = FLOW_CONTROL | PARTIAL | FORCE_SETARG;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
			}
		}

		private sealed class IF_Instruction : AbstractInstruction
		{
			public IF_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				LogicalLine ifJumpto = func.JumpTo;//ENDIF
			// pre-checked
			//if (func.IfCaseList == null)
			//	throw new ExeEE("IF's IF-ELSEIF list was not built correctly");
			//if (func.JumpTo == null)
			//	throw new ExeEE("The ENDIF corresponding to IF has not been set");

				InstructionLine line;
				for (int i = 0; i < func.IfCaseList.Count; i++)
				{
					line = func.IfCaseList[i];
					if (line.IsError)
						continue;
					if (line.FunctionCode == FunctionCode.ELSE)
					{
						ifJumpto = line;
						break;
					}

				//ExpressionArgument expArg = (ExpressionArgument)(line.Argument);
				// pre-checked
				//if (expArg == null)
				//	throw new ExeEE("During IF check. argument has not been parsed.", func.IfCaseList[i].Position);

				//1730 Errors from ELSEIF were being detected as errors from IF
				state.CurrentLine = line;
				Int64 value = ((ExpressionArgument)(line.Argument)).Term.GetIntValue(exm);
				if (value != 0)//if the expression is true
					{
						ifJumpto = line;
						break;
					}
				}
				if (ifJumpto != func)//if the jump target is itself, stay as-is
					state.JumpTo(ifJumpto);
				//state.RunningLine = null;
			}
		}


		private sealed class SELECTCASE_Instruction : AbstractInstruction
		{
			public SELECTCASE_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.EXPRESSION);
				flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				LogicalLine caseJumpto = func.JumpTo;//ENDSELECT
				IOperandTerm selectValue = ((ExpressionArgument)func.Argument).Term;
				string sValue = null;
				Int64 iValue = 0;
				if (selectValue.IsInteger)
					iValue = selectValue.GetIntValue(exm);
				else
					sValue = selectValue.GetStrValue(exm);
			// pre-checked
			//if (func.IfCaseList == null)
			//	throw new ExeEE("SELECTCASE's CASE list was not built correctly");
			//if (func.JumpTo == null)
			//	throw new ExeEE("The ENDSELECT corresponding to SELECTCASE has not been set");
				InstructionLine line;
				for (int i = 0; i < func.IfCaseList.Count; i++)
				{
					line = func.IfCaseList[i];
					if (line.IsError)
						continue;
					if (line.FunctionCode == FunctionCode.CASEELSE)
					{
						caseJumpto = line;
						break;
					}
					CaseArgument caseArg = (CaseArgument)(line.Argument);
				// pre-checked
				//if (caseArg == null)
				//	throw new ExeEE("During CASE check. argument has not been parsed.", func.IfCaseList[i].Position);

					state.CurrentLine = line;
					if (selectValue.IsInteger)
					{
						Int64 Is = iValue;
						foreach (CaseExpression caseExp in caseArg.CaseExps)
						{
							if (caseExp.GetBool(Is, exm))
							{
								caseJumpto = line;
								goto casefound;
							}
						}
					}
					else
					{
						string Is = sValue;
						foreach (CaseExpression caseExp in caseArg.CaseExps)
						{
							if (caseExp.GetBool(Is, exm))
							{
								caseJumpto = line;
								goto casefound;
							}
						}
					}

				}
			casefound:
				state.JumpTo(caseJumpto);
				//state.RunningLine = null;
			}
		}

		private sealed class RETURNFORM_Instruction : AbstractInstruction
		{
			public RETURNFORM_Instruction()
			{
				//ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR_ANY);
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.FORM_STR);
				flag = EXTENDED | FLOW_CONTROL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				//int termnum = 0;
				//foreach (IOperandTerm term in ((ExpressionArrayArgument)func.Argument).TermList)
				//{
				//    string arg = term.GetStrValue(exm);
				//    StringStream aSt = new StringStream(arg);
				//    WordCollection wc = LexicalAnalyzer.Analyse(aSt, LexEndWith.EoL, LexAnalyzeFlag.None);
				//    exm.VEvaluator.SetResultX((ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL).GetIntValue(exm)), termnum);
				//    termnum++;
				//}
				//state.Return(exm.VEvaluator.RESULT);
				//if (state.ScriptEnd)
				//    return;
				//int termnum = 0;
				StringStream aSt = new StringStream(((ExpressionArgument)func.Argument).Term.GetStrValue(exm));
				List<long> termList = new List<long>();
				while (!aSt.EOS)
				{
					WordCollection wc = LexicalAnalyzer.Analyse(aSt, LexEndWith.Comma, LexAnalyzeFlag.None);
					//exm.VEvaluator.SetResultX(ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL).GetIntValue(exm), termnum++);
					termList.Add(ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.EoL).GetIntValue(exm));
					aSt.ShiftNext();
					LexicalAnalyzer.SkipHalfSpace(aSt);
					//termnum++;
				}
				if (termList.Count == 0)
					termList.Add(0);
				exm.VEvaluator.SetResultX(termList);
				state.Return(exm.VEvaluator.RESULT);
				if (state.ScriptEnd)
					return;
			}
		}

		private sealed class RETURN_Instruction : AbstractInstruction
		{
			public RETURN_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_ANY);
				flag = FLOW_CONTROL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				//int termnum = 0;
				ExpressionArrayArgument expArrayArg = (ExpressionArrayArgument)func.Argument;
				if (expArrayArg.TermList.Length == 0)
				{
					exm.VEvaluator.RESULT = 0;
					state.Return(0);
					return;
				}
				List<long> termList = new List<long>();
				foreach (IOperandTerm term in expArrayArg.TermList)
				{
					termList.Add(term.GetIntValue(exm));
					//exm.VEvaluator.SetResultX(term.GetIntValue(exm), termnum++);
				}
				if (termList.Count == 0)
					termList.Add(0);
				exm.VEvaluator.SetResultX(termList);
				state.Return(exm.VEvaluator.RESULT);
			}
		}

		private sealed class CATCH_Instruction : AbstractInstruction
		{
			public CATCH_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				//if (sequential)//if flow arrives from above, do nothing and jump to ENDCATCH
				state.JumpTo(func.JumpToEndCatch);
			}
		}

		private sealed class RESTART_Instruction : AbstractInstruction
		{
			public RESTART_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | FLOW_CONTROL | EXTENDED;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				state.JumpTo(func.ParentLabelLine);
			}
		}

		private sealed class BREAK_Instruction : AbstractInstruction
		{
			public BREAK_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | FLOW_CONTROL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				////The JUMP target of BREAK is REND or NEXT. Assign the corresponding REPEAT or FOR to iLine.
				//1.723 Spec change. BREAK's JUMP target stores REPEAT, FOR, or WHILE. That JUMP target is the actual destination.
				InstructionLine jumpTo = (InstructionLine)func.JumpTo;
				InstructionLine iLine = (InstructionLine)jumpTo.JumpTo;
				// WHILE and DO have no counter, so jump immediately
				if (jumpTo.FunctionCode != FunctionCode.WHILE && jumpTo.FunctionCode != FunctionCode.DO)
				{
				unchecked
				{//in eramaker, COUNT is incremented on BREAK
						jumpTo.LoopCounter.PlusValue(jumpTo.LoopStep, exm);
					}
				}
				state.JumpTo(iLine);
			}
		}

		private sealed class CONTINUE_Instruction : AbstractInstruction
		{
			public CONTINUE_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | FLOW_CONTROL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				InstructionLine jumpTo = (InstructionLine)func.JumpTo;
				if ((jumpTo.FunctionCode == FunctionCode.REPEAT) || (jumpTo.FunctionCode == FunctionCode.FOR))
				{
					// Loop variable unknown (if attempting to loop without going through REPEAT or FOR, ignore and exit the loop (this is how eramaker works))
					if (jumpTo.LoopCounter == null)
					{
						state.JumpTo(jumpTo.JumpTo);
						return;
					}
					unchecked
					{
						jumpTo.LoopCounter.PlusValue(jumpTo.LoopStep, exm);
					}
					Int64 counter = jumpTo.LoopCounter.GetIntValue(exm);
					// if there are remaining iterations,
					if (((jumpTo.LoopStep > 0) && (jumpTo.LoopEnd > counter))
						|| ((jumpTo.LoopStep < 0) && (jumpTo.LoopEnd < counter)))
						state.JumpTo(func.JumpTo);
					else
						state.JumpTo(jumpTo.JumpTo);
					return;
				}
				if (jumpTo.FunctionCode == FunctionCode.WHILE)
				{
					if (((ExpressionArgument)jumpTo.Argument).Term.GetIntValue(exm) != 0)
						state.JumpTo(func.JumpTo);
					else
						state.JumpTo(jumpTo.JumpTo);
					return;
				}
				if (jumpTo.FunctionCode == FunctionCode.DO)
				{
					// For DO only, the condition line comes after CONTINUE, so an error in the condition line can cause problems
					InstructionLine tFunc = (InstructionLine)((InstructionLine)func.JumpTo).JumpTo;//LOOP
					if (tFunc.IsError)
						throw new CodeEE(tFunc.ErrMes, tFunc.Position);
					ExpressionArgument expArg = (ExpressionArgument)tFunc.Argument;
				if (expArg.Term.GetIntValue(exm) != 0)//if the expression is true
					state.JumpTo(jumpTo);//DO
					else
						state.JumpTo(tFunc);//LOOP
					return;
				}
				throw new ExeEE(GameMessages.T("Abnormal CONTINUE"));
			}
		}

		private sealed class REND_Instruction : AbstractInstruction
		{
			public REND_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | FLOW_CONTROL | PARTIAL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				InstructionLine jumpTo = (InstructionLine)func.JumpTo;
			// Loop variable unknown (if attempting to loop without going through REPEAT or FOR, ignore and exit the loop (this is how eramaker works))
			if (jumpTo.LoopCounter == null)
			{
				state.JumpTo(jumpTo.JumpTo);
				return;
			}
			unchecked
			{
				jumpTo.LoopCounter.PlusValue(jumpTo.LoopStep, exm);
			}
			Int64 counter = jumpTo.LoopCounter.GetIntValue(exm);
			// if there are remaining iterations,
				if (((jumpTo.LoopStep > 0) && (jumpTo.LoopEnd > counter))
					|| ((jumpTo.LoopStep < 0) && (jumpTo.LoopEnd < counter)))
					state.JumpTo(func.JumpTo);
			}
		}

		private sealed class WEND_Instruction : AbstractInstruction
		{
			public WEND_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.VOID);
				flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				InstructionLine jumpTo = (InstructionLine)func.JumpTo;
				if (((ExpressionArgument)jumpTo.Argument).Term.GetIntValue(exm) != 0)
					state.JumpTo(func.JumpTo);
			}
		}

		private sealed class LOOP_Instruction : AbstractInstruction
		{
			public LOOP_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.INT_EXPRESSION);
				flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL | PARTIAL | FORCE_SETARG;
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
			ExpressionArgument expArg = (ExpressionArgument)func.Argument;
			if (expArg.Term.GetIntValue(exm) != 0)//if the expression is true
				state.JumpTo(func.JumpTo);
		}
	}



		private sealed class RETURNF_Instruction : AbstractInstruction
		{
			public RETURNF_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.EXPRESSION_NULLABLE);
				flag = METHOD_SAFE | EXTENDED | FLOW_CONTROL;
			}

			public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
			{
				FunctionLabelLine label = func.ParentLabelLine;
				if (!label.IsMethod)
				{
					ParserMediator.Warn(GameMessages.T("RETURNF can only be used inside #FUNCTION"), func, 2, true, false);
				}
				if (func.Argument != null)
				{
					IOperandTerm term = ((ExpressionArgument)func.Argument).Term;
					if (term != null)
					{
						if (label.MethodType != term.GetOperandType())
						{
							if (label.MethodType == typeof(Int64))
								ParserMediator.Warn(GameMessages.T("A string type was specified for the return value of a #FUNCTION"), func, 2, true, false);
							else if (label.MethodType == typeof(string))
								ParserMediator.Warn(GameMessages.T("A numeric type was specified for the return value of a #FUNCTIONS"), func, 2, true, false);
						}
					}
				}
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				IOperandTerm term = ((ExpressionArgument)func.Argument).Term;
				SingleTerm ret = null;
				if (term != null)
				{
					ret = term.GetValue(exm);
				}
				state.ReturnF(ret);
			}
		}

		private sealed class CALL_Instruction : AbstractInstruction
		{
			public CALL_Instruction(bool form, bool isJump, bool isTry, bool isTryCatch)
			{
				if (form)
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORM);
				else
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALL);
				flag = FLOW_CONTROL | FORCE_SETARG;
				if (isJump)
					flag |= IS_JUMP;
				if (isTry)
					flag |= IS_TRY;
				if (isTryCatch)
					flag |= IS_TRYC | PARTIAL;
				this.isJump = isJump;
				this.isTry = isTry;
			}
			readonly bool isJump;
			readonly bool isTry;

			public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
			{
				if (!func.Argument.IsConst)
				{
					useCallForm = true;
					return;
				}
				SpCallArgment callArg = (SpCallArgment)func.Argument;
				string labelName = callArg.ConstStr;
				if (Config.ICFunction)
					labelName = labelName.ToUpper();
				CalledFunction call = CalledFunction.CallFunction(GlobalStatic.Process, labelName, func);
				if ((call == null) && (!func.Function.IsTry()))
				{
					FunctionoNotFoundName = labelName;
					return;
				}
				if (call != null)
				{
					func.JumpTo = call.TopLabel;
					if (call.TopLabel.Depth < 0)
						call.TopLabel.Depth = currentDepth + 1;
					if (call.TopLabel.IsError)
					{
						func.IsError = true;
						func.ErrMes = call.TopLabel.ErrMes;
						return;
					}
					string errMes;
					callArg.UDFArgument = call.ConvertArg(callArg.RowArgs, out errMes);
					if (callArg.UDFArgument == null)
					{
						ParserMediator.Warn(errMes, func, 2, true, false);
						return;
					}
				}
				callArg.CallFunc = call;
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				SpCallArgment spCallArg = (SpCallArgment)func.Argument;
				CalledFunction call;
				string labelName;
				UserDefinedFunctionArgument arg = null;
				if (spCallArg.IsConst)
				{
					call = spCallArg.CallFunc;
					labelName = spCallArg.ConstStr;
					arg = spCallArg.UDFArgument;
				}
				else
				{
					labelName = spCallArg.FuncnameTerm.GetStrValue(exm);
					if (Config.ICFunction)
						labelName = labelName.ToUpper();
					call = CalledFunction.CallFunction(GlobalStatic.Process, labelName, func);
				}
			if (call == null && spCallArg.IsConst)
			{
				// Static calls may have been unresolved during an earlier syntax pass.
				// CalledFunction now routes the retry through FunctionResolver.
				call = CalledFunction.CallFunction(GlobalStatic.Process, labelName, func);
			}
			if (call != null && spCallArg.IsConst)
			{
				// Cache the result so subsequent static CALLs skip the lookup.
				if (spCallArg.UDFArgument == null)
				{
					string errMes2;
					spCallArg.CallFunc      = call;
					spCallArg.UDFArgument   = call.ConvertArg(spCallArg.RowArgs, out errMes2);
					if (spCallArg.UDFArgument == null)
					{
						if (!isTry)
							throw new CodeEE(errMes2 ?? GameMessages.T("Function argument conversion failed"));
						if (func.JumpToEndCatch != null)
							state.JumpTo(func.JumpToEndCatch);
						return;
					}
					arg = spCallArg.UDFArgument;
				}
			}
			if (call == null)
			{
			if (!isTry)
				throw new CodeEE(string.Format(GameMessages.T("Function \"@{0}\" not found"), labelName));
				if (func.JumpToEndCatch != null)
					state.JumpTo(func.JumpToEndCatch);
				return;
			}
				call.IsJump = isJump;
				if (arg == null)
				{
					string errMes;
					arg = call.ConvertArg(spCallArg.RowArgs, out errMes);
					if (arg == null)
						throw new CodeEE(errMes);
				}
				state.IntoFunction(call, arg, exm);
			}
		}

		private sealed class CALLEVENT_Instruction : AbstractInstruction
		{
			public CALLEVENT_Instruction()
			{
				ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.STR);
				flag = FLOW_CONTROL | EXTENDED;
			}

			public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
			{
				// Patterns where CALLEVENT is called from a function called from an EVENT function are caught in IntoFunction
				FunctionLabelLine label = func.ParentLabelLine;
				if (label.IsEvent)
				{
					ParserMediator.Warn(GameMessages.T("CALLEVENT cannot be used inside an EVENT function"), func, 2, true, false);
				}
			}

			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				string labelName = func.Argument.ConstStr;
				if (Config.ICFunction)
					labelName = labelName.ToUpper();
				CalledFunction call = CalledFunction.CallEventFunction(GlobalStatic.Process, labelName, func);
				if (call == null)
					return;
				state.IntoFunction(call, null, null);
			}
		}

		private sealed class GOTO_Instruction : AbstractInstruction
		{
			public GOTO_Instruction(bool form, bool isTry, bool isTryCatch)
			{
				if (form)
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALLFORM);
				else
					ArgBuilder = ArgumentParser.GetArgumentBuilder(FunctionArgType.SP_CALL);
				this.isTry = isTry;
				flag = METHOD_SAFE | FLOW_CONTROL | FORCE_SETARG;
				if (isTry)
					flag |= IS_TRY;
				if (isTryCatch)
					flag |= IS_TRYC | PARTIAL;
			}
			readonly bool isTry;

			public override void SetJumpTo(ref bool useCallForm, InstructionLine func, int currentDepth, ref string FunctionoNotFoundName)
			{
				GotoLabelLine jumpto;
				func.JumpTo = null;
				if (func.Argument.IsConst)
				{
					string labelName = func.Argument.ConstStr;
					if (Config.ICVariable)//in eramaker, GOTO label names are case-insensitive
						labelName = labelName.ToUpper();
					jumpto = GlobalStatic.LabelDictionary.GetLabelDollar(labelName, func.ParentLabelLine);
					if (jumpto == null)
					{
					if (!func.Function.IsTry())
						ParserMediator.Warn(string.Format(GameMessages.T("The specified label name \"${0}\" does not exist in the current function"), labelName), func, 2, true, false);
						else
							return;
					}
				else if (jumpto.IsError)
					ParserMediator.Warn(string.Format(GameMessages.T("The specified label name \"${0}\" is an invalid $ label line"), labelName), func, 2, true, false);
					else if (jumpto != null)
					{
						func.JumpTo = jumpto;
					}
				}
			}
			public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
			{
				string label;
				LogicalLine jumpto;
				if (func.Argument.IsConst)
				{
					label = func.Argument.ConstStr;
					if (func.JumpTo != null)
						jumpto = func.JumpTo;
					else
						return;
				}
				else
				{
					label = ((SpCallArgment)func.Argument).FuncnameTerm.GetStrValue(exm);
					if (Config.ICVariable)
						label = label.ToUpper();
					jumpto = state.CurrentCalled.CallLabel(GlobalStatic.Process, label);
				}
				if (jumpto == null)
				{
				if (!func.Function.IsTry())
					throw new CodeEE(string.Format(GameMessages.T("The specified label name \"${0}\" does not exist in the current function"), label));
					if (func.JumpToEndCatch != null)
						state.JumpTo(func.JumpToEndCatch);
					return;
				}
			else if (jumpto.IsError)
				throw new CodeEE(string.Format(GameMessages.T("The specified label name \"${0}\" is an invalid $ label line"), label));
				state.JumpTo(jumpto);
			}
		}
		#endregion
	}
}
