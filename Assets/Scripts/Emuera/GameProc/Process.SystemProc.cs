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

		// FLOWINPUT / FLOWINPUTS state
		public long flowinputDef = 0;
		public bool flowinput = false;
		public bool flowinputCanSkip = false;
		public string flowinputDefString = "";
		public bool flowinputString = false;
		public bool flowinputForceSkip = false;
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
		//(value in Train.csv・-1 if not defined) == comAble[(displayed value)];
		int[] comAble;//


		private void runSystemProc()
		{
			//should not reach here during script execution
			//if (!state.ScriptEnd)
			//    throw new ExeEE("Invalid 呼び出し");

			//there is currently no processing that passes something that doesn't exist
			//if (systemProcessDictionary.ContainsKey(state.SystemState))
			systemProcessDictionary[state.SystemState]();
			//else
			//    throw new ExeEE("未定義の状態");//undefined state

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
					throw new CodeEE("関数\"@" + functionName + "\"が見つかりません");
			//since even a non-event function only provides a single function's worth, the condition cannot possibly be met
			//if ((!isEvent) && (call.Count > 1))
			//    throw new ExeEE("イベント関数でない関数\"@" + functionName + "\"の候補が複数ある");
			state.IntoFunction(call, null, null);
			return true;
		}

		//function group called from CheckState(). Processing when ScriptEnd is reached.

		void beginTitle()
		{
			//if a state was carried over during continuous training command processing, clear it here
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			console.ResetStyle();
			deleteAllPrevState();
			if (Program.AnalysisMode)
			{
				console.PrintSystemLine(GameMessages.FileAnalysisComplete);
				console.OutputLog(Program.ExeDir + "Analysis.log");
				console.noOutputLog = true;
				console.PrintSystemLine(GameMessages.PressEnterToExit);
				uEmuera.Media.SystemSounds.Asterisk.Play();
				console.ThrowTitleError(false);
				return;
			}
			if ((!noError) && (!Config.CompatiErrorLine))
			{
				console.PrintSystemLine(GameMessages.ErbCodeError);
				console.PrintSystemLine(GameMessages.CompatibilityOptionHint + "「" + Config.GetConfigName(ConfigCode.CompatiErrorLine) + "」");
				console.PrintSystemLine(GameMessages.OutputLogToFile);
				console.OutputLog(Program.ExeDir + "emuera.log");
				console.noOutputLog = true;
				console.PrintSystemLine(GameMessages.PressEnterToExit);
				//System.Media.SystemSounds.Asterisk.Play();
				console.ThrowTitleError(true);
				return;
			}
			if (callFunction("SYSTEM_TITLE", false, false))
			{//custom definition
				state.SystemState = SystemStateCode.Normal;
				return;
			}
			//standard title screen
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
			{//[0] start from the beginning
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
				{//custom definition
					state.SystemState = SystemStateCode.Openning_TitleLoadgame;
				}
				else
				{//standard LOADGAME
					beginLoadGameOpening();
				}
			}
			else//if input is invalid, rewrite the options again and require a valid choice.
			{//changed to do the same processing as RESTLASTLINE
				console.deleteLine(1);
				console.PrintTemporaryLine(GameMessages.InvalidValue);
				console.updatedGeneration = true;
				openingInput();
				//beginTitle();
			}

		}

		void beginFirst()
		{
			state.SystemState = SystemStateCode.Normal;
			//if a state was carried over during continuous training command processing, clear it here
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
			//call EVENTTRAIN and move to Train_CallEventTrain.
			if (!callFunction("EVENTTRAIN", false, true))
			{
				//if not existing, skip and treat as if Train_CallEventTrain finished.
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
			{//NEXTCOM processing
				state.SystemState = SystemStateCode.Train_CallEventCom;
				vEvaluator.SELECTCOM = vEvaluator.NEXTCOM;
				vEvaluator.NEXTCOM = 0;
				//assigns 0 instead of -1 so unless the ERB side changes it it will loop forever, but this is eramaker's spec.
				callEventCom();
				return;
			}
			else
			{
				//if (!isCTrain)
				//{
				//call SHOW_STATUS and move to Train_CallShowStatus.
				if (isCTrain)
					skipPrint = true;
				callFunction("SHOW_STATUS", true, false);
				state.SystemState = SystemStateCode.Train_CallShowStatus;
				//}
				//else
				//{
				//if in continuous training mode go to COMABLE processing
				//	endCallShowStatus();
				//}
			}
		}

		void endCallShowStatus()
		{
			//when SHOW_STATUS finishes, reset the ComAbleXX call state and move to Train_CallComAbleXX.
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
			//add option. if RESULT is 0, only increase the option number without adding.
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
			//ComAbleXX call. skip ones not defined in train.csv, treat as REUTRN 1 when ComAbleXX is not found.
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
			//when all are searched, finish and call SHOW_USERCOM.
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
				//set to numeric input wait state and move to Train_WaitInput.
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
				console.PrintSingleLine(string.Format(GameMessages.ContinuousCommandFormat, count, coms.Count));
			}
			//TrainName is defined and usable (COMABLE returned non-zero)
			if (selectCom >= 0)
			{
				vEvaluator.SELECTCOM = selectCom;
				callEventCom();
			}
			else
			{//not.
				if (isCTrain)
					console.PrintSingleLine(GameMessages.CommandExecutionFailed);
				vEvaluator.RESULT = systemResult;
				state.SystemState = SystemStateCode.Train_CallEventComEnd;
				callFunction("USERCOM", true, false);
				//all the necessary work during COM is done inside USERCOM.
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
			//execution failed
			if (vEvaluator.RESULT == 0)
			{
				//COM end.
				endCallEventComEnd();
			}
			else
			{//if successful, move to SOURCE_CHECK.
				state.SystemState = SystemStateCode.Train_CallSourceCheck;
				callFunction("SOURCE_CHECK", true, false);
			}
		}

		void endCallSourceCheck()
		{
			//SOURCE is reset here
			vEvaluator.UpdateAfterSourceCheck();
			//call EVENTCOMEND and move to Train_CallEventComEnd.
			state.SystemState = SystemStateCode.Train_CallEventComEnd;
			//if EVENTCOMEND does not exist, or a WAIT-family command is not performed inside EVENTCOMEND, add a WAIT after EVENTCOMEND.
			NeedWaitToEventComEnd = true;
			if (!callFunction("EVENTCOMEND", false, true))
			{
				//if not found, skip and treat as if Train_CallEventComEnd finished.
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
                    console.PrintTemporaryLine(GameMessages.InvalidValue);
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
				////1.701	WAIT was not needed here.
				////setWait();
				//1.703 it was needed after all in some cases
				if (NeedWaitToEventComEnd)
					setWait();
				NeedWaitToEventComEnd = false;
				//restart from SHOW_STATUS.
				//processing is the same as Train_CallEventTrain.
				endCallEventTrain();
			}
		}

		void beginAfterTrain()
		{
			//may reach here during continuous training mode, so cancel here
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			state.SystemState = SystemStateCode.Normal;
			//call EVENTEND. move to Normal since the exe side no longer needs to track the state.
			callFunction("EVENTEND", true, true);
		}

		void beginAblup()
		{
			//if a state was carried over during continuous training command processing, clear it here
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			state.SystemState = SystemStateCode.Ablup_CallShowJuel;
			//call SHOW_JUEL and move to Ablup_CallShowJuel.
			callFunction("SHOW_JUEL", true, false);
		}

		void endCallShowJuel()
		{
			state.SystemState = SystemStateCode.Ablup_CallShowAblupSelect;
			//call SHOW_ABLUP_SELECT and move to Ablup_CallAblupSelect.
			callFunction("SHOW_ABLUP_SELECT", true, false);
		}

		void endCallShowAblupSelect()
		{
			//enter numeric input wait state and move to Ablup_WaitInput.
			setWaitInput();
			state.SystemState = SystemStateCode.Ablup_WaitInput;
		}

		void ablupWaitInput()
		{
			//if not defined but < 100, ABLUP is called and USERABLUP is not called. otherwise things like [99] resistance brand mark wouldn't work.
			if ((systemResult >= 0) && (systemResult < 100))
			{
				state.SystemState = SystemStateCode.Ablup_CallAblupXX;
				string ablName = string.Format("ABLUP{0}", systemResult);
				if (!callFunction(ablName, false, false))
				{
					//if not found, end
					console.deleteLine(1);
					console.PrintTemporaryLine(GameMessages.InvalidValue);
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
                    console.PrintTemporaryLine(GameMessages.InvalidValue);
                }
				console.updatedGeneration = true;
				endCallShowAblupSelect();
			}
			else
				beginAblup();
		}

		void beginTurnend()
		{
			//if a state was carried over during continuous training command processing, clear it here
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			//call EVENTTURNEND and move to Normal
			callFunction("EVENTTURNEND", true, true);
			state.SystemState = SystemStateCode.Normal;
		}

		void beginShop()
		{
			//if a state was carried over during continuous training command processing, clear it here
			if (isCTrain)
				if (ClearCommands())
					return;
			skipPrint = false;
			state.SystemState = SystemStateCode.Shop_CallEventShop;
			//call EVENTSHOP and move to Shop_CallEventShop.
			if (!callFunction("EVENTSHOP", false, true))
			{
				//if not present, skip and treat as if Shop_CallEventShop finished.
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
			{//use @SYSTEM_AUTOSAVE if it exists.
				state.SystemState = SystemStateCode.AutoSave_CallUniqueAutosave;
				return;
			}
			saveTarget = AutoSaveIndex;
			vEvaluator.SAVEDATA_TEXT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ";
			state.SystemState = SystemStateCode.AutoSave_CallSaveInfo;
			if (!callFunction("SAVEINFO", false, false))
				endAutoSaveCallSaveInfo();//skip if not exists
		}

		void endAutoSaveCallSaveInfo()
		{
			if (saveTarget == AutoSaveIndex)
			{
				if (!vEvaluator.SaveTo(saveTarget, vEvaluator.SAVEDATA_TEXT))
				{
					console.PrintError(GameMessages.AutoSaveError);
					console.PrintError(GameMessages.AutoSaveSkipped);
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
			//call SHOW_SHOP and move to Shop_CallShowShop
			callFunction("SHOW_SHOP", true, false);
		}

		void endCallShowShop()
		{
			//enter numeric input wait state and move to Shop_WaitInput.
			setWaitInput();
			state.SystemState = SystemStateCode.Shop_WaitInput;
		}

		//independent of PRINT_SHOPITEM.
		//even if there is an item with BOUGHT >= 100 and ITEMSALES is TRUE, forced to go to @USERSHOP.
		void shopWaitInput()
		{
			if ((systemResult >= 0) && (systemResult < Config.MaxShopItem))
			{
				if (vEvaluator.ItemSales(systemResult))
				{
					if (vEvaluator.BuyItem(systemResult))
					{
						state.SystemState = SystemStateCode.Shop_CallEventBuy;
						//call EVENTBUY and move to Shop_CallEventBuy
						if (!callFunction("EVENTBUY", false, true))
							endCallEventBuy();
						return;
					}
					else
					{
						//console.Print("お金が足りません。");
						//console.NewLine();
						console.deleteLine(1);
						console.PrintTemporaryLine(GameMessages.NotEnoughMoney);
					}
				}
				else
				{
					//console.Print("売っていません。");
					//console.NewLine();
					console.deleteLine(1);
					console.PrintTemporaryLine(GameMessages.NotForSale);
				}
				//if purchase failed, return to endCallEventShop().
				//endCallEventShop();
				endCallShowShop();
				return;
			}
			else
			{
				//update RESULT
				vEvaluator.RESULT = systemResult;

				//call USERSHOP and move to Shop_CallEventBuy
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
                    console.PrintTemporaryLine(GameMessages.InvalidValue);
                }
				console.updatedGeneration = true;
				endCallShowShop();
			}
			else
			{
				//return to the beginning
				endAutoSave();
			}
		}


		void beginDataLoaded()
		{
			state.SystemState = SystemStateCode.LoadData_CallSystemLoad;
			
			if (!callFunction("SYSTEM_LOADEND", false, false))
				endSystemLoad();//skip if not exists
		}
		void endSystemLoad()
		{
			state.SystemState = SystemStateCode.LoadData_CallEventLoad;
			//call EVENTLOAD and move to LoadData_CallEventLoad.
			if (!callFunction("EVENTLOAD", false, true))
			{
				//if not existing, skip and treat as if LoadData_CallEventLoad finished.
				endAutoSave();
			}
		}

		void endEventLoad()
		{
			//if BEGIN command is done during @EVENTLOAD, won't reach here.
			//if reached here, treat as BEGIN SHOP. no autosave.
			endAutoSave();
		}

		void beginSaveGame()
		{
			console.PrintSingleLine(GameMessages.WhichNumberToSave);
			state.SystemState = SystemStateCode.SaveGame_Begin;
			printSaveDataText();
		}

		void beginLoadGame()
		{
			console.PrintSingleLine(GameMessages.WhichNumberToLoad);
			state.SystemState = SystemStateCode.LoadGame_Begin;
			printSaveDataText();
		}

		void beginLoadGameOpening()
		{
			console.PrintSingleLine(GameMessages.WhichNumberToLoad);
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
				console.Print(string.Format("[{0, 2}] " + GameMessages.ShowSaveData, i * 20, i * 20 + 19));
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
				console.Print(string.Format("[{0, 2}] " + GameMessages.ShowSaveData, (i + 1) * 20, (i + 1) * 20 + 19));
			}
			//autosave processing is cut out separately (because of display processing)
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
			//all drawing finished
			console.PrintSingleLine("[100] " + GameMessages.Back);
			setWaitInput();
			if (state.SystemState == SystemStateCode.SaveGame_Begin)
				state.SystemState = SystemStateCode.SaveGame_WaitInput;
			else if (state.SystemState == SystemStateCode.LoadGame_Begin)
				state.SystemState = SystemStateCode.LoadGame_WaitInput;
			else// if (state.SystemState == SystemStateCode.LoadGameOpenning_Begin)
				state.SystemState = SystemStateCode.LoadGameOpenning_WaitInput;
			//properly processed so never reach here
			//else
			//    throw new ExeEE("異常な状態");
		}

		int saveTarget = -1;
		void saveGameWaitInput()
		{
			if (systemResult == 100)
			{
				//if cancel, restore the previous state
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
			{//input again
				console.deleteLine(1);
				console.PrintTemporaryLine(GameMessages.InvalidValue);
				console.updatedGeneration = true;
				setWaitInput();
				return;
			}
			saveTarget = (int)systemResult;
			//if existing data, display options and move to SaveGame_WaitInputOverwrite.
			if (available)
			{
				console.PrintSingleLine(GameMessages.DataExistsOverwrite);
				console.PrintC("[0] " + GameMessages.Yes, false);
				console.PrintC("[1] " + GameMessages.No, false);
				setWaitInput();
				state.SystemState = SystemStateCode.SaveGame_WaitInputOverwrite;
				return;
			}
			//if no existing data, treat as if "yes" was chosen and jump directly
			systemResult = 0;
			saveGameWaitInputOverwrite();
		}

		void saveGameWaitInputOverwrite()
		{
			if (systemResult == 1)//no
			{
				beginSaveGame();
				return;
			}
			else if (systemResult != 0)//not "yes" either
			{//input again
				console.deleteLine(1);
				console.PrintTemporaryLine(GameMessages.InvalidValue);
				console.updatedGeneration = true;
				setWaitInput();
				return;
			}
			vEvaluator.SAVEDATA_TEXT = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " ";
			state.SystemState = SystemStateCode.SaveGame_CallSaveInfo;
			if (!callFunction("SAVEINFO", false, false))
				endCallSaveInfo();//skip if not exists
		}

		void endCallSaveInfo()
		{
			if (!vEvaluator.SaveTo(saveTarget, vEvaluator.SAVEDATA_TEXT))
			{
				console.PrintError(GameMessages.SaveError);
				console.ReadAnyKey();
			}
			loadPrevState();
		}

		void loadGameWaitInput()
		{
			if (systemResult == 100)
			{//if cancel
				//if opening, return to opening
				if (state.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
				{
					beginTitle();
					return;
				}
				//if from something else, restore the previous state
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
			{//input again
				console.deleteLine(1);
				console.PrintTemporaryLine(GameMessages.InvalidValue);
				console.updatedGeneration = true;
				setWaitInput();
				return;
			}
			if (!available)
			{
				console.PrintSingleLine(systemResult.ToString());
				console.PrintError(GameMessages.NoData);
				if (state.SystemState == SystemStateCode.LoadGameOpenning_WaitInput)
				{
					beginLoadGameOpening();
					return;
				}
				beginLoadGame();
				return;
			}

			if (!vEvaluator.LoadFrom((int)systemResult))
				throw new ExeEE("ファイルのロード中に予期しないErrorが発生しました");
			deletePrevState();
			beginDataLoaded();
		}


		void endNormal()
		{
			throw new CodeEE("予期しないスクリプト終端です");
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

		//1808 moved to vEvaluator.SaveTo() etc.
		//private bool loadFrom(int dataIndex)
		//private bool saveTo(int saveIndex, string saveText)
		//private string getSaveDataPath(int index)
	}

}