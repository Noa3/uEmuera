using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameProc.Function;

namespace MinorShift.Emuera.GameProc
{

	internal sealed class UserDefinedVariableData
	{
		public string Name = null;
		public bool TypeIsStr = false;
		public bool Reference = false;
		public int Dimension = 1;
		public int[] Lengths = null;
		public Int64[] DefaultInt = null;
		public string[] DefaultStr = null;
		public bool Global = false;
		public bool Save = false;
		public bool Static = true;
		public bool Private = false;
		public bool CharaData = false;
		public bool Const = false;
		
		//1822 Tried delaying DIM for Private too, but there are too many issues so dropped it
		public static UserDefinedVariableData Create(DimLineWC dimline)
		{
			return Create(dimline.WC, dimline.Dims, dimline.IsPrivate, dimline.SC);
		}

		public static UserDefinedVariableData Create(WordCollection wc, bool dims, bool isPrivate, ScriptPosition sc)
		{
			// NOTE: We must NOT expand macros on the variable name itself!
			// For example, "#DIM CONST MAX_CHARA_NUM = 3000" must keep MAX_CHARA_NUM as the variable name.
			// However, macros SHOULD be expanded for size specifications like "#DIM MY_ARRAY, MAX_CHARA_NUM".
			// We handle this by deferring macro expansion until after we extract the variable name.
			
			string dimtype = dims ? "#DIM" : "#DIMS";
			UserDefinedVariableData ret = new UserDefinedVariableData();
			ret.TypeIsStr = dims;

			IdentifierWord idw;
			bool staticDefined = false;
			ret.Const = false;
			string keyword = dimtype;
			
			// First pass: extract keywords and variable name WITHOUT macro expansion
			while (!wc.EOL && (idw = wc.Current as IdentifierWord) != null)
			{
				wc.ShiftNext();
				keyword = idw.Code;
				if (Config.ICVariable)
					keyword = keyword.ToUpper();
				switch (keyword)
				{
					case "CONST":
						if (ret.CharaData)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CHARADATA keyword"), sc);
						if (ret.Global)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the GLOBAL keyword"), sc);
						if (ret.Save)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the SAVEDATA keyword"), sc);
						if (ret.Reference)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the REF keyword"), sc);
						if (!ret.Static)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the DYNAMIC keyword"), sc);
						if (ret.Const)
							throw new CodeEE(keyword + GameMessages.T(" keyword specified twice"), sc);
						ret.Const = true;
						break;
					case "REF":
						if (staticDefined && ret.Static)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the STATIC keyword"), sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CHARADATA keyword"), sc);
						if (ret.Global)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the GLOBAL keyword"), sc);
						if (ret.Save)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the SAVEDATA keyword"), sc);
						if (ret.Const)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CONST keyword"), sc);
						if (ret.Reference)
							throw new CodeEE(keyword + GameMessages.T(" keyword specified twice"), sc);
						ret.Reference = true;
						ret.Static = false;
						break;
					case "DYNAMIC":
						if (!isPrivate)
							throw new CodeEE(GameMessages.T("Global variable declarations cannot use the ") + keyword + GameMessages.T(" keyword"), sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CHARADATA keyword"), sc);
						if (ret.Const)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CONST keyword"), sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE(GameMessages.T("STATIC and DYNAMIC keywords cannot be specified together"), sc);
							else
								throw new CodeEE(keyword + GameMessages.T(" keyword specified twice"), sc);
						staticDefined = true;
						ret.Static = false;
						break;
					case "STATIC":
						if (!isPrivate)
							throw new CodeEE(GameMessages.T("Global variable declarations cannot use the ") + keyword + GameMessages.T(" keyword"), sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CHARADATA keyword"), sc);
						if (staticDefined)
							if (!ret.Static)
								throw new CodeEE(GameMessages.T("STATIC and DYNAMIC keywords cannot be specified together"), sc);
							else
								throw new CodeEE(keyword + GameMessages.T(" keyword specified twice"), sc);
						if (ret.Reference)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the REF keyword"), sc);
						staticDefined = true;
						ret.Static = true;
						break;
					case "GLOBAL":
						if (isPrivate)
							throw new CodeEE(GameMessages.T("Local variable declarations cannot use the ") + keyword + GameMessages.T(" keyword"), sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CHARADATA keyword"), sc);
						if (ret.Reference)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the REF keyword"), sc);
						if (ret.Const)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CONST keyword"), sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE(GameMessages.T("STATIC and GLOBAL keywords cannot be specified together"), sc);
							else
								throw new CodeEE(GameMessages.T("DYNAMIC and GLOBAL keywords cannot be specified together"), sc);
						ret.Global = true;
						break;
					case "SAVEDATA":
						if (isPrivate)
							throw new CodeEE(GameMessages.T("Local variable declarations cannot use the ") + keyword + GameMessages.T(" keyword"), sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE(GameMessages.T("STATIC and SAVEDATA keywords cannot be specified together"), sc);
							else
								throw new CodeEE(GameMessages.T("DYNAMIC and SAVEDATA keywords cannot be specified together"), sc);
						if (ret.Reference)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the REF keyword"), sc);
						if (ret.Const)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CONST keyword"), sc);
						if (ret.Save)
							throw new CodeEE(keyword + GameMessages.T(" keyword specified twice"), sc);
						ret.Save = true;
						break;
					case "CHARADATA":
						if (isPrivate)
							throw new CodeEE(GameMessages.T("Local variable declarations cannot use the ") + keyword + GameMessages.T(" keyword"), sc);
						if (ret.Reference)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the REF keyword"), sc);
						if (ret.Const)
							throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the CONST keyword"), sc);
						if (staticDefined)
							if (ret.Static)
                                throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the STATIC keyword"), sc);
							else
                                throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the DYNAMIC keyword"), sc);
						if (ret.Global)
                            throw new CodeEE(keyword + GameMessages.T(" cannot be specified together with the GLOBAL keyword"), sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + GameMessages.T(" keyword specified twice"), sc);
						ret.CharaData = true;
						break;
					default:
						ret.Name = keyword;
						goto whilebreak;
				}
			}
		whilebreak:
			if (ret.Name == null)
			{
				string contextMsg = (sc != null) ? $" at {sc.Filename}:{sc.LineNo}" : "";
				UnityEngine.Debug.LogError($"[UserDefinedVariable] CONST parsing error: No valid variable name after '{keyword}'{contextMsg}");
				throw new CodeEE(keyword + GameMessages.T(" was not followed by a valid variable name"), sc);
			}
			
			// Now that we have the variable name, expand macros for the remaining tokens (size specifications)
			// This ensures the variable name is NOT replaced by a macro, but size specs can use macros
			if (LexicalAnalyzer.UseMacro && !wc.EOL)
			{
				// Create a new WordCollection with just the remaining tokens and expand macros
				int currentPos = wc.Pointer;
				WordCollection remaining = new WordCollection();
				while (!wc.EOL)
				{
					remaining.Add(wc.Current);
					wc.ShiftNext();
				}
				remaining.Pointer = 0;
				remaining = LexicalAnalyzer.ExpandMacroPublic(remaining);
				remaining.Pointer = 0;
				
				// Replace the remaining tokens in wc with expanded ones
				wc.Pointer = currentPos;
				// Clear remaining items and add expanded ones
				while (wc.Collection.Count > currentPos)
				{
					wc.Collection.RemoveAt(wc.Collection.Count - 1);
				}
				remaining.Pointer = 0;
				while (!remaining.EOL)
				{
					wc.Collection.Add(remaining.Current);
					remaining.ShiftNext();
				}
				wc.Pointer = currentPos;
			}
			
			string errMes = "";
			int errLevel = -1;
			if (isPrivate)
				GlobalStatic.IdentifierDictionary.CheckUserPrivateVarName(ref errMes, ref errLevel, ret.Name);
			else
				GlobalStatic.IdentifierDictionary.CheckUserVarName(ref errMes, ref errLevel, ret.Name);
			if (errLevel >= 0)
			{
				if (errLevel >= 2)
					throw new CodeEE(errMes, sc);
				ParserMediator.Warn(errMes, sc, errLevel);
			}


			List<int> sizeNum = new List<int>();
			if (wc.EOL)//size omitted
			{
				if (ret.Const)
					throw new CodeEE(GameMessages.T("The CONST keyword is specified but no initial value is set"));
				sizeNum.Add(1);
			}
			else if (wc.Current.Type == ',')//size specified
			{
				while (!wc.EOL)
				{
					if (wc.Current.Type == '=')//size specification fully read & initial value specified
						break;
					if (wc.Current.Type != ',')
						throw new CodeEE(GameMessages.T("Incorrect format"), sc);
					wc.ShiftNext();
					if (ret.Reference)//element count not needed for reference type
					{
						sizeNum.Add(0);
						if (wc.EOL)
							break;
						if (wc.Current.Type == ',')
							continue;
					}
				if (wc.EOL)
					throw new CodeEE(GameMessages.T("No valid constant expression was specified after the comma"), sc);
				IOperandTerm arg = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.Comma_Assignment);
				// Use EMediator so user-defined CONST variables (e.g. #DIM CONST OBJ_ID_LAST = ...) can be
				// resolved as array sizes — Restructure(null) cannot evaluate them, but EMediator can.
				SingleTerm sizeTerm = arg.Restructure(GlobalStatic.EMediator) as SingleTerm;
				if ((sizeTerm == null) || (sizeTerm.GetOperandType() != typeof(Int64)))
					// Throw retryable exception so multi-pass DIM resolution can handle forward-references
					// to other CONST DIM variables (e.g. #DIM Arr, OBJ_ID_LAST where OBJ_ID_LAST is itself
					// a #DIM CONST resolved in a later pass).
					throw new IdentifierNotFoundCodeEE(GameMessages.T("No valid constant expression was specified after the comma"), sc);
					if (ret.Reference)//element count cannot be specified for reference type (either write 0 or omit)
					{
						if (sizeTerm.Int != 0)
							throw new CodeEE(GameMessages.T("Sizes cannot be specified for reference-type variables (omit the size or specify 0)"), sc);

						continue;
					}
					else if ((sizeTerm.Int <= 0) || (sizeTerm.Int > 1000000))
						throw new CodeEE(GameMessages.T("The size of a user-defined variable must be between 1 and 1000000"), sc);
					sizeNum.Add((int)sizeTerm.Int);
				}
			}


			if (wc.Current.Type != '=')//no initial value specified
			{
				if (ret.Const)
					throw new CodeEE(GameMessages.T("The CONST keyword is specified but no initial value is set"));
			}
			else//initial value specified
			{
				if (((OperatorWord)wc.Current).Code != OperatorCode.Assignment)
					throw new CodeEE(GameMessages.T("Unexpected operator found"));
				if (ret.Reference)
					throw new CodeEE(GameMessages.T("Initial values cannot be set for reference-type variables"));
				if (sizeNum.Count >= 2)
					throw new CodeEE(GameMessages.T("Initial values cannot be set for multidimensional variables"));
				if (ret.CharaData)
					throw new CodeEE(GameMessages.T("Initial values cannot be set for character-type variables"));
				int size = 0;
				if (sizeNum.Count == 1)
					size = sizeNum[0];
				wc.ShiftNext();
				IOperandTerm[] terms = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
				if (terms.Length == 0)
					throw new CodeEE(GameMessages.T("Initial values for arrays cannot be omitted"));
				if (size > 0)
				{
					if (terms.Length > size)
						throw new CodeEE(GameMessages.T("The number of initial values exceeds the array size"));
					if (ret.Const && terms.Length != size)
						throw new CodeEE(GameMessages.T("The number of initial values for a constant does not match the array size"));
				}
				if (dims)
					ret.DefaultStr = new string[terms.Length];
				else
					ret.DefaultInt = new Int64[terms.Length];

				for (int i = 0; i < terms.Length; i++)
				{
					if (terms[i] == null)
						throw new CodeEE(GameMessages.T("Initial values for arrays cannot be omitted"));
					terms[i] = terms[i].Restructure(GlobalStatic.EMediator);
					SingleTerm sTerm = terms[i] as SingleTerm;
					if (sTerm == null)
						throw new CodeEE(GameMessages.T("Only constants can be specified as initial values for arrays"));
					if (dims != sTerm.IsString)
						throw new CodeEE(GameMessages.T("The variable type does not match the initial value type"));
					if (dims)
						ret.DefaultStr[i] = sTerm.Str;
					else
						ret.DefaultInt[i] = sTerm.Int;
				}
				if (sizeNum.Count == 0)
					sizeNum.Add(terms.Length);
			}
			if (!wc.EOL)
				throw new CodeEE(GameMessages.T("Incorrect format"), sc);

			if (sizeNum.Count == 0)
				sizeNum.Add(1);

			ret.Private = isPrivate;
			ret.Dimension = sizeNum.Count;
			if (ret.Const && ret.Dimension > 1)
				throw new CodeEE(GameMessages.T("Variables with the CONST keyword cannot be multidimensional arrays"));
			if (ret.CharaData && ret.Dimension > 2)
				throw new CodeEE(GameMessages.T("Cannot declare character-type variables with 3 or more dimensions"), sc);
			if (ret.Dimension > 3)
				throw new CodeEE(GameMessages.T("Cannot declare array variables with 4 or more dimensions"), sc);
			ret.Lengths = new int[sizeNum.Count];
			if (ret.Reference)
				return ret;
			Int64 totalBytes = 1;
			for (int i = 0; i < sizeNum.Count; i++)
			{
				ret.Lengths[i] = sizeNum[i];
				totalBytes *= ret.Lengths[i];
			}
			if ((totalBytes <= 0) || (totalBytes > 1000000))
				throw new CodeEE(GameMessages.T("The size of a user-defined variable must be between 1 and 1000000"), sc);
			if (!isPrivate && ret.Save && !Config.SystemSaveInBinary)
			{
				if (dims && ret.Dimension > 1)
					throw new CodeEE(GameMessages.T("The \"Binary Save\" option is required when adding the SAVEDATA flag to a string-type multidimensional array variable"), sc);
				else if (ret.CharaData)
					throw new CodeEE(GameMessages.T("The \"Binary Save\" option is required when adding the SAVEDATA flag to a character-type variable"), sc);
			}
			return ret;
		}
	}
	internal sealed class DimLineWC
	{
		public WordCollection WC;
		public bool Dims;
		public bool IsPrivate;
		public ScriptPosition SC;
		public DimLineWC(WordCollection wc, bool isString, bool isPrivate, ScriptPosition position)
		{
			WC = wc;
			Dims = isString;
			IsPrivate = isPrivate;
			SC = position;
		}
	}

}