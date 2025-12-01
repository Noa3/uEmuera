using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
//using System.Windows.Forms;
//using System.Drawing;
using MinorShift.Emuera.Sub;
using System.Text.RegularExpressions;
using MinorShift.Emuera.GameData.Expression;
using uEmuera.Drawing;

namespace MinorShift.Emuera
{
	/// <summary>
	/// Values used throughout the program that are set before Window creation and not changed afterwards
	/// (that was the plan, but it's different now)
	/// 1756 Config -> Renamed to ConfigData
	/// </summary>
	internal sealed class ConfigData
	{
		static string configPath
        { get { return Program.ExeDir + "emuera.config"; } }
		static string configdebugPath
        { get { return Program.DebugDir + "debug.config"; } }

static ConfigData() { }
		private static ConfigData instance = new ConfigData();
		public static ConfigData Instance { get { return instance; } }

		private ConfigData() { setDefault(); }

		//Create a reasonably large array.
		private AConfigItem[] configArray = new AConfigItem[70];
		private AConfigItem[] replaceArray = new AConfigItem[50];
		private AConfigItem[] debugArray = new AConfigItem[20];

		private void setDefault()
		{
			int i = 0;
			configArray[i++] = new ConfigItem<bool>(ConfigCode.IgnoreCase, "大文字小文字の違いignore do", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.UseRenameFile, "_Rename.csvを利fordo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.UseReplaceFile, "_Replace.csvを利fordo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.UseMouse, "マウスをusedo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.UseMenu, "メニューをusedo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.UseDebugCommand, "debugcommandをusedo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.AllowMultipleInstances, "多重起動をallowdo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.AutoSave, "オートsaveをlineなう", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.UseKeyMacro, "keyボードマクロをusedo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SizableWindow, "windowのheightさを可変にdo", true);
			configArray[i++] = new ConfigItem<TextDrawingMode>(ConfigCode.TextDrawingMode, "描画インターフェース", TextDrawingMode.TEXTRENDERER);
			//configArray[i++] = new ConfigItem<bool>(ConfigCode.UseImageBuffer, "イメージバッファをusedo", true);
			configArray[i++] = new ConfigItem<int>(ConfigCode.WindowX, "window幅", 760);
			configArray[i++] = new ConfigItem<int>(ConfigCode.WindowY, "windowheightさ", 480);
			configArray[i++] = new ConfigItem<int>(ConfigCode.WindowPosX, "window位置X", 0);
			configArray[i++] = new ConfigItem<int>(ConfigCode.WindowPosY, "window位置Y", 0);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SetWindowPos, "起動whenのwindow位置を指定do", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.WindowMaximixed, "起動whenにwindowを最大化do", false);
			configArray[i++] = new ConfigItem<int>(ConfigCode.MaxLog, "履歴ログのline数", 5000);
			configArray[i++] = new ConfigItem<int>(ConfigCode.PrintCPerLine, "PRINTCを並べる数", 3);
			configArray[i++] = new ConfigItem<int>(ConfigCode.PrintCLength, "PRINTCの文字数", 25);
			configArray[i++] = new ConfigItem<string>(ConfigCode.FontName, "フォント名", "ＭＳ ゴシック");
			configArray[i++] = new ConfigItem<int>(ConfigCode.FontSize, "フォントサイズ", 18);
			configArray[i++] = new ConfigItem<int>(ConfigCode.LineHeight, "一lineのheightさ", 19);
			configArray[i++] = new ConfigItem<Color>(ConfigCode.ForeColor, "文字色", Color.FromArgb(192, 192, 192));//LIGHTGRAY
			configArray[i++] = new ConfigItem<Color>(ConfigCode.BackColor, "背景色", Color.FromArgb(0, 0, 0));//BLACK
			configArray[i++] = new ConfigItem<Color>(ConfigCode.FocusColor, "選択inside文字色", Color.FromArgb(255, 255, 0));//YELLOW
			configArray[i++] = new ConfigItem<Color>(ConfigCode.LogColor, "履歴文字色", Color.FromArgb(192, 192, 192));//LIGHTGRAY//Color.FromArgb(128, 128, 128);//GRAY
			configArray[i++] = new ConfigItem<int>(ConfigCode.FPS, "フレーム毎秒", 5);
			configArray[i++] = new ConfigItem<int>(ConfigCode.SkipFrame, "最大スキップフレーム数", 3);
			configArray[i++] = new ConfigItem<int>(ConfigCode.ScrollHeight, "スクロールline数", 1);
			configArray[i++] = new ConfigItem<int>(ConfigCode.InfiniteLoopAlertTime, "無限ループwarninguntilのミリ秒数", 5000);
			configArray[i++] = new ConfigItem<int>(ConfigCode.DisplayWarningLevel, "displaydo最低warningレベル", 1);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.DisplayReport, "loadwhenにレポートをdisplaydo", false);
			configArray[i++] = new ConfigItem<ReduceArgumentOnLoadFlag>(ConfigCode.ReduceArgumentOnLoad, "loadwhenにargumentをparsedo", ReduceArgumentOnLoadFlag.NO);
			//configArray[i++] = new ConfigItem<bool>(ConfigCode.ReduceFormattedStringOnLoad, "loadwhenにFORMstringをparsedo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.IgnoreUncalledFunction, "呼び出されなかったfunctionignore do", true);
			configArray[i++] = new ConfigItem<DisplayWarningFlag>(ConfigCode.FunctionNotFoundWarning, "functionが見つfromnotwarningの扱い", DisplayWarningFlag.IGNORE);
			configArray[i++] = new ConfigItem<DisplayWarningFlag>(ConfigCode.FunctionNotCalledWarning, "functionが呼び出されなかったwarningの扱い", DisplayWarningFlag.IGNORE);
			//configArray[i++] = new ConfigItem<List<string>>(ConfigCode.IgnoreWarningFiles, "指定したfileinsideのwarningignore do", new List<string>());
			configArray[i++] = new ConfigItem<bool>(ConfigCode.ChangeMasterNameIfDebug, "debugcommandをuseしたwhenにMASTERのnameを変更do", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.ButtonWrap, "buttonの途insideでlineを折りかえさnot", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SearchSubdirectory, "サブdirectoryを検索do", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SortWithFilename, "loading順をfile名順にソートdo", false);
			configArray[i++] = new ConfigItem<long>(ConfigCode.LastKey, "最終updateコード", 0);
			configArray[i++] = new ConfigItem<int>(ConfigCode.SaveDataNos, "displaydosavedata数", 20);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.WarnBackCompatibility, "eramaker互換性に関dowarningをdisplaydo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.AllowFunctionOverloading, "systemfunctionのabove書きをallowdo", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.WarnFunctionOverloading, "systemfunctionがabove書きされたときwarningをdisplaydo", true);
			configArray[i++] = new ConfigItem<string>(ConfigCode.TextEditor, "関連づけるtextエディタ", "notepad");
            configArray[i++] = new ConfigItem<TextEditorType>(ConfigCode.EditorType, "textエディタcommandライン指定", TextEditorType.USER_SETTING);
			configArray[i++] = new ConfigItem<string>(ConfigCode.EditorArgument, "エディタに渡すline指定argument", "");
			configArray[i++] = new ConfigItem<bool>(ConfigCode.WarnNormalFunctionOverloading, "同名の非eventfunctionが複数definitionされたときwarningdo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiErrorLine, "解釈不possibleなlineがあっても実linedo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiCALLNAME, "CALLNAMEが空stringのwhenにNAMEを代入do", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.UseSaveFolder, "savedataをsavfolderinにcreatedo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiRAND, "擬似variableRANDの仕様をeramakerに合わせる", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiDRAWLINE, "DRAWLINEを常に新しいlineでlineう", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiFunctionNoignoreCase, "function-attributeについては大文字小文字ignore しnot", false); ;
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemAllowFullSpace, "全角スペースをホワイトスペースに含める", true);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemSaveInUTF8, "savedataをUTF-8で保存do", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiLinefeedAs1739, "ver1739以previous 非button折り返しを再現do", false);
            configArray[i++] = new ConfigItem<UseLanguage>(ConfigCode.useLanguage, "in部でusedo東アジア言語", UseLanguage.JAPANESE);
            configArray[i++] = new ConfigItem<bool>(ConfigCode.AllowLongInputByMouse, "ONEINPUT系命令でマウスによる2文字以aboveのinputをallowdo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiCallEvent, "eventfunctionのCALLをallowdo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiSPChara, "SPキャラをusedo", false);
			
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemSaveInBinary, "savedataをバイナリ形式で保存do", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiFuncArgOptional, "ユーザーfunctionのallのargumentの省略をallowdo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.CompatiFuncArgAutoConvert, "ユーザーfunctionのargumentに自動的にTOSTRを補完do", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemIgnoreTripleSymbol, "FORMinsideの三連記号を展開しnot", false);
            configArray[i++] = new ConfigItem<bool>(ConfigCode.TimesNotRigorousCalculation, "TIMESのcalculateをeramakerにあわせる", false);
            //一文字variableのprohibitedoptionを考えた名残
			//configArray[i++] = new ConfigItem<bool>(ConfigCode.ForbidOneCodeVariable, "一文字variableのuseをprohibiteddo", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemNoTarget, "charactervariableのargumentを補完しnot", false);
			configArray[i++] = new ConfigItem<bool>(ConfigCode.SystemIgnoreStringSet, "stringvariableの代入にstring式を強制do", false);

