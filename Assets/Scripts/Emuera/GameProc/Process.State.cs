using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Variable;

namespace MinorShift.Emuera.GameProc
{
	//1756 Removed inner class, exposed to general use


	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude = false)]
	internal enum SystemStateCode
	{
		__CAN_SAVE__ = 0x10000,//can the save/load screen be called?
		__CAN_BEGIN__ = 0x20000,//can the BEGIN command be called?
		Title_Begin = 0,//initial state
		Openning = 1,//first input wait
		Train_Begin = 0x10,//from BEGIN TRAIN.
		Train_CallEventTrain = 0x11,//while @EVENTTRAIN is being called. skippable
		Train_CallShowStatus = 0x12,//while @SHOW_STATUS is being called
		Train_CallComAbleXX = 0x13,//while @COM_ABLExx is being called. if skipped, treat as RETURN 1.
		Train_CallShowUserCom = 0x14,//while @SHOW_USERCOM is being called
		Train_WaitInput = 0x15,//input wait state. if the selection can run pass from EVENTCOM to COMxx, otherwise pass RESULT to @USERCOM
		Train_CallEventCom = 0x16 | __CAN_BEGIN__,//while @EVENTCOM is being called

		Train_CallComXX = 0x17 | __CAN_BEGIN__,//while @COMxx is being called
		Train_CallSourceCheck = 0x18 | __CAN_BEGIN__,//while @SOURCE_CHECK is being called
		Train_CallEventComEnd = 0x19 | __CAN_BEGIN__,//while @EVENTCOMEND is being called. skippable. returns to Train_CallEventTrain. also here while @USERCOM is being called

		Train_DoTrain = 0x1A,

		AfterTrain_Begin = 0x20 | __CAN_BEGIN__,//from BEGIN AFTERTRAIN. call @EVENTEND then move to Normal.

		Ablup_Begin = 0x30,//from BEGIN ABLUP.
		Ablup_CallShowJuel = 0x31,//@SHOW_JUEL
		Ablup_CallShowAblupSelect = 0x32,//@SHOW_ABLUP_SELECT
		Ablup_WaitInput = 0x33,//
		Ablup_CallAblupXX = 0x34 | __CAN_BEGIN__,//if @ABLUPxx does not exist, pass RESULT to @USERABLUP. returns to Ablup_CallShowJuel.

		Turnend_Begin = 0x40 | __CAN_BEGIN__,//from BEGIN TURNEND. calls @EVENTTURNEND then moves to Normal.

		Shop_Begin = 0x50 | __CAN_SAVE__,//from BEGIN SHOP
		Shop_CallEventShop = 0x51 | __CAN_BEGIN__ | __CAN_SAVE__,//while @EVENTSHOP is being called. skippable
		Shop_CallShowShop = 0x52 | __CAN_SAVE__,//while @SHOW_SHOP is being called
		Shop_WaitInput = 0x53 | __CAN_SAVE__,//input wait state. if an item exists pass BOUGHT to EVENTBUY, otherwise pass RESULT to @USERSHOP
		Shop_CallEventBuy = 0x54 | __CAN_BEGIN__ | __CAN_SAVE__,//while @USERSHOP or @EVENTBUY is being called

		SaveGame_Begin = 0x100,//from SAVEGAME
		SaveGame_WaitInput = 0x101,//input wait
		SaveGame_WaitInputOverwrite = 0x102,//waiting for overwrite permission
		SaveGame_CallSaveInfo = 0x103,//while @SAVEINFO is being called. 20 times.
		LoadGame_Begin = 0x110,//from LOADGAME
		LoadGame_WaitInput = 0x111,//input wait
		LoadGameOpenning_Begin = 0x120,//when [1] is selected for the first time.
		LoadGameOpenning_WaitInput = 0x121,//input wait


		//AutoSave_Begin = 0x200,
		AutoSave_CallSaveInfo = 0x201,
		AutoSave_CallUniqueAutosave = 0x202,
		AutoSave_Skipped = 0x203,

		LoadData_DataLoaded = 0x210,//right after data load
		LoadData_CallSystemLoad = 0x211 | __CAN_BEGIN__,//right after data load
		LoadData_CallEventLoad = 0x212 | __CAN_BEGIN__,//while @EVENTLOAD is being called. skippable

		Openning_TitleLoadgame = 0x220,

		System_Reloaderb = 0x230,
		First_Begin = 0x240,

		Normal = 0xFFFF | __CAN_BEGIN__ | __CAN_SAVE__,//when nothing in particular. Error when ScriptEnd is reached
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
			if (Program.DebugMode)//only need to know if not DebugMode
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

		//set when the working Line differs from CurrentLine such as when checking the content of ELSEIF inside an IF statement
		//public LogicalLine RunningLine { get; set; }
		//1755a caller eliminated
		//public bool Sequential { get { return sequential; } }
		public CalledFunction CurrentCalled
		{
			get
			{
				//a state with no executing function only exists for some system INPUT, so in relation to reaching here only via GOTO-type processing, the precondition cannot be met
				//if (functionList.Count == 0)
				//    throw new ExeEE("実行中関数がない");//no executing function
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
		/// Movement within a function. GOTO and IF statements, not JUMP
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
		{//should already be Trimmed and ToUpper'ed
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
			throw new CodeEE("BEGINのキーワード\"" + keyword + "\"は未定義です");
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
				//1.729 allow BEGIN TITLE everywhere
				case BeginType.TITLE:
					break;
				//already checked during BEGIN processing
				//default:
				//    throw new ExeEE("不適当なBEGIN呼び出し");
			}
			begintype = type;
			return;
		err:
			CalledFunction func = functionList[0];
			string funcName = func.FunctionName;
			throw new CodeEE("@" + funcName + "中で" + errmes + "命令を実行することはできません");
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
		/// Program state transition caused by the BEGIN command
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public void Begin()
		{
			//call from @EVENTSHOP is discarded for now
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
				//judged at set time, so should never reach here
				//default:
				//    throw new ExeEE("不適当なBEGIN呼び出し");
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
		/// Forced BEGIN by the system
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
				//only called from script execution processing, so should not be case here... probably
				//if (functionList.Count == 0)
				//{
				//    throw new ExeEE("実行中の関数が存在しません");
				//}
				if (functionList.Count == 0)
					return null;//1756 now called from debug commands
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
			//sequential = false;//not sequential either way.
			//all callers are script processing
			//if (functionList.Count == 0)
			//{
			//    throw new ExeEE("実行中の関数が存在しません");
			//}
			CalledFunction called = functionList[functionList.Count - 1];
			if (called.IsJump)
			{//when JUMPed. immediately RETURN RESULT.
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
				//1 was returned from a function with #Single flag.
				//1752 corrected to check equivalence with 1 rather than non-zero
				//1756 corrected to per-#PRI/#LATER group instead of ending everything
                if (called.IsOnly)
                    called.FinishEvent();
				else if ((called.HasSingleFlag) && (ret == 1))
					called.ShiftNextGroup();
				else
                    called.ShiftNext();//proceed to next same-name function.
                currentLine = called.CurrentLabel;//move to function start (@~~). null if there is no function to call
                if (called.CurrentLabel != null)
                {
                    lineCount++;
                    if (called.CurrentLabel.hasPrivDynamicVar)
                        called.CurrentLabel.In();
                }
            }
			if (Program.DebugMode)
				console.DebugRemoveTraceLog();
			//function end
            if (currentLine == null)
            {
                currentLine = called.ReturnAddress;
                functionList.RemoveAt(functionList.Count - 1);
				if (currentLine == null)
				{
					//functionList should be empty at this point
					//functionList.Clear();//all finished. return processing to stateEndProcess
					if (begintype != BeginType.NULL)//if BEGIN XX was done
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
				console.DebugAddTraceLog("CALL :@" + label.LabelName + ":" + label.Position.ToString() + "行目");
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
						throw new CodeEE("EVENT関数の解決前にCALLEVENT命令が行われました");
				}
			}
			if (Program.DebugMode)
			{
				FunctionLabelLine label = call.CurrentLabel;
				if (call.IsJump)
					console.DebugAddTraceLog("JUMP :@" + label.LabelName + ":" + label.Position.ToString() + "行目");
				else
					console.DebugAddTraceLog("CALL :@" + label.LabelName + ":" + label.Position.ToString() + "行目");
			}
            if (srcArgs != null)
            {
                //finalize argument values
                srcArgs.SetTransporter(exm);
                //update private variables
                if (call.TopLabel.hasPrivDynamicVar)
                    call.TopLabel.In();
                //assign arguments to the updated variables
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
            else//reach here only from system calls = functions with no arguments only. could move outside the if nest but ah well
            {
                //update private variables
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
			//should already be checked at load time
			//if (!IsFunctionMethod)
			//    throw new ExeEE("ReturnFと#FUNCTIONのチェックがおかしい");
			//sequential = false;//not sequential either way anyway
			//callers are only the RETURNF command or function end
			//if (functionList.Count == 0)
			//    throw new ExeEE("実行中の関数が存在しません");
			//since this is a non-event call, this cannot happen
			//else if (functionList.Count != 1)
			//    throw new ExeEE("関数が複数ある");
			if (Program.DebugMode)
			{
				console.DebugRemoveTraceLog();
			}
			//Out is done on the GetValue side
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

		// decided not to copy since there was no caller that needed a copy of functionList
		public ProcessState Clone()
		{
			ProcessState ret = new ProcessState(console);
			ret.isClone = true;
//it will be discarded anyway so no copy needed
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

		//    //it will be discarded anyway so no copy needed
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