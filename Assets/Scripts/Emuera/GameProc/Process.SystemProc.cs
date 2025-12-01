using System;
using System.Collections.Generic;
using System.IO;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData;

namespace MinorShift.Emuera.GameProc
{
	internal sealed partial class Process
	{
		private string[] TrainName = null;
		delegate void SystemProcess();
		Dictionary<SystemStateCode, SystemProcess> systemProcessDictionary = new Dictionary<SystemStateCode, SystemProcess>();
		private void initSystemProcess()
		{
			comAble = new int[TrainName.Length];
			systemProcessDictionary.Add(SystemStateCode.Title_Begin, new SystemProcess(this.beginTitle));
			systemProcessDictionary.Add(SystemStateCode.Openning, new SystemProcess(this.endOpenning));

			systemProcessDictionary.Add(SystemStateCode.Train_Begin, new SystemProcess(this.beginTrain));
			systemProcessDictionary.Add(SystemStateCode.Train_CallEventTrain, new SystemProcess(this.endCallEventTrain));
			systemProcessDictionary.Add(SystemStateCode.Train_CallShowStatus, new SystemProcess(this.endCallShowStatus));
			systemProcessDictionary.Add(SystemStateCode.Train_CallComAbleXX, new SystemProcess(this.endCallComAbleXX));
			systemProcessDictionary.Add(SystemStateCode.Train_CallShowUserCom, new SystemProcess(this.endCallShowUserCom));
			systemProcessDictionary.Add(SystemStateCode.Train_WaitInput, new SystemProcess(this.trainWaitInput));
			systemProcessDictionary.Add(SystemStateCode.Train_CallEventCom, new SystemProcess(this.endEventCom));
			systemProcessDictionary.Add(SystemStateCode.Train_CallComXX, new SystemProcess(this.endCallComXX));
			systemProcessDictionary.Add(SystemStateCode.Train_CallSourceCheck, new SystemProcess(this.endCallSourceCheck));
			systemProcessDictionary.Add(SystemStateCode.Train_CallEventComEnd, new SystemProcess(this.endCallEventComEnd)); ;
			systemProcessDictionary.Add(SystemStateCode.Train_DoTrain, new SystemProcess(this.doTrain));

			systemProcessDictionary.Add(SystemStateCode.AfterTrain_Begin, new SystemProcess(this.beginAfterTrain));

			systemProcessDictionary.Add(SystemStateCode.Ablup_Begin, new SystemProcess(this.beginAblup));
			systemProcessDictionary.Add(SystemStateCode.Ablup_CallShowJuel, new SystemProcess(this.endCallShowJuel));
			systemProcessDictionary.Add(SystemStateCode.Ablup_CallShowAblupSelect, new SystemProcess(this.endCallShowAblupSelect));
			systemProcessDictionary.Add(SystemStateCode.Ablup_WaitInput, new SystemProcess(this.ablupWaitInput));
			systemProcessDictionary.Add(SystemStateCode.Ablup_CallAblupXX, new SystemProcess(this.endCallAblupXX));

			systemProcessDictionary.Add(SystemStateCode.Turnend_Begin, new SystemProcess(this.beginTurnend));

			systemProcessDictionary.Add(SystemStateCode.Shop_Begin, new SystemProcess(this.beginShop));
			systemProcessDictionary.Add(SystemStateCode.Shop_CallEventShop, new SystemProcess(this.endCallEventShop));
			systemProcessDictionary.Add(SystemStateCode.Shop_CallShowShop, new SystemProcess(this.endCallShowShop));
			systemProcessDictionary.Add(SystemStateCode.Shop_WaitInput, new SystemProcess(this.shopWaitInput));
			systemProcessDictionary.Add(SystemStateCode.Shop_CallEventBuy, new SystemProcess(this.endCallEventBuy));

			systemProcessDictionary.Add(SystemStateCode.SaveGame_Begin, new SystemProcess(this.beginSaveGame));
			systemProcessDictionary.Add(SystemStateCode.SaveGame_WaitInput, new SystemProcess(this.saveGameWaitInput));
			systemProcessDictionary.Add(SystemStateCode.SaveGame_WaitInputOverwrite, new SystemProcess(this.saveGameWaitInputOverwrite));
			systemProcessDictionary.Add(SystemStateCode.SaveGame_CallSaveInfo, new SystemProcess(this.endCallSaveInfo));
			systemProcessDictionary.Add(SystemStateCode.LoadGame_Begin, new SystemProcess(this.beginLoadGame));
			systemProcessDictionary.Add(SystemStateCode.LoadGame_WaitInput, new SystemProcess(this.loadGameWaitInput));
			systemProcessDictionary.Add(SystemStateCode.LoadGameOpenning_Begin, new SystemProcess(this.beginLoadGameOpening));
			systemProcessDictionary.Add(SystemStateCode.LoadGameOpenning_WaitInput, new SystemProcess(this.loadGameWaitInput));

			//stateEndProcessDictionary.Add(ProgramState.AutoSave_Begin, new stateEndProcess(this.beginAutoSave));
			systemProcessDictionary.Add(SystemStateCode.AutoSave_CallSaveInfo, new SystemProcess(this.endAutoSaveCallSaveInfo));
			systemProcessDictionary.Add(SystemStateCode.AutoSave_CallUniqueAutosave, new SystemProcess(this.endAutoSave));

			systemProcessDictionary.Add(SystemStateCode.LoadData_DataLoaded, new SystemProcess(this.beginDataLoaded));
			systemProcessDictionary.Add(SystemStateCode.LoadData_CallSystemLoad, new SystemProcess(this.endSystemLoad));
			systemProcessDictionary.Add(SystemStateCode.LoadData_CallEventLoad, new SystemProcess(this.endEventLoad));

			systemProcessDictionary.Add(SystemStateCode.Openning_TitleLoadgame, new SystemProcess(this.endTitleLoadgame));

			systemProcessDictionary.Add(SystemStateCode.System_Reloaderb, new SystemProcess(this.endReloaderb));
			systemProcessDictionary.Add(SystemStateCode.First_Begin, new SystemProcess(this.beginFirst));


			systemProcessDictionary.Add(SystemStateCode.Normal, new SystemProcess(this.endNormal));
			return;
		}



