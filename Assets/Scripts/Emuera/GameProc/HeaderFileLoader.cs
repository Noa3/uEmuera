using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc.Function;
using MinorShift._Library;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Function;

namespace MinorShift.Emuera.GameProc
{
	internal sealed class HeaderFileLoader
	{
		public HeaderFileLoader(EmueraConsole main, IdentifierDictionary idDic, Process proc)
		{
			output = main;
			parentProcess = proc;
			this.idDic = idDic;
		}
		readonly Process parentProcess;
		readonly EmueraConsole output;
		readonly IdentifierDictionary idDic;

		bool noError = true;
		Queue<DimLineWC> dimlines;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="erbDir"></param>
		/// <param name="displayReport"></param>
		/// <returns></returns>
		public bool LoadHeaderFiles(string headerDir, bool displayReport)
		{
			List<KeyValuePair<string, string>> headerFiles = Config.GetFiles(headerDir, "*.ERH");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            headerFiles.AddRange(Config.GetFiles(headerDir, "*.erh"));
#endif
            bool noError = true;
			dimlines = new Queue<DimLineWC>();
			try
			{
				for (int i = 0; i < headerFiles.Count; i++)
				{
					string filename = headerFiles[i].Key;
					string file = headerFiles[i].Value;
					if (displayReport)
						output.PrintSystemLine(string.Format(GameMessages.LoadingHeader, filename));
					noError = loadHeaderFile(file, filename);
					if (!noError)
						break;
					//System.Windows.Forms.//Application.DoEvents();
				}
				//even if an Error has occurred, check the portion that was loadable
				if (dimlines.Count > 0)
				{
					//if not &=, an Error that occurs here cannot be caught
					noError &= analyzeSharpDimLines();
				}

				dimlines.Clear();
			}
			finally
			{
				ParserMediator.FlushWarningList();
			}
			return noError;
		}


		private bool loadHeaderFile(string filepath, string filename)
		{
			StringStream st;
			ScriptPosition position = null;
			EraStreamReader eReader = new EraStreamReader(true);

#if UEMUERA_DEBUG
			UnityEngine.Debug.Log($"[HeaderFileLoader] Attempting to open: {filename} at path: {filepath}");
#endif
			
			if (!eReader.Open(filepath, filename))
			{
				// Check if file actually exists
				if (System.IO.File.Exists(filepath))
				{
					UnityEngine.Debug.LogError($"[HeaderFileLoader] File exists but failed to open: {filename} - possible encoding or permission issue");
					throw new CodeEE(filename + GameMessages.T(" failed to open (file exists - possible encoding/permission issue)"));
				}
				else
				{
					UnityEngine.Debug.LogError($"[HeaderFileLoader] File not found: {filepath}");
					throw new CodeEE(filename + GameMessages.T(" failed to open (file not found)"));
				}
			}
			
#if UEMUERA_DEBUG
			UnityEngine.Debug.Log($"[HeaderFileLoader] Successfully opened: {filename}");
#endif
			try
			{
				int lineCount = 0;
				while ((st = eReader.ReadEnabledLine()) != null)
				{
					lineCount++;
					if (!noError)
						return false;
					position = new ScriptPosition(filename, eReader.LineNo);
				LexicalAnalyzer.SkipWhiteSpace(st);
				if (st.Current != '#')
				{
					// Skip lines that don't start with '#'. Occurs in games that use
					// multi-line expressions (e.g. MAX() spanning lines) without {} blocks.
					// Warn and continue rather than aborting the whole file.
					string preview = st.Substring();
					if (preview.Length > 40) preview = preview.Substring(0, 40) + "...";
					ParserMediator.Warn(
						GameMessages.T("Line in header does not start with '#', skipping: ") + preview,
						position, 1);
					continue;
				}
					st.ShiftNext();
					string sharpID = LexicalAnalyzer.ReadSingleIdentifier(st);
					if (sharpID == null)
					{
						ParserMediator.Warn(GameMessages.T("Unrecognizable # line"), position, 1);
						return false;
					}
					if (Config.ICFunction)
						sharpID = sharpID.ToUpper();
					LexicalAnalyzer.SkipWhiteSpace(st);
					
					if (sharpID == "DEFINE")
					{
#if UEMUERA_DEBUG
						UnityEngine.Debug.Log($"[HeaderFileLoader] Processing #DEFINE at {filename}:{eReader.LineNo}");
#endif
					}
					
					switch (sharpID)
					{
						case "DEFINE":
							analyzeSharpDefine(st, position);
							break;
						case "FUNCTION":
						case "FUNCTIONS":
							analyzeSharpFunction(st, position, sharpID == "FUNCTIONS");
							break;
						case "DIM":
						case "DIMS":
#if UEMUERA_DEBUG
							UnityEngine.Debug.Log($"[HeaderFileLoader] Queuing #DIM at {filename}:{eReader.LineNo}");
#endif
							{
								WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
								dimlines.Enqueue(new DimLineWC(wc, sharpID == "DIMS", false, position));
							}
							break;
						default:
							throw new CodeEE("#" + sharpID + GameMessages.T(" is an unrecognizable preprocessor"), position);
					}
				}
#if UEMUERA_DEBUG
				UnityEngine.Debug.Log($"[HeaderFileLoader] Finished reading {filename}, processed {lineCount} lines");
#endif
			}
			catch (CodeEE e)
			{
				if (e.Position != null)
					position = e.Position;
				UnityEngine.Debug.LogError($"[HeaderFileLoader] Error in {filename}: {e.Message}");
				ParserMediator.Warn(e.Message, position, 2);
				return false;
			}
			finally
			{
				eReader.Close();
			}
			return true;
		}

