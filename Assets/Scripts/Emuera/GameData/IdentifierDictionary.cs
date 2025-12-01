using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameView;
using System.IO;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameData.Expression;
using MinorShift._Library;

namespace MinorShift.Emuera
{
	//1756 新設.
	//また,useされているnameを記憶し衝突を検出do.
	internal sealed class IdentifierDictionary
	{
		private enum DefinedNameType
		{
			None = 0,
			Reserved,
			SystemVariable,
			SystemMethod,
			SystemInstrument,
			//UserIdentifier,
			UserGlobalVariable,
			UserMacro,
			UserRefMethod,
			NameSpace,
		}
		readonly static char[] badSymbolAsIdentifier = new char[]
		{
			'+', '-', '*', '/', '%', '=', '!', '<', '>', '|', '&', '^', '~',
			' ', '　', '\t' ,
			'\"','(', ')', '{', '}', '[', ']', ',', '.', ':',
			'\\', '@', '$', '#', '?', ';', '\'',
			//'_'はOK
		};
		readonly static Regex regexCom = new Regex("^COM[0-9]+$");
		readonly static Regex regexComAble = new Regex("^COM_ABLE[0-9]+$");
		readonly static Regex regexAblup = new Regex("^ABLUP[0-9]+$");
		#region static
		
		public static bool IsEventLabelName(string labelName)
		{
			switch (labelName)
			{
				case "EVENTFIRST":
				case "EVENTTRAIN":
				case "EVENTSHOP":
				case "EVENTBUY":
				case "EVENTCOM":
				case "EVENTTURNEND":
				case "EVENTCOMEND":
				case "EVENTEND":
				case "EVENTLOAD":
					return true;
			}
			return false;
		}
		public static bool IsSystemLabelName(string labelName)
		{
			switch (labelName)
			{
				case "EVENTFIRST":
				case "EVENTTRAIN":
				case "EVENTSHOP":
				case "EVENTBUY":
				case "EVENTCOM":
				case "EVENTTURNEND":
				case "EVENTCOMEND":
				case "EVENTEND":
				case "SHOW_STATUS":
				case "SHOW_USERCOM":
				case "USERCOM":
				case "SOURCE_CHECK":
				case "CALLTRAINEND":
				case "SHOW_JUEL":
				case "SHOW_ABLUP_SELECT":
				case "USERABLUP":
				case "SHOW_SHOP":
				case "SAVEINFO":
				case "USERSHOP":

				case "EVENTLOAD":
				case "TITLE_LOADGAME":
				case "SYSTEM_AUTOSAVE":
				case "SYSTEM_TITLE":
				case "SYSTEM_LOADEND":
					return true;
			}

			if (labelName.StartsWith("COM"))
			{
				if (regexCom.IsMatch(labelName))
					return true;
				if (regexComAble.IsMatch(labelName))
					return true;
			}
			if (labelName.StartsWith("ABLUP"))
				if (regexAblup.IsMatch(labelName))
					return true;
			return false;
		}
		#endregion


		Dictionary<string, DefinedNameType> nameDic = new Dictionary<string, DefinedNameType>();

		List<string> privateDimList = new List<string>();
		List<string> disableList = new List<string>();
		//Dictionary<string, VariableToken> userDefinedVarDic = new Dictionary<string, VariableToken>();

