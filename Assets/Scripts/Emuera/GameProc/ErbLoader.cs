using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc.Function;
using MinorShift._Library;
using MinorShift.Emuera.GameData;
using uEmuera.VisualBasic;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Loads and parses ERB (ERA script) files into executable game functions.
    /// Handles script file reading, function definition parsing, label management,
    /// and conversion of script text into LogicalLine objects for execution.
    /// </summary>
    internal sealed class ErbLoader
    {
		public ErbLoader(EmueraConsole main, ExpressionMediator exm, Process proc)
		{
			output = main;
			parentProcess = proc;
			this.exm = exm;
		}
		readonly Process parentProcess;
		readonly ExpressionMediator exm;
		readonly EmueraConsole output;
        readonly List<string> ignoredFNFWarningFileList = new List<string>();
		int ignoredFNFWarningCount = 0;

		int enabledLineCount = 0;
		LabelDictionary labelDic;

		bool noError = true;

		/// <summary>
		/// Read multiple files
		/// </summary>
		/// <param name="filepath"></param>
public bool LoadErbFiles(string erbDir, bool displayReport, LabelDictionary labelDictionary)
		{
			//1.713 changed the position where labelDic is newed.
			//Because ExpressionParser requires Process.instance.LabelDic at the point of checkScript();
			// Full (safe) load replaces any on-demand compiler session.
			OnDemandErbCompiler.Clear();
			labelDic = labelDictionary;
			labelDic.Initialized = false;

			List<KeyValuePair<string, string>> erbFiles = Config.GetFiles(erbDir, "*.ERB");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            erbFiles.AddRange(Config.GetFiles(erbDir, "*.erb"));
#endif
            List<string> isOnlyEvent = new List<string>();
            noError = true;
			uint starttime = WinmmTimer.TickCount;
			int totalFiles = erbFiles.Count;
			try
			{
				labelDic.RemoveAll();
				for (int i = 0; i < erbFiles.Count; i++)
				{
					string filename = erbFiles[i].Key;
					string file = erbFiles[i].Value;
					
					// Update loading status with progress
					GenericUtils.SetLoadingStatus($"Loading ERB ({i + 1}/{totalFiles}): {filename}");
					
#if UEMUERA_DEBUG
					if (displayReport)
						output.PrintSystemLine(string.Format(GameMessages.ElapsedTime, (WinmmTimer.TickCount - starttime).ToString("D4")) + ":" + string.Format(GameMessages.LoadingFile, filename));
#else
					if (displayReport)
						output.PrintSystemLine(string.Format(GameMessages.LoadingFile, filename));
#endif
					//System.Windows.Forms.//Application.DoEvents();
					loadErb(file, filename, isOnlyEvent);
				}
				ParserMediator.FlushWarningList();
#if UEMUERA_DEBUG
				output.PrintSystemLine(string.Format(GameMessages.ElapsedTime, (WinmmTimer.TickCount - starttime).ToString("D4")) + ":");
#endif
				GenericUtils.SetLoadingStatus("Building function list...");
				if (displayReport)
					output.PrintSystemLine(GameMessages.BuildingUserFunctionList);
				setLabelsArg();
				ParserMediator.FlushWarningList();
				labelDic.Initialized = true;
#if UEMUERA_DEBUG
				output.PrintSystemLine(string.Format(GameMessages.ElapsedTime, (WinmmTimer.TickCount - starttime).ToString("D4")) + ":");
#endif
				GenericUtils.SetLoadingStatus("Checking syntax...");
				if (displayReport)
					output.PrintSystemLine(GameMessages.CheckingSyntax);
				checkScript();
				ParserMediator.FlushWarningList();

#if UEMUERA_DEBUG
				output.PrintSystemLine(string.Format(GameMessages.ElapsedTime, (WinmmTimer.TickCount - starttime).ToString("D4")) + ":");
#endif
				GenericUtils.SetLoadingStatus("Load complete!");
				if (displayReport)
					output.PrintSystemLine(GameMessages.LoadComplete);
			}
			catch (Exception e)
			{
				ParserMediator.FlushWarningList();
				uEmuera.Media.SystemSounds.Hand.Play();
				output.PrintError(GameMessages.UnexpectedError + Program.ExeName);
				output.PrintError(e.GetType().ToString() + ":" + e.Message);
				return false;
			}
			finally
			{
				parentProcess.SetBackgroundScanLine(null);
			}
            isOnlyEvent.Clear();
			return noError;
		}

		/// <summary>
		/// Load the specified file
		/// </summary>
		/// <param name="filename"></param>
		public bool loadErbs(List<string> path, LabelDictionary labelDictionary)
		{
			string fname;
            List<string> isOnlyEvent = new List<string>();
            noError = true;
			// Partial reload replaces any on-demand compiler session.
			OnDemandErbCompiler.Clear();
			labelDic = labelDictionary;
			labelDic.Initialized = false;
			foreach (string fpath in path)
			{
				if (fpath.StartsWith(Program.ErbDir, Config.SCIgnoreCase) && !Program.AnalysisMode)
					fname = fpath.Substring(Program.ErbDir.Length);
				else
					fname = fpath;
				if (Program.AnalysisMode)
					output.PrintSystemLine(string.Format(GameMessages.LoadingFile, fname));
				//System.Windows.Forms.//Application.DoEvents();
                loadErb(fpath, fname, isOnlyEvent);
			}
            if (Program.AnalysisMode)
                output.NewLine();
            ParserMediator.FlushWarningList();
			setLabelsArg();
			ParserMediator.FlushWarningList();
			labelDic.Initialized = true;
            checkScript();
			ParserMediator.FlushWarningList();
			parentProcess.SetBackgroundScanLine(null);
            isOnlyEvent.Clear();
            return noError;
		}

		private sealed class PPState
		{
			bool skip = false;
			bool done = false;
			public bool Disabled = false;
            readonly Stack<bool> disabledStack = new Stack<bool>();
            readonly Stack<bool> doneStack = new Stack<bool>();
            readonly Stack<string> ppMatch = new Stack<string>();

			internal void AddKeyWord(string token, string token2, ScriptPosition position)
			{
				//bool token2enabled = string.IsNullOrEmpty(token2);
				switch (token)
				{
					case "SKIPSTART":
						if (!string.IsNullOrEmpty(token2))
						{
							ParserMediator.Warn(token + GameMessages.T(" has extra arguments"), position, 1);
							break;
						}
						if (skip)
						{
							ParserMediator.Warn(GameMessages.T("[SKIPSTART] is used more than once"), position, 1);
							break;
						}
						ppMatch.Push("SKIPEND");
						disabledStack.Push(Disabled);
						doneStack.Push(done);
						skip = true;
						Disabled = true;
						done = false;
						break;
					case "IF_DEBUG":
						if (!string.IsNullOrEmpty(token2))
						{
							ParserMediator.Warn(token + GameMessages.T(" has extra arguments"), position, 1);
							break;
						}
						ppMatch.Push("ELSEIF");
						disabledStack.Push(Disabled);
						doneStack.Push(done);
						Disabled = !Program.DebugMode;
						done = !Disabled;
						break;
					case "IF_NDEBUG":
						if (!string.IsNullOrEmpty(token2))
						{
							ParserMediator.Warn(token + GameMessages.T(" has extra arguments"), position, 1);
							break;
						}
						ppMatch.Push("ELSEIF");
						disabledStack.Push(Disabled);
						doneStack.Push(done);
						Disabled = Program.DebugMode;
						done = !Disabled;
						break;
					case "IF":
						if (string.IsNullOrEmpty(token2))
						{
							ParserMediator.Warn(token + GameMessages.T(" has no argument"), position, 1);
							break;
						}
						ppMatch.Push("ELSEIF");
						disabledStack.Push(Disabled);
						doneStack.Push(done);
						Disabled = GlobalStatic.IdentifierDictionary.GetMacro(token2) == null;
						done = !Disabled;
						break;
					case "ELSEIF":
						if (string.IsNullOrEmpty(token2))
						{
							ParserMediator.Warn(token + GameMessages.T(" has no argument"), position, 1);
							break;
						}
						if (ppMatch.Count == 0 || ppMatch.Pop() != "ELSEIF")
						{
							ParserMediator.Warn(GameMessages.T("Invalid [ELSEIF]"), position, 1);
							break;
						}
						ppMatch.Push("ELSEIF");
						Disabled = done || (GlobalStatic.IdentifierDictionary.GetMacro(token2) == null);
						done |= !Disabled;
						break;
					case "ELSE":
						if (!string.IsNullOrEmpty(token2))
						{
							ParserMediator.Warn(token + GameMessages.T(" has extra arguments"), position, 1);
							break;
						}
						if (ppMatch.Count == 0 || ppMatch.Pop() != "ELSEIF")
						{
							ParserMediator.Warn(GameMessages.T("Invalid [ELSE]"), position, 1);
							break;
						}
						ppMatch.Push("ENDIF");
						Disabled = done;
						done = true;
						break;

					case "SKIPEND":
						{
							if (!string.IsNullOrEmpty(token2))
							{
								ParserMediator.Warn(token + GameMessages.T(" has extra arguments"), position, 1);
								break;
							}
							string match = ppMatch.Count == 0 ? "" : ppMatch.Pop();
							if (match != "SKIPEND")
							{
								ParserMediator.Warn(GameMessages.T("[SKIPEND] does not match [SKIPSTART]"), position, 1);
								break;
							}
							skip = false;
							Disabled = disabledStack.Pop();
							done = doneStack.Pop();
						}
						break;
					case "ENDIF":
						{
							if (!string.IsNullOrEmpty(token2))
							{
								ParserMediator.Warn(token + GameMessages.T(" has extra arguments"), position, 1);
								break;
							}
							string match = ppMatch.Count == 0 ? "" : ppMatch.Pop();
							if (match != "ENDIF" && match != "ELSEIF")
							{
								ParserMediator.Warn(GameMessages.T("[ENDIF] has no matching [IF]"), position, 1);
								break;
							}
							Disabled = disabledStack.Pop();
							done = doneStack.Pop();
						}
						break;
					default:
						ParserMediator.Warn(GameMessages.T("Unrecognized preprocessor"), position, 1);
						break;
				}
				if (skip)
					Disabled = true;
			}

			internal void FileEnd(ScriptPosition position)
			{
				if (ppMatch.Count != 0)
				{
					string match = ppMatch.Pop();
					if (match == "ELSEIF")
						match = "ENDIF";
					ParserMediator.Warn("[" + match + GameMessages.T("] is missing"), position, 1);
				}
			}
		}

		/// <summary>
		/// Read one file
		/// </summary>
		/// <param name="filepath"></param>
		private void loadErb(string filepath, string filename, List<string> isOnlyEvent)
		{
			//record the path of the loaded file
			//for processing when some files are reloaded
			labelDic.AddFilename(filename);
			EraStreamReader eReader = new EraStreamReader(Config.UseRenameFile && ParserMediator.RenameDic != null);
			if (!eReader.Open(filepath, filename))
			{
				output.PrintError(string.Format(GameMessages.FileOpenFailed, eReader.Filename));
				return;
			}
			try
			{
				PPState ppstate = new PPState();
				LogicalLine nextLine = new NullLine();
				LogicalLine lastLine = new NullLine();
				FunctionLabelLine lastLabelLine = null;
				StringStream st = null;
				ScriptPosition position = null;
				int funcCount = 0;
				if (Program.AnalysisMode)
					output.PrintSystemLine(GameMessages.T("　"));
				while ((st = eReader.ReadEnabledLine(ppstate.Disabled)) != null)
				{
					position = new ScriptPosition(eReader.Filename, eReader.LineNo);
					//rename processing moved to EraStreamReader
					//LexAnalyzer throws an Error for [[～～]] that could not be converted
					if (st.Current == '[' && st.Next != '[')
					{
						st.ShiftNext();
						string token = LexicalAnalyzer.ReadSingleIdentifier(st);
						LexicalAnalyzer.SkipWhiteSpace(st);
						string token2 = LexicalAnalyzer.ReadSingleIdentifier(st);
						if ((string.IsNullOrEmpty(token)) || (st.Current != ']'))
							ParserMediator.Warn(GameMessages.T("Invalid use of []"), position, 1);
						ppstate.AddKeyWord(token, token2, position);
						st.ShiftNext();
						if (!st.EOS)
							ParserMediator.Warn("[" + token + GameMessages.T("] is ignored."), position, 1);
						continue;
					}
					//if ((skip) || (Program.DebugMode && ifndebug) || (!Program.DebugMode && ifdebug))
					//	continue;
					if (ppstate.Disabled)
						continue;
					//up to here is the preprocessor

					if (st.Current == '#')
					{
						if ((lastLine == null) || !(lastLine is FunctionLabelLine))
						{
							ParserMediator.Warn(GameMessages.T("# line is used other than immediately after a function declaration"), position, 1);
							continue;
						}
						if (!LogicalLineParser.ParseSharpLine((FunctionLabelLine)lastLine, st, position, isOnlyEvent))
							noError = false;
						continue;
					}
					if ((st.Current == '$') || (st.Current == '@'))
					{
						bool isFunction = (st.Current == '@');
						nextLine = LogicalLineParser.ParseLabelLine(st, position, output);
						if (isFunction)
						{
							FunctionLabelLine label = (FunctionLabelLine)nextLine;
							lastLabelLine = label;
							if (label is InvalidLabelLine)
							{
								noError = false;
								ParserMediator.Warn(nextLine.ErrMes, position, 2);
								labelDic.AddInvalidLabel(label);
							}
							else// if (label is FunctionLabelLine)
							{
								labelDic.AddLabel(label);
								if (!label.IsEvent && (Config.WarnNormalFunctionOverloading || Program.AnalysisMode))
								{
									FunctionLabelLine seniorLabel = labelDic.GetSameNameLabel(label);
                                    if (seniorLabel != null)
                                    {
                                        //output.NewLine();
                                        ParserMediator.Warn(GameMessages.T("Function @") + label.LabelName + GameMessages.T(" is already defined (") + seniorLabel.Position.Filename + GameMessages.T(" at line ") + seniorLabel.Position.LineNo.ToString() + GameMessages.T(")"), position, 1);
                                        funcCount = -1;
                                    }
								}
								funcCount++;
								if (Program.AnalysisMode && (Config.PrintCPerLine > 0 && (funcCount % Config.PrintCPerLine) == 0))
								{
									output.NewLine();
									output.PrintSystemLine(GameMessages.T("　"));
								}
							}
						}
						else
						{
                            if (nextLine is GotoLabelLine gotoLabel)
                            {
                                gotoLabel.ParentLabelLine = lastLabelLine;
                                if (lastLabelLine != null && !labelDic.AddLabelDollar(gotoLabel))
                                {
                                    ScriptPosition pos = labelDic.GetLabelDollar(gotoLabel.LabelName, lastLabelLine).Position;
                                    ParserMediator.Warn(GameMessages.T("Label name $") + gotoLabel.LabelName + GameMessages.T(" is already used in the same function (") + pos.Filename + GameMessages.T(" at line ") + pos.LineNo.ToString() + GameMessages.T(")"), position, 2);
                                }
                            }
                        }
						if (nextLine is InvalidLine)
						{
							noError = false;
							ParserMediator.Warn(nextLine.ErrMes, position, 2);
						}
					}
					else
					{
						//1808alpha006 changed processing position
                        ////full replacement handled here
                        ////1756beta1+++ if replaced all at the start, function definitions could be reprocessed via _Rename and so on, which is out of the question, so it was permanently sealed
                        //if (ParserMediator.RenameDic != null && st.CurrentEqualTo("[[") && (rowLine.TrimEnd().IndexOf("]]") == rowLine.TrimEnd().Length - 2))
                        //{
                        //    string replacedLine = st.Substring();
                        //    foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
                        //        replacedLine = replacedLine.Replace(pair.Key, pair.Value);
                        //    st = new StringStream(replacedLine);
                        //}
                        nextLine = LogicalLineParser.ParseLine(st, position, output);
						if (nextLine == null)
							continue;
						if (nextLine is InvalidLine)
						{
							noError = false;
                            ParserMediator.Warn(nextLine.ErrMes, position, 2);
						}
					}
					if (lastLabelLine == null)
						ParserMediator.Warn(GameMessages.T("There are lines before a function is defined"), position, 1);
					nextLine.ParentLabelLine = lastLabelLine;
					lastLine = addLine(nextLine, lastLine);
				}
				addLine(new NullLine(), lastLine);
				position = new ScriptPosition(eReader.Filename, -1);
				ppstate.FileEnd(position);
			}
			finally
			{
				eReader.Close();
			}
			return;
		}
		
		private LogicalLine addLine(LogicalLine nextLine, LogicalLine lastLine)
		{
			if (nextLine == null)
				return null;
			enabledLineCount++;
			lastLine.NextLine = nextLine;
			return nextLine;
		}

		private void setLabelsArg()
		{
			List<FunctionLabelLine> labelList = labelDic.GetAllLabels(false);
			foreach (FunctionLabelLine label in labelList)
			{
				try
				{
					if (label.Arg != null)
						continue;
					parentProcess.SetBackgroundScanLine(label);
					parseLabel(label);
				}
				catch (Exception exc)
				{
					uEmuera.Media.SystemSounds.Hand.Play();
					string errmes = exc.Message;
					if (!(exc is EmueraException))
						errmes = exc.GetType().ToString() + ":" + errmes;
					ParserMediator.Warn(GameMessages.T("Function @") + label.LabelName + GameMessages.T(" argument Error: ") + errmes, label, 2, true, false);
					label.ErrMes = GameMessages.T("A function that failed to parse at load time was called");
                    label.IsError = true;
				}
				finally
				{
					parentProcess.SetBackgroundScanLine(null);
				}
			}
			labelDic.SortLabels();
		}

		private void parseLabel(FunctionLabelLine label)
		{
			WordCollection wc = label.PopRowArgs();
			string errMes;
			SingleTerm[] subNames;
			VariableTerm[] args = new VariableTerm[0];
			SingleTerm[] defs = new SingleTerm[0];
			int maxArg = -1;
			int maxArgs = -1;
			//1807 for system functions of non-event functions, lower the warning level, clear Error, and set the argument.
			if (label.IsEvent)
			{
				if (!wc.EOL)
					ParserMediator.Warn(GameMessages.T("Event function @") + label.LabelName + GameMessages.T(" cannot have arguments set"), label, 2, true, false);
				//label.SubNames = subNames;
				label.Arg = args;
				label.Def = defs;
				label.ArgLength = -1;
				label.ArgsLength = -1;
				return;
			}

			if (!wc.EOL)
			{
				if (label.IsSystem)
					ParserMediator.Warn(GameMessages.T("System function @") + label.LabelName + GameMessages.T(" has arguments set"), label, 1, false, false);
				SymbolWord symbol = wc.Current as SymbolWord;
				wc.ShiftNext();
                if (symbol == null)
				{ errMes = GameMessages.T("Incorrect argument format"); goto err; }
				if (symbol.Type == '[')//TODO:subNames maybe not implemented after all
				{
					IOperandTerm[] subNamesRow = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightBracket, false);
					if (subNamesRow.Length == 0)
					{ errMes = GameMessages.T("The argument inside [] in a function definition cannot be empty"); goto err; }
					subNames = new SingleTerm[subNamesRow.Length];
					for (int i = 0; i < subNamesRow.Length; i++)
					{
						if (subNamesRow[i] == null)
						{ errMes = GameMessages.T("Arguments in a function definition cannot be omitted"); goto err; }
						IOperandTerm term = subNamesRow[i].Restructure(exm);
						subNames[i] = term as SingleTerm;
						if (subNames[i] == null)
						{ errMes = GameMessages.T("Only constants can be specified for the argument inside [] in a function definition"); goto err; }
					}
					symbol = wc.Current as SymbolWord;
					if ((!wc.EOL) && (symbol == null))
					{ errMes = GameMessages.T("Incorrect argument format"); goto err; }
					wc.ShiftNext();
				}
				if (!wc.EOL)
				{
					IOperandTerm[] argsRow;
                    if (symbol.Type == ',')
						argsRow = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, true);
					else if (symbol.Type == '(')
						argsRow = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightParenthesis, true);
					else
					{ errMes = GameMessages.T("Incorrect argument format"); goto err; }
					int length = argsRow.Length / 2;
					args = new VariableTerm[length];
                    defs = new SingleTerm[length];
					for (int i = 0; i < length; i++)
					{
						SingleTerm def = null;
						IOperandTerm term = argsRow[i * 2];
                        //must be determined at the point of argument reading
                        //if (term == null)
                        //{ errMes = GameMessages.T("Arguments in a function definition cannot be omitted"); goto err; }
                        if ((!(term.Restructure(exm) is VariableTerm vTerm)) || (vTerm.Identifier.IsConst))
                        { errMes = GameMessages.T("Please specify an assignable variable for the argument in a function definition"); goto err; }
                        else if (!vTerm.Identifier.IsReference)//reference type does not need a subscript
                        {
                            if (vTerm is VariableNoArgTerm)
                            { errMes = GameMessages.T("The non-reference argument \"") + vTerm.Identifier.Name + GameMessages.T("\" has no subscript specified"); goto err; }
                            if (!vTerm.isAllConst)
                            { errMes = GameMessages.T("Please specify constants for the subscripts of the argument in a function definition"); goto err; }
                        }
                        for (int j = 0; j < i; j++)
                        {
                            if (vTerm.checkSameTerm(args[j]))
                                ParserMediator.Warn(GameMessages.T("The ") +  Strings.StrConv((i + 1).ToString(), VbStrConv.Wide, Config.Language) + GameMessages.T("argument \"") + vTerm.GetFullString() + GameMessages.T("\" is already declared as the ") + Strings.StrConv((j + 1).ToString(), VbStrConv.Wide, Config.Language) + GameMessages.T("argument"), label, 1, false, false);
                        }
						if (vTerm.Identifier.Code == VariableCode.ARG)
						{
							if (maxArg < vTerm.getEl1forArg + 1)
								maxArg = vTerm.getEl1forArg + 1;
						}
						else if (vTerm.Identifier.Code == VariableCode.ARGS)
						{
							if (maxArgs < vTerm.getEl1forArg + 1)
								maxArgs = vTerm.getEl1forArg + 1;
						}
						bool canDef = (vTerm.Identifier.Code == VariableCode.ARG || vTerm.Identifier.Code == VariableCode.ARGS || vTerm.Identifier.IsPrivate);
						term = argsRow[i * 2 + 1];
						if (term is NullTerm)
						{
							if (canDef)// && label.ArgOptional)
							{
								if (vTerm.GetOperandType() == typeof(Int64))
									def = new SingleTerm(0);
								else
									def = new SingleTerm("");
							}
						}
						else
						{
							def = term.Restructure(exm) as SingleTerm;
							if (def == null)
							{ errMes = GameMessages.T("Only constants can be specified as the initial value of an argument"); goto err; }
							if (!canDef)
							{ errMes = GameMessages.T("Only \"ARG\", \"ARGS\" or private variables can define initial values for arguments"); goto err; }
							else if (vTerm.Identifier.IsReference)
							{ errMes = GameMessages.T("Initial values cannot be defined for reference-type arguments"); goto err; }
							if (vTerm.GetOperandType() != def.GetOperandType())
							{ errMes = GameMessages.T("The type of the argument does not match the type of the initial value"); goto err; }
						}
						args[i] = vTerm;
						defs[i] = def;
					}

				}
			}
			if (!wc.EOL)
			{ errMes = GameMessages.T("Incorrect argument format"); goto err; }

            //label.SubNames = subNames;
			label.Arg = args;
			label.Def = defs;
			label.ArgLength = maxArg;
			label.ArgsLength = maxArgs;
			return;
		err:
			ParserMediator.Warn(GameMessages.T("Function @") + label.LabelName + GameMessages.T(" argument Error: ") + errMes, label, 2, true, false);
			return;
		}


		public bool useCallForm = false;
		/// <summary>
		/// Check files that have finished loading
		/// </summary>
		private void checkScript()
		{
			int usedLabelCount = 0;
			int labelDepth = -1;
			List<FunctionLabelLine> labelList = labelDic.GetAllLabels(true);

			while (true)
			{
				labelDepth++;
				int countInDepth = 0;
				foreach (FunctionLabelLine label in labelList)
				{
					if (label.Depth != labelDepth)
						continue;
					//1756beta003 why did I add this? Did I do something while debugging? Commented out for now
					//if (label.LabelName == "EVENTTURNEND")
					//    useCallForm = true;
					usedLabelCount++;
					countInDepth++;
					checkFunctionWithCatch(label);
				}
				if (countInDepth == 0)
					break;
			}
            labelDepth = -1;
			List<string> ignoredFNCWarningFileList = new List<string>();
			int ignoredFNCWarningCount = 0;

			bool ignoreAll = false;
			DisplayWarningFlag notCalledWarning = Config.FunctionNotCalledWarning;
			switch (notCalledWarning)
			{
				case DisplayWarningFlag.IGNORE:
				case DisplayWarningFlag.LATER:
					ignoreAll = true;
					break;
			}
			if (useCallForm)
			{//if the callform family is used, consider all functions called.
                if (Program.AnalysisMode)
					output.PrintSystemLine(GameMessages.CallformUsedNoCheck);
				foreach (FunctionLabelLine label in labelList)
				{
					if (label.Depth != labelDepth)
						continue;
					checkFunctionWithCatch(label);
				}
			}
			else
			{
				bool ignoreUncalledFunction = Config.IgnoreUncalledFunction;
				foreach (FunctionLabelLine label in labelList)
				{
					if (label.Depth != labelDepth)
						continue;
                    //in analysis mode, analyze the ones not called here
                    if (Program.AnalysisMode)
                        checkFunctionWithCatch(label);
					bool ignore = false;
					if (notCalledWarning == DisplayWarningFlag.ONCE)
					{
						string filename = label.Position.Filename.ToUpper();

						if (!string.IsNullOrEmpty(filename))
						{
							if (ignoredFNCWarningFileList.Contains(filename))
							{
								ignore = true;
							}
							else
							{
								ignore = false;
								ignoredFNCWarningFileList.Add(filename);
							}
						}
                        //break;
					}
					if (ignoreAll || ignore)
						ignoredFNCWarningCount++;
					else
						ParserMediator.Warn(GameMessages.T("Function @") + label.LabelName + GameMessages.T(" is defined but never called"), label, 1, false, false);
					if (!ignoreUncalledFunction)
						checkFunctionWithCatch(label);
					else
					{
						if (!(label.NextLine is NullLine) && !(label.NextLine is FunctionLabelLine))
						{
							if (!label.NextLine.IsError)
							{
								label.NextLine.IsError = true;
								label.NextLine.ErrMes = GameMessages.T("A function that should not have been called was called");
							}
						}
					}
				}
			}
			if (Program.AnalysisMode && (warningDic.Keys.Count > 0 || GlobalStatic.tempDic.Keys.Count > 0))
			{
				output.PrintError(GameMessages.FunctionNotFoundWarning);
				if (warningDic.Keys.Count > 0)
				{
					output.PrintError(GameMessages.GeneralFunctions);
					foreach (string labelName in warningDic.Keys)
					{
						output.PrintError(GameMessages.T("　　") + labelName + ": " + warningDic[labelName].ToString() + GameMessages.TimesCount);
					}
				}
				if (GlobalStatic.tempDic.Keys.Count > 0)
				{
					output.PrintError(GameMessages.InlineFunctions);
					foreach (string labelName in GlobalStatic.tempDic.Keys)
					{
						output.PrintError(GameMessages.T("　　") + labelName + ": " + GlobalStatic.tempDic[labelName].ToString() + GameMessages.TimesCount);
					}
				}
			}
			else
			{
				if ((ignoredFNCWarningCount > 0) && (Config.DisplayWarningLevel <= 1) && (notCalledWarning != DisplayWarningFlag.IGNORE))
					output.PrintError(string.Format(GameMessages.WarningLv1Ignored, ignoredFNCWarningCount));
				if ((ignoredFNFWarningCount > 0) && (Config.DisplayWarningLevel <= 2) && (notCalledWarning != DisplayWarningFlag.IGNORE))
					output.PrintError(string.Format(GameMessages.WarningLv2Ignored, ignoredFNFWarningCount));
			}
			ParserMediator.FlushWarningList();
			if (Config.DisplayReport)
				output.PrintError(string.Format(GameMessages.ErbStatistics, enabledLineCount, labelDic.Count, usedLabelCount));
			if (Config.AllowFunctionOverloading && Config.WarnFunctionOverloading)
			{
				List<string> overloadedList = GlobalStatic.IdentifierDictionary.GetOverloadedList(labelDic);
				if (overloadedList.Count > 0)
				{
					output.NewLine();
					output.PrintError(GameMessages.WarningBanner);
					foreach (string funcname in overloadedList)
					{
						output.PrintSystemLine(string.Format(GameMessages.SystemFunctionOverwritten, funcname));
					}
					output.PrintSystemLine(GameMessages.ScriptMayNotWorkAsIntended);
					output.NewLine();
					output.PrintSystemLine(GameMessages.WarningForEmueraScripts);
					output.PrintSystemLine(GameMessages.NoEffectOnEramaker);
					output.PrintSystemLine(GameMessages.DisableWarningHint);
					output.PrintSystemLine(GameMessages.WarningBannerEnd);
				}
			}
		}


		public Dictionary<string, Int64> warningDic = new Dictionary<string, Int64>();
		private void printFunctionNotFoundWarning(string str, LogicalLine line, int level, bool isError)
		{
			if (Program.AnalysisMode)
			{
                long l = 0;
				if (warningDic.TryGetValue(str, out l))
					warningDic[str] = l + 1;
				else
					warningDic.Add(str, 1);
				return;
			}
			if (isError)
			{
				line.IsError = true;
				line.ErrMes = str;
			}
			if (level < Config.DisplayWarningLevel)
				return;
			bool ignore = false;
			DisplayWarningFlag warnFlag = Config.FunctionNotFoundWarning;
			if (warnFlag == DisplayWarningFlag.IGNORE)
				ignore = true;
			else if (warnFlag == DisplayWarningFlag.DISPLAY)
				ignore = false;
			else if (warnFlag == DisplayWarningFlag.ONCE)
			{

				string filename = line.Position.Filename.ToUpper();
				if (!string.IsNullOrEmpty(filename))
				{
					if (ignoredFNFWarningFileList.Contains(filename))
					{
						ignore = true;
					}
					else
					{
						ignore = false;
						ignoredFNFWarningFileList.Add(filename);
					}
				}
			}
			if (ignore && !Program.AnalysisMode)
			{
				ignoredFNFWarningCount++;
				return;
			}
			ParserMediator.Warn(str, line, level, isError, false);
		}

		private void checkFunctionWithCatch(FunctionLabelLine label)
		{//catching an Error here should normally never happen. Equivalent to ExeEE.
			try
			{
				//System.Windows.Forms.//Application.DoEvents();
				string filename = label.Position.Filename.ToUpper();
				setArgument(label);
				nestCheck(label);
                setJumpTo(label);
			}
			catch (Exception exc)
			{
				uEmuera.Media.SystemSounds.Hand.Play();
                //1756beta2+v6.1 to make fixes more efficient, if any Error not handled in parsing comes out, throw a stack trace
                string errmes = (exc is EmueraException) ? exc.Message : exc.GetType().ToString() + ":" + exc.Message;
                ParserMediator.Warn("@" + label.LabelName + GameMessages.T(" Error while parsing: ") + errmes, label, 2, true, false, !(exc is EmueraException) ? exc.StackTrace : null);
                label.ErrMes = GameMessages.T("A function that failed to parse at load time was called");
			}
			finally
			{
				parentProcess.SetBackgroundScanLine(null);
			}

		}

		private void setArgument(FunctionLabelLine label)
		{
			//pass 1/3
			//argument analysis etc.
			LogicalLine nextLine = label;
			bool inMethod = label.IsMethod;
			while (true)
			{
				nextLine = nextLine.NextLine;
				parentProcess.SetBackgroundScanLine(nextLine);
                if (!(nextLine is InstructionLine func))
                {
                    if ((nextLine is NullLine) || (nextLine is FunctionLabelLine))
                        break;
                    continue;
                }
                if (inMethod)
				{
					if (!func.Function.IsMethodSafe())
					{
						ParserMediator.Warn(func.Function.Name + GameMessages.T(" command cannot be used inside #FUNCTION"), nextLine, 2, true, false);
						continue;
					}
				}
                if (Config.NeedReduceArgumentOnLoad || Program.AnalysisMode || func.Function.IsForceSetArg())
                    ArgumentParser.SetArgumentTo(func);
			}
		}

		private void nestCheck(FunctionLabelLine label)
		{
			//pass 2/3
			//correspondence checks such as IF-ELSEIF-ENDIF, REPEAT-REND
			//PRINTDATA family also checked here
			LogicalLine nextLine = label;
			List<InstructionLine> tempLineList = new List<InstructionLine>();
			Stack<InstructionLine> nestStack = new Stack<InstructionLine>();
            Stack<InstructionLine> SelectcaseStack = new Stack<InstructionLine>();
			InstructionLine pairLine = null;
			while (true)
			{
				nextLine = nextLine.NextLine;
				parentProcess.SetBackgroundScanLine(nextLine);
                if ((nextLine is NullLine) || (nextLine is FunctionLabelLine))
                    break;
                if (!(nextLine is InstructionLine))
                {
                    if (nextLine is GotoLabelLine)
                    {
                        InstructionLine currentBaseFunc = nestStack.Count == 0 ? null : nestStack.Peek();
                        if (currentBaseFunc != null)
                        {
                            if ((currentBaseFunc.FunctionCode == FunctionCode.PRINTDATA)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATAL)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATAW)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATAD)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATADL)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATADW)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATAK)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATAKL)
                                || (currentBaseFunc.FunctionCode == FunctionCode.PRINTDATAKW)
                                || (currentBaseFunc.FunctionCode == FunctionCode.STRDATA)
                                || (currentBaseFunc.FunctionCode == FunctionCode.DATALIST)
                                || (currentBaseFunc.FunctionCode == FunctionCode.TRYCALLLIST)
                                || (currentBaseFunc.FunctionCode == FunctionCode.TRYJUMPLIST)
                                || (currentBaseFunc.FunctionCode == FunctionCode.TRYGOTOLIST))
                                //|| (currentBaseFunc.FunctionCode == FunctionCode.SELECTCASE))
                            {
                                ParserMediator.Warn(currentBaseFunc.Function.Name + GameMessages.T(" syntax cannot contain a $ label definition"), nextLine, 2, true, false);
                            }
                        }
                    }
                    continue;
                }
				InstructionLine func = (InstructionLine)nextLine;
				InstructionLine baseFunc = nestStack.Count == 0 ? null : nestStack.Peek();
				if (baseFunc != null)
				{
					if ((baseFunc.Function.IsPrintData() || baseFunc.FunctionCode == FunctionCode.STRDATA) )
					{
						if ((func.FunctionCode != FunctionCode.DATA) && (func.FunctionCode != FunctionCode.DATAFORM) && (func.FunctionCode != FunctionCode.DATALIST)
							&& (func.FunctionCode != FunctionCode.ENDLIST) && (func.FunctionCode != FunctionCode.ENDDATA))
						{
							ParserMediator.Warn(baseFunc.Function.Name + GameMessages.T(" syntax contains a command that cannot be used: \'") + func.Function.Name + GameMessages.T("\'"), func, 2, true, false);
							continue;
						}
					}
					else if (baseFunc.FunctionCode == FunctionCode.DATALIST)
					{
						if ((func.FunctionCode != FunctionCode.DATA) && (func.FunctionCode != FunctionCode.DATAFORM) && (func.FunctionCode != FunctionCode.ENDLIST))
						{
							ParserMediator.Warn(GameMessages.T("DATALIST syntax contains a command that cannot be used: \'") + func.Function.Name + GameMessages.T("\'"), func, 2, true, false);
							continue;
						}
					}
					else if ((baseFunc.FunctionCode == FunctionCode.TRYCALLLIST) || (baseFunc.FunctionCode == FunctionCode.TRYJUMPLIST) || (baseFunc.FunctionCode == FunctionCode.TRYGOTOLIST))
					{
						if ((func.FunctionCode != FunctionCode.FUNC) && (func.FunctionCode != FunctionCode.ENDFUNC))
						{
							ParserMediator.Warn(baseFunc.Function.Name + GameMessages.T(" syntax contains a command that cannot be used: \'") + func.Function.Name + GameMessages.T("\'"), func, 2, true, false);
							continue;
						}
					}
					else if (baseFunc.FunctionCode == FunctionCode.SELECTCASE)
					{
						if ((baseFunc.IfCaseList.Count == 0) && (func.FunctionCode != FunctionCode.CASE) && (func.FunctionCode != FunctionCode.CASEELSE) && (func.FunctionCode != FunctionCode.ENDSELECT))
						{
							ParserMediator.Warn(GameMessages.T("A command outside the branches of the SELECTCASE syntax: \'") + func.Function.Name + GameMessages.T("\'"), func, 2, true, false);
							continue;
						}
					}
				}
				switch (func.FunctionCode)
				{
					case FunctionCode.REPEAT:
						foreach (InstructionLine iLine in nestStack)
						{
							if (iLine.FunctionCode == FunctionCode.REPEAT)
							{
								ParserMediator.Warn(GameMessages.T("REPEAT statements are nested (risk of infinite loop)"), func, 1, false, false);
							}
                            else if (iLine.FunctionCode == FunctionCode.FOR)
                            {
                                VariableTerm cnt = ((SpForNextArgment)iLine.Argument).Cnt;
                                if (cnt.Identifier.Name == "COUNT" && (cnt.isAllConst && cnt.getEl1forArg == 0))
                                {
                                    ParserMediator.Warn(GameMessages.T("REPEAT is called inside a FOR statement that uses COUNT:0 as the counter variable"), func, 1, false, false);
                                }
                            }
                        }
                        if (func.IsError)
                            break;
						nestStack.Push(func);
						break;
					case FunctionCode.IF:
						nestStack.Push(func);
                        func.IfCaseList = new List<InstructionLine>
                        {
                            func
                        };
                        break;
					case FunctionCode.SELECTCASE:
						nestStack.Push(func);
						func.IfCaseList = new List<InstructionLine>();
                        SelectcaseStack.Push(func);
						break;
					case FunctionCode.FOR:
                        //costly but check here for nested Error checks
                        if (func.Argument == null)
                            ArgumentParser.SetArgumentTo(func);
                        //since argument analysis above is guaranteed to have been done,
                        //this only becomes false when an Error occurred in argument analysis
                        if (func.Argument != null)
                        {
                            VariableTerm Cnt = ((SpForNextArgment)func.Argument).Cnt;
                            if (Cnt.Identifier.Name == "COUNT")
                            {
                                foreach (InstructionLine iLine in nestStack)
                                {
                                    if (iLine.FunctionCode == FunctionCode.REPEAT && (Cnt.isAllConst && Cnt.getEl1forArg == 0))
                                    {
                                        ParserMediator.Warn(GameMessages.T("FOR using COUNT:0 as the counter variable is used inside a REPEAT statement (risk of infinite loop)"), func, 1, false, false);
                                    }
                                    else if (iLine.FunctionCode == FunctionCode.FOR)
                                    {
                                        VariableTerm destCnt = ((SpForNextArgment)iLine.Argument).Cnt;
                                        if (destCnt.Identifier.Name == "COUNT" && (Cnt.isAllConst && destCnt.isAllConst && destCnt.getEl1forArg == Cnt.getEl1forArg))
                                        {
                                            ParserMediator.Warn(GameMessages.T("FOR statements using COUNT:") + Cnt.getEl1forArg.ToString() + GameMessages.T(" as the counter variable are nested (risk of infinite loop)"), func, 1, false, false);
                                        }
                                    }
                                }
                            }
                        }
                        if (func.IsError)
                            break;
                        nestStack.Push(func);
                        break;
                    case FunctionCode.WHILE:
					case FunctionCode.TRYCGOTO:
					case FunctionCode.TRYCJUMP:
					case FunctionCode.TRYCCALL:
					case FunctionCode.TRYCGOTOFORM:
					case FunctionCode.TRYCJUMPFORM:
					case FunctionCode.TRYCCALLFORM:
					case FunctionCode.DO:
						nestStack.Push(func);
						break;
					case FunctionCode.BREAK:
					case FunctionCode.CONTINUE:
						InstructionLine[] array = nestStack.ToArray();
						for (int i = 0; i < array.Length; i++)
						{
							if ((array[i].FunctionCode == FunctionCode.REPEAT)
								|| (array[i].FunctionCode == FunctionCode.FOR)
								|| (array[i].FunctionCode == FunctionCode.WHILE)
								|| (array[i].FunctionCode == FunctionCode.DO))
							{
								pairLine = array[i];
								break;
							}
						}
						if (pairLine == null)
						{
							ParserMediator.Warn(GameMessages.T("Used outside REPEAT, FOR, WHILE, DO: ") + func.Function.Name + GameMessages.T(" statement"), func, 2, true, false);
							break;
						}
						func.JumpTo = pairLine;
						break;

					case FunctionCode.ELSEIF:
					case FunctionCode.ELSE:
						{
							//1.725 I had assumed the design that Stack<T>.Peek() returns null when the Stack is empty.
							InstructionLine ifLine = nestStack.Count == 0 ? null : nestStack.Peek();
							if ((ifLine == null) || (ifLine.FunctionCode != FunctionCode.IF))
							{
								ParserMediator.Warn(GameMessages.T("Used outside IF~ENDIF: ") + func.Function.Name + GameMessages.T(" statement"), func, 2, true, false);
                                break;
							}
							if (ifLine.IfCaseList[ifLine.IfCaseList.Count - 1].FunctionCode == FunctionCode.ELSE)
								ParserMediator.Warn(GameMessages.T("Used after an ELSE statement: ") + func.Function.Name + GameMessages.T(" statement"), func, 1, false, false);
							ifLine.IfCaseList.Add(func);
						}
						break;
					case FunctionCode.ENDIF:
						{
							InstructionLine ifLine = nestStack.Count == 0 ? null : nestStack.Peek();
							if ((ifLine == null) || (ifLine.FunctionCode != FunctionCode.IF))
							{
								ParserMediator.Warn(GameMessages.T("ENDIF statement has no matching IF"), func, 2, true, false);
								break;
							}
							foreach (InstructionLine ifelseifLine in ifLine.IfCaseList)
							{
								ifelseifLine.JumpTo = func;
							}
							nestStack.Pop();
						}
						break;
					case FunctionCode.CASE:
					case FunctionCode.CASEELSE:
						{
							InstructionLine selectLine = nestStack.Count == 0 ? null : nestStack.Peek();
							if ((selectLine == null) || (selectLine.FunctionCode != FunctionCode.SELECTCASE && SelectcaseStack.Count == 0))
							{
								ParserMediator.Warn(GameMessages.T("Used outside SELECTCASE~ENDSELECT: ") + func.Function.Name + GameMessages.T(" statement"), func, 2, true, false);
								break;
							}
                            else if (selectLine.FunctionCode != FunctionCode.SELECTCASE && SelectcaseStack.Count > 0)
                            {
                                do
                                {
                                    ParserMediator.Warn(selectLine.Function.Name + GameMessages.T(" statement has no matching ") + FunctionIdentifier.getMatchFunction(selectLine.FunctionCode) + GameMessages.T(" when reaching the ") + func.Function.Name + GameMessages.T(" statement"), func, 2, true, false);
                                    //so that IF etc. cannot be closed across this.
                                    nestStack.Pop();
                                    //if (nestStack.Count > 0)　//whether it's empty can be determined below, so there is no need to check this
                                    selectLine = nestStack.Count == 0 ? null : nestStack.Peek(); //incidentally it will never be null (because when there is no SELECTCASE it is filtered above)
                                } while (selectLine != null && selectLine.FunctionCode != FunctionCode.SELECTCASE);
                                break;
                            }
							if ((selectLine.IfCaseList.Count > 0) &&
								(selectLine.IfCaseList[selectLine.IfCaseList.Count - 1].FunctionCode == FunctionCode.CASEELSE))
								ParserMediator.Warn(GameMessages.T("Used after a CASEELSE statement: ") + func.Function.Name + GameMessages.T(" statement"), func, 1, false, false);
							selectLine.IfCaseList.Add(func);
						}
						break;
					case FunctionCode.ENDSELECT:
						{
							InstructionLine selectLine = nestStack.Count == 0 ? null : nestStack.Peek();
							if ((selectLine == null) || (selectLine.FunctionCode != FunctionCode.SELECTCASE && SelectcaseStack.Count == 0))
							{
								ParserMediator.Warn(GameMessages.T("ENDSELECT statement has no matching SELECTCASE"), func, 2, true, false);
                                break;
							}
                            else if (selectLine.FunctionCode != FunctionCode.SELECTCASE && SelectcaseStack.Count > 0)
                            {
                                do
                                {
                                    ParserMediator.Warn(selectLine.Function.Name + GameMessages.T(" statement has no matching ") + FunctionIdentifier.getMatchFunction(selectLine.FunctionCode) + GameMessages.T(" when reaching the ") + func.Function.Name + GameMessages.T(" statement"), func, 2, true, false);
                                    //so that IF etc. cannot be closed across this.
                                    nestStack.Pop();
                                    //if (nestStack.Count > 0)　//whether it's empty can be determined below, so there is no need to check this
                                    selectLine = nestStack.Count == 0 ? null : nestStack.Peek(); //incidentally it will never be null (because when there is no SELECTCASE it is filtered above)
                                } while (selectLine != null && selectLine.FunctionCode != FunctionCode.SELECTCASE);　
                                //for now, close the spanning matching SELECTCASE
                                SelectcaseStack.Pop();
                                //if this one isn't popped, SELECTCASE will be paired with 2 ENDSELECTs
                                nestStack.Pop();
                                break;
                            }
                            nestStack.Pop();
                            SelectcaseStack.Pop();
							selectLine.JumpTo = func;
							if (selectLine.IsError)
								break;
							IOperandTerm term = ((ExpressionArgument)selectLine.Argument).Term;
							if (term == null)
							{
								ParserMediator.Warn(GameMessages.T("SELECTCASE has no argument"), selectLine, 2, true, false);
								break;
							}
							foreach (InstructionLine caseLine in selectLine.IfCaseList)
							{
								caseLine.JumpTo = func;
								if (caseLine.IsError)
									continue;
								if (caseLine.FunctionCode == FunctionCode.CASEELSE)
									continue;
								CaseExpression[] caseExps = ((CaseArgument)caseLine.Argument).CaseExps;
								if (caseExps.Length == 0)
									ParserMediator.Warn(GameMessages.T("CASE has no argument"), caseLine, 2, true, false);

								foreach (CaseExpression exp in caseExps)
								{
									if (exp.GetOperandType() != term.GetOperandType())
										ParserMediator.Warn(GameMessages.T("The type of the CASE argument does not match the SELECTCASE"), caseLine, 2, true, false);
								}

							}
						}
						break;
					case FunctionCode.REND:
					case FunctionCode.NEXT:
					case FunctionCode.WEND:
					case FunctionCode.LOOP:
						FunctionCode parentFunc = FunctionIdentifier.getParentFunc(func.FunctionCode);
						//if (parentFunc == FunctionCode.__NULL__)
						//    throw new ExeEE("something wrong?");
						if ((nestStack.Count == 0)
							|| (nestStack.Peek().FunctionCode != parentFunc))
						{
                            ParserMediator.Warn(GameMessages.T("No matching ") + parentFunc.ToString() + GameMessages.T(" for the ") + func.Function.Name + GameMessages.T(" statement"), func, 2, true, false);
							break;
						}
						pairLine = nestStack.Pop();//REPEAT
						func.JumpTo = pairLine;
						pairLine.JumpTo = func;
						break;
					case FunctionCode.CATCH:
						pairLine = nestStack.Count == 0 ? null : nestStack.Peek();
						if ((pairLine == null)
							|| ((pairLine.FunctionCode != FunctionCode.TRYCGOTO)
							&& (pairLine.FunctionCode != FunctionCode.TRYCCALL)
							&& (pairLine.FunctionCode != FunctionCode.TRYCJUMP)
							&& (pairLine.FunctionCode != FunctionCode.TRYCGOTOFORM)
							&& (pairLine.FunctionCode != FunctionCode.TRYCCALLFORM)
							&& (pairLine.FunctionCode != FunctionCode.TRYCJUMPFORM)))
						{
							ParserMediator.Warn(GameMessages.T("No matching TRYC-family command"), func, 2, true, false);
							break;
						}
						pairLine = nestStack.Pop();//TRYC
						pairLine.JumpToEndCatch = func;//tell TRYC the position of CATCH
						nestStack.Push(func);
						break;
					case FunctionCode.ENDCATCH:
						if ((nestStack.Count == 0)
							|| (nestStack.Peek().FunctionCode != FunctionCode.CATCH))
						{
							ParserMediator.Warn(GameMessages.T("ENDCATCH has no matching CATCH"), func, 2, true, false);
							break;
						}
						pairLine = nestStack.Pop();//CATCH
						pairLine.JumpToEndCatch = func;//tell CATCH the position of ENDCATCH
						break;
                    case FunctionCode.PRINTDATA:
                    case FunctionCode.PRINTDATAL:
                    case FunctionCode.PRINTDATAW:
                    case FunctionCode.PRINTDATAD:
                    case FunctionCode.PRINTDATADL:
                    case FunctionCode.PRINTDATADW:
                    case FunctionCode.PRINTDATAK:
                    case FunctionCode.PRINTDATAKL:
                    case FunctionCode.PRINTDATAKW:
                        {
                            foreach (InstructionLine iLine in nestStack)
                            {
                                if (iLine.Function.IsPrintData())
                                {
                                    ParserMediator.Warn(GameMessages.T("PRINTDATA-family commands are nested"), func, 2, true, false);
                                    break;
                                }
                                if (iLine.FunctionCode == FunctionCode.STRDATA)
                                {
                                    ParserMediator.Warn(GameMessages.T("A STRDATA-family command is included inside a PRINTDATA-family command"), func, 2, true, false);
                                    break;
                                }
                            }
                            if (func.IsError)
                                break;
                            func.dataList = new List<List<InstructionLine>>();
                            nestStack.Push(func);
                            break;
                        }
                    case FunctionCode.STRDATA:
                        {
                            foreach (InstructionLine iLine in nestStack)
                            {
                                if (iLine.FunctionCode == FunctionCode.STRDATA)
                                {
                                    ParserMediator.Warn(GameMessages.T("STRDATA commands are nested"), func, 2, true, false);
                                    break;
                                }
                                if (iLine.Function.IsPrintData())
                                {
                                    ParserMediator.Warn(GameMessages.T("A PRINTDATA-family command is included inside a STRDATA-family command"), func, 2, true, false);
                                    break;
                                }
                            }
                            if (func.IsError)
                                break;
                            func.dataList = new List<List<InstructionLine>>();
                            nestStack.Push(func);
                            break;
                        }
                    case FunctionCode.DATALIST:
                        {
                            InstructionLine pline = (nestStack.Count == 0) ? null : nestStack.Peek();
                            if ((pline == null) || ((!pline.Function.IsPrintData()) && (pline.FunctionCode != FunctionCode.STRDATA)))
                            {
                                ParserMediator.Warn(GameMessages.T("DATALIST has no matching PRINTDATA-family command"), func, 2, true, false);
                                break;
                            }
                            tempLineList = new List<InstructionLine>();
                            nestStack.Push(func);

                            break;
                        }
                    case FunctionCode.ENDLIST:
                        {
                            if ((nestStack.Count == 0) || (nestStack.Peek().FunctionCode != FunctionCode.DATALIST))
                            {
                                ParserMediator.Warn(GameMessages.T("ENDLIST has no matching DATALIST"), func, 2, true, false);
                                break;
                            }
                            if (tempLineList.Count == 0)
                                ParserMediator.Warn(GameMessages.T("No display data was given to the DATALIST command (this DATALIST will display an empty string)"), func, 1, false, false);
                            nestStack.Pop();
                            nestStack.Peek().dataList.Add(tempLineList);
                            break;
                        }
                    case FunctionCode.DATA:
                    case FunctionCode.DATAFORM:
                        {
                            InstructionLine pdata = (nestStack.Count == 0) ? null : nestStack.Peek();
                            if ((pdata == null) || (!pdata.Function.IsPrintData() && pdata.FunctionCode != FunctionCode.DATALIST && pdata.FunctionCode != FunctionCode.STRDATA))
                            {
                                ParserMediator.Warn(GameMessages.T("No matching PRINTDATA-family command for the ") + func.Function.Name + GameMessages.T(" command"), func, 2, true, false);
                                break;
                            }
                            List<InstructionLine> iList = new List<InstructionLine>();
                            if (pdata.FunctionCode != FunctionCode.DATALIST)
                            {
                                iList.Add(func);
                                pdata.dataList.Add(iList);
                            }
                            else
                                tempLineList.Add(func);
                            break;
                        }
                    case FunctionCode.ENDDATA:
                        {
                            InstructionLine pline = (nestStack.Count == 0) ? null : nestStack.Peek();
                            if ((pline == null) || ((!pline.Function.IsPrintData()) && (pline.FunctionCode != FunctionCode.STRDATA)))
                            {
                                ParserMediator.Warn(GameMessages.T("No matching PRINTDATA-family command or STRDATA for the ") + func.Function.Name + GameMessages.T(" command"), func, 2, true, false);
                                break;
                            }
                            if (pline.FunctionCode == FunctionCode.DATALIST)
                                ParserMediator.Warn(GameMessages.T("DATALIST is not closed"), func, 2, true, false);
                            if (pline.dataList.Count == 0)
                                ParserMediator.Warn(pline.Function.Name + GameMessages.T(" command has no display data (this command will be ignored)"), func, 1, false, false);
                            pline.JumpTo = func;
                            nestStack.Pop();
                            break;
                        }
					case FunctionCode.TRYCALLLIST:
					case FunctionCode.TRYJUMPLIST:
					case FunctionCode.TRYGOTOLIST:
						foreach (InstructionLine iLine in nestStack)
						{
							if (iLine.FunctionCode == FunctionCode.TRYCALLLIST || iLine.FunctionCode == FunctionCode.TRYJUMPLIST || iLine.FunctionCode == FunctionCode.TRYGOTOLIST)
							{
								ParserMediator.Warn(GameMessages.T("TRYCALLLIST-family commands are nested"), func, 2, true, false);
								break;
							}
						}
						if (func.IsError)
							break;
						func.callList = new List<InstructionLine>();
						nestStack.Push(func);
						break;
					case FunctionCode.FUNC:
						{
							InstructionLine pFunc = (nestStack.Count == 0) ? null : nestStack.Peek();
							if ((pFunc == null) ||
								(pFunc.FunctionCode != FunctionCode.TRYCALLLIST && pFunc.FunctionCode != FunctionCode.TRYJUMPLIST && pFunc.FunctionCode != FunctionCode.TRYGOTOLIST))
							{
								ParserMediator.Warn(GameMessages.T("No matching TRYCALLLIST-family command for the ") + func.Function.Name + GameMessages.T(" command"), func, 2, true, false);
								break;
							}
                            if (func.Argument == null)
                            {
                                ParserMediator.Warn(GameMessages.T("An invalid ") + func.Function.Name + GameMessages.T(" exists inside a TRYCALLLIST-family command"), pFunc, 2, true, false);
                                break;
                            }
							if (pFunc.FunctionCode == FunctionCode.TRYGOTOLIST)
							{
								if (((SpCallArgment)func.Argument).SubNames.Length != 0)
								{
									ParserMediator.Warn(GameMessages.T("[~~] is set as the call target of TRYGOTOLIST"), func, 2, true, false);
									break;
								}
								if (((SpCallArgment)func.Argument).RowArgs.Length != 0)
								{
									ParserMediator.Warn(GameMessages.T("An argument is set as the call target of TRYGOTOLIST"), func, 2, true, false);
									break;
								}
							}
							pFunc.callList.Add(func);
							break;
						}
					case FunctionCode.ENDFUNC:
						InstructionLine pf = (nestStack.Count == 0) ? null : nestStack.Peek();
						if ((pf == null) ||
							(pf.FunctionCode != FunctionCode.TRYCALLLIST && pf.FunctionCode != FunctionCode.TRYJUMPLIST && pf.FunctionCode != FunctionCode.TRYGOTOLIST))
						{
							ParserMediator.Warn(GameMessages.T("No matching TRYCALLLIST-family command for the ") + func.Function.Name + GameMessages.T(" command"), func, 2, true, false);
							break;
						}
						pf.JumpTo = func;
						nestStack.Pop();
						break;
					case FunctionCode.NOSKIP:
						foreach (InstructionLine iLine in nestStack)
						{
							if (iLine.FunctionCode == FunctionCode.NOSKIP)
							{
								ParserMediator.Warn(GameMessages.T("NOSKIP-family commands are nested"), func, 2, true, false);
								break;
							}
						}
						if (func.IsError)
							break;
						nestStack.Push(func);
						break;
					case FunctionCode.ENDNOSKIP:
						InstructionLine pfunc = (nestStack.Count == 0) ? null : nestStack.Peek();
						if ((pfunc == null) ||
							(pfunc.FunctionCode != FunctionCode.NOSKIP))
						{
							ParserMediator.Warn(GameMessages.T("No matching NOSKIP-family command for the ") + func.Function.Name + GameMessages.T(" command"), func, 2, true, false);
							break;
						}
						//for Error handling
						pfunc.JumpTo = func;
						func.JumpTo = pfunc;
						nestStack.Pop();
						break;
				}

			}

			while (nestStack.Count != 0)
			{
				InstructionLine func = nestStack.Pop();
				string funcName = func.Function.Name;
				string funcMatch = FunctionIdentifier.getMatchFunction(func.FunctionCode);
				if (func != null)
					ParserMediator.Warn(funcName + GameMessages.T(": no matching ") + funcMatch + GameMessages.T(" found"), func, 2, true, false);
				else
					ParserMediator.Warn(GameMessages.T("Default Error (Emuera configuration omission)"), func, 2, true, false);
			}
            //clear the used stacks
            SelectcaseStack.Clear();
		}

		private void setJumpTo(FunctionLabelLine label)
		{
			//pass 3/3
			//set the jump targets of flow control instructions
			LogicalLine nextLine = label;
			int depth = label.Depth;
			if (depth < 0)
				depth = -2;
			while (true)
			{
				nextLine = nextLine.NextLine;
                if (!(nextLine is InstructionLine func))
                {
                    if ((nextLine is NullLine) || (nextLine is FunctionLabelLine))
                        break;
                    continue;
                }
                if (func.IsError)
					continue;
				parentProcess.SetBackgroundScanLine(func);

				if (func.Function.Instruction != null)
				{
					string FunctionNotFoundName = null;
					try
					{
						func.Function.Instruction.SetJumpTo(ref useCallForm, func, depth, ref FunctionNotFoundName);
					}
					catch (CodeEE e)
					{
						ParserMediator.Warn(e.Message, func, 2, true, false);
						continue;
					}
					if (FunctionNotFoundName != null)
					{
						// Progressive/lazy loading: if the missing function is defined in a
						// file that has not been compiled yet (lazy compiler or legacy
						// background loader), suppress the (false) warning. The runtime
						// fallback in CALL_Instruction resolves it on demand.
						if (FunctionResolver.IsKnown(FunctionNotFoundName))
						{
							FunctionNotFoundName = null;
						}
						else
						{
							if (!Program.AnalysisMode)
								printFunctionNotFoundWarning(GameMessages.T("The specified function name \"@") + FunctionNotFoundName + GameMessages.T("\" does not exist"), func, 2, true);
							else
								printFunctionNotFoundWarning(FunctionNotFoundName, func, 2, true);
						}
					}
                    continue;
				}
			if ((func.FunctionCode == FunctionCode.TRYCALLLIST) || (func.FunctionCode == FunctionCode.TRYJUMPLIST))
				useCallForm = true;
		}
	}

	// ================================================================== //
	//  Progressive Loading Support                                        //
	// ================================================================== //

	/// <summary>
	/// Performs a quick scan of all ERB files to build a function-name → file-path
	/// index without fully parsing the files.  Only lines starting with '@' are
	/// examined; the rest of each file is skipped.
	/// </summary>
	internal static Dictionary<string, string> QuickScanFunctionNames(
		List<KeyValuePair<string, string>> erbFiles)
	{
		var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var kv in erbFiles)
		{
			try
			{
				string filePath = kv.Value;
				using (var sr = new System.IO.StreamReader(
					filePath, System.Text.Encoding.GetEncoding("shift-jis"), true))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						if (line.Length < 2) continue;
						if (line[0] != '@') continue;
						if (line[1] == '@') continue; // @@ is not a function label
						// Extract function name: everything after @ up to the argument
						// list '(' or whitespace / comment / comma. @NAME(ARG) and
						// @NAME,OTHER are both valid label syntax.
						int end = 1;
						while (end < line.Length)
						{
							char c = line[end];
							if (c == '(' || c == ' ' || c == '\t' || c == ';' || c == ',')
								break;
							end++;
						}
						string funcName = line.Substring(1, end - 1).Trim();
						if (funcName.Length == 0) continue;
						if (Config.ICFunction)
							funcName = funcName.ToUpper();
						if (!index.ContainsKey(funcName))
							index[funcName] = filePath;
					}
				}
			}
			catch { /* skip unreadable files */ }
		}
		return index;
	}

	/// <summary>
	/// Loads ERB files progressively.
	/// <list type="number">
	///   <item>Loads priority files (SYSTEM_*.ERB and a few hardcoded system functions)
	///         synchronously, then signals the game to start.</item>
	///   <item>Loads remaining ERBs in a background thread via
	///         <see cref="BackgroundErbLoader"/>.</item>
	/// </list>
	/// When a function that has not yet been loaded is called, the game thread
	/// temporarily waits for the background loader to finish loading the file
	/// that contains it (see <see cref="BackgroundErbLoader.WaitForFunction"/>).
	/// </summary>
	public bool LoadErbFilesProgressive(string erbDir, bool displayReport, LabelDictionary labelDictionary)
	{
		labelDic = labelDictionary;
		labelDic.Initialized = false;
		labelDic.RemoveAll();

		List<KeyValuePair<string, string>> erbFiles = Config.GetFiles(erbDir, "*.ERB");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		erbFiles.AddRange(Config.GetFiles(erbDir, "*.erb"));
#endif

		if (erbFiles.Count == 0)
			return LoadErbFiles(erbDir, displayReport, labelDictionary);

		noError = true;
		List<string> isOnlyEvent = new List<string>();

		try
		{
			// ---------------------------------------------------------- //
			//  Phase 1: Quick-scan to build function → file index         //
			// ---------------------------------------------------------- //
			GenericUtils.SetLoadingStatus("Scanning ERB files...");
			var functionIndex = QuickScanFunctionNames(erbFiles);

			// ---------------------------------------------------------- //
			//  Phase 2: Separate priority files from background files     //
			// ---------------------------------------------------------- //
			var priorityFiles    = new List<KeyValuePair<string, string>>();
			var backgroundFiles  = new List<KeyValuePair<string, string>>();

			foreach (var kv in erbFiles)
			{
				string name = System.IO.Path.GetFileName(kv.Key).ToUpper();
				bool isPriority =
					name.StartsWith("SYSTEM_") ||
					name == "GAMEBASE.ERB"     ||
					name == "TITLE.ERB"        ||
					name == "START.ERB"        ||
					name == "COMMON.ERB";
				if (isPriority)
					priorityFiles.Add(kv);
				else
					backgroundFiles.Add(kv);
			}

			// Guarantee we always have something to start with.
			if (priorityFiles.Count == 0)
			{
				int seed = System.Math.Min(30, erbFiles.Count);
				for (int i = 0; i < seed; i++) priorityFiles.Add(erbFiles[i]);
				backgroundFiles.RemoveRange(0, seed);
			}

			// ---------------------------------------------------------- //
			//  Phase 3: Load priority files synchronously                 //
			// ---------------------------------------------------------- //
			int total = erbFiles.Count;
			var priorityLabels = new List<FunctionLabelLine>();
			for (int i = 0; i < priorityFiles.Count; i++)
			{
				GenericUtils.SetLoadingStatus(
					$"Loading ERB (priority {i + 1}/{priorityFiles.Count}): {priorityFiles[i].Key}");
				loadErb(priorityFiles[i].Value, priorityFiles[i].Key, isOnlyEvent);
				priorityLabels.AddRange(labelDic.LatestFileLabels);
			}

			ParserMediator.FlushWarningList();
			GenericUtils.SetLoadingStatus("Building function list (priority)...");
			setLabelsArg();   // process priority labels + SortLabels()
			labelDic.Initialized = true; // matching original LoadErbFiles ordering (set before syntax check)

			// ---------------------------------------------------------- //
			//  Phase 4: Activate background loading for remaining files   //
			//  (Must run before the priority syntax check so that
			//   checkScript's "function not found" detection can consult
			//   IsFunctionPending() and suppress false warnings for
			//   files that load later in the background.)
			// ---------------------------------------------------------- //
			if (backgroundFiles.Count > 0)
			{
				GenericUtils.SetLoadingStatus(
					$"Starting game (loading {backgroundFiles.Count} remaining files in background)...");
				// Create a second ErbLoader instance for background use so that
				// the foreground ErbLoader's state is not touched by the background thread.
				var bgLoader = new ErbLoader(output, exm, parentProcess);
				bgLoader.labelDic = labelDic;
				var alreadyLoaded = new List<string>(priorityFiles.Count);
				foreach (var kv in priorityFiles) alreadyLoaded.Add(kv.Value);
				BackgroundErbLoader.Activate(functionIndex, backgroundFiles, labelDic, bgLoader, alreadyLoaded);
			}

			// ---------------------------------------------------------- //
			//  Phase 5: Syntax-check the priority functions               //
			//  (setArgument + nestCheck + setJumpTo). Control flow such as
			//  IF/REPEAT must be built before the game starts, otherwise
			//  runtime NREs occur. Cross-file references to background files
			//  stay unresolved here and are fixed at runtime by the CALL
			//  fallback in CALL_Instruction.DoInstruction.
			// ---------------------------------------------------------- //
			GenericUtils.SetLoadingStatus("Checking syntax (priority)...");
			foreach (FunctionLabelLine label in priorityLabels)
				checkFunctionWithCatch(label);
			ParserMediator.FlushWarningList();
		}
		catch (Exception e)
		{
			ParserMediator.FlushWarningList();
			uEmuera.Media.SystemSounds.Hand.Play();
			output.PrintError(GameMessages.UnexpectedError + Program.ExeName);
			output.PrintError(e.GetType().ToString() + ":" + e.Message);
			return false;
		}
		finally
		{
			parentProcess.SetBackgroundScanLine(null);
			isOnlyEvent.Clear();
		}
		return noError;
	}

	// ------------------------------------------------------------------ //
	//  Interpreter-owned lazy loading (Phase 6 — Fast boot)                //
	// ------------------------------------------------------------------ //

	/// <summary>
	/// Interpreter-owned lazy ERB loader (Phase 6 — Fast boot).
	///
	/// <para>Compiles only the file containing the catalogued SYSTEM_TITLE declaration
	/// synchronously so a custom title can start quickly, then defers every other file.
	/// When no custom title exists, no ERB body is compiled before the standard title.
	/// Deferred files are compiled on demand by <see cref="OnDemandErbCompiler"/>
	/// from the interpreter thread the first time the game references one of their
	/// functions. Unlike <see cref="LoadErbFilesProgressive"/> there is NO background
	/// thread mutating semantic state — all compilation happens on the interpreter
	/// thread, so this path is race-free by construction.</para>
	///
	/// <para>Boot-time cost is O(priority files); full-parallel work is replaced by
	/// per-file hitches at first reference, which is the documented Fast-boot
	/// trade-off (no startup stall, no semantic race).</para>
	/// </summary>
	public bool LoadErbFilesLazy(string erbDir, bool displayReport, LabelDictionary labelDictionary)
	{
		labelDic = labelDictionary;
		labelDic.Initialized = false;
		labelDic.RemoveAll();

		List<KeyValuePair<string, string>> erbFiles = Config.GetFiles(erbDir, "*.ERB");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		erbFiles.AddRange(Config.GetFiles(erbDir, "*.erb"));
#endif
		if (erbFiles.Count == 0)
			return LoadErbFiles(erbDir, displayReport, labelDictionary);

		noError = true;
		List<string> isOnlyEvent = new List<string>();

		try
		{
			// Function names, not filenames, determine the smallest semantically required
			// body set. A standard title needs no ERB body; a custom SYSTEM_TITLE needs its
			// containing file and nothing else until runtime asks for it.
			var priorityFiles = new List<KeyValuePair<string, string>>();
			var deferredFiles = new List<KeyValuePair<string, string>>();
			var requiredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			FunctionCatalog catalog = FunctionCatalog.Instance;
			if (catalog == null || !catalog.IsReady)
				return LoadErbFiles(erbDir, displayReport, labelDictionary);
			FunctionMetadata systemTitle = catalog.GetFirst("SYSTEM_TITLE");
			if (systemTitle != null)
				requiredPaths.Add(systemTitle.FilePath);
			foreach (var kv in erbFiles)
			{
				if (requiredPaths.Contains(kv.Value))
					priorityFiles.Add(kv);
				else
					deferredFiles.Add(kv);
			}

			// ---- Load priority files synchronously ------------------------ //
			int total = priorityFiles.Count;
			var priorityLabels = new List<FunctionLabelLine>();
			for (int i = 0; i < priorityFiles.Count; i++)
			{
					GenericUtils.SetLoadingStatus(
						$"Loading required ERB ({i + 1}/{total}): {priorityFiles[i].Key}");
				loadErb(priorityFiles[i].Value, priorityFiles[i].Key, isOnlyEvent);
				priorityLabels.AddRange(labelDic.LatestFileLabels);
			}
			ParserMediator.FlushWarningList();
			GenericUtils.SetLoadingStatus("Building required function list...");
			setLabelsArg();
			labelDic.Initialized = true;

			// ---- Activate the on-demand compiler before required syntax checks so
			//      forward references to deferred files are catalogued, not reported missing.
			var alreadyLoaded = new List<string>(priorityFiles.Count);
			foreach (var kv in priorityFiles) alreadyLoaded.Add(kv.Value);
			OnDemandErbCompiler.Activate(this, labelDic, FunctionCatalog.Instance, deferredFiles, alreadyLoaded);

			// ---- Syntax-check the priority functions ---------------------- //
			GenericUtils.SetLoadingStatus("Checking required syntax...");
			foreach (FunctionLabelLine label in priorityLabels)
				checkFunctionWithCatch(label);
			ParserMediator.FlushWarningList();
		}
		catch (Exception e)
		{
			ParserMediator.FlushWarningList();
			uEmuera.Media.SystemSounds.Hand.Play();
			output.PrintError(GameMessages.UnexpectedError + Program.ExeName);
			output.PrintError(e.GetType().ToString() + ":" + e.Message);
			return false;
		}
		finally
		{
			parentProcess.SetBackgroundScanLine(null);
			isOnlyEvent.Clear();
		}
		return noError;
	}

	// ------------------------------------------------------------------ //
    //  Lazy-loading helpers (called by the interpreter-owned compiler)      //
	// ------------------------------------------------------------------ //

	/// <summary>
	/// Loads and links one deferred file on the interpreter thread. A file is published
	/// only after all labels have been parsed, arguments prepared, and control flow linked.
	/// On failure its labels are removed before the caller observes the failure.
	/// </summary>
	internal bool LoadSingleErbLazy(string filepath, string filename, out LazyCompileFailure failure)
	{
		failure = null;
		var isOnlyEvent = new List<string>();
		bool previousNoError = noError;
		try
		{
			// noError is shared with full-load reporting. Isolate this file's parser
			// result so a deferred syntax error can be rolled back deterministically.
			noError = true;
			loadErb(filepath, filename, isOnlyEvent);
			bool fileNoError = noError;
			noError = previousNoError && fileNoError;
			if (!fileNoError)
			{
				failure = FindLazyCompileFailure(filename, filepath);
				labelDic.RemoveLabelWithPath(filename);
				labelDic.SortLabels();
				if (failure == null)
					failure = LazyCompileFailure.CreateGeneric(filename, filepath);
				ParserMediator.ClearWarningList();
				return false;
			}
			List<FunctionLabelLine> labels = new List<FunctionLabelLine>(labelDic.LatestFileLabels);
			setLabelsArg();
			foreach (FunctionLabelLine label in labels)
			{
				checkFunctionWithCatch(label);
				if (label.IsError)
				{
					failure = LazyCompileFailure.CreateParser(filename, filepath, label);
					labelDic.RemoveLabelWithPath(filename);
					labelDic.SortLabels();
					return false;
				}
			}
			labelDic.SortLabels();
			ParserMediator.FlushWarningList();
			return true;
		}
		catch (Exception ex)
		{
			labelDic.RemoveLabelWithPath(filename);
			labelDic.SortLabels();
			failure = LazyCompileFailure.CreateException(filename, filepath, ex);
			ParserMediator.ClearWarningList();
			return false;
		}
		finally
		{
			noError = previousNoError && noError;
			parentProcess.SetBackgroundScanLine(null);
			isOnlyEvent.Clear();
		}
	}

	private LazyCompileFailure FindLazyCompileFailure(string displayName, string filePath)
	{
		foreach (FunctionLabelLine label in labelDic.LatestFileLabels)
		{
			if (label.IsError)
				return LazyCompileFailure.CreateParser(displayName, filePath, label);

			LogicalLine line = label.NextLine;
			while (line != null && !(line is FunctionLabelLine) && !(line is NullLine))
			{
				if (line.IsError)
					return LazyCompileFailure.CreateLine(displayName, filePath, line);
				line = line.NextLine;
			}
		}
		return null;
	}

	/// <summary>
	/// Legacy progressive-loader entry point. Fast boot never calls this method;
	/// retain it only so explicit legacy progressive mode remains source-compatible
	/// until its integration tests are retired.
	/// </summary>
	[Obsolete("Use LoadSingleErbLazy on the interpreter thread.")]
	internal void LoadSingleErbBackground(string filepath, string filename)
	{
		var isOnlyEvent = new List<string>();
		loadErb(filepath, filename, isOnlyEvent);
		BackgroundErbLoader.AcquireWriteLock(() => FlushLabelsBackground());
		CheckScriptSingleFile();
		isOnlyEvent.Clear();
	}

	/// <summary>
	/// Runs setArgument + nestCheck + setJumpTo for the labels of the most recently
	/// loaded file. Referenced functions that are still being loaded in the
	/// background are left unresolved (suppressed in setJumpTo) and are resolved at
	/// runtime by the CALL fallback.
	/// </summary>
	internal void CheckScriptSingleFile()
	{
		List<FunctionLabelLine> labels = labelDic.LatestFileLabels;
		if (labels.Count == 0)
			return;
		foreach (FunctionLabelLine label in labels)
			checkFunctionWithCatch(label);
		ParserMediator.FlushWarningList();
	}

	/// <summary>
	/// Legacy compatibility wrapper. New lazy runtime does not call this method.
	/// </summary>
	internal void FlushLabelsBackground()
	{
		setLabelsArg();   // only processes labels whose .Arg is still null
	}

}
}