			i = 0;
			debugArray[i++] = new ConfigItem<bool>(ConfigCode.DebugShowWindow, "起動whenにdebugウインドウをdisplaydo", true);
			debugArray[i++] = new ConfigItem<bool>(ConfigCode.DebugWindowTopMost, "debugウインドウを最前面にdisplaydo", true);
			debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowWidth, "debugwindow幅", 400);
			debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowHeight, "debugwindowheightさ", 300);
			debugArray[i++] = new ConfigItem<bool>(ConfigCode.DebugSetWindowPos, "debugwindow位置を指定do", false);
			debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowPosX, "debugwindow位置X", 0);
			debugArray[i++] = new ConfigItem<int>(ConfigCode.DebugWindowPosY, "debugwindow位置Y", 0);

			i = 0;
			replaceArray[i++] = new ConfigItem<string>(ConfigCode.MoneyLabel, "お金の単位", "$");
			replaceArray[i++] = new ConfigItem<bool>(ConfigCode.MoneyFirst, "単位の位置", true);
			replaceArray[i++] = new ConfigItem<string>(ConfigCode.LoadLabel, "起動when簡略display", "Now Loading...");
			replaceArray[i++] = new ConfigItem<int>(ConfigCode.MaxShopItem, "販売アイテム数", 100);
			replaceArray[i++] = new ConfigItem<string>(ConfigCode.DrawLineString, "DRAWLINE文字", "-");
			replaceArray[i++] = new ConfigItem<char>(ConfigCode.BarChar1, "BAR文字1", '*');
			replaceArray[i++] = new ConfigItem<char>(ConfigCode.BarChar2, "BAR文字2", '.');
			replaceArray[i++] = new ConfigItem<string>(ConfigCode.TitleMenuString0, "systemメニュー0", "最初fromはじめる");
			replaceArray[i++] = new ConfigItem<string>(ConfigCode.TitleMenuString1, "systemメニュー1", "loadしてはじめる");
			replaceArray[i++] = new ConfigItem<int>(ConfigCode.ComAbleDefault, "COM_ABLEinitialvalue", 1);
			replaceArray[i++] = new ConfigItem<List<Int64>>(ConfigCode.StainDefault, "汚れのinitialvalue", new List<Int64>(new Int64[] { 0, 0, 2, 1, 8 }));
			replaceArray[i++] = new ConfigItem<string>(ConfigCode.TimeupLabel, "when間切れdisplay", "when間切れ");
			replaceArray[i++] = new ConfigItem<List<Int64>>(ConfigCode.ExpLvDef, "EXPLVのinitialvalue", new List<long>(new Int64[] { 0, 1, 4, 20, 50, 200 }));
			replaceArray[i++] = new ConfigItem<List<Int64>>(ConfigCode.PalamLvDef, "PALAMLVのinitialvalue", new List<long>(new Int64[] { 0, 100, 500, 3000, 10000, 30000, 60000, 100000, 150000, 250000 }));
			replaceArray[i++] = new ConfigItem<Int64>(ConfigCode.pbandDef, "PBANDのinitialvalue", 4);
            replaceArray[i++] = new ConfigItem<Int64>(ConfigCode.RelationDef, "RELATIONのinitialvalue", 0);
		}

        public void Clear()
        {
            configArray = new AConfigItem[70];
            replaceArray = new AConfigItem[50];
            debugArray = new AConfigItem[20];
            setDefault();
        }

		public ConfigData Copy()
		{
			ConfigData config = new ConfigData();
			for (int i = 0; i < configArray.Length; i++)
				if ((this.configArray[i] != null) && (config.configArray[i] != null))
					this.configArray[i].CopyTo(config.configArray[i]);
			for (int i = 0; i < configArray.Length; i++)
				if ((this.configArray[i] != null) && (config.configArray[i] != null))
					this.configArray[i].CopyTo(config.configArray[i]);
			for (int i = 0; i < replaceArray.Length; i++)
				if ((this.replaceArray[i] != null) && (config.replaceArray[i] != null))
					this.replaceArray[i].CopyTo(config.replaceArray[i]);
			return config;
		}

		public Dictionary<ConfigCode,string> GetConfigNameDic()
		{
			Dictionary<ConfigCode, string> ret = new Dictionary<ConfigCode, string>();
			foreach (AConfigItem item in configArray)
			{
				if (item != null)
					ret.Add(item.Code, item.Text);
			}
			return ret;
		}

		public T GetConfigValue<T>(ConfigCode code)
		{
			AConfigItem item = GetItem(code);
            //if ((item != null) && (item is ConfigItem<T>))
				return ((ConfigItem<T>)item).Value;
            //throw new ExeEE("Code or type for GetConfigValue is inappropriate");
		}

