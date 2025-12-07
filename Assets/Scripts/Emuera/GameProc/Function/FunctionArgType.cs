namespace MinorShift.Emuera.GameProc.Function
{
	/// <summary>
	/// Argument type for instructions
	/// </summary>
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude=false)]
	enum FunctionArgType
	{//No numeric values required
		__NULL__ = 0x0000,//Not set. Error. If there are no arguments, specify VOID.
		METHOD,//Expression function.

		VOID,//No arguments
		INT_EXPRESSION,//Numeric expression type. Can be omitted
		INT_EXPRESSION_NULLABLE,//Numeric expression type
		STR_EXPRESSION,//String expression type
        STR_EXPRESSION_NULLABLE,
		STR,//Simple string type
		KEYWORD,//Simple string type. Only specific characters are valid
		STR_NULLABLE,//Simple string type, can be omitted
		FORM_STR,//Formatted string type.
		FORM_STR_NULLABLE,//Formatted string type. Can be omitted
		SP_PRINTV,//Multiple numeric expression type. '~~, strings allowed
		SP_TIMES,//<numeric variable>,<real number constant>
		SP_BAR,//<number>,<number>,<number>
		SP_SET,//Variable numeric variable/numeric expression type.
		SP_SETS,//Variable string variable/simple or compound string type.
		SP_SWAP,//<number>,<number>
		SP_VAR,//<variable>
		SP_SAVEDATA,//<number>,<string expression>
        SP_INPUT,//(<number>) //Arguments are not optional by default, processing differs from INT_EXPRESSION_NULLABLE
        SP_INPUTS,//(<FORM string>) //Arguments are not optional by default, processing differs from STR_EXPRESSION_NULLABLE
        SP_ONEINPUT,//(<number>, <number>) //Arguments are not optional by default, second argument specifies handling for values with 2+ digits during mouse input
        SP_ONEINPUTS,//(<FORM string>, <number>) //Arguments are not optional by default, second argument specifies handling for strings with 2+ characters during mouse input
        SP_TINPUT,//<number>,<number>(,<number>,<string>)
        SP_TINPUTS,//<number>,<string expression>(,<number>,<string>)
		SP_SORTCHARA,//<character variable>,<sort order>(both can be omitted)
		SP_CALL,//<string>,<arguments>,... //Arguments can be omitted
		SP_CALLF,
		SP_CALLFORM,//<formatted string>,<arguments>,... //Arguments can be omitted
		SP_CALLFORMF,//<formatted string>,<arguments>,... //Arguments can be omitted
		SP_FOR_NEXT,//<variable numeric variable>,<number>,<number>,<number> //Arguments can be omitted
		SP_POWER,//<variable numeric variable>,<number>,<number>
		SP_SWAPVAR,//<variable variable>,<variable variable>(same type only)
		EXPRESSION,//<expression>, variable type doesn't matter
		EXPRESSION_NULLABLE,//<expression>, variable type doesn't matter
		CASE,//<CASE condition expression>(, <CASE condition expression>...)
		

        //TODO: There are differences in processing when omitted but they should be able to be unified
		VAR_INT,//<variable numeric variable> //Arguments can be omitted
        SP_GETINT,//<variable numeric variable>(surprised this didn't exist before)

		VAR_STR,//<variable numeric variable> //Arguments can be omitted
		BIT_ARG,//<variable numeric variable>,<number>*n (newly created because SP_SET cannot be used)
		SP_VAR_SET,//<variable variable>,<numeric expression or string expression or null>(,<range start value>, <range end value>)
		SP_BUTTON,//<string expression>,<numeric expression>
		SP_SET_ARRAY,//Variable numeric variable/<numeric array type>. Unused
		SP_SETS_ARRAY,//Variable string variable/<string array type>. Unused
		SP_COLOR,
		SP_SPLIT,//<string expression>, <string expression>, <variable char variable>
		SP_CVAR_SET,//<variable variable>,<expression>,<numeric expression or string expression or null>(,<range start value>, <range end value>)
		SP_CONTROL_ARRAY,//<variable variable>,<number>,<number>
		SP_SHIFT_ARRAY,//<variable variable>,<number>,<number or string>(,<number>,<number>)
        SP_SORTARRAY,//<target variable>, (<sort order>, <range start value>, <range end value>)
        INT_ANY,//One or more numbers in any quantity
		FORM_STR_ANY,//One or more FORM strings in any quantity  
		SP_COPYCHARA,//<number>(, <number>) second argument can be omitted
		SP_COPY_ARRAY,//<string expression>,<string expression>
		SP_SAVEVAR,//<number>,<string expression>, <variable>(, <variable>...)
		SP_SAVECHARA,//<number>, <string expression>, <number>(, <number>...) second argument can be omitted
		SP_REF,
		SP_REFBYNAME,
		SP_HTMLSPLIT,
	}
}
