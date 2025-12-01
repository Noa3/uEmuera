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
	/* 1756 create
	 * canだけdataはprivateにして必要なthingだけが参照doようにしようcalled 設計だったのは今は昔.
	 * 改変のたびにProcess.Instance.XXXなんかがどんどん増えていく.
	 * まあ,増えるのは仕方notと諦める事にして,line儀の悪い参照の仕方をdothingたちをせめて一箇所に集めて管理しようcalled 計画でexist.
	 * これfromはInstanceを public static に解放dothisはやめ,ここfrom参照do.
	 * しかし,canならここfromの参照は減らしたい.
	 */
	internal static class GlobalStatic
	{
		//これは生成be done順序で並んでいる.
		//belowfromaboveを参照したcase,nullを返be donethisがexist.
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


		//ERBloaderにargumentparseの結果を渡すbecauseの橋渡しvariable
		//1756 Processfrommove.Program.AnalysisModefor
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