		Int64 systemResult = 0;
		int lastCalledComable = -1;
		int lastAddCom = -1;
		//(Train.csvinsideのvalue-definitionされていなければ-1) == comAble[(displayされているvalue)];
		int[] comAble;//


		private void runSystemProc()
		{
			//script実lineduring ここには来notはず
			//if (!state.ScriptEnd)
			//    throw new ExeEE("Invalid call");

			//not物を渡すprocessは現状not
			//if (systemProcessDictionary.ContainsKey(state.SystemState))
			systemProcessDictionary[state.SystemState]();
			//else
			//    throw new ExeEE("未definitionの状態");

		}

		void setWait()
		{
			console.ReadAnyKey();
		}

		void setWaitInput()
		{
			InputRequest req = new InputRequest();
			req.InputType = InputType.IntValue;
			req.IsSystemInput = true;
			console.WaitInput(req);
		}


		private bool callFunction(string functionName, bool force, bool isEvent)
		{
			CalledFunction call;
			if (isEvent)
				call = CalledFunction.CallEventFunction(this, functionName, null);
			else
				call = CalledFunction.CallFunction(this, functionName, null);
			if (call == null)
				if (!force)
					return false;
				else
					throw new CodeEE("function\"@" + functionName + "\"not found");
			//そもそも非eventfunctionではfunction1個分しか与えnotので条件を満たすreasonがnot
			//if ((!isEvent) && (call.Count > 1))
			//    throw new ExeEE("eventfunctionでnotfunction\"@" + functionName + "\"の候補が複数exist");
			state.IntoFunction(call, null, null);
			return true;
		}

		//CheckState()from呼ばれるfunction群.ScriptEndに達したときのprocess.

