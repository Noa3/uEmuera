using System;
using System.Collections.Generic;
//using Microsoft.VisualBasic;
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
		/// 複数のfileを読む
		/// </summary>
		/// <param name="filepath"></param>
		public bool LoadErbFiles(string erbDir, bool displayReport, LabelDictionary labelDictionary)
		{
			//1.713 labelDicをnewdo位置を変更.
			//checkScript();のwhenpointでExpressionPerserがProcess.instance.LabelDicを必要とdofrom.
			labelDic = labelDictionary;
			labelDic.Initialized = false;
			List<KeyValuePair<string, string>> erbFiles = Config.GetFiles(erbDir, "*.ERB");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            erbFiles.AddRange(Config.GetFiles(erbDir, "*.erb"));
#endif
            List<string> isOnlyEvent = new List<string>();
            noError = true;
			uint starttime = WinmmTimer.TickCount;
			try
			{
				labelDic.RemoveAll();
				for (int i = 0; i < erbFiles.Count; i++)
				{
					string filename = erbFiles[i].Key;
					string file = erbFiles[i].Value;
#if UEMUERA_DEBUG
					if (displayReport)
						output.PrintSystemLine("経過when間:" + (WinmmTimer.TickCount - starttime).ToString("D4") + "ms:" + filename + "Loading---");
#else
					if (displayReport)
						output.PrintSystemLine(filename + "Loading---");
#endif
					//System.Windows.Forms.//Application.DoEvents();
					loadErb(file, filename, isOnlyEvent);
				}
				ParserMediator.FlushWarningList();
#if UEMUERA_DEBUG
				output.PrintSystemLine("経過when間:" + (WinmmTimer.TickCount - starttime).ToString("D4") + "ms:");
#endif
				if (displayReport)
					output.PrintSystemLine("ユーザーdefinitionfunctionのリストを構築inside---");
				setLabelsArg();
				ParserMediator.FlushWarningList();
				labelDic.Initialized = true;
#if UEMUERA_DEBUG
				output.PrintSystemLine("経過when間:" + (WinmmTimer.TickCount - starttime).ToString("D4") + "ms:");
#endif
				if (displayReport)
					output.PrintSystemLine("scriptの構文チェックinside---");
				checkScript();
				ParserMediator.FlushWarningList();

#if UEMUERA_DEBUG
				output.PrintSystemLine("経過when間:" + (WinmmTimer.TickCount - starttime).ToString("D4") + "ms:");
#endif
				if (displayReport)
					output.PrintSystemLine("loadcomplete");
			}
			catch (Exception e)
			{
				ParserMediator.FlushWarningList();
				uEmuera.Media.SystemSounds.Hand.Play();
				output.PrintError("予期しnotErroroccurred:" + Program.ExeName);
				output.PrintError(e.GetType().ToString() + ":" + e.Message);
				return false;
			}
			finally
			{
				parentProcess.scaningLine = null;
			}
            isOnlyEvent.Clear();
			return noError;
		}

		/// <summary>
		/// 指定されたfileを読み込む
		/// </summary>
		/// <param name="filename"></param>
		public bool loadErbs(List<string> path, LabelDictionary labelDictionary)
		{
			string fname;
            List<string> isOnlyEvent = new List<string>();
            noError = true;
			labelDic = labelDictionary;
			labelDic.Initialized = false;
			foreach (string fpath in path)
			{
				if (fpath.StartsWith(Program.ErbDir, Config.SCIgnoreCase) && !Program.AnalysisMode)
					fname = fpath.Substring(Program.ErbDir.Length);
				else
					fname = fpath;
				if (Program.AnalysisMode)
					output.PrintSystemLine(fname + "Loading---");
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
			parentProcess.scaningLine = null;
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
							ParserMediator.Warn(token + "に余分なargumentがあります", position, 1);
							break;
						}
						if (skip)
						{
							ParserMediator.Warn("[SKIPSTART]が重複してuseされています", position, 1);
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
							ParserMediator.Warn(token + "に余分なargumentがあります", position, 1);
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
							ParserMediator.Warn(token + "に余分なargumentがあります", position, 1);
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
							ParserMediator.Warn(token + "にargumentがdoes not exist", position, 1);
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
							ParserMediator.Warn(token + "にargumentがdoes not exist", position, 1);
							break;
						}
						if (ppMatch.Count == 0 || ppMatch.Pop() != "ELSEIF")
						{
							ParserMediator.Warn("不適切な[ELSEIF]です", position, 1);
							break;
						}
						ppMatch.Push("ELSEIF");
						Disabled = done || (GlobalStatic.IdentifierDictionary.GetMacro(token2) == null);
						done |= !Disabled;
						break;
					case "ELSE":
						if (!string.IsNullOrEmpty(token2))
						{
							ParserMediator.Warn(token + "に余分なargumentがあります", position, 1);
							break;
						}
						if (ppMatch.Count == 0 || ppMatch.Pop() != "ELSEIF")
						{
							ParserMediator.Warn("不適切な[ELSE]です", position, 1);
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
								ParserMediator.Warn(token + "に余分なargumentがあります", position, 1);
								break;
							}
							string match = ppMatch.Count == 0 ? "" : ppMatch.Pop();
							if (match != "SKIPEND")
							{
								ParserMediator.Warn("[SKIPSTART]と対応しnot[SKIPEND]です", position, 1);
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
								ParserMediator.Warn(token + "に余分なargumentがあります", position, 1);
								break;
							}
							string match = ppMatch.Count == 0 ? "" : ppMatch.Pop();
							if (match != "ENDIF" && match != "ELSEIF")
							{
								ParserMediator.Warn("対応do[IF]のnot[ENDIF]です", position, 1);
								break;
							}
							Disabled = disabledStack.Pop();
							done = doneStack.Pop();
						}
						break;
					default:
						ParserMediator.Warn("認識できnotプリプロセッサです", position, 1);
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
					ParserMediator.Warn("[" + match + "]がdoes not exist", position, 1);
				}
			}
		}

		/// <summary>
		/// file一つを読む
		/// </summary>
		/// <param name="filepath"></param>
		private void loadErb(string filepath, string filename, List<string> isOnlyEvent)
		{
			//読み込んだfileのpathを記録
			//partfileの再loadingwhenのprocessfor
			labelDic.AddFilename(filename);
			EraStreamReader eReader = new EraStreamReader(Config.UseRenameFile && ParserMediator.RenameDic != null);
			if (!eReader.Open(filepath, filename))
			{
				output.PrintError(eReader.Filename + "のオープンにfailedしました");
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
					output.PrintSystemLine("　");
				while ((st = eReader.ReadEnabledLine(ppstate.Disabled)) != null)
				{
					position = new ScriptPosition(eReader.Filename, eReader.LineNo);
					//renameprocessing EraStreamReaderに移管
					//convertできなかった[[～～]]についてはLexAnalyzerがErrorを投げる
					if (st.Current == '[' && st.Next != '[')
					{
						st.ShiftNext();
						string token = LexicalAnalyzer.ReadSingleIdentifier(st);
						LexicalAnalyzer.SkipWhiteSpace(st);
						string token2 = LexicalAnalyzer.ReadSingleIdentifier(st);
						if ((string.IsNullOrEmpty(token)) || (st.Current != ']'))
							ParserMediator.Warn("[]の使い方が不正です", position, 1);
						ppstate.AddKeyWord(token, token2, position);
						st.ShiftNext();
						if (!st.EOS)
							ParserMediator.Warn("[" + token + "]のafterろはwill be ignored.", position, 1);
						continue;
					}
					//if ((skip) || (Program.DebugMode && ifndebug) || (!Program.DebugMode && ifdebug))
					//	continue;
					if (ppstate.Disabled)
						continue;
					//ここuntilプリプロセッサ

					if (st.Current == '#')
					{
						if ((lastLine == null) || !(lastLine is FunctionLabelLine))
						{
							ParserMediator.Warn("function宣言の直after以outで#lineが使われています", position, 1);
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
                                        ParserMediator.Warn("function@" + label.LabelName + "は既にdefinition(" + seniorLabel.Position.Filename + "の" + seniorLabel.Position.LineNo.ToString() + "line )されています", position, 1);
                                        funcCount = -1;
                                    }
								}
								funcCount++;
								if (Program.AnalysisMode && (Config.PrintCPerLine > 0 && (funcCount % Config.PrintCPerLine) == 0))
								{
									output.NewLine();
									output.PrintSystemLine("　");
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
                                    ParserMediator.Warn("ラベル名$" + gotoLabel.LabelName + "は既にsamefunctionin(" + pos.Filename + "の" + pos.LineNo.ToString() + "line )でuseされています", position, 2);
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
						//1808alpha006 process位置変更
                        ////全置換はここで対応
                        ////1756beta1+++　最初に全置換してしまうとfunctiondefinitionを_Renameでとか論outなthisができてしまうので永久封印した
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
						ParserMediator.Warn("functionがdefinitionbe donethanbefore lineがあります", position, 1);
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
					parentProcess.scaningLine = label;
					parseLabel(label);
				}
				catch (Exception exc)
				{
					uEmuera.Media.SystemSounds.Hand.Play();
					string errmes = exc.Message;
					if (!(exc is EmueraException))
						errmes = exc.GetType().ToString() + ":" + errmes;
					ParserMediator.Warn("function@" + label.LabelName + " のargumentのError:" + errmes, label, 2, true, false);
					label.ErrMes = "loadwhenにparseにfailedしたfunctionが呼び出されました";
                    label.IsError = true;
				}
				finally
				{
					parentProcess.scaningLine = null;
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
			//1807 非eventfunctionのsystemfunctionについてはwarningレベル低below＆Error解除＆argumentをsettingdoように.
			if (label.IsEvent)
			{
				if (!wc.EOL)
					ParserMediator.Warn("eventfunction@" + label.LabelName + " にargumentはsettingできません", label, 2, true, false);
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
					ParserMediator.Warn("systemfunction@" + label.LabelName + " にargumentがsettingされています", label, 1, false, false);
				SymbolWord symbol = wc.Current as SymbolWord;
				wc.ShiftNext();
                if (symbol == null)
				{ errMes = "argumentの書式が間違っています"; goto err; }
				if (symbol.Type == '[')//TODO:subNames 結局実装しnotかも
				{
					IOperandTerm[] subNamesRow = ExpressionParser.ReduceArguments(wc, ArgsEndWith.RightBracket, false);
					if (subNamesRow.Length == 0)
					{ errMes = "functiondefinitionの[]inのargumentは空にできません"; goto err; }
					subNames = new SingleTerm[subNamesRow.Length];
					for (int i = 0; i < subNamesRow.Length; i++)
					{
						if (subNamesRow[i] == null)
						{ errMes = "functiondefinitionのargumentは省略できません"; goto err; }
						IOperandTerm term = subNamesRow[i].Restructure(exm);
						subNames[i] = term as SingleTerm;
						if (subNames[i] == null)
						{ errMes = "functiondefinitionの[]inのargumentは定数のみ指定できます"; goto err; }
					}
					symbol = wc.Current as SymbolWord;
					if ((!wc.EOL) && (symbol == null))
					{ errMes = "argumentの書式が間違っています"; goto err; }
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
					{ errMes = "argumentの書式が間違っています"; goto err; }
					int length = argsRow.Length / 2;
					args = new VariableTerm[length];
                    defs = new SingleTerm[length];
					for (int i = 0; i < length; i++)
					{
						SingleTerm def = null;
						IOperandTerm term = argsRow[i * 2];
                        //argument読み取りwhenpointで判別されnotといけnot
                        //if (term == null)
                        //{ errMes = "functiondefinitionのargumentは省略できません"; goto err; }
                        if ((!(term.Restructure(exm) is VariableTerm vTerm)) || (vTerm.Identifier.IsConst))
                        { errMes = "functiondefinitionのargumentには代入possibleなvariableを指定してください"; goto err; }
                        else if (!vTerm.Identifier.IsReference)//参照typeなら添え字不要
                        {
                            if (vTerm is VariableNoArgTerm)
                            { errMes = "functiondefinitionの参照typeでnotargument\"" + vTerm.Identifier.Name + "\"に添え字が指定されていません"; goto err; }
                            if (!vTerm.isAllConst)
                            { errMes = "functiondefinitionのargumentの添え字には定数を指定してください"; goto err; }
                        }
                        for (int j = 0; j < i; j++)
                        {
                            if (vTerm.checkSameTerm(args[j]))
                                ParserMediator.Warn("第" +  Strings.StrConv((i + 1).ToString(), VbStrConv.Wide, Config.Language) + "argument\"" + vTerm.GetFullString() + "\"はすでに第" + Strings.StrConv((j + 1).ToString(), VbStrConv.Wide, Config.Language) + "argumentas宣言されています", label, 1, false, false);
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
							{ errMes = "argumentのinitialvalueには定数のみを指定できます"; goto err; }
							if (!canDef)
							{ errMes = "argumentのinitialvalueをdefinitioncanのは\"ARG\",\"ARGS\"orプライベートvariableのみです"; goto err; }
							else if (vTerm.Identifier.IsReference)
							{ errMes = "参照渡しのargumentにinitialvalueはdefinitionできません"; goto err; }
							if (vTerm.GetOperandType() != def.GetOperandType())
							{ errMes = "argumentのtypeとinitialvalueのtypeが一致していません"; goto err; }
						}
						args[i] = vTerm;
						defs[i] = def;
					}

				}
			}
			if (!wc.EOL)
			{ errMes = "argumentの書式が間違っています"; goto err; }

            //label.SubNames = subNames;
			label.Arg = args;
			label.Def = defs;
			label.ArgLength = maxArg;
			label.ArgsLength = maxArgs;
			return;
		err:
			ParserMediator.Warn("function@" + label.LabelName + " のargumentのError:" + errMes, label, 2, true, false);
			return;
		}


		public bool useCallForm = false;
		/// <summary>
		/// 読込終わったfileをチェックdo
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
					//1756beta003 なんでaddしたんだろう デバグduring なんかやったのか とりあえずコメントアウトしておく
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
			{//callform系が使われたらallのfunctionが呼び出されたとみなす.
                if (Program.AnalysisMode)
					output.PrintSystemLine("CALLFORM系命令が使われたbecause,呼び出されnotfunctionのチェックはlineわれません.");
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
                    //parseモードwhenは呼ばれなかったthingをここでparse
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
						ParserMediator.Warn("function@" + label.LabelName + "はdefinitionされていますが一度も呼び出されません", label, 1, false, false);
					if (!ignoreUncalledFunction)
						checkFunctionWithCatch(label);
					else
					{
						if (!(label.NextLine is NullLine) && !(label.NextLine is FunctionLabelLine))
						{
							if (!label.NextLine.IsError)
							{
								label.NextLine.IsError = true;
								label.NextLine.ErrMes = "呼び出されnotはずのfunctionが呼ばれた";
							}
						}
					}
				}
			}
			if (Program.AnalysisMode && (warningDic.Keys.Count > 0 || GlobalStatic.tempDic.Keys.Count > 0))
			{
				output.PrintError("-definitionが見つfromなかったfunction: 他のfileでdefinitionされているcaseはこのwarningは無視できます");
				if (warningDic.Keys.Count > 0)
				{
					output.PrintError("　○一般function:");
					foreach (string labelName in warningDic.Keys)
					{
						output.PrintError("　　" + labelName + ": " + warningDic[labelName].ToString() + "回");
					}
				}
				if (GlobalStatic.tempDic.Keys.Count > 0)
				{
					output.PrintError("　○文insidefunction:");
					foreach (string labelName in GlobalStatic.tempDic.Keys)
					{
						output.PrintError("　　" + labelName + ": " + GlobalStatic.tempDic[labelName].ToString() + "回");
					}
				}
			}
			else
			{
				if ((ignoredFNCWarningCount > 0) && (Config.DisplayWarningLevel <= 1) && (notCalledWarning != DisplayWarningFlag.IGNORE))
					output.PrintError(string.Format("warningLv1:definitionされたfunctionが一度も呼び出されていnot事に関dowarningを{0}件無視しました", ignoredFNCWarningCount));
				if ((ignoredFNFWarningCount > 0) && (Config.DisplayWarningLevel <= 2) && (notCalledWarning != DisplayWarningFlag.IGNORE))
					output.PrintError(string.Format("warningLv2:definitionされていnotfunctionをcallた事に関dowarningを{0}件無視しました", ignoredFNFWarningCount));
			}
			ParserMediator.FlushWarningList();
			if (Config.DisplayReport)
				output.PrintError(string.Format("非コメントline数:{0}, 全function合計:{1}, 被呼出function合計:{2}", enabledLineCount, labelDic.Count, usedLabelCount));
			if (Config.AllowFunctionOverloading && Config.WarnFunctionOverloading)
			{
				List<string> overloadedList = GlobalStatic.IdentifierDictionary.GetOverloadedList(labelDic);
				if (overloadedList.Count > 0)
				{
					output.NewLine();
					output.PrintError("＊＊＊＊＊warning＊＊＊＊＊");
					foreach (string funcname in overloadedList)
					{
						output.PrintSystemLine("  systemfunction\"" + funcname + "\"がユーザーdefinitionfunctionによってabove書きされています");
					}
					output.PrintSystemLine("  above記のfunctionを利fordoscriptは意図wayに動かnotpossible性があります");
					output.NewLine();
					output.PrintSystemLine("  ※このwarningは該当do式insidefunctionを利forしているEmuera専forscript向けのwarningです.");
					output.PrintSystemLine("  eramakerforのscriptの動作には影響しません.");
					output.PrintSystemLine("  今afterこのwarningが不要ならばconfigの"systemfunctionがabove書きされたときwarningをdisplaydo"をOFFにしてbelowさい.");
					output.PrintSystemLine("＊＊＊＊＊＊＊＊＊＊＊＊");
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
		{//ここでErrorを捕まえるthisは本来はnotはず.ExeEE相当.
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
                //1756beta2+v6.1 修正の効率化のbecauseに何かパース関係でハンドリングできてnotErrorが出たcaseはスタックトレースを投げるようにした
                string errmes = (exc is EmueraException) ? exc.Message : exc.GetType().ToString() + ":" + exc.Message;
                ParserMediator.Warn("@" + label.LabelName + " のparseduring Error:" + errmes, label, 2, true, false, !(exc is EmueraException) ? exc.StackTrace : null);
                label.ErrMes = "loadwhenにparseにfailedしたfunctionが呼び出されました";
			}
			finally
			{
				parentProcess.scaningLine = null;
			}

		}

		private void setArgument(FunctionLabelLine label)
		{
			//1周目/3周
			//argumentのparseとか
			LogicalLine nextLine = label;
			bool inMethod = label.IsMethod;
			while (true)
			{
				nextLine = nextLine.NextLine;
				parentProcess.scaningLine = nextLine;
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
						ParserMediator.Warn(func.Function.Name + "命令は#FUNCTIONinsideで使うcannot be", nextLine, 2, true, false);
						continue;
					}
				}
                if (Config.NeedReduceArgumentOnLoad || Program.AnalysisMode || func.Function.IsForceSetArg())
                    ArgumentParser.SetArgumentTo(func);
			}
		}

		private void nestCheck(FunctionLabelLine label)
		{
			//2周目/3周
			//IF-ELSEIF-ENDIF,REPEAT-RENDの対応チェックetc
			//PRINTDATA系もここでチェック
			LogicalLine nextLine = label;
			List<InstructionLine> tempLineList = new List<InstructionLine>();
			Stack<InstructionLine> nestStack = new Stack<InstructionLine>();
            Stack<InstructionLine> SelectcaseStack = new Stack<InstructionLine>();
			InstructionLine pairLine = null;
			while (true)
			{
				nextLine = nextLine.NextLine;
				parentProcess.scaningLine = nextLine;
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
                                ParserMediator.Warn(currentBaseFunc.Function.Name + "構文during $ラベルをdefinitiondocannot be", nextLine, 2, true, false);
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
							ParserMediator.Warn(baseFunc.Function.Name + "構文にuseできnot命令\'" + func.Function.Name + "\'が含まれています", func, 2, true, false);
							continue;
						}
					}
					else if (baseFunc.FunctionCode == FunctionCode.DATALIST)
					{
						if ((func.FunctionCode != FunctionCode.DATA) && (func.FunctionCode != FunctionCode.DATAFORM) && (func.FunctionCode != FunctionCode.ENDLIST))
						{
							ParserMediator.Warn("DATALIST構文にuseできnot命令\'" + func.Function.Name + "\'が含まれています", func, 2, true, false);
							continue;
						}
					}
					else if ((baseFunc.FunctionCode == FunctionCode.TRYCALLLIST) || (baseFunc.FunctionCode == FunctionCode.TRYJUMPLIST) || (baseFunc.FunctionCode == FunctionCode.TRYGOTOLIST))
					{
						if ((func.FunctionCode != FunctionCode.FUNC) && (func.FunctionCode != FunctionCode.ENDFUNC))
						{
							ParserMediator.Warn(baseFunc.Function.Name + "構文にuseできnot命令\'" + func.Function.Name + "\'が含まれています", func, 2, true, false);
							continue;
						}
					}
					else if (baseFunc.FunctionCode == FunctionCode.SELECTCASE)
					{
						if ((baseFunc.IfCaseList.Count == 0) && (func.FunctionCode != FunctionCode.CASE) && (func.FunctionCode != FunctionCode.CASEELSE) && (func.FunctionCode != FunctionCode.ENDSELECT))
						{
							ParserMediator.Warn("SELECTCASE構文の分岐のoutに命令\'" + func.Function.Name + "\'が含まれています", func, 2, true, false);
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
								ParserMediator.Warn("REPEAT文が入れ子にされています(無限ループの恐れがあります)", func, 1, false, false);
							}
                            else if (iLine.FunctionCode == FunctionCode.FOR)
                            {
                                VariableTerm cnt = ((SpForNextArgment)iLine.Argument).Cnt;
                                if (cnt.Identifier.Name == "COUNT" && (cnt.isAllConst && cnt.getEl1forArg == 0))
                                {
                                    ParserMediator.Warn("カウンタvariableにCOUNT:0をforいたFOR文のinsideでREPEATが呼び出されています", func, 1, false, false);
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
                        //ネストErrorチェックのbecauseにコストはかかるが,ここでチェックdo
                        if (func.Argument == null)
                            ArgumentParser.SetArgumentTo(func);
                        //aboveでargumentparseがなされているthisは保証されているので,
                        //それでこれがfalseになるのは,argumentparseでErrorが起きたcaseのみ
                        if (func.Argument != null)
                        {
                            VariableTerm Cnt = ((SpForNextArgment)func.Argument).Cnt;
                            if (Cnt.Identifier.Name == "COUNT")
                            {
                                foreach (InstructionLine iLine in nestStack)
                                {
                                    if (iLine.FunctionCode == FunctionCode.REPEAT && (Cnt.isAllConst && Cnt.getEl1forArg == 0))
                                    {
                                        ParserMediator.Warn("REPEAT文のinsideでカウンタvariableにCOUNT:0をforいたFORが使われています(無限ループの恐れがあります)", func, 1, false, false);
                                    }
                                    else if (iLine.FunctionCode == FunctionCode.FOR)
                                    {
                                        VariableTerm destCnt = ((SpForNextArgment)iLine.Argument).Cnt;
                                        if (destCnt.Identifier.Name == "COUNT" && (Cnt.isAllConst && destCnt.isAllConst && destCnt.getEl1forArg == Cnt.getEl1forArg))
                                        {
                                            ParserMediator.Warn("カウンタvariableにCOUNT:" + Cnt.getEl1forArg.ToString() + "をforいたFOR文が入れ子にされています(無限ループの恐れがあります)", func, 1, false, false);
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
							ParserMediator.Warn("REPEAT, FOR, WHILE, DOのinside以outで" + func.Function.Name + "文が使われました", func, 2, true, false);
							break;
						}
						func.JumpTo = pairLine;
						break;

					case FunctionCode.ELSEIF:
					case FunctionCode.ELSE:
						{
							//1.725 Stack<T>.Peek()はStackが空のwhenはnullを返す仕様だと思いこんでおりました.
							InstructionLine ifLine = nestStack.Count == 0 ? null : nestStack.Peek();
							if ((ifLine == null) || (ifLine.FunctionCode != FunctionCode.IF))
							{
								ParserMediator.Warn("IF～ENDIFのoutで" + func.Function.Name + "文が使われました", func, 2, true, false);
                                break;
							}
							if (ifLine.IfCaseList[ifLine.IfCaseList.Count - 1].FunctionCode == FunctionCode.ELSE)
								ParserMediator.Warn("ELSE文thanafterで" + func.Function.Name + "文が使われました", func, 1, false, false);
							ifLine.IfCaseList.Add(func);
						}
						break;
					case FunctionCode.ENDIF:
						{
							InstructionLine ifLine = nestStack.Count == 0 ? null : nestStack.Peek();
							if ((ifLine == null) || (ifLine.FunctionCode != FunctionCode.IF))
							{
								ParserMediator.Warn("対応doIFの無いENDIF文です", func, 2, true, false);
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
								ParserMediator.Warn("SELECTCASE～ENDSELECTのoutで" + func.Function.Name + "文が使われました", func, 2, true, false);
								break;
							}
                            else if (selectLine.FunctionCode != FunctionCode.SELECTCASE && SelectcaseStack.Count > 0)
                            {
                                do
                                {
                                    ParserMediator.Warn(selectLine.Function.Name + "文に対応do" + FunctionIdentifier.getMatchFunction(selectLine.FunctionCode) + "がnot状態で" + func.Function.Name + "文に到達しました", func, 2, true, false);
                                    //これを跨いでIF等が閉じられるthisがnotようにdo.
                                    nestStack.Pop();
                                    //if (nestStack.Count > 0)　//空になってるかはbelowで判定canので,これを見る必要がnot
                                    selectLine = nestStack.Count == 0 ? null : nestStack.Peek(); //ちなみにnullになるthisはnot(SELECTCASEがnotcaseはaboveで弾けるfrom)
                                } while (selectLine != null && selectLine.FunctionCode != FunctionCode.SELECTCASE);
                                break;
                            }
							if ((selectLine.IfCaseList.Count > 0) &&
								(selectLine.IfCaseList[selectLine.IfCaseList.Count - 1].FunctionCode == FunctionCode.CASEELSE))
								ParserMediator.Warn("CASEELSE文thanafterで" + func.Function.Name + "文が使われました", func, 1, false, false);
							selectLine.IfCaseList.Add(func);
						}
						break;
					case FunctionCode.ENDSELECT:
						{
							InstructionLine selectLine = nestStack.Count == 0 ? null : nestStack.Peek();
							if ((selectLine == null) || (selectLine.FunctionCode != FunctionCode.SELECTCASE && SelectcaseStack.Count == 0))
							{
								ParserMediator.Warn("対応doSELECTCASEの無いENDSELECT文です", func, 2, true, false);
                                break;
							}
                            else if (selectLine.FunctionCode != FunctionCode.SELECTCASE && SelectcaseStack.Count > 0)
                            {
                                do
                                {
                                    ParserMediator.Warn(selectLine.Function.Name + "文に対応do" + FunctionIdentifier.getMatchFunction(selectLine.FunctionCode) + "がnot状態で" + func.Function.Name + "文に到達しました", func, 2, true, false);
                                    //これを跨いでIF等が閉じられるthisがnotようにdo.
                                    nestStack.Pop();
                                    //if (nestStack.Count > 0)　//空になってるかはbelowで判定canので,これを見る必要がnot
                                    selectLine = nestStack.Count == 0 ? null : nestStack.Peek(); //ちなみにnullになるthisはnot(SELECTCASEがnotcaseはaboveで弾けるfrom)
                                } while (selectLine != null && selectLine.FunctionCode != FunctionCode.SELECTCASE);　
                                //とりあえず,対応doSELECTCASE跨ぎは閉じる
                                SelectcaseStack.Pop();
                                //こっちでも抜かnotとSELECTCASEが2つのENDSELECTに対応してしまう
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
								ParserMediator.Warn("SELECTCASEのargumentがdoes not exist", selectLine, 2, true, false);
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
									ParserMediator.Warn("CASEのargumentがdoes not exist", caseLine, 2, true, false);

								foreach (CaseExpression exp in caseExps)
								{
									if (exp.GetOperandType() != term.GetOperandType())
										ParserMediator.Warn("CASEのargumentのtypeがSELECTCASEと一致しません", caseLine, 2, true, false);
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
						//    throw new ExeEE("何か変？");
						if ((nestStack.Count == 0)
							|| (nestStack.Peek().FunctionCode != parentFunc))
						{
                            ParserMediator.Warn("対応do" + parentFunc.ToString() + "の無い" + func.Function.Name + "文です", func, 2, true, false);
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
							ParserMediator.Warn("対応doTRYC系命令がdoes not exist", func, 2, true, false);
							break;
						}
						pairLine = nestStack.Pop();//TRYC
						pairLine.JumpToEndCatch = func;//TRYCにCATCHの位置を教える
						nestStack.Push(func);
						break;
					case FunctionCode.ENDCATCH:
						if ((nestStack.Count == 0)
							|| (nestStack.Peek().FunctionCode != FunctionCode.CATCH))
						{
							ParserMediator.Warn("対応doCATCHのnotENDCATCHです", func, 2, true, false);
							break;
						}
						pairLine = nestStack.Pop();//CATCH
						pairLine.JumpToEndCatch = func;//CATCHにENDCATCHの位置を教える
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
                                    ParserMediator.Warn("PRINTDATA系命令が入れ子にされています", func, 2, true, false);
                                    break;
                                }
                                if (iLine.FunctionCode == FunctionCode.STRDATA)
                                {
                                    ParserMediator.Warn("PRINTDATA系命令のduring STRDATA系命令が含まれています", func, 2, true, false);
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
                                    ParserMediator.Warn("STRDATA命令が入れ子にされています", func, 2, true, false);
                                    break;
                                }
                                if (iLine.Function.IsPrintData())
                                {
                                    ParserMediator.Warn("STRDATA系命令のduring PRINTDATA系命令が含まれています", func, 2, true, false);
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
                                ParserMediator.Warn("対応doPRINTDATA系命令のnotDATALISTです", func, 2, true, false);
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
                                ParserMediator.Warn("対応doDATALISTのnotENDLISTです", func, 2, true, false);
                                break;
                            }
                            if (tempLineList.Count == 0)
                                ParserMediator.Warn("DATALIST命令にdisplaydataが与えられていません(このDATALISTは空stringをdisplayします)", func, 1, false, false);
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
                                ParserMediator.Warn("対応doPRINTDATA系命令のnot" + func.Function.Name + "です", func, 2, true, false);
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
                                ParserMediator.Warn("対応doPRINTDATA系命令もしくはSTRDATAのnot" + func.Function.Name + "です", func, 2, true, false);
                                break;
                            }
                            if (pline.FunctionCode == FunctionCode.DATALIST)
                                ParserMediator.Warn("DATALISTが閉じられていません", func, 2, true, false);
                            if (pline.dataList.Count == 0)
                                ParserMediator.Warn(pline.Function.Name + "命令にdisplaydataがdoes not exist(この命令はwill be ignored)", func, 1, false, false);
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
								ParserMediator.Warn("TRYCALLLIST系命令が入れ子にされています", func, 2, true, false);
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
								ParserMediator.Warn("対応doTRYCALLLIST系命令のnot" + func.Function.Name + "です", func, 2, true, false);
								break;
							}
                            if (func.Argument == null)
                            {
                                ParserMediator.Warn("TRYCALLLIST系命令during 無効な" + func.Function.Name + "がexists", pFunc, 2, true, false);
                                break;
                            }
							if (pFunc.FunctionCode == FunctionCode.TRYGOTOLIST)
							{
								if (((SpCallArgment)func.Argument).SubNames.Length != 0)
								{
									ParserMediator.Warn("TRYGOTOLISTのcall対象に[～～]がsettingされています", func, 2, true, false);
									break;
								}
								if (((SpCallArgment)func.Argument).RowArgs.Length != 0)
								{
									ParserMediator.Warn("TRYGOTOLISTのcall対象にargumentがsettingされています", func, 2, true, false);
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
							ParserMediator.Warn("対応doTRYCALLLIST系命令のnot" + func.Function.Name + "です", func, 2, true, false);
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
								ParserMediator.Warn("NOSKIP系命令が入れ子にされています", func, 2, true, false);
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
							ParserMediator.Warn("対応doNOSKIP系命令のnot" + func.Function.Name + "です", func, 2, true, false);
							break;
						}
						//Errorハンドリングfor
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
					ParserMediator.Warn(funcName + "に対応do" + funcMatch + "not found", func, 2, true, false);
				else
					ParserMediator.Warn("ディフォルトError(Emuerasetting漏れ)", func, 2, true, false);
			}
            //使ったスタックをクリア
            SelectcaseStack.Clear();
		}

		private void setJumpTo(FunctionLabelLine label)
		{
			//3周目/3周
			//フロー制御命令のジャンプaheadをsetting
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
				parentProcess.scaningLine = func;

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
						if (!Program.AnalysisMode)
							printFunctionNotFoundWarning("指定されたfunction名\"@" + FunctionNotFoundName + "\"は存在しません", func, 2, true);
						else
							printFunctionNotFoundWarning(FunctionNotFoundName, func, 2, true);
					}
                    continue;
				}
				if ((func.FunctionCode == FunctionCode.TRYCALLLIST) || (func.FunctionCode == FunctionCode.TRYJUMPLIST))
					useCallForm = true;
			}
		}

	}
}
