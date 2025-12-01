using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.GameData
{
	internal sealed class GameBase
	{
		public string ScriptAutherName = "";
		public string ScriptDetail = "";//詳細な説明
		public string ScriptYear = "";
		public string ScriptTitle = "";
		public Int64 ScriptUniqueCode = 0;
		//1.713 訂正.eramakerのバージョンのinitialvalueは1000ではなく0だった
		public Int64 ScriptVersion = 0;//1000;
		//1.713 aboveの変更とあわせて.savedataのバージョンが1000であり,currentのバージョンが未definitionでexistcase,savedataのバージョンをsameとみなす
		public bool ScriptVersionDefined = false;
		public Int64 ScriptCompatibleMinVersion = -1;
        public string Compatible_EmueraVer = "0.000.0.0";

		//1.727 add.Form.Text
		public string ScriptWindowTitle = null;
		public string ScriptVersionText
		{
			get
			{
				StringBuilder versionStr = new StringBuilder();
				versionStr.Append((ScriptVersion / 1000).ToString());
				versionStr.Append(".");
				if ((ScriptVersion % 10) != 0)
					versionStr.Append((ScriptVersion % 1000).ToString("000"));
				else
					versionStr.Append((ScriptVersion % 1000 / 10).ToString("00"));
				return versionStr.ToString();
			}
		}
		public bool UniqueCodeEqualTo(Int64 target)
		{
			//1804 UniqueCode Int64toの拡張に伴い修正
			if (target == 0L)
				return true;
			return target == ScriptUniqueCode;
		}

		public bool CheckVersion(Int64 target)
		{
			if (!ScriptVersionDefined && target != 1000)
				return true;
			if (ScriptCompatibleMinVersion <= target)
				return true;
			return ScriptVersion == target;
		}

		public Int64 DefaultCharacter = -1;
		public Int64 DefaultNoItem = 0;

		private bool tryatoi(string str, out Int64 i)
		{
			if (Int64.TryParse(str, out i))
				return true;
			StringStream st = new StringStream(str);
			StringBuilder sb = new StringBuilder(str.Length);
			while (true)
			{
				if (st.EOS)
					break;
				if (!char.IsNumber(st.Current))
					break;
				sb.Append(st.Current);
				st.ShiftNext();
			}
			if (sb.Length > 0)
				if (Int64.TryParse(sb.ToString(), out i))
					return true;
			return false;
		}

		/// <summary>
		/// GAMEBASEloading.GAMEBASE.csvの存在は必須ではnotのでloadingfailedしたらなかったthisにdo.
		/// </summary>
		/// <param name="basePath"></param>
		/// <returns>loading続linedoなら真,Errorendなら偽</returns>
		public bool LoadGameBaseCsv(string basePath)
		{
            if (!File.Exists(basePath))
            {
                return true;
            }
			ScriptPosition pos = null;
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(basePath))
			{
				//output.PrintLine(eReader.Filename + "のオープンにfailedしました");
				return true;
			}
			try
			{
				StringStream st = null;
				while ((st = eReader.ReadEnabledLine()) != null)
				{
					string[] tokens = st.Substring().Split(',');
					if (tokens.Length < 2)
						continue;
					string param = tokens[1].Trim();
					pos = new ScriptPosition(eReader.Filename, eReader.LineNo);
					switch (tokens[0])
					{
						case "コード":
							if (tryatoi(tokens[1], out ScriptUniqueCode))
							{
								if (ScriptUniqueCode == 0L)
									ParserMediator.Warn("コード:0のsavedataはいかなるコードのscriptfromも読めるdataas扱われます", pos, 0);
							}							
							break;
						case "バージョン":
							ScriptVersionDefined = tryatoi(tokens[1], out ScriptVersion);
							break;
						case "バージョン違い認める":
							tryatoi(tokens[1], out ScriptCompatibleMinVersion);
							break;
						case "最初fromいるキャラ":
							tryatoi(tokens[1], out DefaultCharacter);
							break;
						case "アイテムなし":
							tryatoi(tokens[1], out DefaultNoItem);
							break;
						case "タイトル":
							ScriptTitle = tokens[1];
							break;
						case "作者":
							ScriptAutherName = tokens[1];
							break;
						case "製作年":
							ScriptYear = tokens[1];
							break;
						case "addinfo":
							ScriptDetail = tokens[1];
							break;
						case "windowタイトル":
							ScriptWindowTitle = tokens[1];
							break;
							
                        case "動作に必要なEmueraのバージョン":
                            Compatible_EmueraVer = tokens[1];
                            if (!Regex.IsMatch(Compatible_EmueraVer, @"^\d+\.\d+\.\d+\.\d+$"))
                            {
                                ParserMediator.Warn("バージョン指定を読み取れなかったのでprocessing 省略します", pos, 0);
                                break;
                            }
                            Version curerntVersion = new Version(GlobalStatic.MainWindow.InternalEmueraVer);
                            Version targetVersoin = new Version(Compatible_EmueraVer);
                            if (curerntVersion < targetVersoin)
                            {
                                ParserMediator.Warn("このバリアント動作させるにはVer. " + GlobalStatic.MainWindow.EmueraVerText + "以降のバージョンのEmueraが必要です", pos, 2);
                                return false;
                            }
                            break;
					}
				}
			}
			catch
			{
                ParserMediator.Warn("GAMEBASE.CSVのLoadingにErrorが発生したbecause,読みこみand abortます", pos, 1);
				return true;
			}
			finally
			{
				eReader.Close();
			}
			if (ScriptWindowTitle == null)
			{
				if (string.IsNullOrEmpty(ScriptTitle))
					ScriptWindowTitle = "Emuera";
				else
					ScriptWindowTitle = ScriptTitle + " " + ScriptVersionText;
			}
			return true;
		}
	}





}
