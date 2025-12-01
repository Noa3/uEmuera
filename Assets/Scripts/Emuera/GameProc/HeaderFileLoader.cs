using System;
using System.Collections.Generic;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameProc.Function;
using MinorShift._Library;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Function;

namespace MinorShift.Emuera.GameProc
{
	internal sealed class HeaderFileLoader
	{
		public HeaderFileLoader(EmueraConsole main, IdentifierDictionary idDic, Process proc)
		{
			output = main;
			parentProcess = proc;
			this.idDic = idDic;
		}
		readonly Process parentProcess;
		readonly EmueraConsole output;
		readonly IdentifierDictionary idDic;

		bool noError = true;
		Queue<DimLineWC> dimlines;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="erbDir"></param>
		/// <param name="displayReport"></param>
		/// <returns></returns>
		public bool LoadHeaderFiles(string headerDir, bool displayReport)
		{
			List<KeyValuePair<string, string>> headerFiles = Config.GetFiles(headerDir, "*.ERH");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            headerFiles.AddRange(Config.GetFiles(headerDir, "*.erh"));
#endif
            bool noError = true;
			dimlines = new Queue<DimLineWC>();
			try
			{
				for (int i = 0; i < headerFiles.Count; i++)
				{
					string filename = headerFiles[i].Key;
					string file = headerFiles[i].Value;
					if (displayReport)
						output.PrintSystemLine(filename + "Loading---");
					noError = loadHeaderFile(file, filename);
					if (!noError)
						break;
					//System.Windows.Forms.//Application.DoEvents();
				}
				//Errorが起きてるcaseでも読み込めてる分だけはチェックdo
				if (dimlines.Count > 0)
				{
					//&=でnotと,ここで起きたErrorをキャッチできnot
					noError &= analyzeSharpDimLines();
				}

				dimlines.Clear();
			}
			finally
			{
				ParserMediator.FlushWarningList();
			}
			return noError;
		}


		private bool loadHeaderFile(string filepath, string filename)
		{
			StringStream st;
			ScriptPosition position = null;
			//EraStreamReader eReader = new EraStreamReader(false);
			//1815修正 _rename.csvの適for
			//eramakerEXの仕様的には.ERHに適fordoのはおかしいけど,もうEmueraの仕様になっちゃってるのでしかたnotか
			EraStreamReader eReader = new EraStreamReader(true);

			if (!eReader.Open(filepath, filename))
			{
				throw new CodeEE(eReader.Filename + "のオープンにfailedしました");
				//return false;
			}
			try
			{
				while ((st = eReader.ReadEnabledLine()) != null)
				{
					if (!noError)
						return false;
					position = new ScriptPosition(filename, eReader.LineNo);
					LexicalAnalyzer.SkipWhiteSpace(st);
					if (st.Current != '#')
						throw new CodeEE("ヘッダーのduring #で始まらnotlineがあります", position);
					st.ShiftNext();
					string sharpID = LexicalAnalyzer.ReadSingleIdentifier(st);
					if (sharpID == null)
					{
						ParserMediator.Warn("解釈できnot#lineです", position, 1);
						return false;
					}
					if (Config.ICFunction)
						sharpID = sharpID.ToUpper();
					LexicalAnalyzer.SkipWhiteSpace(st);
					switch (sharpID)
					{
						case "DEFINE":
							analyzeSharpDefine(st, position);
							break;
						case "FUNCTION":
						case "FUNCTIONS":
							analyzeSharpFunction(st, position, sharpID == "FUNCTIONS");
							break;
						case "DIM":
						case "DIMS":
							//1822 #DIMは保留しておいてafterでまとめてやる
							{
								WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
								dimlines.Enqueue(new DimLineWC(wc, sharpID == "DIMS", false, position));
							}
							//analyzeSharpDim(st, position, sharpID == "DIMS");
							break;
						default:
							throw new CodeEE("#" + sharpID + "は解釈できnotプリプロセッサです", position);
					}
				}
			}
			catch (CodeEE e)
			{
				if (e.Position != null)
					position = e.Position;
				ParserMediator.Warn(e.Message, position, 2);
				return false;
			}
			finally
			{
				eReader.Close();
			}
			return true;
		}

		//#define FOO (～～)     id to wc
		//#define BAR($1) (～～)     idwithargs to wc(replaced)
		//#diseble FOOBAR             
		//#dim piyo, i
		//#dims puyo, j
		//static List<string> keywordsList = new List<string>();