		VariableData varData;
		Dictionary<string, VariableToken> varTokenDic;
		Dictionary<string, VariableLocal> localvarTokenDic;
		Dictionary<string, FunctionIdentifier> instructionDic;
		Dictionary<string, FunctionMethod> methodDic;
		Dictionary<string, UserDefinedRefMethod> refmethodDic;
		public List<UserDefinedCharaVariableToken> CharaDimList = new List<UserDefinedCharaVariableToken>();
		#region initialize
		public IdentifierDictionary(VariableData varData)
		{
			this.varData = varData;
			nameDic.Clear();
			//予約語を登録.式during 登場doと構文parseが崩壊doname群.
			//ただしeramakerforscriptなら特に気にdothisはnot.式during 出てこnot単語も同様.
			nameDic.Add("IS", DefinedNameType.Reserved);
			nameDic.Add("TO", DefinedNameType.Reserved);
			nameDic.Add("INT", DefinedNameType.Reserved);
			nameDic.Add("STR", DefinedNameType.Reserved);
			nameDic.Add("REFFUNC", DefinedNameType.Reserved);
			nameDic.Add("STATIC", DefinedNameType.Reserved);
			nameDic.Add("DYNAMIC", DefinedNameType.Reserved);
			nameDic.Add("GLOBAL", DefinedNameType.Reserved);
			nameDic.Add("PRIVATE", DefinedNameType.Reserved);
			nameDic.Add("SAVEDATA", DefinedNameType.Reserved);
			nameDic.Add("CHARADATA", DefinedNameType.Reserved);//CHARDATAfrom変更
			nameDic.Add("REF", DefinedNameType.Reserved);
			nameDic.Add("__DEBUG__", DefinedNameType.Reserved);
			nameDic.Add("__SKIP__", DefinedNameType.Reserved);
			nameDic.Add("_", DefinedNameType.Reserved);
			instructionDic = FunctionIdentifier.GetInstructionNameDic();

			varTokenDic = varData.GetVarTokenDicClone();
			localvarTokenDic = varData.GetLocalvarTokenDic();
			methodDic = FunctionMethodCreator.GetMethodList();
			refmethodDic = new Dictionary<string, UserDefinedRefMethod>();

			foreach(KeyValuePair<string, FunctionMethod> pair in methodDic)
			{
				nameDic.Add(pair.Key, DefinedNameType.SystemMethod);
			}

			foreach (KeyValuePair<string, VariableToken> pair in varTokenDic)
			{
				//RANDが衝突している
				//1808a3 GLOBAL,PRIVATEも
				//1808beta009 REFも
				if (!nameDic.ContainsKey(pair.Key)) 
					nameDic.Add(pair.Key, DefinedNameType.SystemVariable);
			}

			foreach (KeyValuePair<string, VariableLocal> pair in localvarTokenDic)
			{
				nameDic.Add(pair.Key, DefinedNameType.SystemVariable);
			}

			foreach (KeyValuePair<string, FunctionIdentifier> pair in instructionDic)
			{
				//Methodと被る
				//1808a3 SAVEDATAも
				if (!nameDic.ContainsKey(pair.Key))
					nameDic.Add(pair.Key, DefinedNameType.SystemInstrument);
			}
		}
		
		//public void SetSystemInstrumentName(List<string> names)
		//{
		//}
		