		void beginTitle()
		{
			//連続調教commandprocessinsideの状態が持ち越されていたらここで消しておく
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			console.ResetStyle();
			deleteAllPrevState();
			if (Program.AnalysisMode)
			{
				console.PrintSystemLine("fileparseend:Analysis.logにoutputします");
				console.OutputLog(Program.ExeDir + "Analysis.log");
				console.noOutputLog = true;
				console.PrintSystemLine("エンターkeyもしくはクリックでwill terminate");
				uEmuera.Media.SystemSounds.Asterisk.Play();
				console.ThrowTitleError(false);
				return;
			}
			if ((!noError) && (!Config.CompatiErrorLine))
			{
				console.PrintSystemLine("ERBコードに解釈不possibleなlineがexistbecauseEmueraをwill terminate");
				console.PrintSystemLine("※互換性option"" + Config.GetConfigName(ConfigCode.CompatiErrorLine) + ""にthan強制的に動作させるthisができます");
				console.PrintSystemLine("emuera.logにログをoutputします");
				console.OutputLog(Program.ExeDir + "emuera.log");
				console.noOutputLog = true;
				console.PrintSystemLine("エンターkeyもしくはクリックでwill terminate");
				//System.Media.SystemSounds.Asterisk.Play();
				console.ThrowTitleError(true);
				return;
			}
			if (callFunction("SYSTEM_TITLE", false, false))
			{//独自definition
				state.SystemState = SystemStateCode.Normal;
				return;
			}
			//標準のタイトル画面
			console.PrintBar();
			console.NewLine();
			console.Alignment = GameView.DisplayLineAlignment.CENTER;
			console.PrintSingleLine(gamebase.ScriptTitle);
			if (gamebase.ScriptVersion != 0)
				console.PrintSingleLine(gamebase.ScriptVersionText);
			console.PrintSingleLine(gamebase.ScriptAutherName);
			console.PrintSingleLine("(" + gamebase.ScriptYear + ")");
			console.NewLine();
			console.PrintSingleLine(gamebase.ScriptDetail);
			console.Alignment = GameView.DisplayLineAlignment.LEFT;

			console.PrintBar();
			console.NewLine();
			console.PrintSingleLine("[0] " + Config.TitleMenuString0);
			console.PrintSingleLine("[1] " + Config.TitleMenuString1);
			openingInput();
			return;
		}

		void openingInput()
		{
			setWaitInput();
			state.SystemState = SystemStateCode.Openning;
			return;
		}

		void endOpenning()
		{
			if (systemResult == 0)
			{//[0] 最初fromはじめる
				vEvaluator.ResetData();
				//vEvaluator.AddCharacter(0, false);
				vEvaluator.AddCharacterFromCsvNo(0);
				if (gamebase.DefaultCharacter > 0)
					//vEvaluator.AddCharacter(gamebase.DefaultCharacter, false);
					vEvaluator.AddCharacterFromCsvNo(gamebase.DefaultCharacter);
				console.PrintBar();
				console.NewLine();
				beginFirst();
			}
			else if (systemResult == 1)
			{
				if (callFunction("TITLE_LOADGAME", false, false))
				{//独自definition
					state.SystemState = SystemStateCode.Openning_TitleLoadgame;
				}
				else
				{//標準のLOADGAME
					beginLoadGameOpening();
				}
			}
			else//inputが正しくnotならもう一回選択肢を書き直し,正しい選択を要求do.
			{//RESUELASTLINEと同様のprocessing lineうように変更
				console.deleteLine(1);
				console.PrintTemporaryLine("無効なvalueです");
				console.updatedGeneration = true;
				openingInput();
				//beginTitle();
			}

		}

		void beginFirst()
		{
			state.SystemState = SystemStateCode.Normal;
			//連続調教commandprocessinsideの状態が持ち越されていたらここで消しておく
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			callFunction("EVENTFIRST", true, true);
		}

		void endTitleLoadgame()
		{
			beginTitle();
		}

		void beginTrain()
		{
			vEvaluator.UpdateInBeginTrain();
			state.SystemState = SystemStateCode.Train_CallEventTrain;
			//EVENTTRAINをcallてTrain_CallEventTrainto移line.
			if (!callFunction("EVENTTRAIN", false, true))
			{
				//存在しなければスキップしてTrain_CallEventTrainが終わったthisにdo.
				endCallEventTrain();
			}
		}

		List<Int64> coms = new List<long>();
		bool isCTrain = false;
		int count = 0;
		bool skipPrint = false;
		public bool SkipPrint { get { return skipPrint; } set { skipPrint = value; } }
		void endCallEventTrain()
		{
			if (vEvaluator.NEXTCOM >= 0)
			{//NEXTCOMのprocess
				state.SystemState = SystemStateCode.Train_CallEventCom;
				vEvaluator.SELECTCOM = vEvaluator.NEXTCOM;
				vEvaluator.NEXTCOM = 0;
				//-1ではなく0を代入doのでERB側で変更しnot限り無限にはまるthisになるがeramakerfromの仕様でexist.
				callEventCom();
				return;
			}
			else
			{
				//if (!isCTrain)
				//{
				//SHOW_STATUSをcallてTrain_CallShowStatusto移line.
				if (isCTrain)
					skipPrint = true;
				callFunction("SHOW_STATUS", true, false);
				state.SystemState = SystemStateCode.Train_CallShowStatus;
				//}
				//else
				//{
				//連続調教モードならCOMABLEprocessto
				//	endCallShowStatus();
				//}
			}
		}

