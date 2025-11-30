
namespace MinorShift.Emuera.GameProc.Function
{
	/// <summary>
	/// Instruction/function codes for the Emuera script processor
	/// </summary>
	// Obfuscation attribute: Set Exclude=true if enum.ToString() or enum.Parse() will be used.
	[global::System.Reflection.Obfuscation(Exclude = true)]
	enum FunctionCode
	{// Numeric values are not required for these enums
		// When defining a FunctionCode, call addFunction in the static FunctionIdentifier() constructor.
		// Choose the appropriate FunctionArg and flags at that time.

		// When defining a FunctionMethod (in-expression function), it will be picked up automatically.
		// However, if the in-expression version and the instruction version behave differently, you need to add it.

		__NULL__ = 0x0000,
		SET,// Numeric assignment or string assignment statement
		//SETS,// String assignment statement
		PRINT,// Display text
		PRINTL,// Print with newline
		PRINTW,// Wait for input (effectively a newline)

		PRINTV,// Print variable contents
		PRINTVL,
		PRINTVW,

		PRINTS,// Print string variable contents
		PRINTSL,
		PRINTSW,

		PRINTFORM,// Supports format syntax like {expression} and %string_variable%
		PRINTFORML,
		PRINTFORMW,

		PRINTFORMS,// Convert and display string variable contents
		PRINTFORMSL,
		PRINTFORMSW,

		PRINTC,// Print with column alignment (fixed-width columnar output)

		CLEARLINE,
		REUSELASTLINE,

		WAIT,// Wait for line break input
		INPUT,// Integer input. Result goes to RESULT variable.
		INPUTS,// String input. Result goes to RESULTS variable.
		TINPUT,
		TINPUTS,
		TWAIT,
		WAITANYKEY,
		FORCEWAIT,// WAIT that cannot be skipped - unlike forced TWAIT, this interrupts skip mode
		ONEINPUT,
		ONEINPUTS,
		TONEINPUT,
		TONEINPUTS,
		AWAIT,// No input allowed - DoEvents only

		DRAWLINE,// Draw a line of dashes from left edge to right edge of screen
		BAR,// Draw a graph like [*****....]. Usage: BAR (variable), (max_value), (length)
		BARL,// BAR with newline
		TIMES,// Decimal calculation. Usage: TIMES (variable), (decimal_value)

		PRINT_ABL,// Print ability. Argument is registration number
		PRINT_TALENT,// Print talent/trait
		PRINT_MARK,// Print mark/stamp
		PRINT_EXP,// Print experience
		PRINT_PALAM,// Print parameter
		PRINT_ITEM,// Print held items
		PRINT_SHOPITEM,// Print items for sale in shop

		UPCHECK,// Parameter change check
		CUPCHECK,
		ADDCHARA,// Add character by character number
		ADDSPCHARA,// Add SP character by number (created with flag 0 set to 1)
		ADDDEFCHARA,
		ADDVOIDCHARA,// Create a character with no variable settings
		DELCHARA,// Delete character by registration number

		PUTFORM,// Only usable in @SAVEINFO function. Same format as PRINTFORM but adds description to save data.
		QUIT,// End the game
		OUTPUTLOG,

		BEGIN,// Execute system function. Executing this loses the CALL origin and other context.

		SAVEGAME,// Call save screen. Shop only.
		LOADGAME,//

		SIF,// Single-line IF
		IF,
		ELSE,
		ELSEIF,
		ENDIF,

		REPEAT,// Repeat until REND. Number of repetitions goes to COUNT. Cannot nest.
		REND,
		CONTINUE,// Return to REPEAT
		BREAK,// Jump to line after REND

		GOTO,// Jump to $ label

		JUMP,// Move to function
		CALL,// Move to function. Remembers origin and returns with RETURN.
		CALLEVENT,
		RETURN,//__INT_EXPRESSION__,// End function. Can store integer in RESULT. Defaults to 0 if omitted. (Next @~~ is treated as RETURN.)
		RETURNFORM,//__FORM_STR__,// End function. Can store integer in RESULT. Defaults to 0 if omitted. (Next @~~ is treated as RETURN.)
		RETURNF,
		RESTART,// Restart function. Returns to the beginning of the function.


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
		VARSIZE,// Cannot convert to __METHOD__ because behavior differs
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
		GETTIME,// Cannot convert to __METHOD__ because it needs to assign to two variables

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

		POWER,// Cannot convert to METHOD because arguments differ
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

		PRINTCPERLINE,// Cannot convert - argument specification differs


		SETBIT,
		CLEARBIT,
		INVERTBIT,
		DELALLCHARA,
		PICKUPCHARA,

		VARSET,
		CVARSET,

		RESET_STAIN,

		SAVENOS,// Cannot convert - argument specification differs

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

		PRINTVK,// Print variable contents (Kana variant)
		PRINTVKL,
		PRINTVKW,

		PRINTSK,// Print string variable contents (Kana variant)
		PRINTSKL,
		PRINTSKW,

		PRINTFORMK,// Supports format syntax like {expression} and %string_variable% (Kana variant)
		PRINTFORMKL,
		PRINTFORMKW,

		PRINTFORMSK,// Convert and display string variable contents (Kana variant)
		PRINTFORMSKL,
		PRINTFORMSKW,

		PRINTCK,// Print with column alignment (Kana variant)
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

		PRINTD,// Display text (Debug variant)
		PRINTDL,// Print with newline (Debug variant)
		PRINTDW,// Wait for input (Debug variant)

		PRINTVD,// Print variable contents (Debug variant)
		PRINTVDL,
		PRINTVDW,

		PRINTSD,// Print string variable contents (Debug variant)
		PRINTSDL,
		PRINTSDW,

		PRINTFORMD,// Supports format syntax like {expression} and %string_variable% (Debug variant)
		PRINTFORMDL,
		PRINTFORMDW,

		PRINTFORMSD,// Convert and display string variable contents (Debug variant)
		PRINTFORMSDL,
		PRINTFORMSDW,

		PRINTCD,// Print with column alignment (Debug variant)
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
