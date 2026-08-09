
namespace MinorShift.Emuera.GameProc.Function
{
	/// <summary>
	/// Function code
	/// </summary>
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude = true)]
	enum FunctionCode
	{//no value needed
		//When a FunctionCode is defined, addFunction must be called in static FunctionIdentifier().
		//At that time choose an appropriate FunctionArg and flags.

		//If a FunctionMethod (in-expression function) is defined, it is picked up automatically so nothing needs to be done.
		//However, if the in-expression function version and the instruction version behave differently, it is necessary to add it.

		__NULL__ = 0x0000,
		SET,//numeric assignment statement or string assignment statement
		//SETS,//string assignment statement
		PRINT,//display characters
		PRINTL,//newline
		PRINTW,//wait for input (effectively newline)

		PRINTV,//variable contents
		PRINTVL,
		PRINTVW,

		PRINTS,//string variable contents
		PRINTSL,
		PRINTSW,

		PRINTFORM,//formats such as {expression}, %string variable%, etc. are available.
		PRINTFORML,
		PRINTFORMW,

		PRINTFORMS,//convert and display the contents of string variables.
		PRINTFORMSL,
		PRINTFORMSW,

		PRINTC,//??

		CLEARLINE,
		REUSELASTLINE,

		WAIT,//wait for newline.
		INPUT,//integer input. Input goes to RESULT.
		INPUTS,//string input. Input goes to RESULTS.
		TINPUT,
		TINPUTS,
		TWAIT,
		WAITANYKEY,
		FORCEWAIT,//WAIT that cannot be omitted by skipping. Unlike forced TWAIT, it breaks the skip
		ONEINPUT,
		ONEINPUTS,
		TONEINPUT,
		TONEINPUTS,
		AWAIT,//no input possible DoEvents

		DRAWLINE,//draw a ---- line from the left edge to the right edge of the screen.
		BAR,//draw a graph like [*****....]. BAR (variable), (max value), (length)
		BARL,//with newline.
		TIMES,//decimal calculation. Used in the form TIMES (variable), (decimal value).

		PRINT_ABL,//ability. argument is the registration number
		PRINT_TALENT,//talent
		PRINT_MARK,//mark (imprint)
		PRINT_EXP,//experience
		PRINT_PALAM,//parameter
		PRINT_ITEM,//held item
		PRINT_SHOPITEM,//item sold in shop

		UPCHECK,//parameter fluctuation
		CUPCHECK,
		ADDCHARA,//add character (character number)
		ADDSPCHARA,//add SP character (character number) (created with flag 0 set to 1)
		ADDDEFCHARA,
		ADDVOIDCHARA,//create a character with no settings in the variable
		DELCHARA,//delete character (character registration number).

		PUTFORM,//only valid in the @SAVEINFO function. Adds an overview to save data in the same format as PRINTFORM.
		QUIT,//end the game
		OUTPUTLOG,

		BEGIN,//executes a system function. Once executed, the CALL caller etc. is forgotten.

		SAVEGAME,//calls the save screen. Only in the shop.
		LOADGAME,//

		SIF,//one-line IF
		IF,
		ELSE,
		ELSEIF,
		ENDIF,

		REPEAT,//repeat until REND. Repeat count goes to COUNT. Nesting not allowed.
		REND,
		CONTINUE,//return to REPEAT
		BREAK,//to the line after REND

		GOTO,//jump to $label

		JUMP,//move to function
		CALL,//move to function. Remembers the origin and returns via RETURN.
		CALLEVENT,
		RETURN,//__INT_EXPRESSION__,//function end. Integer can be stored in RESULT. If omitted, 0. (The next @~~ is considered RETURN.)  
		RETURNFORM,//__FORM_STR__,//function end. Integer can be stored in RESULT. If omitted, 0. (The next @~~ is considered RETURN.)  
		RETURNF,
		RESTART,//restart the function. Return to the beginning of the function.


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
		VARSIZE,//behavior differs so cannot be made a __METHOD__
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
		GETTIME,//needs to assign to 2 values so cannot be made a __METHOD__

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

		POWER,//argument differs so cannot be made a METHOD.
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

		PRINTCPERLINE,//on reflection the argument spec differs


		SETBIT,
		CLEARBIT,
		INVERTBIT,
		DELALLCHARA,
		PICKUPCHARA,

		VARSET,
		CVARSET,

		RESET_STAIN,

		SAVENOS,//argument spec differs so (ry

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

		PRINTVK,//variable contents
		PRINTVKL,
		PRINTVKW,

		PRINTSK,//string variable contents
		PRINTSKL,
		PRINTSKW,

		PRINTFORMK,//formats such as {expression}, %string variable%, etc. are available.
		PRINTFORMKL,
		PRINTFORMKW,

		PRINTFORMSK,//convert and display the contents of string variables.
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

		PRINTD,//display characters
		PRINTDL,//newline
		PRINTDW,//wait for input (effectively newline)

		PRINTVD,//variable contents
		PRINTVDL,
		PRINTVDW,

		PRINTSD,//string variable contents
		PRINTSDL,
		PRINTSDW,

		PRINTFORMD,//formats such as {expression}, %string variable%, etc. are available.
		PRINTFORMDL,
		PRINTFORMDW,

		PRINTFORMSD,//convert and display the contents of string variables.
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

		// Emuera EM/EE Extensions
		BINPUT,//button-only input. Integer input. Input goes to RESULT.
		BINPUTS,//button-only input. String input. Input goes to RESULTS.
		
		TRYCALLF,//TRY pattern for CALLF
		TRYCALLFORMF,//TRY pattern for CALLFORMF

		// Sound commands
		PLAYSOUND,//play a sound file
		STOPSOUND,//stop sound playback
		PLAYBGM,//play BGM
		STOPBGM,//stop BGM playback
		SETSOUNDVOLUME,//set sound effect volume
		SETBGMVOLUME,//set BGM volume

		// Extended graphics commands
		GDRAWTEXT,//draw text
		GDRAWSPRITE,//draw sprite

		// Extended output log command
		OUTPUTLOG_EXTENDED,//extended version OUTPUTLOG

		// HTML island buffering (EM/EE)
		HTML_PRINT_ISLAND,//accumulate HTML into island buffer and display
		HTML_PRINT_ISLAND_CLEAR,//clear the HTML island buffer
	}
}