			//#define FOO (...)     id to wc
			//#define BAR($1) (...)     idwithargs to wc(replaced)
		//#diseble FOOBAR             
		//#dim piyo, i
		//#dims puyo, j
		//static List<string> keywordsList = new List<string>();

		private void analyzeSharpDefine(StringStream st, ScriptPosition position)
		{
			//do it before calling LexicalAnalyzer.SkipWhiteSpace(st).
			string srcID = LexicalAnalyzer.ReadSingleIdentifier(st);
			if (srcID == null)
				throw new CodeEE(GameMessages.T("No replacement source identifier"), position);
			if (Config.ICVariable)
				srcID = srcID.ToUpper();

            //if name duplication isn't checked here, it becomes a serious problem
            string errMes = "";
            int errLevel = -1;
            idDic.CheckUserMacroName(ref errMes, ref errLevel, srcID);
            if (errLevel >= 0)
            {
                ParserMediator.Warn(errMes, position, errLevel);
                if (errLevel >= 2)
                {
                    noError = false;
                    return;
                }
            }
            
            bool hasArg = st.Current == '(';//when specifying an argument, ( must immediately follow. Whitespace also forbidden.
			//1808a3 assignment operator allowed (for function declarations)
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
			if (wc.EOL)
			{
				//throw new CodeEE(GameMessages.T("No replacement expression"), position);
				//1808a3 empty macro allowed
				DefineMacro nullmac = new DefineMacro(srcID, new WordCollection(), 0);
				idDic.AddMacro(nullmac);
				return;
			}

			List<string> argID = new List<string>();
			if (hasArg)//Function-type macro argument analysis
			{
				wc.ShiftNext();//Skip past '('
				if (wc.Current.Type == ')')
					throw new CodeEE(GameMessages.T("Function-type macro cannot have 0 arguments"), position);
				while (!wc.EOL)
				{
					IdentifierWord word = wc.Current as IdentifierWord;
					if (word == null)
						throw new CodeEE(GameMessages.T("Incorrect format for the replacement source argument specification"), position);
					word.SetIsMacro();
					string id = word.Code;
					if (argID.Contains(id))
						throw new CodeEE(GameMessages.T("The same argument is used more than once in the replacement source"), position);
					argID.Add(id);
					wc.ShiftNext();
					if (wc.Current.Type == ',')
					{
						wc.ShiftNext();
						continue;
					}
					if (wc.Current.Type == ')')
						break;
					throw new CodeEE(GameMessages.T("Incorrect format for the replacement source argument specification"), position);
				}
				if (wc.EOL)
					throw new CodeEE(GameMessages.T("')' is not closed"), position);

				wc.ShiftNext();
			}
			if (wc.EOL)
throw new CodeEE(GameMessages.T("No replacement expression"), position);
			WordCollection destWc = new WordCollection();
			while (!wc.EOL)
			{
				destWc.Add(wc.Current);
				wc.ShiftNext();
			}
			if (hasArg)//Function-type macro argument set
			{
				while (!destWc.EOL)
				{
					IdentifierWord word = destWc.Current as IdentifierWord;
					if (word == null)
					{
						destWc.ShiftNext();
						continue;
					}
					for (int i = 0; i < argID.Count; i++)
					{
						if (string.Equals(word.Code, argID[i], Config.SCVariable))
						{
							destWc.Remove();
							destWc.Insert(new MacroWord(i));
							break;
						}
					}
					destWc.ShiftNext();
				}
				destWc.Pointer = 0;
			}
			if (hasArg)//1808a3 function-type macros sealed
				throw new CodeEE(GameMessages.T("Function-type macros cannot be declared"), position);
			DefineMacro mac = new DefineMacro(srcID, destWc, argID.Count);
			idDic.AddMacro(mac);
		}