		public void CheckUserLabelName(ref string errMes, ref int warnLevel, bool isFunction, string labelName)
		{
			if (labelName.Length == 0)
			{
				errMes = "ラベル名がdoes not exist";
				warnLevel = 2;
				return;
			}
			//1.721 記号をサポートしnot方向に変更
			if (labelName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "ラベル名" + labelName + "に\"_\"以outの記号が含まれています";
				warnLevel = 1;
				return;
			}
			if (char.IsDigit(labelName[0]) && (labelName[0].ToString()).Length == LangManager.GetStrlenLang(labelName[0].ToString()))
			{
                errMes = "ラベル名" + labelName + "が半角数字from始まっています";
				warnLevel = 0;
				return;
			}
			if (!isFunction || !Config.WarnFunctionOverloading)
				return;

            DefinedNameType nametype = DefinedNameType.None;
			if (!nameDic.TryGetValue(labelName, out nametype))
				return;
            else
			{
				switch (nametype)
				{
					case DefinedNameType.Reserved:
						if (Config.AllowFunctionOverloading)
						{
							errMes = "function名" + labelName + "はEmueraの予約語と衝突しています.Emuera専for構文の構文parseに支障をきたす恐れがあります";
							warnLevel = 1;
						}
						else
						{
							errMes = "function名" + labelName + "はEmueraの予約語です";
							warnLevel = 2;
						}
						break;
					case DefinedNameType.SystemMethod:
						if (Config.AllowFunctionOverloading)
						{
							errMes = "function名" + labelName + "はEmueraの式insidefunctionをabove書きします";
							warnLevel = 1;
						}
						else
						{
							errMes = "function名" + labelName + "はEmueraの式insidefunction名as使われています";
							warnLevel = 2;
						}
						break;
					case DefinedNameType.SystemVariable:
						errMes = "function名" + labelName + "はEmueraのvariableで使われています";
						warnLevel = 1;
						break;
					case DefinedNameType.SystemInstrument:
						errMes = "function名" + labelName + "はEmueraのvariableもしくは命令で使われています";
						warnLevel = 1;
						break;
					case DefinedNameType.UserMacro:
						//字句parseがうまくいっていれば本来あり得notはず
						errMes = "function名" + labelName + "はマクロにuseされています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "function名" + labelName + "は参照typefunctionの名称にuseされています";
						warnLevel = 2;
						break;
				}
			}
		}
		
		public void CheckUserVarName(ref string errMes, ref int warnLevel, string varName)
		{
			//if (varName.Length == 0)
			//{
			//    errMes = "variable名がdoes not exist";
			//    warnLevel = 2;
			//    return;
			//}
			//1.721 記号をサポートしnot方向に変更
			if (varName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "variable名" + varName + "に\"_\"以outの記号が含まれています";
				warnLevel = 2;
				return;
			}
            //if (char.IsDigit(varName[0]))
            //{
            //    errMes = "variable名" + varName + "が半角数字from始まっています";
            //    warnLevel = 2;
            //    return;
            //}

            DefinedNameType nametype = DefinedNameType.None;
			if (nameDic.TryGetValue(varName, out nametype))
			{
				switch (nametype)
				{
					case DefinedNameType.Reserved:
						errMes = "variable名" + varName + "はEmueraの予約語です";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//代入文が使えなくなるbecauseに命令名との衝突は致命的.
						errMes = "variable名" + varName + "はEmueraの命令名as使われています";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemVariable:
						errMes = "variable名" + varName + "はEmueraのvariable名as使われています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						errMes = "variable名" + varName + "は既にマクロ名にuseされています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						errMes = "variable名" + varName + "はユーザーdefinitionの広域variable名にuseされています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "variable名" + varName + "は参照typefunctionの名称にuseされています";
						warnLevel = 2;
						break;
				}
			}
		}

		public void CheckUserMacroName(ref string errMes, ref int warnLevel, string macroName)
		{
			if (macroName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "マクロ名" + macroName + "に\"_\"以outの記号が含まれています";
				warnLevel = 2;
				return;
			}
            DefinedNameType nametype = DefinedNameType.None;
			if (nameDic.TryGetValue(macroName, out nametype))
			{
				switch (nametype)
				{
					case DefinedNameType.Reserved:
						errMes = "マクロ名" + macroName + "はEmueraの予約語です";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//命令名をabove書きしたwhenが面倒なのでとりあえずallowしnot
						errMes = "マクロ名" + macroName + "はEmueraの命令名as使われています";
						warnLevel = 2;
						break;
					case DefinedNameType.SystemVariable:
						//別にabove書きしてもいいがとりあえずallowしnotでおく.いずれ解放doかもしれnot
						errMes = "マクロ名" + macroName + "はEmueraのvariable名as使われています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						errMes = "マクロ名" + macroName + "は既にマクロ名にuseされています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						errMes = "マクロ名" + macroName + "はユーザーdefinitionの広域variable名にuseされています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "マクロ名" + macroName + "は参照typefunctionの名称にuseされています";
						warnLevel = 2;
						break;
				}
			}
		}