#region getitem
		public AConfigItem GetItem(ConfigCode code)
		{
			AConfigItem item = GetConfigItem(code);
            if (item == null)
            {
                item = GetReplaceItem(code);
	            if (item == null)
	            {
	                item = GetDebugItem(code);
	            }
            }
			return item;
		}
		public AConfigItem GetItem(string key)
		{
			AConfigItem item = GetConfigItem(key);
			if (item == null)
			{
				item = GetReplaceItem(key);
	            if (item == null)
	            {
					item = GetDebugItem(key);
	            }
	        }
			return item;
		}

		public AConfigItem GetConfigItem(ConfigCode code)
		{
			foreach (AConfigItem item in configArray)
			{
				if (item == null)
					continue;
				if (item.Code == code)
					return item;
			}
			return null;
		}
		public AConfigItem GetConfigItem(string key)
		{
			foreach (AConfigItem item in configArray)
			{
				if (item == null)
					continue;
				if (item.Name == key)
					return item;
				if (item.Text == key)
					return item;
			}
			return null;
		}

		public AConfigItem GetReplaceItem(ConfigCode code)
		{
			foreach (AConfigItem item in replaceArray)
			{
				if (item == null)
					continue;
				if (item.Code == code)
					return item;
			}
			return null;
		}
		public AConfigItem GetReplaceItem(string key)
		{
			foreach (AConfigItem item in replaceArray)
			{
				if (item == null)
					continue;
				if (item.Name == key)
					return item;
				if (item.Text == key)
					return item;
			}
			return null;
		}
		
		public AConfigItem GetDebugItem(ConfigCode code)
		{
			foreach (AConfigItem item in debugArray)
			{
				if (item == null)
					continue;
				if (item.Code == code)
					return item;
			}
			return null;
		}
		public AConfigItem GetDebugItem(string key)
		{
			foreach (AConfigItem item in debugArray)
			{
				if (item == null)
					continue;
				if (item.Name == key)
					return item;
				if (item.Text == key)
					return item;
			}
			return null;
		}
		
		public SingleTerm GetConfigValueInERB(string text, ref string errMes)
		{
			AConfigItem item = ConfigData.Instance.GetItem(text);
			if(item == null)
			{
				errMes = "String \"" + text + "\" is not a valid config name";
				return null;
			}
			SingleTerm term;
			switch(item.Code)
			{
				//<bool>
				case ConfigCode.AutoSave://"Enable auto save"
				case ConfigCode.MoneyFirst://"Unit position"
					if(item.GetValue<bool>())
						term = new SingleTerm(1);
					else
						term = new SingleTerm(0);
					break;
				//<int>
				case ConfigCode.WindowX:// "Window width"
				case ConfigCode.PrintCPerLine:// "PRINTC count per line"
				case ConfigCode.PrintCLength:// "PRINTC character count"
				case ConfigCode.FontSize:// "Font size"
				case ConfigCode.LineHeight:// "Line height"
				case ConfigCode.SaveDataNos:// "Number of save data to display"
				case ConfigCode.MaxShopItem:// "Shop item count"
				case ConfigCode.ComAbleDefault:// "COM_ABLE initial value"
					term = new SingleTerm(item.GetValue<int>());
					break;
				//<Color>
				case ConfigCode.ForeColor://"Text color"
				case ConfigCode.BackColor://"Background color"
				case ConfigCode.FocusColor://"Selected text color"
				case ConfigCode.LogColor://"History text color"
					{
						Color color = item.GetValue<Color>();
						term = new SingleTerm( ((color.R * 256) + color.G) * 256 + color.B);
					}
					break;

				//<Int64>
				case ConfigCode.pbandDef:// "PBAND initial value"
				case ConfigCode.RelationDef:// "RELATION initial value"
					term = new SingleTerm(item.GetValue<Int64>());
					break;

				//<string>
				case ConfigCode.FontName:// "Font name"
				case ConfigCode.MoneyLabel:// "Money unit"
				case ConfigCode.LoadLabel:// "Startup brief display"
				case ConfigCode.DrawLineString:// "DRAWLINE character"
				case ConfigCode.TitleMenuString0:// "System menu 0"
				case ConfigCode.TitleMenuString1:// "System menu 1"
				case ConfigCode.TimeupLabel:// "Timeout display"
					term = new SingleTerm(item.GetValue<string>());
					break;
				
				//<char>
				case ConfigCode.BarChar1:// "BAR character 1"
				case ConfigCode.BarChar2:// "BAR character 2"
					term = new SingleTerm(item.GetValue<char>().ToString());
					break;
				//<TextDrawingMode>
				case ConfigCode.TextDrawingMode:// "Drawing interface"
					term = new SingleTerm(item.GetValue<TextDrawingMode>().ToString());
					break;
				default:
				{
					errMes = "Getting value of config string \"" + text + "\" is not allowed";
					return null;
				}
			}
			return term;
		}