		//private void analyzeSharpDim(StringStream st, ScriptPosition position, bool dims)
		//{
		//	//WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
		//	//UserDefinedVariableData data = UserDefinedVariableData.Create(wc, dims, false, position);
		//	//if (data.Reference)
		//	//	throw new NotImplCodeEE();
		//	//VariableToken var = null;
		//	//if (data.CharaData)
		//	//	var = parentProcess.VEvaluator.VariableData.CreateUserDefCharaVariable(data);
		//	//else
		//	//	var = parentProcess.VEvaluator.VariableData.CreateUserDefVariable(data);
		//	//idDic.AddUseDefinedVariable(var);
		//}

		//1822 keep only #DIM together and process later
		private bool analyzeSharpDimLines()
		{
			bool noError = true;
			
			// Enable macro expansion for DIM processing since macros should be defined by now
			bool previousUseMacro = LexicalAnalyzer.UseMacro;
			LexicalAnalyzer.UseMacro = idDic.UseMacro();
			
#if UEMUERA_DEBUG
			UnityEngine.Debug.Log($"[HeaderFileLoader] Starting DIM line analysis with UseMacro={LexicalAnalyzer.UseMacro}, {dimlines.Count} DIM lines queued");
#endif
			
			try
			{
				// TWO-PASS APPROACH:
				// Pass 1: Process all CONST declarations first and register them as macros
				//         (with retries for CONSTs that depend on other CONSTs)
				// Pass 2: Process all other DIM declarations (which can now use the constants)
				
				Queue<DimLineWC> constLines = new Queue<DimLineWC>();
				Queue<DimLineWC> nonConstLines = new Queue<DimLineWC>();
				int totalLines = dimlines.Count;
				
				// Separate CONST and non-CONST lines
				while (dimlines.Count > 0)
				{
					DimLineWC dimline = dimlines.Dequeue();
					dimline.WC.Pointer = 0;
					
					bool isConst = IsConstDeclaration(dimline.WC);
					dimline.WC.Pointer = 0;
					
					if (isConst)
						constLines.Enqueue(dimline);
					else
						nonConstLines.Enqueue(dimline);
				}
				
#if UEMUERA_DEBUG
				UnityEngine.Debug.Log($"[HeaderFileLoader] Separated {constLines.Count} CONST lines and {nonConstLines.Count} non-CONST lines");
#endif
				
				// === PASS 1: Process CONST declarations with retry support ===
#if UEMUERA_DEBUG
				UnityEngine.Debug.Log($"[HeaderFileLoader] Pass 1: Processing CONST declarations...");
#endif
				
				int constCount = 0;
				bool constTryAgain = true;
				int constAttemptCount = 0;
				
				while (constLines.Count > 0)
				{
					int count = constLines.Count;
					constAttemptCount++;
					
					for (int i = 0; i < count; i++)
					{
						DimLineWC dimline = constLines.Dequeue();
						try
						{
							dimline.WC.Pointer = 0;
							UserDefinedVariableData data = UserDefinedVariableData.Create(dimline);
							if (data.Reference)
								throw new NotImplCodeEE();
							
						// Register CONST as a macro so other DIM lines can use it
						if (data.Const && data.DefaultInt != null && data.DefaultInt.Length == 1)
						{
							idDic.AddIntegerMacro(data.Name, data.DefaultInt[0]);
							// Update UseMacro since we added a new macro
							LexicalAnalyzer.UseMacro = idDic.UseMacro();
						}
						// Register string CONST as a macro so DIMS CONST chain declarations work
						// (e.g. #DIMS CONST B = A + "suffix" requires A to be a macro first)
						if (data.Const && data.TypeIsStr && data.DefaultStr != null && data.DefaultStr.Length == 1)
						{
							idDic.AddStringMacro(data.Name, data.DefaultStr[0]);
							LexicalAnalyzer.UseMacro = idDic.UseMacro();
						}
							
							VariableToken var = null;
							if (data.CharaData)
								var = parentProcess.VEvaluator.VariableData.CreateUserDefCharaVariable(data);
							else
								var = parentProcess.VEvaluator.VariableData.CreateUserDefVariable(data);
							idDic.AddUseDefinedVariable(var);
							constCount++;
						}
						catch (IdentifierNotFoundCodeEE e)
						{
							// This CONST depends on another CONST that hasn't been processed yet
							if (constTryAgain)
							{
								dimline.WC.Pointer = 0;
								constLines.Enqueue(dimline);
							}
							else
							{
								UnityEngine.Debug.LogError($"[HeaderFileLoader] CONST identifier not found at {dimline.SC.Filename}:{dimline.SC.LineNo}: {e.Message}");
								ParserMediator.Warn(e.Message, dimline.SC, 2);
								noError = false;
							}
						}
					catch (CodeEE e)
					{
						// Retry if the error is likely caused by a CONST dependency not yet registered.
						// "unrecognized identifier"    — unknown name (defined by a later CONST)
						// "Only constants can be"      — variable found instead of a constant literal;
						//                               happens when a dependent macro wasn't in macroDic yet
						// "String type and number type"— type mismatch from an unresolved string CONST
						bool retryable = constTryAgain && (
							e.Message.Contains("unrecognized identifier") ||
							e.Message.Contains("Only constants can be") ||
							e.Message.Contains("String type and number type"));
						if (retryable)
						{
							dimline.WC.Pointer = 0;
							constLines.Enqueue(dimline);
						}
						else
						{
							UnityEngine.Debug.LogError($"[HeaderFileLoader] CONST error at {dimline.SC.Filename}:{dimline.SC.LineNo}: {e.Message}");
								ParserMediator.Warn(e.Message, dimline.SC, 2);
								noError = false;
							}
						}
					}
					
					if (constLines.Count == count)
					{
						// No progress made, stop retrying
						constTryAgain = false;
					}
				}
				
#if UEMUERA_DEBUG
				UnityEngine.Debug.Log($"[HeaderFileLoader] Pass 1 complete: {constCount} CONST declarations processed in {constAttemptCount} attempts");
#endif
				
				// === PASS 2: Process non-CONST declarations ===
#if UEMUERA_DEBUG
				UnityEngine.Debug.Log($"[HeaderFileLoader] Pass 2: Processing non-CONST declarations with UseMacro={LexicalAnalyzer.UseMacro}...");
#endif
				
				bool tryAgain = true;
				int processedCount = 0;
				int attemptCount = 0;
				
				while (nonConstLines.Count > 0)
				{
					int count = nonConstLines.Count;
					attemptCount++;
					
					for (int i = 0; i < count; i++)
					{
						DimLineWC dimline = nonConstLines.Dequeue();
						try
						{
							dimline.WC.Pointer = 0;
							
							UserDefinedVariableData data = UserDefinedVariableData.Create(dimline);
							if (data.Reference)
								throw new NotImplCodeEE();
							VariableToken var = null;
							if (data.CharaData)
								var = parentProcess.VEvaluator.VariableData.CreateUserDefCharaVariable(data);
							else
								var = parentProcess.VEvaluator.VariableData.CreateUserDefVariable(data);
							idDic.AddUseDefinedVariable(var);
							processedCount++;
						}
						catch (IdentifierNotFoundCodeEE e)
						{
							//if there is a prospect of resolving it by repeating, add to the end of the queue
							if (tryAgain)
							{
								dimline.WC.Pointer = 0;
								nonConstLines.Enqueue(dimline);
							}
							else
							{
								UnityEngine.Debug.LogError($"[HeaderFileLoader] Identifier not found at {dimline.SC.Filename}:{dimline.SC.LineNo}: {e.Message}");
								ParserMediator.Warn(e.Message, dimline.SC, 2);
								noError = false;
							}
						}
						catch (CodeEE e)
						{
							UnityEngine.Debug.LogError($"[HeaderFileLoader] Code error at {dimline.SC.Filename}:{dimline.SC.LineNo}: {e.Message}");
							ParserMediator.Warn(e.Message, dimline.SC, 2);
							noError = false;
						}
					}
					if (nonConstLines.Count == count)
					{
						tryAgain = false;
					}
				}
				
#if UEMUERA_DEBUG
				UnityEngine.Debug.Log($"[HeaderFileLoader] Pass 2 complete: {processedCount} non-CONST variables processed in {attemptCount} attempts");
				UnityEngine.Debug.Log($"[HeaderFileLoader] DIM line analysis complete. Total: {constCount + processedCount} of {totalLines} variables processed");
#endif
			}
			finally
			{
				// Restore previous UseMacro state
				LexicalAnalyzer.UseMacro = previousUseMacro;
			}
			
			return noError;
		}
		