		void endCallShowStatus()
		{
			//SHOW_STATUSが終わったらComAbleXXのcall状態をリセットしてTrain_CallComAbleXXto移line.
			state.SystemState = SystemStateCode.Train_CallComAbleXX;
			lastCalledComable = -1;
			lastAddCom = -1;
			printComCount = 0;
			for (int i = 0; i < comAble.Length; i++)
				comAble[i] = -1;
			endCallComAbleXX();
		}

		string getTrainComString(int trainCode, int comNo)
		{
			string trainName = TrainName[trainCode];
			return string.Format("{0}[{1,3}]", trainName, comNo);
		}

		int printComCount = 0;
		void endCallComAbleXX()
		{
			//選択肢add.RESULTが0のcaseは選択肢の番号のみ増やしてaddはしnot.
			if ((lastCalledComable >= 0) && (TrainName[lastCalledComable] != null))
			{
				lastAddCom++;
				if (vEvaluator.RESULT != 0)
				{
					comAble[lastAddCom] = lastCalledComable;
					if (!isCTrain)
					{
						console.PrintC(getTrainComString(lastCalledComable, lastAddCom), true);
						printComCount++;
						if ((Config.PrintCPerLine > 0) && (printComCount % Config.PrintCPerLine == 0))
							console.PrintFlush(false);
					}
					console.RefreshStrings(false);
				}
			}
			//ComAbleXXのcall.train.csvにdefinitionされていnotthingはスキップ,ComAbleXXが見つfromなければREUTRN 1と同様に扱う.
			while (++lastCalledComable < TrainName.Length)
			{
				if (TrainName[lastCalledComable] == null)
					continue;
				string comName = string.Format("COM_ABLE{0}", lastCalledComable);
				if (!callFunction(comName, false, false))
				{
					lastAddCom++;
					if (Config.ComAbleDefault == 0)
						continue;
					comAble[lastAddCom] = lastCalledComable;
					if (!isCTrain)
					{
						console.PrintC(getTrainComString(lastCalledComable, lastAddCom), true);
						printComCount++;
						if ((Config.PrintCPerLine > 0) && (printComCount % Config.PrintCPerLine == 0))
							console.PrintFlush(false);
					}
					continue;
				}
				console.RefreshStrings(false);
				return;
			}
			//全部検索したらendし,SHOW_USERCOMを呼び出す.
			if (lastCalledComable >= TrainName.Length)
			{
				state.SystemState = SystemStateCode.Train_CallShowUserCom;
				//if (!isCTrain)
				//{
				console.PrintFlush(false);
				console.RefreshStrings(false);
				callFunction("SHOW_USERCOM", true, false);
				//}
				//else
				//	endCallShowUserCom();
			}
		}

		void endCallShowUserCom()
		{
			if (skipPrint)
				skipPrint = false;
			vEvaluator.UpdateAfterShowUsercom();
			if (!isCTrain)
			{
				//numericinput待ち状態にしてTrain_WaitInputto移line.
				setWaitInput();

				state.SystemState = SystemStateCode.Train_WaitInput;
			}
			else
			{
				if (count < coms.Count)
				{
					systemResult = coms[count];
					count++;
					trainWaitInput();
				}
			}
		}

		void trainWaitInput()
		{
			int selectCom = -1;
			if (!isCTrain)
			{
				if ((systemResult >= 0) && (systemResult < comAble.Length))
					selectCom = comAble[systemResult];
			}
			else
			{
				for (int i = 0; i < comAble.Length; i++)
				{
					if (comAble[i] == systemResult)
						selectCom = (int)systemResult;
				}
				console.PrintSingleLine(string.Format("＜command連続実line:{0}/{1}＞", count, coms.Count));
			}
			//TrainNameがdefinitionされていてusepossible(COMABLEが非0を返した)でexist
			if (selectCom >= 0)
			{
				vEvaluator.SELECTCOM = selectCom;
				callEventCom();
			}
			else
			{//されていnot.
				if (isCTrain)
					console.PrintSingleLine("commandを実lineできませんでした");
				vEvaluator.RESULT = systemResult;
				state.SystemState = SystemStateCode.Train_CallEventComEnd;
				callFunction("USERCOM", true, false);
				//COMinsideの必要なthisは全部USERCOMinでやる.
			}
		}

		private Int64 doTrainSelectCom = -1;
		void doTrain()
		{
			vEvaluator.UpdateAfterShowUsercom();
			vEvaluator.SELECTCOM = doTrainSelectCom;
			callEventCom();
		}

