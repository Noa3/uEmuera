using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Variable;

namespace MinorShift.Emuera.GameProc
{
	//1756 インナーclass解除して一般に開放


	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude = false)]
	internal enum SystemStateCode
	{
		__CAN_SAVE__ = 0x10000,//saveload画面をcallpossibleか？
		__CAN_BEGIN__ = 0x20000,//BEGIN命令をcallpossibleか？
		Title_Begin = 0,//initial状態
		Openning = 1,//first input待ち
		Train_Begin = 0x10,//BEGIN TRAINfrom.
		Train_CallEventTrain = 0x11,//@EVENTTRAINのcallinside.スキップpossible
		Train_CallShowStatus = 0x12,//@SHOW_STATUSのcallinside
		Train_CallComAbleXX = 0x13,//@COM_ABLExxのcallinside.スキップのcase,RETURN 1とdo.
		Train_CallShowUserCom = 0x14,//@SHOW_USERCOMのcallinside
		Train_WaitInput = 0x15,//input待ち状態.選択が実linepossibleならEVENTCOMfromCOMxx,そうでなければ@USERCOMにRESULTを渡す
		Train_CallEventCom = 0x16 | __CAN_BEGIN__,//@EVENTCOMのcallinside

		Train_CallComXX = 0x17 | __CAN_BEGIN__,//@COMxxのcallinside
		Train_CallSourceCheck = 0x18 | __CAN_BEGIN__,//@SOURCE_CHECKのcallinside
		Train_CallEventComEnd = 0x19 | __CAN_BEGIN__,//@EVENTCOMENDのcallinside.スキップpossible.Train_CallEventTrainto帰る.@USERCOMのcallinsideもここ

		Train_DoTrain = 0x1A,

		AfterTrain_Begin = 0x20 | __CAN_BEGIN__,//BEGIN AFTERTRAINfrom.@EVENTENDをcallてNormalto.

		Ablup_Begin = 0x30,//BEGIN ABLUPfrom.
		Ablup_CallShowJuel = 0x31,//@SHOW_JUEL
		Ablup_CallShowAblupSelect = 0x32,//@SHOW_ABLUP_SELECT
		Ablup_WaitInput = 0x33,//
		Ablup_CallAblupXX = 0x34 | __CAN_BEGIN__,//@ABLUPxxがnotcaseは,@USERABLUPにRESULTを渡す.Ablup_CallShowJuelto戻る.

		Turnend_Begin = 0x40 | __CAN_BEGIN__,//BEGIN TURNENDfrom.@EVENTTURNENDをcallてNormalto.

		Shop_Begin = 0x50 | __CAN_SAVE__,//BEGIN SHOPfrom
		Shop_CallEventShop = 0x51 | __CAN_BEGIN__ | __CAN_SAVE__,//@EVENTSHOPのcallinside.スキップpossible
		Shop_CallShowShop = 0x52 | __CAN_SAVE__,//@SHOW_SHOPのcallinside
		Shop_WaitInput = 0x53 | __CAN_SAVE__,//input待ち状態.アイテムが存在doならEVENTBUYにBOUGHT,そうでなければ@USERSHOPにRESULTを渡す
		Shop_CallEventBuy = 0x54 | __CAN_BEGIN__ | __CAN_SAVE__,//@USERSHOPまた@EVENTBUYはのcallinside

		SaveGame_Begin = 0x100,//SAVEGAMEfrom
		SaveGame_WaitInput = 0x101,//input待ち
		SaveGame_WaitInputOverwrite = 0x102,//above書きのallow待ち
		SaveGame_CallSaveInfo = 0x103,//@SAVEINFOcallinside.20回.
		LoadGame_Begin = 0x110,//LOADGAMEfrom
		LoadGame_WaitInput = 0x111,//input待ち
		LoadGameOpenning_Begin = 0x120,//最初に[1]を選択したとき.
		LoadGameOpenning_WaitInput = 0x121,//input待ち


		//AutoSave_Begin = 0x200,
		AutoSave_CallSaveInfo = 0x201,
		AutoSave_CallUniqueAutosave = 0x202,
		AutoSave_Skipped = 0x203,

		LoadData_DataLoaded = 0x210,//dataload直after
		LoadData_CallSystemLoad = 0x211 | __CAN_BEGIN__,//dataload直after
		LoadData_CallEventLoad = 0x212 | __CAN_BEGIN__,//@EVENTLOADのcallinside.スキップpossible

		Openning_TitleLoadgame = 0x220,

		System_Reloaderb = 0x230,
		First_Begin = 0x240,

		Normal = 0xFFFF | __CAN_BEGIN__ | __CAN_SAVE__,//特に何でもnotとき.ScriptEndに達したらError
	}

	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude = false)]
	internal enum BeginType
	{
		NULL = 0,
		SHOP = 2,
		TRAIN = 3,
		AFTERTRAIN = 4,
		ABLUP = 5,
		TURNEND = 6,
		FIRST = 7,
		TITLE = 8,
	}

	internal sealed class ProcessState
	{
		public ProcessState(EmueraConsole console)
		{
			if (Program.DebugMode)//DebugModeでなければ知らなくて良い
				this.console = console;
		}
		readonly EmueraConsole console = null;
		readonly List<CalledFunction> functionList = new List<CalledFunction>();
		private LogicalLine currentLine;
		//private LogicalLine nextLine;
		public int lineCount = 0;
        public int currentMin = 0;
        //private bool sequential;

		public bool ScriptEnd
		{
			get
			{
                return functionList.Count == currentMin;
            }
		}

        public int functionCount
        {
            get
            {
                return functionList.Count;
            }
        }

		SystemStateCode sysStateCode = SystemStateCode.Title_Begin;
		BeginType begintype = BeginType.NULL;
		public bool isBegun { get { return (begintype != BeginType.NULL) ? true : false; } }

        public LogicalLine CurrentLine { get { return currentLine; } set { currentLine = value; } }
        public LogicalLine ErrorLine
		{
			get
			{
				//if (RunningLine != null)
				//	return RunningLine;
				return currentLine;
			}
		}

		//IF文insideでELSEIF文のinside身をチェックdoetcCurrentLineと作業insideのLineがdifferentwhenにセットdo
		//public LogicalLine RunningLine { get; set; }
		//1755a calloriginal消滅
		//public bool Sequential { get { return sequential; } }
		public CalledFunction CurrentCalled
		{
			get
			{
				//実linefunctionなしの状態はpartのsystemINPUT以outではdoes not existのでGOTO系のprocessでしかここに来not関係above,前提を満たしようがnot
				//if (functionList.Count == 0)
				//    throw new ExeEE("実lineinsidefunctionがnot");
				return functionList[functionList.Count - 1];
			}
		}
		public SystemStateCode SystemState
		{
			get { return sysStateCode; }
			set { sysStateCode = value; }
		}

		public void ShiftNextLine()
		{
            currentLine = currentLine.NextLine;
            //nextLine = nextLine.NextLine;
            //RunningLine = null;
            //sequential = true;
			//GlobalStatic.Process.lineCount++;
			lineCount++;
		}

		/// <summary>
		/// functioninのmove.JUMPではなくGOTOやIF文etc
		/// </summary>
		/// <param name="line"></param>
		public void JumpTo(LogicalLine line)
		{
            currentLine = line;
            lineCount++;
            //sequential = false;
			//ShfitNextLine();
		}

		public void SetBegin(string keyword)
		{//TrimとToUpper済みのはず
			switch (keyword)
			{
				case "SHOP":
					SetBegin(BeginType.SHOP); return;
				case "TRAIN":
					SetBegin(BeginType.TRAIN); return;
				case "AFTERTRAIN":
					SetBegin(BeginType.AFTERTRAIN); return;
				case "ABLUP":
					SetBegin(BeginType.ABLUP); return;
				case "TURNEND":
					SetBegin(BeginType.TURNEND); return;
				case "FIRST":
					SetBegin(BeginType.FIRST); return;
				case "TITLE":
					SetBegin(BeginType.TITLE); return;
			}
			throw new CodeEE("BEGINのkeyワード\"" + keyword + "\"は未definitionです");
		}

		public void SetBegin(BeginType type)
		{
			string errmes;
			switch (type)
			{
				case BeginType.SHOP:
				case BeginType.TRAIN:
				case BeginType.AFTERTRAIN:
				case BeginType.ABLUP:
				case BeginType.TURNEND:
				case BeginType.FIRST:
					if ((sysStateCode & SystemStateCode.__CAN_BEGIN__) != SystemStateCode.__CAN_BEGIN__)
					{
						errmes = "BEGIN";
						goto err;
					}
					break;
				//1.729 BEGIN TITLEはどこでも使えるように
				case BeginType.TITLE:
					break;
				//BEGINのprocessinsideでチェック済み
				//default:
				//    throw new ExeEE("不適当なBEGINcall");
			}
			begintype = type;
			return;
		err:
			CalledFunction func = functionList[0];
			string funcName = func.FunctionName;
			throw new CodeEE("@" + funcName + "insideで" + errmes + "命令を実linedocannot be");
		}

		public void SaveLoadData(bool saveData)
		{

			if (saveData)
				sysStateCode = SystemStateCode.SaveGame_Begin;
			else
				sysStateCode = SystemStateCode.LoadGame_Begin;
			//ClearFunctionList();
			return;
		}

		public void ClearFunctionList()
		{
			if (Program.DebugMode && !isClone && GlobalStatic.Process.MethodStack() == 0)
				console.DebugClearTraceLog();
			foreach (CalledFunction called in functionList)
                if (called.CurrentLabel.hasPrivDynamicVar)
                    called.CurrentLabel.Out();
			functionList.Clear();
			begintype = BeginType.NULL;
		}

		public bool calledWhenNormal = true;
		/// <summary>
		/// BEGIN命令によるプログラム状態の変化
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public void Begin()
		{
			//@EVENTSHOPfromのcallは一旦破棄
			if (sysStateCode == SystemStateCode.Shop_CallEventShop)
				return;

			switch (begintype)
			{
				case BeginType.SHOP:
					if (sysStateCode == SystemStateCode.Normal)
						calledWhenNormal = true;
					else
						calledWhenNormal = false;
					sysStateCode = SystemStateCode.Shop_Begin;
					break;
				case BeginType.TRAIN:
					sysStateCode = SystemStateCode.Train_Begin;
					break;
				case BeginType.AFTERTRAIN:
					sysStateCode = SystemStateCode.AfterTrain_Begin;
					break;
				case BeginType.ABLUP:
					sysStateCode = SystemStateCode.Ablup_Begin;
					break;
				case BeginType.TURNEND:
					sysStateCode = SystemStateCode.Turnend_Begin;
					break;
				case BeginType.FIRST:
					sysStateCode = SystemStateCode.First_Begin;
					break;
				case BeginType.TITLE:
					sysStateCode = SystemStateCode.Title_Begin;
					break;
				//セットwhenに判定してるので,ここには来notはず
				//default:
				//    throw new ExeEE("不適当なBEGINcall");
			}
			if (Program.DebugMode)
			{
				console.DebugClearTraceLog();
				console.DebugAddTraceLog("BEGIN:" + begintype.ToString());
			}
			foreach (CalledFunction called in functionList)
                if (called.CurrentLabel.hasPrivDynamicVar)
                    called.CurrentLabel.Out();
			functionList.Clear();
			begintype = BeginType.NULL;
			return;
		}

		/// <summary>
		/// systemによる強制的なBEGIN
		/// </summary>
		/// <param name="type"></param>
		public void Begin(BeginType type)
		{
			begintype = type;
			sysStateCode = SystemStateCode.Title_Begin;
			Begin();
		}

		public LogicalLine GetCurrentReturnAddress
		{
			get
			{
                if (functionList.Count == currentMin)
                    return null;
				return functionList[functionList.Count - 1].ReturnAddress;
			}
		}

        public LogicalLine GetReturnAddressSequensial(int curerntDepth)
        {
            if (functionList.Count == currentMin)
                return null;
            return functionList[functionList.Count - curerntDepth - 1].ReturnAddress;
        }

		public string Scope
		{
			get
			{
				//scriptの実lineinsideprocessfromしか呼び出されnotので,ここはnot…はず
				//if (functionList.Count == 0)
				//{
				//    throw new ExeEE("実lineinsideのfunctionが存在しません");
				//}
				if (functionList.Count == 0)
					return null;//1756 debugcommandfrom呼び出be doneようになったので
				return functionList[functionList.Count - 1].FunctionName;
			}
		}

		public void Return(Int64 ret)
		{
			if (IsFunctionMethod)
			{
				ReturnF(null);
				return;
			}
			//sequential = false;//いずれにしろ順列ではnot.
			//calloriginalは全部scriptprocess
			//if (functionList.Count == 0)
			//{
			//    throw new ExeEE("実lineinsideのfunctionが存在しません");
			//}
			CalledFunction called = functionList[functionList.Count - 1];
			if (called.IsJump)
			{//JUMPしたcase.即座にRETURN RESULTdo.
                if (called.TopLabel.hasPrivDynamicVar)
                    called.TopLabel.Out();
				functionList.Remove(called);
				if (Program.DebugMode)
					console.DebugRemoveTraceLog();
				Return(ret);
				return;
			}
			if (!called.IsEvent)
			{
                if (called.TopLabel.hasPrivDynamicVar)
                    called.TopLabel.Out();
                currentLine = null;
            }
			else
			{
                if (called.CurrentLabel.hasPrivDynamicVar)
                    called.CurrentLabel.Out();
				//#Singleフラグ付きfunctionで1が返された.
				//1752 非0ではなく1と等価でexistthisを見るように修正
				//1756 allをendではなく#PRIや#LATERのグループごとに修正
                if (called.IsOnly)
                    called.FinishEvent();
				else if ((called.HasSingleFlag) && (ret == 1))
					called.ShiftNextGroup();
				else
                    called.ShiftNext();//next 同名functionに進む.
                currentLine = called.CurrentLabel;//functionの始point(@～～)tomove.呼ぶべきfunctionが無ければnull
                if (called.CurrentLabel != null)
                {
                    lineCount++;
                    if (called.CurrentLabel.hasPrivDynamicVar)
                        called.CurrentLabel.In();
                }
            }
			if (Program.DebugMode)
				console.DebugRemoveTraceLog();
			//functionend
            if (currentLine == null)
            {
                currentLine = called.ReturnAddress;
                functionList.RemoveAt(functionList.Count - 1);
				if (currentLine == null)
				{
					//このwhenpointでfunctionListは空のはず
					//functionList.Clear();//allend.stateEndProcessにprocessing 返す
					if (begintype != BeginType.NULL)//BEGIN XXがlineなわれていれば
					{
						Begin();
					}
					return;
				}
                lineCount++;
                //ShfitNextLine();
                return;
			}
			else if (Program.DebugMode)
			{
				FunctionLabelLine label = called.CurrentLabel;
				console.DebugAddTraceLog("CALL :@" + label.LabelName + ":" + label.Position.ToString() + "line ");
			}
            lineCount++;
            //ShfitNextLine();
            return;
		}

		public void IntoFunction(CalledFunction call, UserDefinedFunctionArgument srcArgs, ExpressionMediator exm)
		{

			if (call.IsEvent)
			{
				foreach (CalledFunction called in functionList)
				{
					if (called.IsEvent)
						throw new CodeEE("EVENTfunctionの解決before CALLEVENT命令がlineわれました");
				}
			}
			if (Program.DebugMode)
			{
				FunctionLabelLine label = call.CurrentLabel;
				if (call.IsJump)
					console.DebugAddTraceLog("JUMP :@" + label.LabelName + ":" + label.Position.ToString() + "line ");
				else
					console.DebugAddTraceLog("CALL :@" + label.LabelName + ":" + label.Position.ToString() + "line ");
			}
            if (srcArgs != null)
            {
                //argumentのvalueを確定させる
                srcArgs.SetTransporter(exm);
                //プライベートvariableupdate
                if (call.TopLabel.hasPrivDynamicVar)
                    call.TopLabel.In();
                //updateしたvariabletoargumentを代入
                for (int i = 0; i < call.TopLabel.Arg.Length; i++)
                {
                    if (srcArgs.Arguments[i] != null)
                    {
						if (call.TopLabel.Arg[i].Identifier.IsReference)
							((ReferenceToken)(call.TopLabel.Arg[i].Identifier)).SetRef(srcArgs.TransporterRef[i]);
                        else if (srcArgs.Arguments[i].GetOperandType() == typeof(Int64))
                            call.TopLabel.Arg[i].SetValue(srcArgs.TransporterInt[i], exm);
                        else
                            call.TopLabel.Arg[i].SetValue(srcArgs.TransporterStr[i], exm);
                    }
                }
            }
            else//こっちに来るのはsystemfromのcall=argumentはdoes not existfunctionのみ ifネストのoutに出していい気もしnotでもnotがはてさて
            {
                //プライベートvariableupdate
                if (call.TopLabel.hasPrivDynamicVar)
                    call.TopLabel.In();
            }
			functionList.Add(call);
			//sequential = false;
            currentLine = call.CurrentLabel;
            lineCount++;
            //ShfitNextLine();
        }

		#region userdifinedmethod
		public bool IsFunctionMethod
		{
			get
			{
                return functionList[currentMin].TopLabel.IsMethod;
            }
		}

		public SingleTerm MethodReturnValue = null;

		public void ReturnF(SingleTerm ret)
		{
			//loadingwhenのチェック済みのはず
			//if (!IsFunctionMethod)
			//    throw new ExeEE("ReturnFと#FUNCTIONのチェックがおかしい");
			//sequential = false;//いずれにしろ順列ではnot.
			//calloriginalはRETURNFcommandかfunctionendwhenのみ
			//if (functionList.Count == 0)
			//    throw new ExeEE("実lineinsideのfunctionが存在しません");
			//非eventcallなので,これは起こりえnot
			//else if (functionList.Count != 1)
			//    throw new ExeEE("functionが複数exist");
			if (Program.DebugMode)
			{
				console.DebugRemoveTraceLog();
			}
			//OutはGetValue側でlineう
			//functionList[0].TopLabel.Out();
            currentLine = functionList[functionList.Count - 1].ReturnAddress;
            functionList.RemoveAt(functionList.Count - 1);
            //nextLine = null;
            MethodReturnValue = ret;
            return;
		}

		#endregion

		bool isClone = false;
        public bool IsClone { get { return isClone; } set { isClone = value; } }

		// functionListのコピーを必要とdocalloriginalが無かったのでコピーしnotthisにdo.
		public ProcessState Clone()
		{
			ProcessState ret = new ProcessState(console);
			ret.isClone = true;
			//どうせ消すfromコピー不要
			//foreach (CalledFunction func in functionList)
			//	ret.functionList.Add(func.Clone());
			ret.currentLine = this.currentLine;
            //ret.nextLine = this.nextLine;
            //ret.sequential = this.sequential;
			ret.sysStateCode = this.sysStateCode;
			ret.begintype = this.begintype;
			//ret.MethodReturnValue = this.MethodReturnValue;
			return ret;

		}
		//public ProcessState CloneForFunctionMethod()
		//{
		//    ProcessState ret = new ProcessState(console);
		//    ret.isClone = true;

		//    //どうせ消すfromコピー不要
		//    //foreach (CalledFunction func in functionList)
		//    //	ret.functionList.Add(func.Clone());
		//    ret.currentLine = this.currentLine;
		//    ret.nextLine = this.nextLine;
		//    //ret.sequential = this.sequential;
		//    ret.sysStateCode = this.sysStateCode;
		//    ret.begintype = this.begintype;
		//    //ret.MethodReturnValue = this.MethodReturnValue;
		//    return ret;
		//}
	}
}