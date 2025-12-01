
namespace MinorShift.Emuera.GameProc.Function
{
	/// <summary>
	/// 命令のargumentタイプ
	/// </summary>
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude=false)]
	enum FunctionArgType
	{//numeric不要
		__NULL__ = 0x0000,//未setting.Error.argumentがnotならばVOIDを指定dothis.
		METHOD,//式insidefunction.

		VOID,//argumentなし
		INT_EXPRESSION,//数式type.省略possible
		INT_EXPRESSION_NULLABLE,//数式type
		STR_EXPRESSION,//string式type
        STR_EXPRESSION_NULLABLE,
		STR,//単純stringtype
		KEYWORD,//単純stringtype.特定の文字のみ有効
		STR_NULLABLE,//単純stringtype,省略possible
		FORM_STR,//書式付stringtype.
		FORM_STR_NULLABLE,//書式付stringtype.省略possible
		SP_PRINTV,//複数数式type.'～～,string可
		SP_TIMES,//<numerictypevariable>,<実数定数>
		SP_BAR,//<numeric>,<numeric>,<numeric>
		SP_SET,//可variablevaluevariable-数式type.
		SP_SETS,//可変stringvariable-単純又は複合stringtype.
		SP_SWAP,//<numeric>,<numeric>
		SP_VAR,//<variable>
		SP_SAVEDATA,//<numeric>,<string式>
        SP_INPUT,//(<numeric>) //argumentはoptionでnotのがデフォ,INT_EXPRESSION_NULLABLEとはprocessがdifferent
        SP_INPUTS,//(<FORMstring>) //argumentはoptionでnotのがデフォ,STR_EXPRESSION_NULLABLEとはprocessがdifferent
        SP_ONEINPUT,//(<numeric>, <numeric>) //argumentはoptionでnotのがデフォ,第2argumentはマウスinputwhenの2桁以aboveのvaluewhenのprocess指定
        SP_ONEINPUTS,//(<FORMstring>, <numeric>) //argumentはoptionでnotのがデフォ,第2argumentはマウスinputwhenの2文字以aboveのstringwhenのprocess指定
        SP_TINPUT,//<numeric>,<numeric>(,<numeric>,<string>)
        SP_TINPUTS,//<numeric>,<string式>(,<numeric>,<string>)
		SP_SORTCHARA,//<charactervariable>,<ソート順序>(両方省略possible)
		SP_CALL,//<string>,<argument>,... //argumentは省略possible
		SP_CALLF,
		SP_CALLFORM,//<書式付string>,<argument>,... //argumentは省略possible
		SP_CALLFORMF,//<書式付string>,<argument>,... //argumentは省略possible
		SP_FOR_NEXT,//<可variablevaluevariable>,<numeric>,<numeric>,<numeric> //argumentは省略possible
		SP_POWER,//<可variablevaluevariable>,<numeric>,<numeric>
		SP_SWAPVAR,//<可変variable>,<可変variable>(同typeのみ)
		EXPRESSION,//<式>,variableのtypeは不問
		EXPRESSION_NULLABLE,//<式>,variableのtypeは不問
		CASE,//<CASE条件式>(, <CASE条件式>...)
		

        //TODO　省略whenのprocessに違いがexistが統合possibleなはず
		VAR_INT,//<可variablevaluevariable> //argumentは省略可
        SP_GETINT,//<可variablevaluevariable>(今untilこれがnotthisに驚いた)

		VAR_STR,//<可variablevaluevariable> //argumentは省略可
		BIT_ARG,//<可variablevaluevariable>,<numeric>*n (SP_SETが使えnotbecause新設)
		SP_VAR_SET,//<可変variable>,<数式 or string式 or null>(,<範囲初value>, <範囲終value>)
		SP_BUTTON,//<string式>,<数式>
		SP_SET_ARRAY,//可variablevaluevariable-<数式arraytype>.未use
		SP_SETS_ARRAY,//可変stringvariable-<stringarraytype>.未use
		SP_COLOR,
		SP_SPLIT,//<string式>, <string式>, <可変文字variable>
		SP_CVAR_SET,//<可変variable>,<式>,<数式 or string式 or null>(,<範囲初value>, <範囲終value>)
		SP_CONTROL_ARRAY,//<可変variable>,<numeric>,<numeric>
		SP_SHIFT_ARRAY,//<可変variable>,<numeric>,<numericorstring>(,<numeric>,<numeric>)
        SP_SORTARRAY,//<対象variable>, (<ソート順序>, <範囲初value>, <範囲終value>)
        INT_ANY,//1つ以aboveのnumericを任意数
		FORM_STR_ANY,//1つ以aboveのFORMstringを任意数  
		SP_COPYCHARA,//<numeric>(, <numeric)第二argument省略可
		SP_COPY_ARRAY,//<string式>,<string式>
		SP_SAVEVAR,//<numeric>,<string式>, <variable>(, <variable>...)
		SP_SAVECHARA,//<numeric>, <string式>, <numeric>(, <numeric>...)第二argument省略可
		SP_REF,
		SP_REFBYNAME,
		SP_HTMLSPLIT,
	}
}