		void callEventCom()
		{
			vEvaluator.UpdateAfterInputCom();
			state.SystemState = SystemStateCode.Train_CallEventCom;
			if (!callFunction("EVENTCOM", false, true))
				endEventCom();
			return;
		}

		void endEventCom()
		{
			long selectCom = vEvaluator.SELECTCOM;
			string comName = string.Format("COM{0}", selectCom);
			state.SystemState = SystemStateCode.Train_CallComXX;
			callFunction(comName, true, false);
		}

		void endCallComXX()
		{
			//実lineにfailedした
			if (vEvaluator.RESULT == 0)
			{
				//Comend.
				endCallEventComEnd();
			}
			else
			{//successしたならSOURCE_CHECKto移line.
				state.SystemState = SystemStateCode.Train_CallSourceCheck;
				callFunction("SOURCE_CHECK", true, false);
			}
		}

		void endCallSourceCheck()
		{
			//SOURCEはここでリセット
			vEvaluator.UpdateAfterSourceCheck();
			//EVENTCOMENDをcallてTrain_CallEventComEndto移line.
			state.SystemState = SystemStateCode.Train_CallEventComEnd;
			//EVENTCOMENDがdoes not exist,orEVENTCOMENDinでWAIT系命令がlineわれnotcase,EVENTCOMENDafter WAITをadddo.
			NeedWaitToEventComEnd = true;
			if (!callFunction("EVENTCOMEND", false, true))
			{
				//見つfromnotならスキップしてTrain_CallEventComEndがendしたとみなす.
				endCallEventComEnd();
			}
		}
		public bool NeedWaitToEventComEnd = false;
		bool needCheck = true;
		void endCallEventComEnd()
		{
			if (console.LastLineIsTemporary && !isCTrain && needCheck)
			{
                if (console.LastLineIsEmpty)
                {
                    console.deleteLine(2);
                    console.PrintTemporaryLine("無効なvalueです");
                }
				console.updatedGeneration = true;
				endCallShowUserCom();
			}
			else
			{
				if (isCTrain && count == coms.Count)
				{
					isCTrain = false;
					skipPrint = false;
					coms.Clear();
					count = 0;
					if (callFunction("CALLTRAINEND", false, false))
					{
						needCheck = false;
						return;
					}
				}
				needCheck = true;
				////1.701	ここでWAITは不要だった.
				////setWait();
				//1.703 やはり必要なcaseもあった
				if (NeedWaitToEventComEnd)
					setWait();
				NeedWaitToEventComEnd = false;
				//SHOW_STATUSfromやり直す.
				//processはTrain_CallEventTrainとsame.
				endCallEventTrain();
			}
		}

		void beginAfterTrain()
		{
			//連続調教モードduring ここに来るcaseがexistので,ここで解除
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			state.SystemState = SystemStateCode.Normal;
			//EVENTENDを呼び出す.exe側が状態を把握do必要が無くなるのでNormalto移line.
			callFunction("EVENTEND", true, true);
		}

		void beginAblup()
		{
			//連続調教commandprocessinsideの状態が持ち越されていたらここで消しておく
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			state.SystemState = SystemStateCode.Ablup_CallShowJuel;
			//SHOW_JUELをcallAblup_CallShowJuelto移line.
			callFunction("SHOW_JUEL", true, false);
		}

		void endCallShowJuel()
		{
			state.SystemState = SystemStateCode.Ablup_CallShowAblupSelect;
			//SHOW_ABLUP_SELECTをcallAblup_CallAblupSelectto移line.
			callFunction("SHOW_ABLUP_SELECT", true, false);
		}

		void endCallShowAblupSelect()
		{
			//numericinput待ち状態にしてAblup_WaitInputto移line.
			setWaitInput();
			state.SystemState = SystemStateCode.Ablup_WaitInput;
		}

		void ablupWaitInput()
		{
			//definitionされていなくても100未満ならABLUPが呼ばれ,USERABLUPは呼ばれnot.そうしnotと[99]反発刻印とかが出来not.
			if ((systemResult >= 0) && (systemResult < 100))
			{
				state.SystemState = SystemStateCode.Ablup_CallAblupXX;
				string ablName = string.Format("ABLUP{0}", systemResult);
				if (!callFunction(ablName, false, false))
				{
					//見つfromなければend
					console.deleteLine(1);
					console.PrintTemporaryLine("無効なvalueです");
					console.updatedGeneration = true;
					endCallShowAblupSelect();
				}
			}
			else
			{
				vEvaluator.RESULT = systemResult;
				state.SystemState = SystemStateCode.Ablup_CallAblupXX;
				callFunction("USERABLUP", true, false);
			}
		}