#endregion


		public bool SaveConfig()
		{
			StreamWriter writer = null;

			try
			{
				writer = new StreamWriter(configPath, false, Config.Encode);
				for (int i = 0; i < configArray.Length; i++)
				{
					AConfigItem item = configArray[i];
					if (item == null)
						continue;
					
					//1806beta001 Deprecated CompatiDRAWLINE, migrated to CompatiLinefeedAs1739
					if (item.Code == ConfigCode.CompatiDRAWLINE)
						continue;
					if ((item.Code == ConfigCode.ChangeMasterNameIfDebug) && (item.GetValue<bool>()))
						continue;
					if ((item.Code == ConfigCode.LastKey) && (item.GetValue<long>() == 0))
						continue;
					//if (item.Code == ConfigCode.IgnoreWarningFiles)
					//{
					//    List<string> files = item.GetValue<List<string>>();
					//    foreach (string filename in files)
					//        writer.WriteLine(item.Text + ":" + filename.ToString());
					//    continue;
					//}
					writer.WriteLine(item.ToString());
				}
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
			return true;
		}

        public bool ReLoadConfig()
        {
            //If the content of _fixed.config has changed, unfixed items might be retained, so release all here first
            foreach (AConfigItem item in configArray)
            {
                if (item == null)
                    continue;
                if (item.Fixed)
                    item.Fixed = false;
            }
            LoadConfig();
            return true;
        }

		public bool LoadConfig()
		{
			Config.ClearFont();
			string defaultConfigPath = Program.CsvDir + "_default.config";
			string fixedConfigPath = Program.CsvDir + "_fixed.config";
			if(!File.Exists(defaultConfigPath))
				defaultConfigPath = Program.CsvDir + "default.config";
			if (!File.Exists(fixedConfigPath))
				fixedConfigPath = Program.CsvDir + "fixed.config";

			loadConfig(defaultConfigPath, false);
			loadConfig(configPath, false);
			loadConfig(fixedConfigPath, true);
			
			Config.SetConfig(this);
			bool needSave = false;
			if (!File.Exists(configPath))
				needSave = true;
			if (Config.CheckUpdate())
			{
				GetItem(ConfigCode.LastKey).SetValue(Config.LastKey);
				needSave = true;
			}
			if (needSave)
				SaveConfig();
            return true;
		}

		private bool loadConfig(string confPath, bool fix)
		{
			if (!File.Exists(confPath))
				return false;
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(confPath))
				return false;

			//加载二进制数据
			var bytes = File.ReadAllBytes(confPath);
			var md5s = GenericUtils.CalcMd5ListForConfig(bytes);

			ScriptPosition pos = null;
			int md5i = 0;
			try
			{
				string line = null;
				//bool defineIgnoreWarningFiles = false;
				while ((line = eReader.ReadLine()) != null)
				{
					var md5 = md5s[md5i++];
					if ((line.Length == 0) || (line[0] == ';'))
						continue;
					pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
					string[] tokens = line.Split(new char[] { ':' });
					if (tokens.Length < 2)
						continue;
                    var token_0 = tokens[0].Trim();
                    AConfigItem item = GetConfigItem(token_0);
                    if(item == null)
                    {
                        token_0 = uEmuera.Utils.SHIFTJIS_to_UTF8(token_0, md5);
                        if(!string.IsNullOrEmpty(token_0))
                            item = GetConfigItem(token_0);
                    }
					if (item != null)
					{
						//1806beta001 Deprecated CompatiDRAWLINE, migrated to CompatiLinefeedAs1739
						if(item.Code == ConfigCode.CompatiDRAWLINE)
						{
							item = GetConfigItem(ConfigCode.CompatiLinefeedAs1739);
						}
						//if ((item.Code == ConfigCode.IgnoreWarningFiles))
						//{ 
						//    if (!defineIgnoreWarningFiles)
						//        (item.GetValue<List<string>>()).Clear();
						//    defineIgnoreWarningFiles = true;
						//    if ((item.Fixed) && (fix))
						//        item.Fixed = false;
						//}
						
						if (item.Code == ConfigCode.TextEditor)
						{
							//Due to path relations, tokens[2] must be used
							if (tokens.Length > 2)
							{
								if (tokens[2].StartsWith("\\"))
									tokens[1] += ":" + tokens[2];
								if (tokens.Length > 3)
								{
									for (int i = 3; i < tokens.Length; i++)
									{
										tokens[1] += ":" + tokens[i];
									}
								}
							}
						}
						if (item.Code == ConfigCode.EditorArgument)
						{
							//Some editors require half-width space in arguments, so handle separately
							((ConfigItem<string>)item).Value = tokens[1];
							continue;
						}
                        if (item.Code == ConfigCode.MaxLog && Program.AnalysisMode)
                        {
                            //In analysis mode, overwrite this to ensure sufficient length
                            tokens[1] = "10000";
                        }
						if ((item.TryParse(tokens[1])) && (fix))
							item.Fixed = true;
					}
#if UEMUERA_DEBUG
					//else
					//	throw new Exception("configfileが変");
#endif
				}
			}
			catch (EmueraException ee)
			{
				ParserMediator.ConfigWarn(ee.Message, pos, 1, null);
			}
			catch (Exception exc)
			{
				ParserMediator.ConfigWarn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
			}
			finally { eReader.Dispose(); }
			return true;
		}