		public void CheckUserPrivateVarName(ref string errMes, ref int warnLevel, string varName)
		{
			if (varName.Length == 0)
			{
				errMes = "variable名がdoes not exist";
				warnLevel = 2;
				return;
			}
			//1.721 記号をサポートしnot方向に変更
			if (varName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = "variable名" + varName + "に\"_\"以outの記号が含まれています";
				warnLevel = 2;
				return;
			}
			if (char.IsDigit(varName[0]))
			{
				errMes = "variable名" + varName + "が半角数字from始まっています";
				warnLevel = 2;
				return;
			}
            DefinedNameType nametype = DefinedNameType.None;
			if(nameDic.TryGetValue(varName, out nametype))
			{
				switch(nametype)
				{
					case DefinedNameType.Reserved:
						errMes = "variable名" + varName + "はEmueraの予約語です";
						warnLevel = 2;
						return;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//代入文が使えなくなるbecauseに命令名との衝突は致命的.
						errMes = "variable名" + varName + "はEmueraの命令名as使われています";
						warnLevel = 2;
						return;
					case DefinedNameType.SystemVariable:
						//systemvariableのabove書きは不可
                        errMes = "variable名" + varName + "はEmueraのvariable名as使われています";
                        warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						//字句parseがうまくいっていれば本来あり得notはず
						errMes = "variable名" + varName + "はマクロにuseされています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						//広域variableのabove書きはprohibitedしておく
						errMes = "variable名" + varName + "はユーザーdefinitionの広域variable名にuseされています";
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = "variable名" + varName + "は参照typefunctionの名称にuseされています";
						warnLevel = 2;
						break;
                }
			}
			privateDimList.Add(varName);
		}
		#endregion

		#region header.erb
		//1807 ErbLoaderにmove
		Dictionary<string, DefineMacro> macroDic = new Dictionary<string, DefineMacro>();

		internal void AddUseDefinedVariable(VariableToken var)
		{
			varTokenDic.Add(var.Name, var);
			if (var.IsCharacterData)
			{

			}
			nameDic.Add(var.Name, DefinedNameType.UserGlobalVariable);
		}
		internal void AddMacro(DefineMacro mac)
		{
			nameDic.Add(mac.Keyword, DefinedNameType.UserMacro);
			macroDic.Add(mac.Keyword, mac);
		}
		internal void AddRefMethod(UserDefinedRefMethod refm)
		{
			refmethodDic.Add(refm.Name, refm);
			nameDic.Add(refm.Name, DefinedNameType.UserRefMethod);
		}
		#endregion

		#region get

		public bool UseMacro()
		{
			return macroDic.Count > 0;
		}

		public DefineMacro GetMacro(string key)
		{
			if (Config.ICVariable)
				key = key.ToUpper();
            DefineMacro dm = null;
            if (macroDic.TryGetValue(key, out dm))
				return dm;
			return null;
		}