		void endCallAblupXX()
		{
			if (console.LastLineIsTemporary)
			{
                if (console.LastLineIsEmpty)
                {
                    console.deleteLine(2);
                    console.PrintTemporaryLine("無効なvalueです");
                }
				console.updatedGeneration = true;
				endCallShowAblupSelect();
			}
			else
				beginAblup();
		}

		void beginTurnend()
		{
			//連続調教commandprocessinsideの状態が持ち越されていたらここで消しておく
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			//EVENTTURNENDをcallNormalto移line
			callFunction("EVENTTURNEND", true, true);
			state.SystemState = SystemStateCode.Normal;
		}

		void beginShop()
		{
			//連続調教commandprocessinsideの状態が持ち越されていたらここで消しておく
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			state.SystemState = SystemStateCode.Shop_CallEventShop;
			//EVENTSHOPをcallてShop_CallEventShopto移line.
			if (!callFunction("EVENTSHOP", false, true))
			{
				//存在しなければスキップしてShop_CallEventShopが終わったthisにdo.
				endCallEventShop();
			}
		}

		void endCallEventShop()
		{
			saveTarget = -1;
			if (Config.AutoSave && state.calledWhenNormal)
				beginAutoSave();
			else
			{
				state.SystemState = SystemStateCode.AutoSave_Skipped;
				endAutoSaveCallSaveInfo();
			}
		}

		void beginAutoSave()
		{
			if (callFunction("SYSTEM_AUTOSAVE", false, false))
			{//@SYSTEM_AUTOSAVEが存在doならそれを使う.
				state.SystemState = SystemStateCode.AutoSave_CallUniqueAutosave;
				return;
			}
			saveTarget = AutoSaveIndex;
			vEvaluator.SAVEDATA_TEXT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ";
			state.SystemState = SystemStateCode.AutoSave_CallSaveInfo;
			if (!callFunction("SAVEINFO", false, false))
				endAutoSaveCallSaveInfo();//存在しなければスキップ
		}

		void endAutoSaveCallSaveInfo()
		{
			if (saveTarget == AutoSaveIndex)
			{
				if (!vEvaluator.SaveTo(saveTarget, vEvaluator.SAVEDATA_TEXT))
				{
					console.PrintError("オートsaveduring 予期しnotErroroccurred");
					console.PrintError("オートsaveをスキップします");
					console.ReadAnyKey();
				}
			}
			endAutoSave();
		}

		void endAutoSave()
		{
			if (state.isBegun)
			{
				state.Begin();
				return;
			}
			state.SystemState = SystemStateCode.Shop_CallShowShop;
			//SHOW_SHOPをcallShop_CallShowShopto移line
			callFunction("SHOW_SHOP", true, false);
		}

		void endCallShowShop()
		{
			//numericinput待ち状態にしてShop_WaitInputto移line.
			setWaitInput();
			state.SystemState = SystemStateCode.Shop_WaitInput;
		}

		//PRINT_SHOPITEMとは独立している.
		//BOUGHTが100以aboveのアイテムが有り,ITEMSALESがTRUEだasも強制的に@USERSHOPlineき.
		void shopWaitInput()
		{
			if ((systemResult >= 0) && (systemResult < Config.MaxShopItem))
			{
				if (vEvaluator.ItemSales(systemResult))
				{
					if (vEvaluator.BuyItem(systemResult))
					{
						state.SystemState = SystemStateCode.Shop_CallEventBuy;
						//EVENTBUYをcallShop_CallEventBuyto移line
						if (!callFunction("EVENTBUY", false, true))
							endCallEventBuy();
						return;
					}
					else
					{
						//console.Print("お金が足りません.");
						//console.NewLine();
						console.deleteLine(1);
						console.PrintTemporaryLine("お金が足りません.");
					}
				}
				else
				{
					//console.Print("売っていません.");
					//console.NewLine();
					console.deleteLine(1);
					console.PrintTemporaryLine("売っていません.");
				}
				//購入にfailedしたcase,endCallEventShop()に戻す.
				//endCallEventShop();
				endCallShowShop();
				return;
			}
			else
			{
				//RESULTをupdate
				vEvaluator.RESULT = systemResult;

				//USERSHOPをcallShop_CallEventBuyto移line
				callFunction("USERSHOP", true, false);
				state.SystemState = SystemStateCode.Shop_CallEventBuy;
				return;
			}
		}

