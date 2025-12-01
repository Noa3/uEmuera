using System.Collections.Generic;
using MinorShift.Emuera.GameData.Function;

namespace MinorShift.Emuera.GameProc.Function
{
	/// <summary>
	/// FunctionCodeのラッパー
	/// BuiltInFunctionManagerをoriginalにclass化
	/// </summary>
	internal sealed partial class FunctionIdentifier
	{
		#region flagdefinition
		//いちいちFunctionFlag.FLOW_CONTROLとか書くのが面倒だったfromenumではなくconst intで
		public const int FLOW_CONTROL = 0x00001;
		public const int EXTENDED = 0x00002;//Emuera拡張命令
		public const int METHOD_SAFE = 0x00004;//#Functioninsideでcallてよい命令.WAITetcinputを伴うthing,CALLetcfunctioncallを伴うthingは不可.
		public const int DEBUG_FUNC = 0x00008;//-Debugargument付きで起動したcaseにのみ実linebe done命令.
		public const int PARTIAL = 0x00010;//複数lineに渡る構文のpartでexist命令.SIF文の次に来てはいけnot.debugcommandfromのcallも不適当.
		public const int FORCE_SETARG = 0x00020;//loadwhenに必ずargumentparseをlineう必要のexistthing.IFやSELECTCASEetc
		public const int IS_JUMP = 0x00040;//JUMP命令
		public const int IS_TRY = 0x00080;//TRY系命令
		public const int IS_TRYC = 0x08000;//TRY系命令

		public const int PRINT_NEWLINE = 0x00100;//PRINT命令でoutputafter改linedothing
		public const int PRINT_WAITINPUT = 0x00200;//PRINT命令でoutputafterinput待ちdothing
		public const int PRINT_SINGLE = 0x00400;//PRINTSINGLE系
		public const int ISPRINTDFUNC = 0x00800;//PRINTD系判定for
		public const int ISPRINTKFUNC = 0x01000;//PRINTK系判定for

		public const int IS_PRINT = 0x02000;//SkipPrintにthan飛ばbe done命令.PRINT系及びWAIT系.DEBUGPRINTは含まnot.
		public const int IS_INPUT = 0x04000;//userDefinedSkipをwarningdo命令.INPUT系.
		public const int IS_PRINTDATA = 0x10000;//PRINTDATA系判定for,PRINTとはpartprocessがdifferentので,それfor.

		#endregion

		#region static
		//originalBuiltInFunctionManager部分
		readonly static Dictionary<string, FunctionIdentifier> funcDic = new Dictionary<string, FunctionIdentifier>();
		readonly static Dictionary<FunctionCode, string> funcMatch = new Dictionary<FunctionCode, string>();
		readonly static Dictionary<FunctionCode, FunctionCode> funcParent = new Dictionary<FunctionCode, FunctionCode>();
		readonly static ArgumentBuilder methodArgumentBuilder = null;
		readonly static AbstractInstruction methodInstruction = null;

		private static void addFunction(FunctionCode code, AbstractInstruction inst)
		{ addFunction(code, inst, 0); }

		private static void addFunction(FunctionCode code, AbstractInstruction inst, int additionalFlag)
		{
			string key = code.ToString();
			if (Config.ICFunction)
				key = key.ToUpper();
			funcDic.Add(key, new FunctionIdentifier(key, code, inst, additionalFlag));
		}

		private static void addFunction(FunctionCode code, ArgumentBuilder arg)
		{ addFunction(code, arg, 0); }

		private static void addFunction(FunctionCode code, ArgumentBuilder arg, int flag)
		{
			string key = code.ToString();
			if (Config.ICFunction)
				key = key.ToUpper();
			funcDic.Add(key, new FunctionIdentifier(key, code, arg, flag));
		}

