using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameData.Function;
using System.Linq;
using uEmuera.Forms;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Main game process controller for the Emuera engine.
    /// Manages script execution, function calls, variable evaluation, and game state.
    /// Coordinates between the game data model, view (console), and script interpreter.
    /// </summary>
    internal sealed partial class Process
    {
		public Process(EmueraConsole view)
		{
			console = view;
		}

        public LogicalLine getCurrentLine { get { return state.CurrentLine; } }

		/// <summary>
		/// Collection of @~~ and $~~. Used by CALL instructions, etc.
		/// Execution order is held by LogicalLine itself.
		/// </summary>
		LabelDictionary labelDic;
		public LabelDictionary LabelDictionary { get { return labelDic; } }

		/// <summary>
		/// All variables. Variables needed in scripts (including those not directly accessible by users) are placed here.
		/// </summary>
		private VariableEvaluator vEvaluator;
		public VariableEvaluator VEvaluator { get { return vEvaluator; } }
		private ExpressionMediator exm;
		private GameBase gamebase;
		readonly EmueraConsole console;
		private IdentifierDictionary idDic;
		ProcessState state;
		ProcessState originalState;//For resetting
        bool noError = false;
        //Revived after various issues
        bool initialiing;
        public bool inInitializeing { get { return initialiing;  } }

        public bool Initialize()
		{
			LexicalAnalyzer.UseMacro = false;
            state = new ProcessState(console);
            originalState = state;
            initialiing = true;
            try
            {
				ParserMediator.Initialize(console);
				//Error handling for config files (config files are loaded before entering this function)
				if (ParserMediator.HasWarning)
				{
					ParserMediator.FlushWarningList();
					if(MessageBox.Show(GameMessages.ConfigFileError, GameMessages.ConfigFileErrorTitle, MessageBoxButtons.YesNo)
						== DialogResult.Yes)
					{
						console.PrintSystemLine(GameMessages.ConfigExitSelected);
						return false;
					}
				}
				//Load resources folder
				if (!Content.AppContents.LoadContents())
				{
					ParserMediator.FlushWarningList();
					console.PrintSystemLine(GameMessages.ResourcesLoadError);
					return false;
				}
				ParserMediator.FlushWarningList();
				//Load key macros
                if (Config.UseKeyMacro && !Program.AnalysisMode)
                {
					string macroPath = uEmuera.Utils.ResolveExistingFilePath(Program.ExeDir + "macro.txt");
                    if (!string.IsNullOrEmpty(macroPath))
                    {
                        if (Config.DisplayReport)
							console.PrintSystemLine(GameMessages.LoadingMacroTxt);
                        KeyMacro.LoadMacroFile(macroPath);
                    }
				}
				//_replace.csv読み込み
                if (Config.UseReplaceFile && !Program.AnalysisMode)
                {
					string replacePath = uEmuera.Utils.ResolveExistingFilePath(Program.CsvDir + "_Replace.csv");
					if (!string.IsNullOrEmpty(replacePath))
					{
						if (Config.DisplayReport)
							console.PrintSystemLine(GameMessages.LoadingReplaceCsv);
						ConfigData.Instance.LoadReplaceFile(replacePath);
						if (ParserMediator.HasWarning)
						{
							ParserMediator.FlushWarningList();
							if (MessageBox.Show(GameMessages.ReplaceCsvError, GameMessages.ReplaceCsvErrorTitle, MessageBoxButtons.YesNo)
								== DialogResult.Yes)
							{
								console.PrintSystemLine(GameMessages.ReplaceCsvExitSelected);
								return false;
							}
						}
					}
                }
                Config.SetReplace(ConfigData.Instance);
                //I realized this would be a good place to set BAR
                console.setStBar(Config.DrawLineString);

				//Load _rename.csv
				if (Config.UseRenameFile)
                {
					string renamePath = uEmuera.Utils.ResolveExistingFilePath(Program.CsvDir + "_Rename.csv");
					if (!string.IsNullOrEmpty(renamePath))
                    {
                        if (Config.DisplayReport || Program.AnalysisMode)
							console.PrintSystemLine(GameMessages.LoadingRenameCsv);
						ParserMediator.LoadEraExRenameFile(renamePath);
                    }
                    else
                        console.PrintError(GameMessages.RenameCsvNotFound);
                }
                if (!Config.DisplayReport)
                {
                    console.PrintSingleLine(Config.LoadLabel);
                    console.RefreshStrings(true);
				}
				//Load gamebase.csv
				gamebase = new GameBase();
                if (!gamebase.LoadGameBaseCsv(Program.CsvDir + "GAMEBASE.CSV"))
                {
					ParserMediator.FlushWarningList();
                    console.PrintSystemLine(GameMessages.GamebaseLoadError);
                    return false;
                }
				console.SetWindowTitle(gamebase.ScriptWindowTitle);
				GlobalStatic.GameBaseData = gamebase;

				//Load all csv files except the above
				ConstantData constant = new ConstantData();
				constant.LoadData(Program.CsvDir, console, Config.DisplayReport);
				GlobalStatic.ConstantData = constant;
				TrainName = constant.GetCsvNameList(VariableCode.TRAINNAME);

                vEvaluator = new VariableEvaluator(gamebase, constant);
				GlobalStatic.VEvaluator = vEvaluator;

				idDic = new IdentifierDictionary(vEvaluator.VariableData);
				GlobalStatic.IdentifierDictionary = idDic;

				StrForm.Initialize();
				VariableParser.Initialize();

				exm = new ExpressionMediator(this, vEvaluator, console);
				GlobalStatic.EMediator = exm;

				labelDic = new LabelDictionary();
				GlobalStatic.LabelDictionary = labelDic;
				HeaderFileLoader hLoader = new HeaderFileLoader(console, idDic, this);

				LexicalAnalyzer.UseMacro = false;

				//Load ERH
				if (!hLoader.LoadHeaderFiles(Program.ErbDir, Config.DisplayReport))
				{
					ParserMediator.FlushWarningList();
					console.PrintSystemLine(GameMessages.ErhLoadError);
					return false;
				}
				LexicalAnalyzer.UseMacro = idDic.UseMacro();

				//Load CSV data for user-defined variables (if available)
				LoadUserDefinedVariablesFromCsv(Program.CsvDir, console, Config.DisplayReport);

				//Load ERB
				ErbLoader loader = new ErbLoader(console, exm, this);
                if (Program.AnalysisMode)
                    noError = loader.loadErbs(Program.AnalysisFiles, labelDic);
                else
                    noError = loader.LoadErbFiles(Program.ErbDir, Config.DisplayReport, labelDic);
                initSystemProcess();
                initialiing = false;
            }
			catch (Exception e)
			{
                handleException(e, null, true);
				console.PrintSystemLine(GameMessages.FatalInitError);
				return false;
			}
			if (labelDic == null)
			{
				return false;
			}
			state.Begin(BeginType.TITLE);
			GC.Collect();
            return true;
		}

		public void ReloadErb()
		{
			saveCurrentState(false);
			state.SystemState = SystemStateCode.System_Reloaderb;
			ErbLoader loader = new ErbLoader(console, exm, this);
            loader.LoadErbFiles(Program.ErbDir, false, labelDic);
			console.ReadAnyKey();
		}

		public void ReloadPartialErb(List<string> path)
		{
			saveCurrentState(false);
			state.SystemState = SystemStateCode.System_Reloaderb;
			ErbLoader loader = new ErbLoader(console, exm, this);
			loader.loadErbs(path, labelDic);
			console.ReadAnyKey();
		}

		public void SetCommnds(Int64 count)
		{
			coms = new List<long>((int)count);
			isCTrain = true;
			Int64[] selectcom = vEvaluator.SELECTCOM_ARRAY;
			if (count >= selectcom.Length)
			{
				throw new CodeEE("CALLTRAIN命令のargumentの値がSELECTCOMの要素数を超えています");
			}
			for (int i = 0; i < (int)count; i++)
			{
				coms.Add(selectcom[i + 1]);
			}
		}

        public bool ClearCommands()
        {
            coms.Clear();
            count = 0;
            isCTrain = false;
            skipPrint = true;
            return (callFunction("CALLTRAINEND", false, false));
        }

		public void InputResult5(int r0, int r1, int r2, int r3, int r4)
		{
			long[] result = vEvaluator.RESULT_ARRAY;
			result[0] = r0;
			result[1] = r1;
			result[2] = r2;
			result[3] = r3;
			result[4] = r4;
		}
		public void InputInteger(Int64 i)
		{
			vEvaluator.RESULT = i;
		}
		public void InputSystemInteger(Int64 i)
		{
			systemResult = i;
		}
		public void InputString(string s)
		{
			vEvaluator.RESULTS = s;
		}

		private uint startTime = 0;
		
		public void DoScript()
		{
			startTime = _Library.WinmmTimer.TickCount;
			state.lineCount = 0;
			bool systemProcRunning = true;
			try
			{
				while (true)
				{
					methodStack = 0;
					systemProcRunning = true;
					while (state.ScriptEnd && console.IsRunning)
						runSystemProc();
					if (!console.IsRunning)
						break;
					systemProcRunning = false;
					runScriptProc();
				}
			}
			catch (Exception ec)
			{
				LogicalLine currentLine = state.ErrorLine;
				if (currentLine != null && currentLine is NullLine)
					currentLine = null;
				if (systemProcRunning)
					handleExceptionInSystemProc(ec, currentLine, true);
				else
					handleException(ec, currentLine, true);
			}
		}
		
		public void BeginTitle()
		{
			vEvaluator.ResetData();
			state = originalState;
			state.Begin(BeginType.TITLE);
		}

		public void UpdateCheckInfiniteLoopState()
		{
			startTime = _Library.WinmmTimer.TickCount;
			state.lineCount = 0;
		}

		private void checkInfiniteLoop()
		{
			//Doesn't work well. Cannot stop BEEP sound so this processing is cancelled (1.51)
			////Freeze prevention. Can view history even during processing
			//System.Windows.Forms.Application.DoEvents();
			////System.Threading.Thread.Sleep(0);

			//if (!console.Enabled)
			//{
			//    //If window is closed during DoEvents(), it's over.
			//    console.ReadAnyKey();
			//    return;
			//}
			uint time = _Library.WinmmTimer.TickCount - startTime;
			if (time < Config.InfiniteLoopAlertTime)
				return;
			LogicalLine currentLine = state.CurrentLine;
			if ((currentLine == null) || (currentLine is NullLine))
				return;//Skip if current line is in a special state
			if (!console.Enabled)
				return;//Cannot show MessageBox if closed.
			string caption = GameMessages.InfiniteLoopDetected;
			string text = string.Format(
				GameMessages.InfiniteLoopMessage,
				currentLine.Position.Filename, currentLine.Position.LineNo, state.lineCount, time);
			DialogResult result = MessageBox.Show(text, caption, MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
			{
				throw new CodeEE("Force quit selected due to suspected infinite loop");
			}
			else
			{
				state.lineCount = 0;
				startTime = _Library.WinmmTimer.TickCount;
			}
		}

		int methodStack = 0;
		public SingleTerm GetValue(SuperUserDefinedMethodTerm udmt)
		{
			methodStack++;
            if (methodStack > 100)
            {
                //StackOverflowException cannot be caught and has no reproducibility, so cut off at a certain number before it occurs.
                //Depending on the environment, StackOverflowException may occur before 100?
                throw new CodeEE("Function call stack overflow (is there infinite recursion?)");
            }
            SingleTerm ret = null;
            int temp_current = state.currentMin;
            state.currentMin = state.functionCount;
            udmt.Call.updateRetAddress(state.CurrentLine);
            try
            {
				state.IntoFunction(udmt.Call, udmt.Argument, exm);
                //Errors thrown inside do-while are not caught here.
				//They are caught in DoScript after exiting all #functions.
    			runScriptProc();
                ret = state.MethodReturnValue;
			}
			finally
			{
				if (udmt.Call.TopLabel.hasPrivDynamicVar)
					udmt.Call.TopLabel.Out();
                //1756beta2+v3: These must be here or there will be a major accident when an expression function crashes in the debug console
                state.currentMin = temp_current;
                methodStack--;
            }
			return ret;
		}

        public void clearMethodStack()
        {
            methodStack = 0;
        }

        public int MethodStack()
        {
            return methodStack;
        }

		public ScriptPosition GetRunningPosition()
		{
			LogicalLine line = state.ErrorLine;
			if (line == null)
				return null;
			return line.Position;
		}
/*
		private readonly string scaningScope = null;
		private string GetScaningScope()
		{
			if (scaningScope != null)
				return scaningScope;
			return state.Scope;
		}
*/
		public LogicalLine scaningLine = null;
		internal LogicalLine GetScaningLine()
		{
			if (scaningLine != null)
				return scaningLine;
			LogicalLine line = state.ErrorLine;
			if (line == null)
				return null;
			return line;
		}
		
		
		private void handleExceptionInSystemProc(Exception exc, LogicalLine current, bool playSound)
		{
			console.ThrowError(playSound);
			if (exc is CodeEE)
			{
				console.PrintError(GameMessages.ErrorAtFunctionEnd + Program.ExeName);
				console.PrintError(exc.Message);
			}
			else if (exc is ExeEE)
			{
				console.PrintError(GameMessages.EmueraErrorAtFunctionEnd + Program.ExeName);
				console.PrintError(exc.Message);
			}
			else
			{
				console.PrintError(GameMessages.UnexpectedErrorAtFunctionEnd + Program.ExeName);
				console.PrintError(exc.GetType().ToString() + ":" + exc.Message);
				string[] stack = exc.StackTrace.Split('\n');
				for (int i = 0; i < stack.Length; i++)
				{
					console.PrintError(stack[i]);
				}
			}
		}
		
		private void handleException(Exception exc, LogicalLine current, bool playSound)
		{
            UnityEngine.Debug.Log(exc);

			console.ThrowError(playSound);
			ScriptPosition position = null;
            if ((exc is EmueraException ee) && (ee.Position != null))
                position = ee.Position;
            else if ((current != null) && (current.Position != null))
				position = current.Position;
			string posString = "";
			if (position != null)
			{
				if (position.LineNo >= 0)
					posString = "At line " + position.LineNo.ToString() + " of " + position.Filename + " ";
				else
					posString = "In " + position.Filename + " ";
					
			}
			if (exc is CodeEE)
			{
                if (position != null)
				{
                    if (current is InstructionLine procline && procline.FunctionCode == FunctionCode.THROW)
                    {
                        console.PrintErrorButton(posString + GameMessages.ThrowOccurred, position);
                        printRawLine(position);
                        console.PrintError(GameMessages.ThrowContent + " " + exc.Message);
                    }
                    else
                    {
                        console.PrintErrorButton(posString + GameMessages.ErrorOccurred + " " + Program.ExeName, position);
						printRawLine(position);
						console.PrintError(GameMessages.ErrorContent + " " + exc.Message);
                    }
                    console.PrintError(GameMessages.CurrentFunction + " @" + current.ParentLabelLine.LabelName + " (line " + current.ParentLabelLine.Position.LineNo.ToString() + " of " + current.ParentLabelLine.Position.Filename + ")");
                    console.PrintError(GameMessages.FunctionCallStack);
                    LogicalLine parent;
                    int depth = 0;
                    while ((parent = state.GetReturnAddressSequensial(depth++)) != null)
                    {
                        if (parent.Position != null)
                        {
                            console.PrintErrorButton("-> line " + parent.Position.LineNo.ToString() + " of " + parent.Position.Filename + " (in function @" + parent.ParentLabelLine.LabelName + ")", parent.Position);
                        }
                    } 
				}
				else
				{
					console.PrintError(posString + GameMessages.ErrorOccurred + " " + Program.ExeName);
					console.PrintError(exc.Message);
				}
			}
			else if (exc is ExeEE)
			{
				console.PrintError(posString + GameMessages.EmueraErrorOccurred + " " + Program.ExeName);
				console.PrintError(exc.Message);
			}
			else
            {
				console.PrintError(posString + GameMessages.UnexpectedErrorOccurred + " " + Program.ExeName);
				console.PrintError(exc.GetType().ToString() + ":" + exc.Message);
				string[] stack = exc.StackTrace.Split('\n');
				for (int i = 0; i < stack.Length; i++)
				{
					console.PrintError(stack[i]);
				}
			}
		}

		public void printRawLine(ScriptPosition position)
		{
			string str = getRawTextFormFilewithLine(position);
			if (str != "")
				console.PrintError(str);
		}

		public string getRawTextFormFilewithLine(ScriptPosition position)
        {
			string extents = position.Filename.Substring(position.Filename.Length - 4).ToLower();
			if (extents == ".erb")
			{
				string path = uEmuera.Utils.ResolveExistingFilePath(Program.ErbDir + position.Filename);
				if (string.IsNullOrEmpty(path))
					return "";
				return position.LineNo > 0 ? File.ReadLines(path, Config.Encode).Skip(position.LineNo - 1).First() : "";
			}
			else if (extents == ".csv")
			{
				string path = uEmuera.Utils.ResolveExistingFilePath(Program.CsvDir + position.Filename);
				if (string.IsNullOrEmpty(path))
					return "";
				return position.LineNo > 0 ? File.ReadLines(path, Config.Encode).Skip(position.LineNo - 1).First() : "";
			}
			else
				return "";
		}

		/// <summary>
		/// Loads CSV data for user-defined variables.
		/// Searches for CSV files named after user-defined variables (e.g., MyVar.CSV)
		/// and initializes the variables with values from the CSV.
		/// CSV format: Each line contains values for array elements, comma-separated.
		/// Lines starting with semicolon (;) are treated as comments and skipped.
		/// </summary>
		/// <param name="csvDir">Directory containing CSV files</param>
		/// <param name="console">Console for output messages</param>
		/// <param name="displayReport">Whether to display loading reports</param>
		private void LoadUserDefinedVariablesFromCsv(string csvDir, EmueraConsole console, bool displayReport)
		{
			if (!Directory.Exists(csvDir))
				return;

			// Get all user-defined variables from the identifier dictionary
			var userVars = idDic.GetAllUserDefinedVariables();
			if (userVars == null || userVars.Count == 0)
				return;

			foreach (var varToken in userVars)
			{
				// Look for a CSV file matching the variable name
				string csvPath = Path.Combine(csvDir, varToken.Name + ".CSV");
				csvPath = uEmuera.Utils.ResolveExistingFilePath(csvPath);
				if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
					continue;

				try
				{
					if (displayReport)
						console.PrintSystemLine($"Loading user variable data: {varToken.Name}.CSV");

					LoadUserVariableFromCsv(csvPath, varToken);
				}
				catch (Exception ex)
				{
					ParserMediator.Warn($"Failed to load CSV for variable {varToken.Name}: {ex.Message}", 
						new ScriptPosition(csvPath, 0), 1);
				}
			}
		}

		/// <summary>
		/// Loads data from a CSV file into a user-defined variable.
		/// Supports integer and string arrays (1D, 2D).
		/// Note: 3D arrays not currently supported due to complexity of CSV representation.
		/// </summary>
		/// <param name="csvPath">Path to the CSV file</param>
		/// <param name="varToken">Variable token to initialize</param>
		private void LoadUserVariableFromCsv(string csvPath, VariableToken varToken)
		{
			const char CSV_COMMENT_CHAR = ';';
			
			using (EraStreamReader reader = new EraStreamReader(false))
			{
				if (!reader.Open(csvPath))
					return;

				List<string> lines = new List<string>();
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					// Skip comment lines (starting with semicolon) and empty lines
					if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith(CSV_COMMENT_CHAR.ToString()))
						continue;
					lines.Add(line);
				}

				if (lines.Count == 0)
					return;

				// Initialize the variable based on its type and dimensionality
				if (varToken.IsString)
					LoadStringVariableFromLines(lines, varToken);
				else
					LoadIntegerVariableFromLines(lines, varToken);
			}
		}

		/// <summary>
		/// Loads string variable data from CSV lines.
		/// Supports 1D and 2D string arrays.
		/// Note: 3D arrays not supported - CSV format would be overly complex for 3D data representation.
		/// </summary>
		private void LoadStringVariableFromLines(List<string> lines, VariableToken varToken)
		{
			int lineCount = lines.Count;
			
			if (varToken.Dimension == 1)
			{
				// 1D array: each line is one element
				int length = varToken.GetLength();
				for (int i = 0; i < Math.Min(lineCount, length); i++)
				{
					varToken.SetValue(lines[i], new long[] { i });
				}
			}
			else if (varToken.Dimension == 2)
			{
				// 2D array: comma-separated values per line
				int length1 = varToken.GetLength(0);
				int length2 = varToken.GetLength(1);
				for (int i = 0; i < Math.Min(lineCount, length1); i++)
				{
					string[] values = lines[i].Split(',');
					for (int j = 0; j < Math.Min(values.Length, length2); j++)
					{
						varToken.SetValue(values[j].Trim(), new long[] { i, j });
					}
				}
			}
		}

		/// <summary>
		/// Loads integer variable data from CSV lines.
		/// Supports 1D and 2D integer arrays.
		/// Note: 3D arrays not supported - CSV format would be overly complex for 3D data representation.
		/// </summary>
		private void LoadIntegerVariableFromLines(List<string> lines, VariableToken varToken)
		{
			int lineCount = lines.Count;
			
			if (varToken.Dimension == 1)
			{
				// 1D array: parse each line as integer
				int length = varToken.GetLength();
				for (int i = 0; i < Math.Min(lineCount, length); i++)
				{
					if (long.TryParse(lines[i].Trim(), out long value))
					{
						varToken.SetValue(value, new long[] { i });
					}
				}
			}
			else if (varToken.Dimension == 2)
			{
				// 2D array: comma-separated integer values per line
				int length1 = varToken.GetLength(0);
				int length2 = varToken.GetLength(1);
				for (int i = 0; i < Math.Min(lineCount, length1); i++)
				{
					string[] values = lines[i].Split(',');
					for (int j = 0; j < Math.Min(values.Length, length2); j++)
					{
						if (long.TryParse(values[j].Trim(), out long value))
						{
							varToken.SetValue(value, new long[] { i, j });
						}
					}
				}
			}
		}

	}
}