#region replace
		// 1.52a modification (config processing for unit replacement and prefix/suffix)
		public void LoadReplaceFile(string filename)
		{
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(filename))
				return;
			ScriptPosition pos = null;
			try
			{
				string line = null;
				while ((line = eReader.ReadLine()) != null)
				{
					if ((line.Length == 0) || (line[0] == ';'))
						continue;
					pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
                    string[] tokens = line.Split(new char[] { ',', ':' });
					if (tokens.Length < 2)
						continue;
                    string itemName = tokens[0].Trim();
                    tokens[1] = line.Substring(tokens[0].Length + 1);
                    if (string.IsNullOrEmpty(tokens[1].Trim()))
                        continue;
                    AConfigItem item = GetReplaceItem(itemName);
                    if (item != null)
                        item.TryParse(tokens[1]);
				}
			}
			catch (EmueraException ee)
			{
				ParserMediator.Warn(ee.Message, pos, 1);
			}
			catch (Exception exc)
			{
				ParserMediator.Warn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
			}
			finally { eReader.Dispose(); }
		}

#endregion 

#region debug


		public bool SaveDebugConfig()
		{
			StreamWriter writer = null;
			try
			{
				writer = new StreamWriter(configdebugPath, false, Config.Encode);
				for (int i = 0; i < debugArray.Length; i++)
				{
					AConfigItem item = debugArray[i];
					if (item == null)
						continue;
					writer.WriteLine(item.ToString());
				}
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
			return true;
		}
		
		public bool LoadDebugConfig()
		{
			if (!File.Exists(configdebugPath))
				goto err;
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(configdebugPath))
				goto err;
			ScriptPosition pos = null;
			try
			{
				string line = null;
				while ((line = eReader.ReadLine()) != null)
				{
					if ((line.Length == 0) || (line[0] == ';'))
						continue;
					pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
					string[] tokens = line.Split(new char[] { ':' });
					if (tokens.Length < 2)
						continue;
					AConfigItem item = GetDebugItem(tokens[0].Trim());
					if (item != null)
					{
						item.TryParse(tokens[1]);
					}
#if UEMUERA_DEBUG
					//else
					//	throw new Exception("configfileが変");
#endif
				}
			}
			catch (EmueraException ee)
			{
				ParserMediator.ConfigWarn(ee.Message, pos, 1, null);
				goto err;
			}
			catch (Exception exc)
			{
				ParserMediator.ConfigWarn(exc.GetType().ToString() + ":" + exc.Message, pos, 1, exc.StackTrace);
				goto err;
			}
			finally { eReader.Dispose(); }
			Config.SetDebugConfig(this);
            return true;
		err:
			Config.SetDebugConfig(this);
			return false;
		}

#endregion
	}
}