using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameView;
using System.IO;
using System.Text.RegularExpressions;

namespace MinorShift.Emuera
{
	//1756 Newly created. Collects information that Parser, LexicalAnalyzer, etc. need to know.
	//Ideally this should be passed as an argument, but rewriting all Parser arguments is tedious, so it is static.
	internal static class ParserMediator
	{
		/// <summary>
		/// Warnings generated from emuera.config etc.
		/// Occurs before Initialize.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="?"></param>
		public static void ConfigWarn(string str, ScriptPosition pos, int level, string stack)
		{
			if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
				return;
			lock (warningList)
				warningList.Add(new ParserWarning(str, pos, level, stack));
		}

		static EmueraConsole console;
		public static void Initialize(EmueraConsole console)
		{
			ParserMediator.console = console;
		}

		#region Rename
		public static Dictionary<string, string> RenameDic { get; private set; }
		//1756 Moved from Process.Load.cs
		public static void LoadEraExRenameFile(string filepath)
		{
			if (RenameDic != null)
				RenameDic.Clear();
			//Always create the dictionary. It is null only when UseRenameFile is NO.
			RenameDic = new Dictionary<string, string>();
			
			// Use case-insensitive file resolution for non-Windows systems
			string resolvedPath = uEmuera.Utils.ResolveExistingFilePath(filepath);
			EraStreamReader eReader = new EraStreamReader(false);
			if ((string.IsNullOrEmpty(resolvedPath)) || (!eReader.Open(resolvedPath)))
			{
				return;
			}
			string line;
			ScriptPosition pos = null;
			Regex reg = new Regex(@"\\,", RegexOptions.Compiled);
			try
			{
                string[] tokens = new string[2];
                while ((line = eReader.ReadLine()) != null)
				{
					if (line.Length == 0)
						continue;
					if (line.StartsWith(";"))
						continue;
					string[] baseTokens = reg.Split(line);
					if (!baseTokens[baseTokens.Length - 1].Contains(","))
						continue;
					string[] last = baseTokens[baseTokens.Length - 1].Split(',');
					baseTokens[baseTokens.Length - 1] = last[0];
					//string[] tokens = new string[2];
					tokens[0] = string.Join(",", baseTokens);
					tokens[1] = last[1];
					pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
					//The right side is the notation in ERB; the left side is the replacement.
					string value = tokens[0].Trim();
					string key = string.Format("[[{0}]]", tokens[1].Trim());
					RenameDic[key] = value;
					pos = null;
				}
			}
			catch (Exception e)
			{
				if (pos != null)
					throw new CodeEE(e.Message, pos);
				else
					throw new CodeEE(e.Message);

			}
			finally
			{
				eReader.Close();
			}
		}
		#endregion


		public static void Warn(string str, ScriptPosition pos, int level)
		{
			Warn(str, pos, level, null);
		}

		public static void Warn(string str, ScriptPosition pos, int level, string stack)
		{
			if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
				return;
			if (console != null && !console.RunERBFromMemory)
				lock (warningList)
					warningList.Add(new ParserWarning(str, pos, level, stack));
		}

		/// <summary>
		/// Warning output during parsing
		/// </summary>
		/// <param name="str"></param>
		/// <param name="line"></param>
		/// <param name="level">Warning level. 0: minor mistake. 1: ignorable line. 2: harmless if the line is not executed. 3: fatal.</param>
		public static void Warn(string str, LogicalLine line, int level, bool isError, bool isBackComp)
		{
            Warn(str, line, level, isError, isBackComp, null);
		}

        public static void Warn(string str, LogicalLine line, int level, bool isError, bool isBackComp, string stack)
        {
            if (isError)
            {
                line.IsError = true;
                line.ErrMes = str;
            }
            if (level < Config.DisplayWarningLevel && !Program.AnalysisMode)
                return;
            if (isBackComp && !Config.WarnBackCompatibility)
                return;
            if (console != null && !console.RunERBFromMemory)
                lock (warningList)
                    warningList.Add(new ParserWarning(str, line.Position, level, stack));
            //				console.PrintWarning(str, line.Position, level);
        }
        
        private static List<ParserWarning> warningList = new List<ParserWarning>();

		public static bool HasWarning{get {lock (warningList) return warningList.Count > 0;}}
		public static void ClearWarningList()
		{
			lock (warningList)
				warningList.Clear();
		}

		public static void FlushWarningList()
		{
			List<ParserWarning> local;
			lock (warningList)
			{
				if (warningList.Count == 0) return;
				local = warningList;
				warningList = new List<ParserWarning>();
			}
			for (int i = 0; i < local.Count; i++)
			{
				ParserWarning warning = local[i];
				console.PrintWarning(warning.WarningMes, warning.WarningPos, warning.WarningLevel);
                if (warning.StackTrace != null)
                {
                    string[] stacks = warning.StackTrace.Split('\n');
                    for (int j = 0; j < stacks.Length; j++)
                    {
						console.PrintSystemLine(stacks[j]);
                    }
                }
            }
		}

		private class ParserWarning
		{
			public ParserWarning(string mes, ScriptPosition pos, int level, string stackTrace)
			{
				WarningMes = mes;
				WarningPos = pos;
				WarningLevel = level;
                StackTrace = stackTrace;
			}
			public string WarningMes;
			public ScriptPosition WarningPos;
			public int WarningLevel;
            public string StackTrace;
		}
	}
}