		void endCallEventBuy()
		{
			if (console.LastLineIsTemporary)
			{
                if (console.LastLineIsEmpty)
                {
                    console.deleteLine(2);
                    console.PrintTemporaryLine("無効なvalueです");
                }
				console.updatedGeneration = true;
				endCallShowShop();
			}
			else
			{
				//最初に戻る
				endAutoSave();
			}
		}


		void beginDataLoaded()
		{
			state.SystemState = SystemStateCode.LoadData_CallSystemLoad;
			
			if (!callFunction("SYSTEM_LOADEND", false, false))
				endSystemLoad();//存在しなければスキップ
		}
		void endSystemLoad()
		{
			state.SystemState = SystemStateCode.LoadData_CallEventLoad;
			//EVENTLOADをcallてLoadData_CallEventLoadto移line.
			if (!callFunction("EVENTLOAD", false, true))
			{
				//存在しなければスキップしてTrain_CallEventTrainが終わったthisにdo.
				endAutoSave();
			}
		}

		void endEventLoad()
		{
			//@EVENTLOADduring BEGIN命令がlineわれればここには来not.
			//ここに来たらBEGIN SHOP扱い.オートsaveはしnot.
			endAutoSave();
		}

		void beginSaveGame()
		{
			console.PrintSingleLine("何番にsave?？");
			state.SystemState = SystemStateCode.SaveGame_Begin;
			printSaveDataText();
		}

		void beginLoadGame()
		{
			console.PrintSingleLine("何番をload?？");
			state.SystemState = SystemStateCode.LoadGame_Begin;
			printSaveDataText();
		}

		void beginLoadGameOpening()
		{
			console.PrintSingleLine("何番をload?？");
			state.SystemState = SystemStateCode.LoadGameOpenning_Begin;
			printSaveDataText();
		}

		bool[] dataIsAvailable = new bool[21];
		bool isFirstTime = true;
		const int AutoSaveIndex = 99;
		int page = 0;
		void printSaveDataText()
		{
			if (isFirstTime)
			{
				isFirstTime = false;
				dataIsAvailable = new bool[Config.SaveDataNos + 1];
			}
			int dataNo = 0;
			for (int i = 0; i < page; i++)
			{
				console.PrintFlush(false);
				console.Print(string.Format("[{0, 2}] savedata{0, 2}～{1, 2}をdisplay", i * 20, i * 20 + 19));
			}
			for (int i = 0; i < 20; i++)
			{
				dataNo = page * 20 + i;
				if (dataNo == dataIsAvailable.Length - 1)
					break;
				dataIsAvailable[dataNo] = false;
				console.PrintFlush(false);
				console.Print(string.Format("[{0, 2}] ", dataNo));
				if (!writeSavedataTextFrom(dataNo))
					continue;
				dataIsAvailable[dataNo] = true;
			}
			for (int i = page; i < ((dataIsAvailable.Length - 2) / 20); i++)
			{
				console.PrintFlush(false);
				console.Print(string.Format("[{0, 2}] savedata{0, 2}～{1, 2}をdisplay", (i + 1) * 20, (i + 1) * 20 + 19));
			}
			//オートsaveのprocessは別途切り出し(displayprocessの都合above)
			dataIsAvailable[dataIsAvailable.Length - 1] = false;
			if (state.SystemState != SystemStateCode.SaveGame_Begin)
			{
				dataNo = AutoSaveIndex;
				console.PrintFlush(false);
				console.Print(string.Format("[{0, 2}] ", dataNo));
				if (writeSavedataTextFrom(dataNo))
					dataIsAvailable[dataIsAvailable.Length - 1] = true;
			}
			console.RefreshStrings(false);
			//描画全部終わり
			console.PrintSingleLine("[100] 戻る");
			setWaitInput();
			if (state.SystemState == SystemStateCode.SaveGame_Begin)
				state.SystemState = SystemStateCode.SaveGame_WaitInput;
			else if (state.SystemState == SystemStateCode.LoadGame_Begin)
				state.SystemState = SystemStateCode.LoadGame_WaitInput;
			else// if (state.SystemState == SystemStateCode.LoadGameOpenning_Begin)
				state.SystemState = SystemStateCode.LoadGameOpenning_WaitInput;
			//きちんとprocessされてるので,ここには来not
			//else
			//    throw new ExeEE("異常な状態");
		}