		public VariableToken GetVariableToken(string key, string subKey, bool allowPrivate)
		{
			VariableToken ret = null;
            if (Config.ICVariable)
                key = key.ToUpper();
            if (allowPrivate)
			{
				LogicalLine line = GlobalStatic.Process.GetScaningLine();
				if ((line != null) && (line.ParentLabelLine != null))
				{
					ret = line.ParentLabelLine.GetPrivateVariable(key);
					if(ret != null)
					{
						if (subKey != null)
							throw new CodeEE("プライベートvariable" + key + "に対して@が使われました");
						return ret;
					}
				}
			}
            VariableLocal vl = null;
			if (localvarTokenDic.TryGetValue(key, out vl))
			{
				if (vl.IsForbid)
                {
					throw new CodeEE("呼び出されたvariable\"" + key + "\"はsettingにthanuseがprohibitedされています");
                }
				LogicalLine line = GlobalStatic.Process.GetScaningLine();
				if (string.IsNullOrEmpty(subKey))
				{
					//systemのinput待ちduring debugcommandfromLOCALを呼んだとき.
					if ((line == null) || (line.ParentLabelLine == null))
						throw new CodeEE("実lineinsideのfunctionがdoes not existbecause" + key + "を取得又は変更できませんでした");
					subKey = line.ParentLabelLine.LabelName;
				}
				else
				{
					ParserMediator.Warn("コードinsideでローカルvariableを@付きで呼ぶthisは推奨されません(代わりに*.ERHfileの利forを検討してください)", line, 1, false, false);
					if (Config.ICFunction)
						subKey = subKey.ToUpper();
				}
                LocalVariableToken retLocal = vl.GetExistLocalVariableToken(subKey);
                if (retLocal == null)
                    retLocal = vl.GetNewLocalVariableToken(subKey, line.ParentLabelLine);
                return retLocal;
			}
			if (varTokenDic.TryGetValue(key, out ret))
			{
                //一文字variableのprohibitedoptionを考えた名残
                //if (Config.ForbidOneCodeVariable && ret.CanForbid)
                //    throw new CodeEE("settingにthansystem一文字numericvariableのuseがprohibitedされています(呼び出されたvariable:" + ret.Name +")");
                if (ret.IsForbid)
                {
					if(!ret.CanForbid)
						throw new ExeEE("CanForbidでnotvariable\"" + ret.Name +"\"にIsForbidがついている");
                    throw new CodeEE("呼び出されたvariable\"" + ret.Name +"\"はsettingにthanuseがprohibitedされています");
                }
				if (subKey != null)
					throw new CodeEE("ローカルvariableでnotvariable" + key + "に対して@が使われました");
                return ret;
            }
			if (subKey != null)
				throw new CodeEE("@の使い方が不正です");
			return null;
		}

		public FunctionIdentifier GetFunctionIdentifier(string str)
		{
			string key = str;
            if (string.IsNullOrEmpty(key))
                return null;
            if (Config.ICFunction)
				key = key.ToUpper();
			if (instructionDic.TryGetValue(key, out FunctionIdentifier ret))
				return ret;
			else
				return null;
		}