		public static Dictionary<string, FunctionIdentifier> GetInstructionNameDic()
		{
			return funcDic;
		}
		private static void addPrintFunction(FunctionCode code)
		{
			addFunction(code, new PRINT_Instruction(code.ToString()));
		}
		private static void addPrintDataFunction(FunctionCode code)
		{
			addFunction(code, new PRINT_DATA_Instruction(code.ToString()));
		}
		static FunctionIdentifier()
		{
			Dictionary<FunctionArgType, ArgumentBuilder> argb = ArgumentParser.GetArgumentBuilderDictionary();
			methodArgumentBuilder = argb[FunctionArgType.METHOD];
			methodInstruction = new METHOD_Instruction();
			setFunc = new FunctionIdentifier("SET", FunctionCode.SET, new SET_Instruction());//代入文
			#region PRINT or INPUT
			addPrintFunction(FunctionCode.PRINT);
			addPrintFunction(FunctionCode.PRINTL);
			addPrintFunction(FunctionCode.PRINTW);
			addPrintFunction(FunctionCode.PRINTV);
			addPrintFunction(FunctionCode.PRINTVL);
			addPrintFunction(FunctionCode.PRINTVW);
			addPrintFunction(FunctionCode.PRINTS);
			addPrintFunction(FunctionCode.PRINTSL);
			addPrintFunction(FunctionCode.PRINTSW);
			addPrintFunction(FunctionCode.PRINTFORM);
			addPrintFunction(FunctionCode.PRINTFORML);
			addPrintFunction(FunctionCode.PRINTFORMW);
			addPrintFunction(FunctionCode.PRINTFORMS);
			addPrintFunction(FunctionCode.PRINTFORMSL);
			addPrintFunction(FunctionCode.PRINTFORMSW);
			addPrintFunction(FunctionCode.PRINTK);
			addPrintFunction(FunctionCode.PRINTKL);
			addPrintFunction(FunctionCode.PRINTKW);
			addPrintFunction(FunctionCode.PRINTVK);
			addPrintFunction(FunctionCode.PRINTVKL);
			addPrintFunction(FunctionCode.PRINTVKW);
			addPrintFunction(FunctionCode.PRINTSK);
			addPrintFunction(FunctionCode.PRINTSKL);
			addPrintFunction(FunctionCode.PRINTSKW);
			addPrintFunction(FunctionCode.PRINTFORMK);
			addPrintFunction(FunctionCode.PRINTFORMKL);
			addPrintFunction(FunctionCode.PRINTFORMKW);
			addPrintFunction(FunctionCode.PRINTFORMSK);
			addPrintFunction(FunctionCode.PRINTFORMSKL);
			addPrintFunction(FunctionCode.PRINTFORMSKW);
			addPrintFunction(FunctionCode.PRINTD);
			addPrintFunction(FunctionCode.PRINTDL);
			addPrintFunction(FunctionCode.PRINTDW);
			addPrintFunction(FunctionCode.PRINTVD);
			addPrintFunction(FunctionCode.PRINTVDL);
			addPrintFunction(FunctionCode.PRINTVDW);
			addPrintFunction(FunctionCode.PRINTSD);
			addPrintFunction(FunctionCode.PRINTSDL);
			addPrintFunction(FunctionCode.PRINTSDW);
			addPrintFunction(FunctionCode.PRINTFORMD);
			addPrintFunction(FunctionCode.PRINTFORMDL);
			addPrintFunction(FunctionCode.PRINTFORMDW);
			addPrintFunction(FunctionCode.PRINTFORMSD);
			addPrintFunction(FunctionCode.PRINTFORMSDL);
			addPrintFunction(FunctionCode.PRINTFORMSDW);
			addPrintFunction(FunctionCode.PRINTSINGLE);
			addPrintFunction(FunctionCode.PRINTSINGLEV);
			addPrintFunction(FunctionCode.PRINTSINGLES);
			addPrintFunction(FunctionCode.PRINTSINGLEFORM);
			addPrintFunction(FunctionCode.PRINTSINGLEFORMS);
			addPrintFunction(FunctionCode.PRINTSINGLEK);
			addPrintFunction(FunctionCode.PRINTSINGLEVK);
			addPrintFunction(FunctionCode.PRINTSINGLESK);
			addPrintFunction(FunctionCode.PRINTSINGLEFORMK);
			addPrintFunction(FunctionCode.PRINTSINGLEFORMSK);
			addPrintFunction(FunctionCode.PRINTSINGLED);
			addPrintFunction(FunctionCode.PRINTSINGLEVD);
			addPrintFunction(FunctionCode.PRINTSINGLESD);
			addPrintFunction(FunctionCode.PRINTSINGLEFORMD);
			addPrintFunction(FunctionCode.PRINTSINGLEFORMSD);

			addPrintFunction(FunctionCode.PRINTC);
			addPrintFunction(FunctionCode.PRINTLC);
			addPrintFunction(FunctionCode.PRINTFORMC);
			addPrintFunction(FunctionCode.PRINTFORMLC);
			addPrintFunction(FunctionCode.PRINTCK);
			addPrintFunction(FunctionCode.PRINTLCK);
			addPrintFunction(FunctionCode.PRINTFORMCK);
			addPrintFunction(FunctionCode.PRINTFORMLCK);
			addPrintFunction(FunctionCode.PRINTCD);
			addPrintFunction(FunctionCode.PRINTLCD);
			addPrintFunction(FunctionCode.PRINTFORMCD);
			addPrintFunction(FunctionCode.PRINTFORMLCD);
			addPrintDataFunction(FunctionCode.PRINTDATA);
			addPrintDataFunction(FunctionCode.PRINTDATAL);
			addPrintDataFunction(FunctionCode.PRINTDATAW);
			addPrintDataFunction(FunctionCode.PRINTDATAK);
			addPrintDataFunction(FunctionCode.PRINTDATAKL);
			addPrintDataFunction(FunctionCode.PRINTDATAKW);
			addPrintDataFunction(FunctionCode.PRINTDATAD);
			addPrintDataFunction(FunctionCode.PRINTDATADL);
			addPrintDataFunction(FunctionCode.PRINTDATADW);


			addFunction(FunctionCode.PRINTBUTTON, argb[FunctionArgType.SP_BUTTON], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.PRINTBUTTONC, argb[FunctionArgType.SP_BUTTON], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.PRINTBUTTONLC, argb[FunctionArgType.SP_BUTTON], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.PRINTPLAIN, argb[FunctionArgType.STR_NULLABLE], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.PRINTPLAINFORM, argb[FunctionArgType.FORM_STR_NULLABLE], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.PRINT_ABL, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE);//能力.argumentは登録番号
			addFunction(FunctionCode.PRINT_TALENT, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE);//素質
			addFunction(FunctionCode.PRINT_MARK, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE);//刻印
			addFunction(FunctionCode.PRINT_EXP, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE);//経験
			addFunction(FunctionCode.PRINT_PALAM, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE);//パラメータ
			addFunction(FunctionCode.PRINT_ITEM, argb[FunctionArgType.VOID], METHOD_SAFE);//所持アイテム
			addFunction(FunctionCode.PRINT_SHOPITEM, argb[FunctionArgType.VOID], METHOD_SAFE);//ショップで売っているアイテム

			addFunction(FunctionCode.DRAWLINE, argb[FunctionArgType.VOID], METHOD_SAFE);//画面の左端from右端until----と線を引く.
			addFunction(FunctionCode.BAR, new BAR_Instruction(false));//[*****....]のようなグラフを書く.BAR (variable) , (最大value), (長さ)
			addFunction(FunctionCode.BARL, new BAR_Instruction(true));//改line付き.
			addFunction(FunctionCode.TIMES, new TIMES_Instruction());//小数calculate.TIMES (variable) , (小numeric)called 形で使う.

			addFunction(FunctionCode.WAIT, new WAIT_Instruction(false));
			addFunction(FunctionCode.INPUT, new INPUT_Instruction());
			addFunction(FunctionCode.INPUTS, new INPUTS_Instruction());
			addFunction(FunctionCode.TINPUT, new TINPUT_Instruction(false));
			addFunction(FunctionCode.TINPUTS, new TINPUTS_Instruction(false));
			addFunction(FunctionCode.TONEINPUT, new TINPUT_Instruction(true));
			addFunction(FunctionCode.TONEINPUTS, new TINPUTS_Instruction(true));
			addFunction(FunctionCode.TWAIT, new TWAIT_Instruction());
			addFunction(FunctionCode.WAITANYKEY, new WAITANYKEY_Instruction());
			addFunction(FunctionCode.FORCEWAIT, new WAIT_Instruction(true));
			addFunction(FunctionCode.ONEINPUT, new ONEINPUT_Instruction());
			addFunction(FunctionCode.ONEINPUTS, new ONEINPUTS_Instruction());
			addFunction(FunctionCode.CLEARLINE, new CLEARLINE_Instruction());
			addFunction(FunctionCode.REUSELASTLINE, new REUSELASTLINE_Instruction());

			#endregion
			addFunction(FunctionCode.UPCHECK, argb[FunctionArgType.VOID], METHOD_SAFE);//パラメータの変動
			addFunction(FunctionCode.CUPCHECK, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.ADDCHARA, new ADDCHARA_Instruction(false, false));//(キャラ番号)のcharacterをadd
			addFunction(FunctionCode.ADDSPCHARA, new ADDCHARA_Instruction(true, false));//(キャラ番号)のSPcharacterをadd(フラグ0を1にしてcreate)
			addFunction(FunctionCode.ADDDEFCHARA, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.ADDVOIDCHARA, new ADDVOIDCHARA_Instruction());//variableに何のsettingのnotキャラをcreate
			addFunction(FunctionCode.DELCHARA, new ADDCHARA_Instruction(false, true));//(キャラ登録番号)のcharacterをdelete.

			addFunction(FunctionCode.PUTFORM, argb[FunctionArgType.FORM_STR_NULLABLE], METHOD_SAFE);//@SAVEINFOfunctionでのみusepossible.PRINTFORMと同様の書式でsavedataに概要をつける.
			addFunction(FunctionCode.QUIT, argb[FunctionArgType.VOID]);//ゲームをend
			addFunction(FunctionCode.OUTPUTLOG, argb[FunctionArgType.VOID]);

			addFunction(FunctionCode.BEGIN, new BEGIN_Instruction());//systemfunctionの実line.実linedoとCALLのcalloriginaletcを忘れてしまう.

			addFunction(FunctionCode.SAVEGAME, new SAVELOADGAME_Instruction(true));//save画面を呼ぶ.ショップのみ.
			addFunction(FunctionCode.LOADGAME, new SAVELOADGAME_Instruction(false));//
			addFunction(FunctionCode.SAVEDATA, argb[FunctionArgType.SP_SAVEDATA], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.LOADDATA, argb[FunctionArgType.INT_EXPRESSION], EXTENDED | FLOW_CONTROL);
			addFunction(FunctionCode.DELDATA, new DELDATA_Instruction());
			addFunction(FunctionCode.SAVEGLOBAL, new SAVEGLOBAL_Instruction());
			addFunction(FunctionCode.LOADGLOBAL, new LOADGLOBAL_Instruction());
			addFunction(FunctionCode.RESETDATA, new RESETDATA_Instruction());
			addFunction(FunctionCode.RESETGLOBAL, new RESETGLOBAL_Instruction());

			addFunction(FunctionCode.SIF, new SIF_Instruction());//一lineのみIF
			addFunction(FunctionCode.IF, new IF_Instruction());
			addFunction(FunctionCode.ELSE, new ELSEIF_Instruction(FunctionArgType.VOID));
			addFunction(FunctionCode.ELSEIF, new ELSEIF_Instruction(FunctionArgType.INT_EXPRESSION));
			addFunction(FunctionCode.ENDIF, new ENDIF_Instruction(), METHOD_SAFE);
			addFunction(FunctionCode.SELECTCASE, new SELECTCASE_Instruction());
			addFunction(FunctionCode.CASE, new ELSEIF_Instruction(FunctionArgType.CASE), EXTENDED);
			addFunction(FunctionCode.CASEELSE, new ELSEIF_Instruction(FunctionArgType.VOID), EXTENDED);
			addFunction(FunctionCode.ENDSELECT, new ENDIF_Instruction(), METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.REPEAT, new REPEAT_Instruction(false));//RENDuntil繰り返し.繰り返した回数がCOUNTto.ネスト不可.
			addFunction(FunctionCode.REND, new REND_Instruction());
			addFunction(FunctionCode.FOR, new REPEAT_Instruction(true), EXTENDED);
			addFunction(FunctionCode.NEXT, new REND_Instruction(), EXTENDED);
			addFunction(FunctionCode.WHILE, new WHILE_Instruction());
			addFunction(FunctionCode.WEND, new WEND_Instruction());
			addFunction(FunctionCode.DO, new ENDIF_Instruction(), METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.LOOP, new LOOP_Instruction());
			addFunction(FunctionCode.CONTINUE, new CONTINUE_Instruction());//REPEATに戻る
			addFunction(FunctionCode.BREAK, new BREAK_Instruction());//RENDのnext lineuntil

			addFunction(FunctionCode.RETURN, new RETURN_Instruction());//functionのend.RESULTにintegerを格納possible.省略したcase,０.(next @～～がRETURNと見なbe done.)  
			addFunction(FunctionCode.RETURNFORM, new RETURNFORM_Instruction());//functionのend.RESULTにintegerを格納possible.省略したcase,０.(next @～～がRETURNと見なbe done.)  
			addFunction(FunctionCode.RETURNF, new RETURNF_Instruction());

			addFunction(FunctionCode.STRLEN, new STRLEN_Instruction(false, false));
			addFunction(FunctionCode.STRLENFORM, new STRLEN_Instruction(true, false));
			addFunction(FunctionCode.STRLENU, new STRLEN_Instruction(false, true));
			addFunction(FunctionCode.STRLENFORMU, new STRLEN_Instruction(true, true));

			addFunction(FunctionCode.SWAPCHARA, new SWAPCHARA_Instruction());
			addFunction(FunctionCode.COPYCHARA, new COPYCHARA_Instruction());
			addFunction(FunctionCode.ADDCOPYCHARA, new ADDCOPYCHARA_Instruction());
			addFunction(FunctionCode.SPLIT, argb[FunctionArgType.SP_SPLIT], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.SETCOLOR, argb[FunctionArgType.SP_COLOR], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.SETCOLORBYNAME, argb[FunctionArgType.STR], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.RESETCOLOR, new RESETCOLOR_Instruction());
			addFunction(FunctionCode.SETBGCOLOR, argb[FunctionArgType.SP_COLOR], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.SETBGCOLORBYNAME, argb[FunctionArgType.STR], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.RESETBGCOLOR, new RESETBGCOLOR_Instruction());
			addFunction(FunctionCode.FONTBOLD, new FONTBOLD_Instruction());
			addFunction(FunctionCode.FONTITALIC, new FONTITALIC_Instruction());
			addFunction(FunctionCode.FONTREGULAR, new FONTREGULAR_Instruction());
			addFunction(FunctionCode.SORTCHARA, new SORTCHARA_Instruction());
			addFunction(FunctionCode.FONTSTYLE, argb[FunctionArgType.INT_EXPRESSION_NULLABLE], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.ALIGNMENT, argb[FunctionArgType.STR], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.CUSTOMDRAWLINE, argb[FunctionArgType.STR], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.DRAWLINEFORM, argb[FunctionArgType.FORM_STR], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.CLEARTEXTBOX, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.SETFONT, argb[FunctionArgType.STR_EXPRESSION_NULLABLE], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.SWAP, argb[FunctionArgType.SP_SWAPVAR], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.RANDOMIZE, new RANDOMIZE_Instruction());
			addFunction(FunctionCode.DUMPRAND, new DUMPRAND_Instruction());
			addFunction(FunctionCode.INITRAND, new INITRAND_Instruction());

			addFunction(FunctionCode.REDRAW, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.CALLTRAIN, argb[FunctionArgType.INT_EXPRESSION], EXTENDED | FLOW_CONTROL);
			addFunction(FunctionCode.STOPCALLTRAIN, argb[FunctionArgType.VOID], EXTENDED | FLOW_CONTROL);
			addFunction(FunctionCode.DOTRAIN, argb[FunctionArgType.INT_EXPRESSION], EXTENDED | FLOW_CONTROL);

			addFunction(FunctionCode.DATA, argb[FunctionArgType.STR_NULLABLE], METHOD_SAFE | EXTENDED | PARTIAL | PARTIAL);
			addFunction(FunctionCode.DATAFORM, argb[FunctionArgType.FORM_STR_NULLABLE], METHOD_SAFE | EXTENDED | PARTIAL);
			addFunction(FunctionCode.ENDDATA, new DO_NOTHING_Instruction());
			addFunction(FunctionCode.DATALIST, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED | PARTIAL);
			addFunction(FunctionCode.ENDLIST, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED | PARTIAL);
			addFunction(FunctionCode.STRDATA, argb[FunctionArgType.VAR_STR], METHOD_SAFE | EXTENDED | PARTIAL);

			addFunction(FunctionCode.SETBIT, new SETBIT_Instruction(1));
			addFunction(FunctionCode.CLEARBIT, new SETBIT_Instruction(0));
			addFunction(FunctionCode.INVERTBIT, new SETBIT_Instruction(-1));
			addFunction(FunctionCode.DELALLCHARA, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.PICKUPCHARA, argb[FunctionArgType.INT_ANY], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.VARSET, new VARSET_Instruction());
			addFunction(FunctionCode.CVARSET, new CVARSET_Instruction());

			addFunction(FunctionCode.RESET_STAIN, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.FORCEKANA, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.SKIPDISP, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.NOSKIP, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED | PARTIAL);
			addFunction(FunctionCode.ENDNOSKIP, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED | PARTIAL);

			addFunction(FunctionCode.ARRAYSHIFT, argb[FunctionArgType.SP_SHIFT_ARRAY], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.ARRAYREMOVE, argb[FunctionArgType.SP_CONTROL_ARRAY], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.ARRAYSORT, argb[FunctionArgType.SP_SORTARRAY], METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.ARRAYCOPY, argb[FunctionArgType.SP_COPY_ARRAY], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.JUMP, new CALL_Instruction(false, true, false, false));//functionにmove
			addFunction(FunctionCode.CALL, new CALL_Instruction(false, false, false, false));//functionにmove.moveoriginalを記憶し,RETURNで帰る.
			addFunction(FunctionCode.TRYJUMP, new CALL_Instruction(false, true, true, false), EXTENDED);
			addFunction(FunctionCode.TRYCALL, new CALL_Instruction(false, false, true, false), EXTENDED);
			addFunction(FunctionCode.JUMPFORM, new CALL_Instruction(true, true, false, false), EXTENDED);
			addFunction(FunctionCode.CALLFORM, new CALL_Instruction(true, false, false, false), EXTENDED);
			addFunction(FunctionCode.TRYJUMPFORM, new CALL_Instruction(true, true, true, false), EXTENDED);
			addFunction(FunctionCode.TRYCALLFORM, new CALL_Instruction(true, false, true, false), EXTENDED);
			addFunction(FunctionCode.TRYCJUMP, new CALL_Instruction(false, true, true, true), EXTENDED);
			addFunction(FunctionCode.TRYCCALL, new CALL_Instruction(false, false, true, true), EXTENDED);
			addFunction(FunctionCode.TRYCJUMPFORM, new CALL_Instruction(true, true, true, true), EXTENDED);
			addFunction(FunctionCode.TRYCCALLFORM, new CALL_Instruction(true, false, true, true), EXTENDED);
			addFunction(FunctionCode.CALLEVENT, new CALLEVENT_Instruction());
			addFunction(FunctionCode.CALLF, new CALLF_Instruction(false));
			addFunction(FunctionCode.CALLFORMF, new CALLF_Instruction(true));
			addFunction(FunctionCode.RESTART, new RESTART_Instruction());//functionの再開.functionの最初に戻る.
			addFunction(FunctionCode.GOTO, new GOTO_Instruction(false, false, false));//$ラベルtoジャンプ
			addFunction(FunctionCode.TRYGOTO, new GOTO_Instruction(false, true, false), EXTENDED);
			addFunction(FunctionCode.GOTOFORM, new GOTO_Instruction(true, false, false), EXTENDED);
			addFunction(FunctionCode.TRYGOTOFORM, new GOTO_Instruction(true, true, false), EXTENDED);
			addFunction(FunctionCode.TRYCGOTO, new GOTO_Instruction(false, true, true), EXTENDED);
			addFunction(FunctionCode.TRYCGOTOFORM, new GOTO_Instruction(true, true, true), EXTENDED);


			addFunction(FunctionCode.CATCH, new CATCH_Instruction());
			addFunction(FunctionCode.ENDCATCH, new ENDIF_Instruction(), METHOD_SAFE | EXTENDED);
			addFunction(FunctionCode.TRYCALLLIST, argb[FunctionArgType.VOID], EXTENDED | FLOW_CONTROL | PARTIAL | IS_TRY);
			addFunction(FunctionCode.TRYJUMPLIST, argb[FunctionArgType.VOID], EXTENDED | FLOW_CONTROL | PARTIAL | IS_JUMP | IS_TRY);
			addFunction(FunctionCode.TRYGOTOLIST, argb[FunctionArgType.VOID], EXTENDED | FLOW_CONTROL | PARTIAL | IS_TRY);
			addFunction(FunctionCode.FUNC, argb[FunctionArgType.SP_CALLFORM], EXTENDED | FLOW_CONTROL | PARTIAL | FORCE_SETARG);
			addFunction(FunctionCode.ENDFUNC, new ENDIF_Instruction(), EXTENDED);

			addFunction(FunctionCode.DEBUGPRINT, new DEBUGPRINT_Instruction(false, false));
			addFunction(FunctionCode.DEBUGPRINTL, new DEBUGPRINT_Instruction(false, true));
			addFunction(FunctionCode.DEBUGPRINTFORM, new DEBUGPRINT_Instruction(true, false));
			addFunction(FunctionCode.DEBUGPRINTFORML, new DEBUGPRINT_Instruction(true, true));
			addFunction(FunctionCode.DEBUGCLEAR, new DEBUGCLEAR_Instruction());
			addFunction(FunctionCode.ASSERT, argb[FunctionArgType.INT_EXPRESSION], METHOD_SAFE | EXTENDED | DEBUG_FUNC);
			addFunction(FunctionCode.THROW, argb[FunctionArgType.FORM_STR_NULLABLE], METHOD_SAFE | EXTENDED);

			addFunction(FunctionCode.SAVEVAR, new SAVEVAR_Instruction());
			addFunction(FunctionCode.LOADVAR, new LOADVAR_Instruction());
			addFunction(FunctionCode.SAVECHARA, new SAVECHARA_Instruction());
			addFunction(FunctionCode.LOADCHARA, new LOADCHARA_Instruction());
			addFunction(FunctionCode.REF, new REF_Instruction(false));
			addFunction(FunctionCode.REFBYNAME, new REF_Instruction(true));
			addFunction(FunctionCode.HTML_PRINT, new HTML_PRINT_Instruction());
			addFunction(FunctionCode.HTML_TAGSPLIT, new HTML_TAGSPLIT_Instruction());
			addFunction(FunctionCode.PRINT_IMG, new PRINT_IMG_Instruction());
			addFunction(FunctionCode.PRINT_RECT, new PRINT_RECT_Instruction());
			addFunction(FunctionCode.PRINT_SPACE, new PRINT_SPACE_Instruction());
			
			addFunction(FunctionCode.TOOLTIP_SETCOLOR, new TOOLTIP_SETCOLOR_Instruction());
			addFunction(FunctionCode.TOOLTIP_SETDELAY, new TOOLTIP_SETDELAY_Instruction());
            addFunction(FunctionCode.TOOLTIP_SETDURATION, new TOOLTIP_SETDURATION_Instruction());

			addFunction(FunctionCode.INPUTMOUSEKEY, new INPUTMOUSEKEY_Instruction());
			addFunction(FunctionCode.AWAIT, new AWAIT_Instruction());
			#region 式insidefunctionのargument違い
			addFunction(FunctionCode.VARSIZE, argb[FunctionArgType.SP_VAR], METHOD_SAFE | EXTENDED);//動作がdifferentのでMETHOD化できnot
			addFunction(FunctionCode.GETTIME, argb[FunctionArgType.VOID], METHOD_SAFE | EXTENDED);//2つに代入do必要がexistのでMETHOD化できnot
			addFunction(FunctionCode.POWER, argb[FunctionArgType.SP_POWER], METHOD_SAFE | EXTENDED);//argumentがdifferentのでMETHOD化できnot.
			addFunction(FunctionCode.PRINTCPERLINE, argb[FunctionArgType.SP_GETINT], METHOD_SAFE | EXTENDED);//よく考えたらargumentの仕様differentや
			addFunction(FunctionCode.SAVENOS, argb[FunctionArgType.SP_GETINT], METHOD_SAFE | EXTENDED);//argumentの仕様がdifferentので(ry
			addFunction(FunctionCode.ENCODETOUNI, argb[FunctionArgType.FORM_STR_NULLABLE], METHOD_SAFE | EXTENDED);//式insidefunction版をadd.processが全然different
			#endregion

			Dictionary<string, FunctionMethod> methodList = FunctionMethodCreator.GetMethodList();
			foreach (KeyValuePair<string, FunctionMethod> pair in methodList)
			{
				string key = pair.Key;
				if (!funcDic.ContainsKey(key))
				{
					funcDic.Add(key, new FunctionIdentifier(key, pair.Value, methodInstruction));
				}
			}
			funcMatch[FunctionCode.IF] = "ENDIF";
			funcMatch[FunctionCode.SELECTCASE] = "ENDSELECT";
			funcMatch[FunctionCode.REPEAT] = "REND";
			funcMatch[FunctionCode.FOR] = "NEXT";
			funcMatch[FunctionCode.WHILE] = "WEND";
			funcMatch[FunctionCode.TRYCGOTO] = "CATCH";
			funcMatch[FunctionCode.TRYCJUMP] = "CATCH";
			funcMatch[FunctionCode.TRYCCALL] = "CATCH";
			funcMatch[FunctionCode.TRYCGOTOFORM] = "CATCH";
			funcMatch[FunctionCode.TRYCJUMPFORM] = "CATCH";
			funcMatch[FunctionCode.TRYCCALLFORM] = "CATCH";
			funcMatch[FunctionCode.CATCH] = "ENDCATCH";
			funcMatch[FunctionCode.DO] = "LOOP";
			funcMatch[FunctionCode.PRINTDATA] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATAL] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATAW] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATAK] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATAKL] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATAKW] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATAD] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATADL] = "ENDDATA";
			funcMatch[FunctionCode.PRINTDATADW] = "ENDDATA";
			funcMatch[FunctionCode.DATALIST] = "ENDLIST";
			funcMatch[FunctionCode.STRDATA] = "ENDDATA";
			funcMatch[FunctionCode.NOSKIP] = "ENDNOSKIP";
			funcMatch[FunctionCode.TRYCALLLIST] = "ENDFUNC";
			funcMatch[FunctionCode.TRYGOTOLIST] = "ENDFUNC";
			funcMatch[FunctionCode.TRYJUMPLIST] = "ENDFUNC";
			funcParent[FunctionCode.REND] = FunctionCode.REPEAT;
			funcParent[FunctionCode.NEXT] = FunctionCode.FOR;
			funcParent[FunctionCode.WEND] = FunctionCode.WHILE;
			funcParent[FunctionCode.LOOP] = FunctionCode.DO;
		}

		private static FunctionIdentifier setFunc;

		public static FunctionIdentifier SETFunction { get { return setFunc; } }

		internal static string getMatchFunction(FunctionCode func)
		{
            if (funcMatch.TryGetValue(func, out string ret))
                return ret;
            else
                return null;
        }


		internal static FunctionCode getParentFunc(FunctionCode func)
		{
            //1755 どうもenum.ToString()が遅いようなのでaheadに逆引き辞書を作るthisに
            if (funcParent.TryGetValue(func, out FunctionCode ret))
                return ret;
            else
                return FunctionCode.__NULL__;
            //if (funcMatch.ContainsValue(func.ToString()))
            //{
            //    foreach (FunctionCode pFunc in funcMatch.Keys)
            //    {
            //        if (funcMatch[pFunc] == func.ToString())
            //            return pFunc;
            //    }
            //}
            //return FunctionCode.__NULL__;
        }
		#endregion

		private FunctionIdentifier(string name, FunctionCode code, AbstractInstruction instruction)
			: this(name, code, instruction, 0)
		{
		}
		private FunctionIdentifier(string name, FunctionCode code, AbstractInstruction instruction, int additionalFlag)
		{
			this.code = code;
			this.arg = instruction.ArgBuilder;
			this.flag = instruction.Flag | additionalFlag;
			this.method = null;
			Name = name;
			Instruction = instruction;
		}

		private FunctionIdentifier(string name, FunctionCode code, ArgumentBuilder arg, int flag)
		{
			this.code = code;
			this.arg = arg;
			this.flag = flag;
			this.method = null;
			Name = name;
			Instruction = null;
		}

		private FunctionIdentifier(string methodName, FunctionMethod method, AbstractInstruction instruction)
		{
			this.code = FunctionCode.__NULL__;
			this.arg = instruction.ArgBuilder;
			this.flag = instruction.Flag;
			this.method = method;
			Name = methodName;
			Instruction = instruction;
		}
		public readonly AbstractInstruction Instruction;
		private FunctionCode code;
		private ArgumentBuilder arg;
		private int flag;
		private FunctionMethod method = null;
		public FunctionCode Code { get { return code; } }
		public ArgumentBuilder ArgBuilder { get { return arg; } }
		public FunctionMethod Method { get { return method; } }

		public string Name { get; private set; }
		internal bool IsFlowContorol()
		{
			return ((flag & FLOW_CONTROL) == FLOW_CONTROL);
		}

		internal bool IsExtended()
		{
			return ((flag & EXTENDED) == EXTENDED);
		}
		internal bool IsPrintDFunction()
		{
			return ((flag & ISPRINTDFUNC) == ISPRINTDFUNC);
		}

		internal bool IsPrintKFunction()
		{
			return ((flag & ISPRINTKFUNC) == ISPRINTKFUNC);
		}

		internal bool IsNewLine()
		{
			return ((flag & PRINT_NEWLINE) == PRINT_NEWLINE);
		}

		internal bool IsWaitInput()
		{
			return ((flag & PRINT_WAITINPUT) == PRINT_WAITINPUT);
		}
		internal bool IsPrintSingle()
		{
			return ((flag & PRINT_SINGLE) == PRINT_SINGLE);
		}

		internal bool IsPartial()
		{
			return ((flag & PARTIAL) == PARTIAL);
		}

		internal bool IsMethodSafe()
		{
			return ((flag & METHOD_SAFE) == METHOD_SAFE);
		}
		internal bool IsPrint()
		{
			return ((flag & IS_PRINT) == IS_PRINT);
		}
		internal bool IsInput()
		{
			return ((flag & IS_INPUT) == IS_INPUT);
		}
		internal bool IsPrintData()
		{
			return ((flag & IS_PRINTDATA) == IS_PRINTDATA);
		}
		internal bool IsForceSetArg()
		{
			return ((flag & FORCE_SETARG) == FORCE_SETARG);
		}
		internal bool IsDebug()
		{
			return ((flag & DEBUG_FUNC) == DEBUG_FUNC);
		}
		internal bool IsTry()
		{
			return ((flag & IS_TRY) == IS_TRY);
		}
		internal bool IsJump()
		{
			return ((flag & IS_JUMP) == IS_JUMP);
		}

		internal bool IsMethod()
		{
			return method != null;
		}
		//internal bool IsFastCall()
		//{
		//    return (arg == FunctionArgType.SP_CALL);
		//}
		public override string ToString()
		{
			return Name;
		}

	}
}