		private void analyzeSharpDefine(StringStream st, ScriptPosition position)
		{
			//LexicalAnalyzer.SkipWhiteSpace(st);callbefore lineう.
			string srcID = LexicalAnalyzer.ReadSingleIdentifier(st);
			if (srcID == null)
				throw new CodeEE("置換originalの識別子がdoes not exist", position);
			if (Config.ICVariable)
				srcID = srcID.ToUpper();

            //ここで名称重複判定しnotと,大変なthisになる
            string errMes = "";
            int errLevel = -1;
            idDic.CheckUserMacroName(ref errMes, ref errLevel, srcID);
            if (errLevel >= 0)
            {
                ParserMediator.Warn(errMes, position, errLevel);
                if (errLevel >= 2)
                {
                    noError = false;
                    return;
                }
            }
            
            bool hasArg = st.Current == '(';//argumentを指定docaseには直after (が続いていなければならnot.ホワイトスペースもprohibited.
			//1808a3 代入演算子allow(function宣言for)
			WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
			if (wc.EOL)
			{
				//throw new CodeEE("置換aheadの式がdoes not exist", position);
				//1808a3 空マクロのallow
				DefineMacro nullmac = new DefineMacro(srcID, new WordCollection(), 0);
				idDic.AddMacro(nullmac);
				return;
			}

			List<string> argID = new List<string>();
			if (hasArg)//functiontypeマクロのargumentparse
			{
				wc.ShiftNext();//'('を読み飛ばす
				if (wc.Current.Type == ')')
					throw new CodeEE("functiontypeマクロのargumentを0個にdocannot be", position);
				while (!wc.EOL)
				{
					IdentifierWord word = wc.Current as IdentifierWord;
					if (word == null)
						throw new CodeEE("置換originalのargument指定の書式が間違っています", position);
					word.SetIsMacro();
					string id = word.Code;
					if (argID.Contains(id))
						throw new CodeEE("置換originalのargumentにsame文字が2回以above使われています", position);
					argID.Add(id);
					wc.ShiftNext();
					if (wc.Current.Type == ',')
					{
						wc.ShiftNext();
						continue;
					}
					if (wc.Current.Type == ')')
						break;
					throw new CodeEE("置換originalのargument指定の書式が間違っています", position);
				}
				if (wc.EOL)
					throw new CodeEE("')'が閉じられていません", position);

				wc.ShiftNext();
			}
			if (wc.EOL)
				throw new CodeEE("置換aheadの式がdoes not exist", position);
			WordCollection destWc = new WordCollection();
			while (!wc.EOL)
			{
				destWc.Add(wc.Current);
				wc.ShiftNext();
			}
			if (hasArg)//functiontypeマクロのargumentセット
			{
				while (!destWc.EOL)
				{
					IdentifierWord word = destWc.Current as IdentifierWord;
					if (word == null)
					{
						destWc.ShiftNext();
						continue;
					}
					for (int i = 0; i < argID.Count; i++)
					{
						if (string.Equals(word.Code, argID[i], Config.SCVariable))
						{
							destWc.Remove();
							destWc.Insert(new MacroWord(i));
							break;
						}
					}
					destWc.ShiftNext();
				}
				destWc.Pointer = 0;
			}
			if (hasArg)//1808a3 functiontypeマクロの封印
				throw new CodeEE("functiontypeマクロは宣言できません", position);
			DefineMacro mac = new DefineMacro(srcID, destWc, argID.Count);
			idDic.AddMacro(mac);
		}

		//private void analyzeSharpDim(StringStream st, ScriptPosition position, bool dims)
		//{
		//	//WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
		//	//UserDefinedVariableData data = UserDefinedVariableData.Create(wc, dims, false, position);
		//	//if (data.Reference)
		//	//	throw new NotImplCodeEE();
		//	//VariableToken var = null;
		//	//if (data.CharaData)
		//	//	var = parentProcess.VEvaluator.VariableData.CreateUserDefCharaVariable(data);
		//	//else
		//	//	var = parentProcess.VEvaluator.VariableData.CreateUserDefVariable(data);
		//	//idDic.AddUseDefinedVariable(var);
		//}

		//1822 #DIMだけまとめておいてafterでprocess
		private bool analyzeSharpDimLines()
		{
			bool noError = true;
			bool tryAgain = true;
			while (dimlines.Count > 0)
			{
				int count = dimlines.Count;
				for (int i = 0; i < count; i++)
				{
					DimLineWC dimline = dimlines.Dequeue();
					try
					{
						UserDefinedVariableData data = UserDefinedVariableData.Create(dimline);
						if (data.Reference)
							throw new NotImplCodeEE();
						VariableToken var = null;
						if (data.CharaData)
							var = parentProcess.VEvaluator.VariableData.CreateUserDefCharaVariable(data);
						else
							var = parentProcess.VEvaluator.VariableData.CreateUserDefVariable(data);
						idDic.AddUseDefinedVariable(var);
					}
					catch (IdentifierNotFoundCodeEE e)
					{
						//繰り返すthisで解決do見込みがexistならキューの最after add
						if (tryAgain)
						{
							dimline.WC.Pointer = 0;
							dimlines.Enqueue(dimline);
						}
						else
						{
							ParserMediator.Warn(e.Message, dimline.SC, 2);
							noError = true;
						}
					}
					catch (CodeEE e)
					{
						ParserMediator.Warn(e.Message, dimline.SC, 2);
						noError = false;
					}
				}
				if (dimlines.Count == count)
					tryAgain = false;
			}
			return noError;
		}

		private void analyzeSharpFunction(StringStream st, ScriptPosition position, bool funcs)
		{
			throw new NotImplCodeEE();
			//WordCollection wc = LexicalAnalyzer.Analyse(st, LexEndWith.EoL, LexAnalyzeFlag.AllowAssignment);
			//UserDefinedFunctionData data = UserDefinedFunctionData.Create(wc, funcs, position);
			//idDic.AddRefMethod(UserDefinedRefMethod.Create(data));
		}
	}
}