		public List<string> GetOverloadedList(LabelDictionary labelDic)
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, FunctionMethod> pair in methodDic)
			{
				FunctionLabelLine func = labelDic.GetNonEventLabel(pair.Key);
				if (func == null)
					continue;
				if (!func.IsMethod)
					continue;
				list.Add(pair.Key);
			}
			return list;
		}

		public UserDefinedRefMethod GetRefMethod(string codeStr)
		{
			if (Config.ICFunction)
				codeStr = codeStr.ToUpper();
            UserDefinedRefMethod ref_method = null;
			if (refmethodDic.TryGetValue(codeStr, out ref_method))
				return ref_method;
			return null;
		}

		public IOperandTerm GetFunctionMethod(LabelDictionary labelDic, string codeStr, IOperandTerm[] arguments, bool userDefinedOnly)
		{
			if (Config.ICFunction)
				codeStr = codeStr.ToUpper();
			if (arguments == null)//argumentなし,nameのみの探索
			{
                UserDefinedRefMethod ref_method = null;
				if (refmethodDic.TryGetValue(codeStr, out ref_method))
					return new UserDefinedRefMethodNoArgTerm(ref_method);
				return null;
			}
			if ((labelDic != null) && (labelDic.Initialized))
			{
                UserDefinedRefMethod ref_method = null;
                if (refmethodDic.TryGetValue(codeStr, out ref_method))
					return new UserDefinedRefMethodTerm(ref_method, arguments);
				FunctionLabelLine func = labelDic.GetNonEventLabel(codeStr);
				if (func != null)
				{
					if (userDefinedOnly && !func.IsMethod)
					{
						throw new CodeEE("#FUNCTIONが指定されていnotfunction\"@" + func.LabelName + "\"をCALLF系命令で呼び出そうとしました");
					}
					if (func.IsMethod)
					{
						string errMes;
						IOperandTerm ret = UserDefinedMethodTerm.Create(func, arguments, out errMes);
						if(ret == null)
							throw new CodeEE(errMes);
						return ret;
					}
					//1.721 #FUNCTIONがdefinitionされていnotfunctionは組み込みfunctionをabove書きしnot方向に. PANCTION.ERBのRANDとか.
					if (!methodDic.ContainsKey(codeStr))
						throw new CodeEE("#FUNCTIONがdefinitionされていnotfunction(" + func.Position.Filename + ":" + func.Position.LineNo + "line )を式insideで呼び出そうとしました");
				}
			}
			if (userDefinedOnly)
				return null;
			FunctionMethod method = null;
			if (!methodDic.TryGetValue(codeStr, out method))
				return null;
			string errmes = method.CheckArgumentType(codeStr, arguments);
			if (errmes != null)
				throw new CodeEE(errmes);
			return new FunctionMethodTerm(method, arguments);
		}

		//1756 createinside途
		//nameリストをoriginalに何がやりたかったのかを推定してCodeEEを投げる
		//1822 DIMリストの解決during IdentifierNotFoundCodeEEが飛んだcaseにはやり直しのpossible性がexist
		public void ThrowException(string str, bool isFunc)
		{
			string idStr = str;
			if(Config.ICFunction || Config.ICVariable) //片方だけなのは互換性foroptionなのでレアケースのはず.対応しnot.
				idStr = idStr.ToUpper();
			if (disableList.Contains(idStr))
				throw new CodeEE("\"" + str + "\"は#DISABLEが宣言されています");
			if (!isFunc && privateDimList.Contains(idStr))
				throw new IdentifierNotFoundCodeEE("variable\"" + str + "\"はこのfunctioninsideではdefinitionされていません");
            DefinedNameType type = DefinedNameType.None;
            if (nameDic.TryGetValue(idStr, out type))
			{
				switch (type)
				{
					case DefinedNameType.Reserved:
						throw new CodeEE("Emueraの予約語\"" + str + "\"がInvalid 使われ方をしています");
					case DefinedNameType.SystemVariable:
					case DefinedNameType.UserGlobalVariable:
						if (isFunc)
							throw new CodeEE("variable名\"" + str + "\"がfunctionのように使われています");
						break;
					case DefinedNameType.SystemMethod:
					case DefinedNameType.UserRefMethod:
						if (!isFunc)
							throw new CodeEE("function名\"" + str + "\"がvariableのように使われています");
						break;
					case DefinedNameType.UserMacro:
						throw new CodeEE("予期しnotマクロ名\"" + str + "\"です");
					case DefinedNameType.SystemInstrument:
						if (isFunc)
							throw new CodeEE("命令名\"" + str + "\"がfunctionのように使われています");
						else
							throw new CodeEE("命令名\"" + str + "\"がvariableのように使われています");
			
				}
			}
			throw new IdentifierNotFoundCodeEE("\"" + idStr + "\"は解釈できnot識別子です");
		}
		#endregion

        #region util
        public void resizeLocalVars(string key, string subKey, int newSize)
        {
            localvarTokenDic[key].ResizeLocalVariableToken(subKey, newSize);
        }

        public int getLocalDefaultSize(string key)
        {
            return localvarTokenDic[key].GetDefaultSize();
        }

		public bool getLocalIsForbid(string key)
		{
			return localvarTokenDic[key].IsForbid;
		}
        public bool getVarTokenIsForbid(string key)
        {
            VariableLocal vlocal = null;
            if (localvarTokenDic.TryGetValue(key, out vlocal))
                return vlocal.IsForbid;
            VariableToken var = null;
            varTokenDic.TryGetValue(key, out var);
            if (var != null)
                return var.IsForbid;
            return true;
        }
        #endregion


	}
}