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
		public string ScriptDetail = "";//Detailed description
		public string ScriptYear = "";
		public string ScriptTitle = "";
		public Int64 ScriptUniqueCode = 0;
		//1.713 Correction. eramaker's version default value was 0, not 1000
		public Int64 ScriptVersion = 0;//1000;
		//1.713 Along with the change above. If the save data version is 1000 and the current version is undefined, treat the save data version as the same
		public bool ScriptVersionDefined = false;
		public Int64 ScriptCompatibleMinVersion = -1;
        public string Compatible_EmueraVer = "0.000.0.0";

		//1.727 Added. Form.Text
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
			//1804 Modified in conjunction with the expansion of UniqueCode to Int64
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
		/// Loads GAMEBASE. GAMEBASE.csv does not have to exist, so treat a load failure as if the file did not exist.
		/// </summary>
		/// <param name="basePath"></param>
		/// <returns>True to continue loading, false to end with an error</returns>
		public bool LoadGameBaseCsv(string basePath)
		{
			// Use case-insensitive file resolution for non-Windows systems
			string resolvedPath = uEmuera.Utils.ResolveExistingFilePath(basePath);
			if (string.IsNullOrEmpty(resolvedPath))
				return true;
			ScriptPosition pos = null;
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(resolvedPath))
			{
				//output.PrintLine(eReader.Filename + "Failed to open");
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
									ParserMediator.Warn(GameMessages.T("Save data with code 0 is treated as data readable from scripts with any code"), pos, 0);
							}							
							break;
						case "バージョン":
							ScriptVersionDefined = tryatoi(tokens[1], out ScriptVersion);
							break;
						case "バージョン違い認める":
							tryatoi(tokens[1], out ScriptCompatibleMinVersion);
							break;
						case "最初からいるキャラ":
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
						case "追加情報":
							ScriptDetail = tokens[1];
							break;
						case "ウィンドウタイトル":
							ScriptWindowTitle = tokens[1];
							break;
							
                        case "動作に必要なEmueraのバージョン":
                            Compatible_EmueraVer = tokens[1];
                            if (!Regex.IsMatch(Compatible_EmueraVer, @"^\d+\.\d+\.\d+\.\d+$"))
                            {
                                ParserMediator.Warn(GameMessages.T("The version specification could not be read, so this process is skipped"), pos, 0);
                                break;
                            }
                            Version curerntVersion = new Version(GlobalStatic.MainWindow.InternalEmueraVer);
                            Version targetVersoin = new Version(Compatible_EmueraVer);
                            if (curerntVersion < targetVersoin)
                            {
                                ParserMediator.Warn(GameMessages.T("To run this variant, Emuera version ") + GlobalStatic.MainWindow.EmueraVerText + GameMessages.T(" or later is required"), pos, 2);
                                return false;
                            }
                            break;
					}
				}
			}
			catch
			{
                ParserMediator.Warn(GameMessages.T("An error occurred while loading GAMEBASE.CSV, so loading was interrupted"), pos, 1);
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