		int saveTarget = -1;
		void saveGameWaitInput()
		{
			if (systemResult == 100)
			{
				//キャンセルなら直previous 状態を呼び戻す
				loadPrevState();
				return;
			}
			else if (((int)systemResult / 20) != page && systemResult != AutoSaveIndex && (systemResult >= 0 && systemResult < dataIsAvailable.Length - 1))
			{
				page = (int)systemResult / 20;
				state.SystemState = SystemStateCode.SaveGame_Begin;
				printSaveDataText();
				return;
			}
			bool available = false;
			if ((systemResult >= 0) && (systemResult < dataIsAvailable.Length - 1))
				available = dataIsAvailable[systemResult];
			else
			{//inputしなおし
				console.deleteLine(1);
				console.PrintTemporaryLine("無効なvalueです");
				console.updatedGeneration = true;
				setWaitInput();
				return;
			}
			saveTarget = (int)systemResult;
			//既存dataがexistなら選択肢をdisplayしてSaveGame_WaitInputOverwriteto移line.
			if (available)
			{
				console.PrintSingleLine("既にdataがexists.above書き?？");
				console.PrintC("[0] はい", false);
				console.PrintC("[1] いいえ", false);
				setWaitInput();
				state.SystemState = SystemStateCode.SaveGame_WaitInputOverwrite;
				return;
			}
			//既存dataがnotなら"はい"を選んだthisにして直接ジャンプ
			systemResult = 0;
			saveGameWaitInputOverwrite();
		}

		void saveGameWaitInputOverwrite()
		{
			if (systemResult == 1)//いいえ
			{
				beginSaveGame();
				return;
			}
			else if (systemResult != 0)//"はい"でもnot
			{//inputしなおし
				console.deleteLine(1);
				console.PrintTemporaryLine("無効なvalueです");
				console.updatedGeneration = true;
				setWaitInput();
				return;
			}
			vEvaluator.SAVEDATA_TEXT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ";
			state.SystemState = SystemStateCode.SaveGame_CallSaveInfo;
			if (!callFunction("SAVEINFO", false, false))
				endCallSaveInfo();//存在しなければスキップ
		}

		void endCallSaveInfo()
		{
			if (!vEvaluator.SaveTo(saveTarget, vEvaluator.SAVEDATA_TEXT))
			{
				console.PrintError("saveduring 予期しnotErroroccurred");
				console.ReadAnyKey();
			}
			loadPrevState();
		}

		void loadGameWaitInput()
		{
			if (systemResult == 100)
			{//キャンセルなら
				//オープニングならオープニングto戻る
				if (state.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
				{
					beginTitle();
					return;
				}
				//それ以outfrom来たなら直previous 状態を呼び戻す
				loadPrevState();
				return;
			}
			else if (((int)systemResult / 20) != page && systemResult != AutoSaveIndex && (systemResult >= 0 && systemResult < dataIsAvailable.Length - 1))
			{
				page = (int)systemResult / 20;
				if (state.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
					state.SystemState = SystemStateCode.LoadGameOpenning_Begin;
				else
					state.SystemState = SystemStateCode.LoadGame_Begin;
				printSaveDataText();
				return;
			}
			bool available = false;
			if ((systemResult >= 0) && (systemResult < dataIsAvailable.Length - 1))
				available = dataIsAvailable[systemResult];
			else if (systemResult == AutoSaveIndex)
				available = dataIsAvailable[dataIsAvailable.Length - 1];
			else
			{//inputしなおし
				console.deleteLine(1);
				console.PrintTemporaryLine("無効なvalueです");
				console.updatedGeneration = true;
				setWaitInput();
				return;
			}
			if (!available)
			{
				console.PrintSingleLine(systemResult.ToString());
				console.PrintError("dataがdoes not exist");
				if (state.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
				{
					beginLoadGameOpening();
					return;
				}
				beginLoadGame();
				return;
			}

			if (!vEvaluator.LoadFrom((int)systemResult))
				throw new ExeEE("fileのloadduring 予期しnotErroroccurred");
			deletePrevState();
			beginDataLoaded();
		}


		void endNormal()
		{
			throw new CodeEE("予期しnotscript終端です");
		}

		void endReloaderb()
		{
			loadPrevState();
			console.ReloadErbFinished();
		}

		private bool writeSavedataTextFrom(int saveIndex)
		{
			EraDataResult result = vEvaluator.CheckData(saveIndex, EraSaveFileType.Normal);
			console.Print(result.DataMes);
			console.NewLine();
			return result.State == EraDataState.OK;
		}

		//1808 vEvaluator.SaveTo()etcにmove
		//private bool loadFrom(int dataIndex)
		//private bool saveTo(int saveIndex, string saveText)
		//private string getSaveDataPath(int index)
	}

}