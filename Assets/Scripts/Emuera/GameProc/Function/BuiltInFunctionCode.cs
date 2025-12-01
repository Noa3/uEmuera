
namespace MinorShift.Emuera.GameProc.Function
{
	/// <summary>
	/// 命令コード
	/// </summary>
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude = true)]
	enum FunctionCode
	{//numeric不要
		//FunctionCodeをdefinitionしたらstatic FunctionIdentifier()inでaddFunctiondothis.
		//その際に適切なFunctionArgとフラグを選ぶ.

		//FunctionMethod(式insidefunction)をdefinitionしたcaseには自動で拾うので何もしなくてよい.
		//ただし,式insidefunctionバージョンと命令バージョンで動作がdifferentならadddo必要がexist.

		__NULL__ = 0x0000,
		SET,//numeric代入文 or string代入文
		//SETS,//string代入文
		PRINT,//文字をdisplaydo
		PRINTL,//改line
		PRINTW,//input待ち(実質改line)

		PRINTV,//variableのin容
		PRINTVL,
		PRINTVW,

		PRINTS,//stringvariableのin容
		PRINTSL,
		PRINTSW,

		PRINTFORM,//{数式},%stringvariable%etcの書式が使える.
		PRINTFORML,
		PRINTFORMW,

		PRINTFORMS,//stringvariableのin容をconvertしてdisplay.
		PRINTFORMSL,
		PRINTFORMSW,

		PRINTC,//??

		CLEARLINE,
		REUSELASTLINE,

		WAIT,//改line待ち.
		INPUT,//integerinput.inputはRESULTto.
		INPUTS,//stringinput.inputはRESULTSto.
		TINPUT,
		TINPUTS,
		TWAIT,
		WAITANYKEY,
		FORCEWAIT,//スキップで省略できnotWAIT,強制TWAITと違い,スキップを打ち切る
		ONEINPUT,
		ONEINPUTS,
		TONEINPUT,
		TONEINPUTS,
		AWAIT,//input不可 DoEvents

		DRAWLINE,//画面の左端from右端until----と線を引く.
		BAR,//[*****....]のようなグラフを書く.BAR (variable) , (最大value), (長さ)
		BARL,//改line付き.
		TIMES,//小数calculate.TIMES (variable) , (小numeric)called 形で使う.

		PRINT_ABL,//能力.argumentは登録番号
		PRINT_TALENT,//素質
		PRINT_MARK,//刻印
		PRINT_EXP,//経験
		PRINT_PALAM,//パラメータ
		PRINT_ITEM,//所持アイテム
		PRINT_SHOPITEM,//ショップで売っているアイテム

		UPCHECK,//パラメータの変動
		CUPCHECK,
		ADDCHARA,//(キャラ番号)のcharacterをadd
		ADDSPCHARA,//(キャラ番号)のSPcharacterをadd(フラグ0を1にしてcreate)
		ADDDEFCHARA,
		ADDVOIDCHARA,//variableに何のsettingのnotキャラをcreate
		DELCHARA,//(キャラ登録番号)のcharacterをdelete.

		PUTFORM,//@SAVEINFOfunctionでのみusepossible.PRINTFORMと同様の書式でsavedataに概要をつける.
		QUIT,//ゲームをend
		OUTPUTLOG,

		BEGIN,//systemfunctionの実line.実linedoとCALLのcalloriginaletcを忘れてしまう.

		SAVEGAME,//save画面を呼ぶ.ショップのみ.
		LOADGAME,//

		SIF,//一lineのみIF
		IF,
		ELSE,
		ELSEIF,
		ENDIF,

		REPEAT,//RENDuntil繰り返し.繰り返した回数がCOUNTto.ネスト不可.
		REND,
		CONTINUE,//REPEATに戻る
		BREAK,//RENDのnext lineuntil

		GOTO,//$ラベルtoジャンプ

		JUMP,//functionにmove
		CALL,//functionにmove.moveoriginalを記憶し,RETURNで帰る.
		CALLEVENT,
		RETURN,//__INT_EXPRESSION__,//functionのend.RESULTにintegerを格納possible.省略したcase,０.(next @～～がRETURNと見なbe done.)  
		RETURNFORM,//__FORM_STR__,//functionのend.RESULTにintegerを格納possible.省略したcase,０.(next @～～がRETURNと見なbe done.)  
		RETURNF,
		RESTART,//functionの再開.functionの最初に戻る.


		STRLEN,
		//STRLENS,//
		STRLENFORM,
		STRLENU,
		//STRLENSU,
		STRLENFORMU,

		PRINTLC,
		PRINTFORMC,
		PRINTFORMLC,

		SWAPCHARA,
		COPYCHARA,
		ADDCOPYCHARA,
		VARSIZE,//動作がdifferentので__METHOD__化できnot
		SPLIT,

		PRINTSINGLE,
		PRINTSINGLEV,
		PRINTSINGLES,
		PRINTSINGLEFORM,
		PRINTSINGLEFORMS,

		PRINTBUTTON,
		PRINTBUTTONC,
		PRINTBUTTONLC,

		PRINTPLAIN,
		PRINTPLAINFORM,

		SAVEDATA,
		LOADDATA,
		DELDATA,
		GETTIME,//2つに代入do必要がexistので__METHOD__化できnot

		TRYJUMP,
		TRYCALL,
		TRYGOTO,
		JUMPFORM,
		CALLFORM,
		GOTOFORM,
		TRYJUMPFORM,
		TRYCALLFORM,
		TRYGOTOFORM,
		CALLTRAIN,
		STOPCALLTRAIN,
		CATCH,
		ENDCATCH,
		TRYCJUMP,
		TRYCCALL,
		TRYCGOTO,
		TRYCJUMPFORM,
		TRYCCALLFORM,
		TRYCGOTOFORM,
		TRYCALLLIST,
		TRYJUMPLIST,
		TRYGOTOLIST,
		FUNC,
		ENDFUNC,
		CALLF,
		CALLFORMF,

		SETCOLOR,
		SETCOLORBYNAME,
		RESETCOLOR,
		SETBGCOLOR,
		SETBGCOLORBYNAME,
		RESETBGCOLOR,
		FONTBOLD,
		FONTITALIC,
		FONTREGULAR,
		SORTCHARA,
		FONTSTYLE,
		ALIGNMENT,
		CUSTOMDRAWLINE,
		DRAWLINEFORM,
		CLEARTEXTBOX,

		SETFONT,

		FOR,
		NEXT,
		WHILE,
		WEND,

		POWER,//argumentがdifferentのでMETHOD化できnot.
		SAVEGLOBAL,
		LOADGLOBAL,
		SWAP,

		RESETDATA,
		RESETGLOBAL,

		RANDOMIZE,
		DUMPRAND,
		INITRAND,

		REDRAW,
		DOTRAIN,

		SELECTCASE,
		CASE,
		CASEELSE,
		ENDSELECT,

		DO,
		LOOP,

		PRINTDATA,
		PRINTDATAL,
		PRINTDATAW,
		DATA,
		DATAFORM,
		ENDDATA,
		DATALIST,
		ENDLIST,
		STRDATA,

		PRINTCPERLINE,//よく考えたらargumentの仕様differentや


		SETBIT,
		CLEARBIT,
		INVERTBIT,
		DELALLCHARA,
		PICKUPCHARA,

		VARSET,
		CVARSET,

		RESET_STAIN,

		SAVENOS,//argumentの仕様がdifferentので(ry

		FORCEKANA,

		SKIPDISP,
		NOSKIP,
		ENDNOSKIP,

		ARRAYSHIFT,
		ARRAYREMOVE,
		ARRAYSORT,
		ARRAYCOPY,

		ENCODETOUNI,

		DEBUGPRINT,
		DEBUGPRINTL,
		DEBUGPRINTFORM,
		DEBUGPRINTFORML,
		DEBUGCLEAR,
		ASSERT,
		THROW,

		SAVEVAR,
		LOADVAR,
		//		CHKVARDATA,
		SAVECHARA,
		LOADCHARA,
		//		CHKCHARADATA,

		REF,
		REFBYNAME,

		PRINTK,
		PRINTKL,
		PRINTKW,

		PRINTVK,//variableのin容
		PRINTVKL,
		PRINTVKW,

		PRINTSK,//stringvariableのin容
		PRINTSKL,
		PRINTSKW,

		PRINTFORMK,//{数式},%stringvariable%etcの書式が使える.
		PRINTFORMKL,
		PRINTFORMKW,

		PRINTFORMSK,//stringvariableのin容をconvertしてdisplay.
		PRINTFORMSKL,
		PRINTFORMSKW,

		PRINTCK,//??
		PRINTLCK,
		PRINTFORMCK,
		PRINTFORMLCK,

		PRINTSINGLEK,
		PRINTSINGLEVK,
		PRINTSINGLESK,
		PRINTSINGLEFORMK,
		PRINTSINGLEFORMSK,

		PRINTDATAK,
		PRINTDATAKL,
		PRINTDATAKW,

		PRINTD,//文字をdisplaydo
		PRINTDL,//改line
		PRINTDW,//input待ち(実質改line)

		PRINTVD,//variableのin容
		PRINTVDL,
		PRINTVDW,

		PRINTSD,//stringvariableのin容
		PRINTSDL,
		PRINTSDW,

		PRINTFORMD,//{数式},%stringvariable%etcの書式が使える.
		PRINTFORMDL,
		PRINTFORMDW,

		PRINTFORMSD,//stringvariableのin容をconvertしてdisplay.
		PRINTFORMSDL,
		PRINTFORMSDW,

		PRINTCD,//??
		PRINTLCD,
		PRINTFORMCD,
		PRINTFORMLCD,

		PRINTSINGLED,
		PRINTSINGLEVD,
		PRINTSINGLESD,
		PRINTSINGLEFORMD,
		PRINTSINGLEFORMSD,

		PRINTDATAD,
		PRINTDATADL,
		PRINTDATADW,

		HTML_PRINT,
		HTML_TAGSPLIT,

		TOOLTIP_SETCOLOR,
		TOOLTIP_SETDELAY,
        TOOLTIP_SETDURATION,

		PRINT_IMG,
		PRINT_RECT,
		PRINT_SPACE,

		INPUTMOUSEKEY,
	}
}
