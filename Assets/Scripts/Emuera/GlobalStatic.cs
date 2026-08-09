using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameView;
using uEmuera.Window;

namespace MinorShift.Emuera
{
	/* 1756 Created
	 * The design intent was to keep data as private as possible, with only what's needed exposed — that's ancient history now.
	 * Every modification adds more Process.Instance.XXX references.
	 * Accepting that growth is unavoidable, the plan is to at least gather all the poorly-behaved references into one place and manage them there.
	 * Going forward, Instance will no longer be released as public static — reference through here instead.
	 * But ideally, reduce references through here too.
	 */
	internal static class GlobalStatic
	{
		//Listed in the order they are created.
		//Referencing from bottom to top may return null.
		//Config Replace
		public static MainWindow MainWindow;
		public static EmueraConsole Console;
		public static Process Process;
		//Config.RenameDic
		public static GameBase GameBaseData;
		public static ConstantData ConstantData;
		public static VariableData VariableData;
		//StrForm
		public static VariableEvaluator VEvaluator;
		public static IdentifierDictionary IdentifierDictionary;
		public static ExpressionMediator EMediator;
		//
		public static LabelDictionary LabelDictionary;


		//Bridge variable for passing argument parse results to ErbLoader
		//1756 Moved from Process. For Program.AnalysisMode
		public static Dictionary<string, Int64> tempDic = new Dictionary<string, long>();
#if UEMUERA_DEBUG
		public static List<FunctionLabelLine> StackList = new List<FunctionLabelLine>();
#endif
		public static void Reset()
		{
			Process = null;
			ConstantData = null;
			GameBaseData = null;
			EMediator = null;
			VEvaluator = null;
			VariableData = null;
			Console = null;
			MainWindow = null;
			LabelDictionary = null;
			IdentifierDictionary = null;
			tempDic.Clear();
		}
	}
}
