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
	//1756 Newly created.
	//Also remembers used names and detects conflicts.
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
			//'_' is OK
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
			//Register reserved words. A group of names whose appearance in an expression would break syntax analysis.
			//However, there is nothing to worry about for eramaker scripts. The same applies to words that do not appear in expressions.
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
			nameDic.Add("CHARADATA", DefinedNameType.Reserved);//Changed from CHARDATA
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
				//RAND conflicts
				//1808a3 Also GLOBAL and PRIVATE
				//1808beta009 Also REF
				if (!nameDic.ContainsKey(pair.Key)) 
					nameDic.Add(pair.Key, DefinedNameType.SystemVariable);
			}

			foreach (KeyValuePair<string, VariableLocal> pair in localvarTokenDic)
			{
				nameDic.Add(pair.Key, DefinedNameType.SystemVariable);
			}

			foreach (KeyValuePair<string, FunctionIdentifier> pair in instructionDic)
			{
				//Overlaps with Method
				//1808a3 Also SAVEDATA
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
				errMes = GameMessages.T("No label name.");
				warnLevel = 2;
				return;
			}
			//1.721 Changed to not support symbols
			if (labelName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = GameMessages.T("Label name ") + labelName + GameMessages.T(" contains a symbol other than \"_\".");
				warnLevel = 1;
				return;
			}
			if (char.IsDigit(labelName[0]) && (labelName[0].ToString()).Length == LangManager.GetStrlenLang(labelName[0].ToString()))
			{
                errMes = GameMessages.T("Label name ") + labelName + GameMessages.T(" starts with a half-width digit.");
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
							errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" conflicts with an Emuera reserved word and may interfere with the syntax analysis of Emuera-specific syntax.");
							warnLevel = 1;
						}
						else
						{
							errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" is an Emuera reserved word.");
							warnLevel = 2;
						}
						break;
					case DefinedNameType.SystemMethod:
						if (Config.AllowFunctionOverloading)
						{
							errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" overrides an Emuera built-in function.");
							warnLevel = 1;
						}
						else
						{
							errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" is already used as an Emuera built-in function name.");
							warnLevel = 2;
						}
						break;
					case DefinedNameType.SystemVariable:
						errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" is used as an Emuera variable.");
						warnLevel = 1;
						break;
					case DefinedNameType.SystemInstrument:
						errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" is used as an Emuera variable or command.");
						warnLevel = 1;
						break;
					case DefinedNameType.UserMacro:
						//Should be impossible if lexical analysis works correctly
						errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" is used as a macro name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = GameMessages.T("Function name ") + labelName + GameMessages.T(" is used as the name of a reference-type function.");
						warnLevel = 2;
						break;
				}
			}
		}
		
		public void CheckUserVarName(ref string errMes, ref int warnLevel, string varName)
		{
			//if (varName.Length == 0)
			//{
			//    errMes = "No variable name.";
			//    warnLevel = 2;
			//    return;
			//}
			//1.721 Changed to not support symbols
			if (varName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" contains a symbol other than \"_\".");
				warnLevel = 2;
				return;
			}
            //if (char.IsDigit(varName[0]))
            //{
            //    errMes = "Variable name " + varName + " starts with a half-width digit.";
            //    warnLevel = 2;
            //    return;
            //}

            DefinedNameType nametype = DefinedNameType.None;
			if (nameDic.TryGetValue(varName, out nametype))
			{
				switch (nametype)
				{
					case DefinedNameType.Reserved:
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is an Emuera reserved word.");
						warnLevel = 2;
						break;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//Collision with a command name is fatal because assignment statements become unusable.
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as an Emuera command name.");
						warnLevel = 2;
						break;
					case DefinedNameType.SystemVariable:
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as an Emuera variable name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is already used as a macro name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as a user-defined global variable name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as the name of a reference-type function.");
						warnLevel = 2;
						break;
				}
			}
		}

		public void CheckUserMacroName(ref string errMes, ref int warnLevel, string macroName)
		{
			if (macroName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = GameMessages.T("Macro name ") + macroName + GameMessages.T(" contains a symbol other than \"_\".");
				warnLevel = 2;
				return;
			}
            DefinedNameType nametype = DefinedNameType.None;
			if (nameDic.TryGetValue(macroName, out nametype))
			{
				switch (nametype)
				{
					case DefinedNameType.Reserved:
						errMes = GameMessages.T("Macro name ") + macroName + GameMessages.T(" is an Emuera reserved word.");
						warnLevel = 2;
						break;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//Overwriting a command name would be troublesome, so it is not allowed for now
						errMes = GameMessages.T("Macro name ") + macroName + GameMessages.T(" is used as an Emuera command name.");
						warnLevel = 2;
						break;
					case DefinedNameType.SystemVariable:
						//Overwriting would be acceptable, but it is not allowed for now. It may be permitted in the future
						errMes = GameMessages.T("Macro name ") + macroName + GameMessages.T(" is used as an Emuera variable name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						errMes = GameMessages.T("Macro name ") + macroName + GameMessages.T(" is already used as a macro name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						errMes = GameMessages.T("Macro name ") + macroName + GameMessages.T(" is used as a user-defined global variable name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = GameMessages.T("Macro name ") + macroName + GameMessages.T(" is used as the name of a reference-type function.");
						warnLevel = 2;
						break;
				}
			}
		}

		public void CheckUserPrivateVarName(ref string errMes, ref int warnLevel, string varName)
		{
			if (varName.Length == 0)
			{
				errMes = GameMessages.T("No variable name.");
				warnLevel = 2;
				return;
			}
			//1.721 Changed to not support symbols
			if (varName.IndexOfAny(badSymbolAsIdentifier) >= 0)
			{
				errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" contains a symbol other than \"_\".");
				warnLevel = 2;
				return;
			}
			if (char.IsDigit(varName[0]))
			{
				errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" starts with a half-width digit.");
				warnLevel = 2;
				return;
			}
            DefinedNameType nametype = DefinedNameType.None;
			if(nameDic.TryGetValue(varName, out nametype))
			{
				switch(nametype)
				{
					case DefinedNameType.Reserved:
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is an Emuera reserved word.");
						warnLevel = 2;
						return;
					case DefinedNameType.SystemInstrument:
					case DefinedNameType.SystemMethod:
						//Collision with a command name is fatal because assignment statements become unusable.
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as an Emuera command name.");
						warnLevel = 2;
						return;
					case DefinedNameType.SystemVariable:
						//System variables cannot be overwritten
                        errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as an Emuera variable name.");
                        warnLevel = 2;
						break;
					case DefinedNameType.UserMacro:
						//Should be impossible if lexical analysis works correctly
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as a macro name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserGlobalVariable:
						//Overwriting global variables is prohibited
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as a user-defined global variable name.");
						warnLevel = 2;
						break;
					case DefinedNameType.UserRefMethod:
						errMes = GameMessages.T("Variable name ") + varName + GameMessages.T(" is used as the name of a reference-type function.");
						warnLevel = 2;
						break;
                }
			}
			privateDimList.Add(varName);
		}
		#endregion

		#region header.erb
		//1807 Moved to ErbLoader
		Dictionary<string, DefineMacro> macroDic = new Dictionary<string, DefineMacro>();

		internal void AddUseDefinedVariable(VariableToken var)
		{
			// Check if variable already exists to avoid duplicate key exception
			if (varTokenDic.ContainsKey(var.Name))
			{
	#if UNITY_EDITOR
				UnityEngine.Debug.LogWarning($"[IdentifierDictionary] Variable '{var.Name}' already defined, skipping duplicate");
#endif
				return;
			}
			
			// Also check nameDic to avoid conflicts
			if (nameDic.ContainsKey(var.Name))
			{
#if UNITY_EDITOR
				// Suppress the warning when the name was legitimately registered by
				// AddIntegerMacro for a #DIM CONST declaration.  In that code path the
				// macro is registered first and AddUseDefinedVariable is called right
				// after — the "conflict" is intentional and the variable doesn't need
				// runtime storage.
				DefinedNameType existingType = DefinedNameType.None;
				nameDic.TryGetValue(var.Name, out existingType);
				if (existingType != DefinedNameType.UserMacro)
					UnityEngine.Debug.LogWarning($"[IdentifierDictionary] Name '{var.Name}' already registered in nameDic, skipping");
#endif
				return;
			}
			
			varTokenDic.Add(var.Name, var);
			if (var.IsCharacterData)
			{

			}
			nameDic.Add(var.Name, DefinedNameType.UserGlobalVariable);
		}
		internal void AddMacro(DefineMacro mac)
		{
			// Check for duplicates to avoid ArgumentException
			if (nameDic.ContainsKey(mac.Keyword))
			{
	#if UNITY_EDITOR
				UnityEngine.Debug.LogWarning($"[IdentifierDictionary] Macro '{mac.Keyword}' already defined in nameDic, skipping");
#endif
				return;
			}
			if (macroDic.ContainsKey(mac.Keyword))
			{
	#if UNITY_EDITOR
				UnityEngine.Debug.LogWarning($"[IdentifierDictionary] Macro '{mac.Keyword}' already defined in macroDic, skipping");
#endif
				return;
			}
			
			nameDic.Add(mac.Keyword, DefinedNameType.UserMacro);
			macroDic.Add(mac.Keyword, mac);
		}

		/// <summary>
		/// Adds a simple integer constant macro (equivalent to #DEFINE NAME value)
		/// </summary>
		/// <param name="name">The macro name</param>
		/// <param name="value">The integer value</param>
		internal void AddIntegerMacro(string name, long value)
		{
			string originalName = name;
			if (Config.ICVariable)
				name = name.ToUpper();
			
			// Check if name is already registered
			DefinedNameType existingType = DefinedNameType.None;
			if (nameDic.TryGetValue(name, out existingType))
			{
				if (existingType == DefinedNameType.SystemVariable)
				{
					// Standard Emuera behaviour: a #DIM CONST in an ERH file may shadow a
					// system variable of the same name (e.g. キャラクタ数上限 = 165).
					// The macro value takes precedence during constant evaluation so that
					// derived CONSTs like OBJ_ID_LAST = キャラクタ数上限 resolve correctly.
					WordCollection wcOvr = new WordCollection();
					wcOvr.Add(new LiteralIntegerWord(value));
					DefineMacro macOvr = new DefineMacro(name, wcOvr, 0);
					nameDic[name]   = DefinedNameType.UserMacro; // replace, not Add
					macroDic[name]  = macOvr;                   // replace or insert
#if UNITY_EDITOR
					UnityEngine.Debug.Log($"[IdentifierDictionary] System variable '{originalName}' shadowed by CONST macro = {value}");
#endif
					return;
				}
#if UNITY_EDITOR
				UnityEngine.Debug.LogWarning($"[IdentifierDictionary] Macro '{originalName}' already defined, skipping");
#endif
				return;
			}
				
			WordCollection wc = new WordCollection();
			wc.Add(new LiteralIntegerWord(value));
			DefineMacro mac = new DefineMacro(name, wc, 0);
		nameDic.Add(mac.Keyword, DefinedNameType.UserMacro);
		macroDic.Add(mac.Keyword, mac);
		}

		/// <summary>
		/// Adds a simple string constant macro (equivalent to #DEFINE NAME "value").
		/// Used to register #DIMS CONST declarations as macros so dependent DIMS CONST
		/// declarations that concatenate them can resolve the values at compile time.
		/// </summary>
		internal void AddStringMacro(string name, string value)
		{
			string originalName = name;
			if (Config.ICVariable)
				name = name.ToUpper();

			DefinedNameType existingType = DefinedNameType.None;
			if (nameDic.TryGetValue(name, out existingType))
			{
				if (existingType == DefinedNameType.SystemVariable)
				{
					WordCollection wcOvr = new WordCollection();
					wcOvr.Add(new LiteralStringWord(value));
					DefineMacro macOvr = new DefineMacro(name, wcOvr, 0);
					nameDic[name]  = DefinedNameType.UserMacro;
					macroDic[name] = macOvr;
					return;
				}
#if UNITY_EDITOR
				UnityEngine.Debug.LogWarning($"[IdentifierDictionary] String macro '{originalName}' already defined, skipping");
#endif
				return;
			}

			WordCollection wc = new WordCollection();
			wc.Add(new LiteralStringWord(value));
			DefineMacro mac = new DefineMacro(name, wc, 0);
			nameDic.Add(mac.Keyword, DefinedNameType.UserMacro);
			macroDic.Add(mac.Keyword, mac);
		}

		/// <summary>
		/// Initializes predefined constants commonly used in ERA game variants.
		/// These constants are typically expected by games like eraTohoTW that use
		/// Emuera extensions (EE variants).
		/// </summary>
		internal void InitializePredefinedConstants()
		{
			// Note: Most constants should NOT be predefined here because they are defined
			// in the game's ERH files using #DIM CONST. If we add them as macros here,
			// macro expansion will replace the variable name in the CONST definition,
			// causing parsing errors like "#DIM CONST 3000 = 3000" instead of
			// "#DIM CONST MAX_CHARA_NUM = 3000".
			//
			// Only add constants here that are:
			// 1. Not defined anywhere in ERH files, AND
			// 2. Used in array size specifications before any ERH defines them
			//
			// The proper solution is to process ERH files in dependency order or
			// use a two-pass approach (first pass collects CONST definitions,
			// second pass processes DIM lines that use those constants).
			
	#if UNITY_EDITOR
			UnityEngine.Debug.Log("[IdentifierDictionary] Predefined constants initialization skipped - constants are defined in ERH files");
#endif
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
							throw new CodeEE(GameMessages.T("Private variable ") + key + GameMessages.T(" was used with @."));
						return ret;
					}
				}
			}
            VariableLocal vl = null;
			if (localvarTokenDic.TryGetValue(key, out vl))
			{
				if (vl.IsForbid)
                {
					throw new CodeEE(GameMessages.T("The called variable \"") + key + GameMessages.T("\" is forbidden by the settings."));
                }
				LogicalLine line = GlobalStatic.Process.GetScaningLine();
				if (string.IsNullOrEmpty(subKey))
				{
					//When LOCAL is called from a debug command while the system is waiting for input.
					if ((line == null) || (line.ParentLabelLine == null))
						throw new CodeEE(GameMessages.T("Cannot get or change ") + key + GameMessages.T(" because there is no running function."));
					subKey = line.ParentLabelLine.LabelName;
				}
				else
				{
					ParserMediator.Warn(GameMessages.T("Calling a local variable with @ in code is not recommended (consider using *.ERH files instead)"), line, 1, false, false);
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
                //Remnant of the idea for an option to forbid single-character variables
                //if (Config.ForbidOneCodeVariable && ret.CanForbid)
                //    throw new CodeEE("The use of system single-character numeric variables is forbidden by the settings (variable called: " + ret.Name +")");
                if (ret.IsForbid)
                {
					if(!ret.CanForbid)
						throw new ExeEE(GameMessages.T("Variable without CanForbid \"") + ret.Name + GameMessages.T("\" has IsForbid set."));
                    throw new CodeEE(GameMessages.T("The called variable \"") + ret.Name + GameMessages.T("\" is forbidden by the settings."));
                }
				if (subKey != null)
					throw new CodeEE(GameMessages.T("Variable ") + key + GameMessages.T(" that is not a local variable was used with @."));
                return ret;
            }
			if (subKey != null)
				throw new CodeEE(GameMessages.T("Invalid use of @."));
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
				FunctionLabelLine func = FunctionResolver.ResolveNormalLabel(labelDic, pair.Key);
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
			if (arguments == null)//Search by name only, without arguments
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
				FunctionLabelLine func = FunctionResolver.ResolveNormalLabel(labelDic, codeStr);
				if (func != null)
				{
					if (userDefinedOnly && !func.IsMethod)
					{
						throw new CodeEE(GameMessages.T("Attempted to call function \"@") + func.LabelName + GameMessages.T("\" without #FUNCTION specified using a CALLF-family command."));
					}
					if (func.IsMethod)
					{
						string errMes;
						IOperandTerm ret = UserDefinedMethodTerm.Create(func, arguments, out errMes);
						if(ret == null)
							throw new CodeEE(errMes);
						return ret;
					}
					//1.721 Changed so that functions without #FUNCTION do not override built-in functions. E.g. RAND in PANCTION.ERB.
					if (!methodDic.ContainsKey(codeStr))
						throw new CodeEE(GameMessages.T("Attempted to call a function without #FUNCTION defined (") + func.Position.Filename + ":" + func.Position.LineNo + GameMessages.T(") in an expression."));
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

		//1756 Half-finished creation
		//Estimate what was intended from the name list and throw a CodeEE
		//1822 If an IdentifierNotFoundCodeEE is thrown while resolving a DIM list, a retry may be possible
		public void ThrowException(string str, bool isFunc)
		{
			string idStr = str;
			if(Config.ICFunction || Config.ICVariable) //Having only one enabled is a compatibility option, so it should be a rare case. Not handled.
				idStr = idStr.ToUpper();
			if (disableList.Contains(idStr))
				throw new CodeEE("\"" + str + GameMessages.T("\" has #DISABLE declared."));
			if (!isFunc && privateDimList.Contains(idStr))
				throw new IdentifierNotFoundCodeEE(GameMessages.T("Variable \"") + str + GameMessages.T("\" is not defined in this function."));
            DefinedNameType type = DefinedNameType.None;
            if (nameDic.TryGetValue(idStr, out type))
			{
				switch (type)
				{
					case DefinedNameType.Reserved:
						throw new CodeEE(GameMessages.T("Emuera reserved word \"") + str + GameMessages.T("\" is being used in an invalid way."));
					case DefinedNameType.SystemVariable:
					case DefinedNameType.UserGlobalVariable:
						if (isFunc)
							throw new CodeEE(GameMessages.T("Variable name \"") + str + GameMessages.T("\" is being used like a function."));
						break;
					case DefinedNameType.SystemMethod:
					case DefinedNameType.UserRefMethod:
						if (!isFunc)
							throw new CodeEE(GameMessages.T("Function name \"") + str + GameMessages.T("\" is being used like a variable."));
						break;
					case DefinedNameType.UserMacro:
						throw new CodeEE(GameMessages.T("Unexpected macro name \"") + str + GameMessages.T("\"."));
					case DefinedNameType.SystemInstrument:
						if (isFunc)
							throw new CodeEE(GameMessages.T("Command name \"") + str + GameMessages.T("\" is being used like a function."));
						else
							throw new CodeEE(GameMessages.T("Command name \"") + str + GameMessages.T("\" is being used like a variable."));
			
				}
			}
			throw new IdentifierNotFoundCodeEE(string.Format(GameMessages.UnrecognizedIdentifier, idStr));
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

		/// <summary>
		/// Gets all user-defined variable tokens.
		/// Returns only variables defined in ERH files (not system variables).
		/// </summary>
		/// <returns>List of user-defined variable tokens</returns>
		public List<VariableToken> GetAllUserDefinedVariables()
		{
			List<VariableToken> userVars = new List<VariableToken>();
			foreach (var kvp in varTokenDic)
			{
				// Filter to only user-defined variables (not system variables)
				if (kvp.Value is UserDefinedVariableToken)
				{
					userVars.Add(kvp.Value);
				}
			}
			return userVars;
		}
        #endregion


	}
}
