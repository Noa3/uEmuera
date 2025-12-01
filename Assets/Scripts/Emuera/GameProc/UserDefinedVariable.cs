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
		
		//1822 Privateの方もDIMだけ遅延させようとしたけどちょっと課題がおおいのでやめとく
		public static UserDefinedVariableData Create(DimLineWC dimline)
		{
			return Create(dimline.WC, dimline.Dims, dimline.IsPrivate, dimline.SC);
		}

		public static UserDefinedVariableData Create(WordCollection wc, bool dims, bool isPrivate, ScriptPosition sc)
		{
			string dimtype = dims ? "#DIM" : "#DIMS";
			UserDefinedVariableData ret = new UserDefinedVariableData();
			ret.TypeIsStr = dims;

			IdentifierWord idw;
			bool staticDefined = false;
			ret.Const = false;
			string keyword = dimtype;
			//List<string> keywords;
			while (!wc.EOL && (idw = wc.Current as IdentifierWord) != null)
			{
				wc.ShiftNext();
				keyword = idw.Code;
				if (Config.ICVariable)
					keyword = keyword.ToUpper();
				//TODO ifの数があたまわるい なんとかしたい
				switch (keyword)
				{
					case "CONST":
						if (ret.CharaData)
							throw new CodeEE(keyword + "とCHARADATAkeyワードは同whenに指定できません", sc);
						if (ret.Global)
							throw new CodeEE(keyword + "とGLOBALkeyワードは同whenに指定できません", sc);
						if (ret.Save)
							throw new CodeEE(keyword + "とSAVEDATAkeyワードは同whenに指定できません", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "とREFkeyワードは同whenに指定できません", sc);
						if (!ret.Static)
							throw new CodeEE(keyword + "とDYNAMICkeyワードは同whenに指定できません", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "keyワードが二重に指定されています", sc);
						ret.Const = true;
						break;
					case "REF":
						//throw new CodeEE("未実装の機能です", sc);
						//if (!isPrivate)
						//	throw new CodeEE("広域variableの宣言に" + keyword + "keyワードは指定できません", sc);
						if (staticDefined && ret.Static)
							throw new CodeEE(keyword + "とSTATICkeyワードは同whenに指定できません", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "とCHARADATAkeyワードは同whenに指定できません", sc);
						if (ret.Global)
							throw new CodeEE(keyword + "とGLOBALkeyワードは同whenに指定できません", sc);
						if (ret.Save)
							throw new CodeEE(keyword + "とSAVEDATAkeyワードは同whenに指定できません", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "とCONSTkeyワードは同whenに指定できません", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "keyワードが二重に指定されています", sc);
						ret.Reference = true;
						ret.Static = false;
						break;
					case "DYNAMIC":
						if (!isPrivate)
							throw new CodeEE("広域variableの宣言に" + keyword + "keyワードは指定できません", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "とCHARADATAkeyワードは同whenに指定できません", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "とCONSTkeyワードは同whenに指定できません", sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE("STATICとDYNAMICkeyワードは同whenに指定できません", sc);
							else
								throw new CodeEE(keyword + "keyワードが二重に指定されています", sc);
						staticDefined = true;
						ret.Static = false;
						break;
					case "STATIC":
						if (!isPrivate)
							throw new CodeEE("広域variableの宣言に" + keyword + "keyワードは指定できません", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "とCHARADATAkeyワードは同whenに指定できません", sc);
						if (staticDefined)
							if (!ret.Static)
								throw new CodeEE("STATICとDYNAMICkeyワードは同whenに指定できません", sc);
							else
								throw new CodeEE(keyword + "keyワードが二重に指定されています", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "とREFkeyワードは同whenに指定できません", sc);
						staticDefined = true;
						ret.Static = true;
						break;
					case "GLOBAL":
						if (isPrivate)
							throw new CodeEE("ローカルvariableの宣言に" + keyword + "keyワードは指定できません", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "とCHARADATAkeyワードは同whenに指定できません", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "とREFkeyワードは同whenに指定できません", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "とCONSTkeyワードは同whenに指定できません", sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE("STATICとGLOBALkeyワードは同whenに指定できません", sc);
							else
								throw new CodeEE("DYNAMICとGLOBALkeyワードは同whenに指定できません", sc);
						ret.Global = true;
						break;
					case "SAVEDATA":
						if (isPrivate)
							throw new CodeEE("ローカルvariableの宣言に" + keyword + "keyワードは指定できません", sc);
						if (staticDefined)
							if (ret.Static)
								throw new CodeEE("STATICとSAVEDATAkeyワードは同whenに指定できません", sc);
							else
								throw new CodeEE("DYNAMICとSAVEDATAkeyワードは同whenに指定できません", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "とREFkeyワードは同whenに指定できません", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "とCONSTkeyワードは同whenに指定できません", sc);
						if (ret.Save)
							throw new CodeEE(keyword + "keyワードが二重に指定されています", sc);
						ret.Save = true;
						break;
					case "CHARADATA":
						if (isPrivate)
							throw new CodeEE("ローカルvariableの宣言に" + keyword + "keyワードは指定できません", sc);
						if (ret.Reference)
							throw new CodeEE(keyword + "とREFkeyワードは同whenに指定できません", sc);
						if (ret.Const)
							throw new CodeEE(keyword + "とCONSTkeyワードは同whenに指定できません", sc);
						if (staticDefined)
							if (ret.Static)
                                throw new CodeEE(keyword + "とSTATICkeyワードは同whenに指定できません", sc);
							else
                                throw new CodeEE(keyword + "とDYNAMICkeyワードは同whenに指定できません", sc);
						if (ret.Global)
                            throw new CodeEE(keyword + "とGLOBALkeyワードは同whenに指定できません", sc);
						if (ret.CharaData)
							throw new CodeEE(keyword + "keyワードが二重に指定されています", sc);
						ret.CharaData = true;
						break;
					default:
						ret.Name = keyword;
						goto whilebreak;
				}
			}
		whilebreak:
			if (ret.Name == null)
				throw new CodeEE(keyword + "のafter 有効なvariable名が指定されていません", sc);
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
			if (wc.EOL)//サイズ省略
			{
				if (ret.Const)
					throw new CodeEE("CONSTkeyワードが指定されていますがinitialvalueがsettingされていません");
				sizeNum.Add(1);
			}
			else if (wc.Current.Type == ',')//サイズ指定
			{
				while (!wc.EOL)
				{
					if (wc.Current.Type == '=')//サイズ指定解読complete＆initialvalue指定
						break;
					if (wc.Current.Type != ',')
						throw new CodeEE("書式が間違っています", sc);
					wc.ShiftNext();
					if (ret.Reference)//参照typeのcaseは要素数不要
					{
						sizeNum.Add(0);
						if (wc.EOL)
							break;
						if (wc.Current.Type == ',')
							continue;
					}
					if (wc.EOL)
						throw new CodeEE("カンマのafter 有効な定数式が指定されていません", sc);
					IOperandTerm arg = ExpressionParser.ReduceIntegerTerm(wc, TermEndWith.Comma_Assignment);
					SingleTerm sizeTerm = arg.Restructure(null) as SingleTerm;
					if ((sizeTerm == null) || (sizeTerm.GetOperandType() != typeof(Int64)))
						throw new CodeEE("カンマのafter 有効な定数式が指定されていません", sc);
					if (ret.Reference)//参照typeには要素数指定不可(0にdoか書かnotかどっちか
					{
						if (sizeTerm.Int != 0)
							throw new CodeEE("参照typevariableにはサイズを指定できません(サイズを省略doか0を指定してください)", sc);

						continue;
					}
					else if ((sizeTerm.Int <= 0) || (sizeTerm.Int > 1000000))
						throw new CodeEE("ユーザーdefinitionvariableのサイズは1以above1000000以belowでなければなりません", sc);
					sizeNum.Add((int)sizeTerm.Int);
				}
			}


			if (wc.Current.Type != '=')//initialvalue指定なし
			{
				if (ret.Const)
					throw new CodeEE("CONSTkeyワードが指定されていますがinitialvalueがsettingされていません");
			}
			else//initialvalue指定あり
			{
				if (((OperatorWord)wc.Current).Code != OperatorCode.Assignment)
					throw new CodeEE("予期しnot演算子を発見しました");
				if (ret.Reference)
					throw new CodeEE("参照typevariableにはinitialvalueをsettingできません");
				if (sizeNum.Count >= 2)
					throw new CodeEE("多次originalvariableにはinitialvalueをsettingできません");
				if (ret.CharaData)
					throw new CodeEE("キャラtypevariableにはinitialvalueをsettingできません");
				int size = 0;
				if (sizeNum.Count == 1)
					size = sizeNum[0];
				wc.ShiftNext();
				IOperandTerm[] terms = ExpressionParser.ReduceArguments(wc, ArgsEndWith.EoL, false);
				if (terms.Length == 0)
					throw new CodeEE("arrayのinitialvalueは省略できません");
				if (size > 0)
				{
					if (terms.Length > size)
						throw new CodeEE("initialvalueの数がarrayのサイズを超えています");
					if (ret.Const && terms.Length != size)
						throw new CodeEE("定数のinitialvalueの数がarrayのサイズと一致しません");
				}
				if (dims)
					ret.DefaultStr = new string[terms.Length];
				else
					ret.DefaultInt = new Int64[terms.Length];

				for (int i = 0; i < terms.Length; i++)
				{
					if (terms[i] == null)
						throw new CodeEE("arrayのinitialvalueは省略できません");
					terms[i] = terms[i].Restructure(GlobalStatic.EMediator);
					SingleTerm sTerm = terms[i] as SingleTerm;
					if (sTerm == null)
						throw new CodeEE("arrayのinitialvalueには定数のみ指定できます");
					if (dims != sTerm.IsString)
						throw new CodeEE("variableのtypeとinitialvalueのtypeが一致していません");
					if (dims)
						ret.DefaultStr[i] = sTerm.Str;
					else
						ret.DefaultInt[i] = sTerm.Int;
				}
				if (sizeNum.Count == 0)
					sizeNum.Add(terms.Length);
			}
			if (!wc.EOL)
				throw new CodeEE("書式が間違っています", sc);

			if (sizeNum.Count == 0)
				sizeNum.Add(1);

			ret.Private = isPrivate;
			ret.Dimension = sizeNum.Count;
			if (ret.Const && ret.Dimension > 1)
				throw new CodeEE("CONSTkeyワードが指定されたvariableを多次originalarrayにはできません");
			if (ret.CharaData && ret.Dimension > 2)
				throw new CodeEE("3次original以aboveのキャラtypevariableを宣言docannot be", sc);
			if (ret.Dimension > 3)
				throw new CodeEE("4次original以aboveのarrayvariableを宣言docannot be", sc);
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
				throw new CodeEE("ユーザーdefinitionvariableのサイズは1以above1000000以belowでなければなりません", sc);
			if (!isPrivate && ret.Save && !Config.SystemSaveInBinary)
			{
				if (dims && ret.Dimension > 1)
					throw new CodeEE("stringtypeの多次originalarrayvariableにSAVEDATAフラグを付けるcaseには"バイナリtypesave"optionが必須です", sc);
				else if (ret.CharaData)
					throw new CodeEE("キャラtypevariableにSAVEDATAフラグを付けるcaseには"バイナリtypesave"optionが必須です", sc);
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