		/// <summary>
		/// Check if a DIM line is a CONST declaration by scanning for CONST keyword.
		/// Does not modify the WordCollection pointer permanently.
		/// </summary>
		private static bool IsConstDeclaration(WordCollection wc)
		{
			int originalPointer = wc.Pointer;
			try
			{
				while (!wc.EOL)
				{
					var word = wc.Current as IdentifierWord;
					if (word == null)
					{
						// Hit a non-identifier (comma, equals, etc.) - no more keywords
						break;
					}
					
					string code = word.Code;
					if (Config.ICVariable)
						code = code.ToUpper();
					
					if (code == "CONST")
						return true;
					
					// Check if this is a known keyword or the variable name
					switch (code)
					{
						case "REF":
						case "DYNAMIC":
						case "STATIC":
						case "GLOBAL":
						case "SAVEDATA":
						case "CHARADATA":
							// Known keyword, continue scanning
							wc.ShiftNext();
							continue;
						default:
							// This is the variable name, stop scanning
							return false;
					}
				}
				return false;
			}
			finally
			{
				wc.Pointer = originalPointer;
			}
		}

		private void analyzeSharpFunction(StringStream st, ScriptPosition position, bool funcs)
		{
			throw new NotImplCodeEE();
			//WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
			//UserDefinedFunctionData data = UserDefinedFunctionData.Create(wc, funcs, position);
			//idDic.AddRefMethod(UserDefinedRefMethod.Create(data));
		}
	}
}
