using System;
//using System.Drawing;
using System.Collections.Generic;
//using System.Windows.Forms;
using MinorShift._Library;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Expression;
using System.IO;
using uEmuera;
using uEmuera.Drawing;
using uEmuera.Forms;
using uEmuera.Window;

namespace MinorShift.Emuera
{
	public static class Program
	{
		/*
		コードのstart地point.
		ここでMainWindowを作り,
		MainWindowがProcessを作り,
		ProcessがGameBase-ConstantData-Variableを作る.
		
		
		*.ERBのloading,実line,その他のprocessing Processが,
		入outputをMainWindowが,
		定数の保存をConstantDataが,
		variableの管理をVariableがlineう.
		 
		と言う予定だったが改変doうちに境界が曖昧になってしまった.
		 
		after EmueraConsoleをaddし,それに入outputを担当させるthisに.
        
        1750 DebugConsoleadd
         Debugをall切り離すthisはできnotのでpartEmueraConsoleにも担当させる
		
		TODO: 1819 MainWindow & Consoleのinput-display組とProcess&Dataのdataprocess組だけでも分離したい

		*/
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです.
		/// </summary>
		//[STAThread]
		public static void Main(string[] args)
		{

			ExeDir = Sys.ExeDir;
#if UEMUERA_DEBUG
			//debugMode = true;

			//ExeDirにバリアントのpathを代入dothisでテスト実linebecauseのコード.
			//ローカルpathの末尾には\必須.
			//ローカルpathを記載したcaseは頒布before deletedothis.
			ExeDir = @"";
			
#endif
			CsvDir = ExeDir + "csv/";
			if (!Directory.Exists(CsvDir)){
				CsvDir = ExeDir + "CSV/";
			}
			ErbDir = ExeDir + "erb/";
			if (!Directory.Exists(ErbDir)){
				ErbDir = ExeDir + "ERB/";
			}
			DebugDir = ExeDir + "debug/";
			if (!Directory.Exists(DebugDir)){
				DebugDir = ExeDir + "DEBUG/";
			}
			DatDir = ExeDir + "dat/";
			if (!Directory.Exists(DatDir)){
				DatDir = ExeDir + "DAT/";
			}
			ContentDir = ExeDir + "resources/";
			if (!Directory.Exists(ContentDir)){
				ContentDir = ExeDir + "RESOURCES/";
			}
			//Erroroutputfor
			//1815 .exeが東方板のNGワードに引っかかるそうなので除去
			//ExeName = Path.GetFileNameWithoutExtension(Sys.ExeName);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			ConfigData.Instance.LoadConfig();
            //二重起動のprohibitedかつ二重起動
			//if ((!Config.AllowMultipleInstances) && (Sys.PrevInstance()))
			//{
			//	MessageBox.Show("多重起動をallowdocase,emuera.configを書き換えてbelowさい", "既に起動しています");
			//	return;
			//}
			if (!Directory.Exists(CsvDir))
			{
				MessageBox.Show("\"" + CsvDir + "\" csvfoldernot found", "folderなし");
				return;
			}
			if (!Directory.Exists(ErbDir))
			{
				MessageBox.Show("\"" + ErbDir + "\" erbfoldernot found", "folderなし");
				return;
			}
            int argsStart = 0;
            if ((args.Length > 0)&&(args[0].Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase)))
            {
                argsStart = 1;//debugモードかつparseモードwhenにfirst 1っこ(-DEBUG)を飛ばす
				debugMode = true;
            }
			if(debugMode)
			{
				ConfigData.Instance.LoadDebugConfig();
				if (!Directory.Exists(DebugDir))
				{
					try
					{
						Directory.CreateDirectory(DebugDir);
					}
					catch
					{
						MessageBox.Show("debugfolderのcreateにfailedしました", "folderなし");
						return;
					}
				}
			}
            if (args.Length > argsStart)
            {
                AnalysisFiles = new List<string>();
                for (int i = argsStart; i < args.Length; i++)
                {
                    if (!File.Exists(args[i]) && !Directory.Exists(args[i]))
                    {
                        MessageBox.Show("与えられたfile-folderは存在しません");
                        return;
                    }
                    if ((File.GetAttributes(args[i]) & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        List<KeyValuePair<string, string>> fnames = Config.GetFiles(args[i] + "\\", "*.ERB");
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                        fnames.AddRange(Config.GetFiles(args[i] + "\\", "*.erb"));
#endif
                        for(int j = 0; j < fnames.Count; j++)
                        {
                            AnalysisFiles.Add(fnames[j].Value);
                        }
                    }
                    else
                    {
                        if (Path.GetExtension(args[i]).ToUpper() != ".ERB")
                        {
                            MessageBox.Show("ドロップpossibleなfileはERBfileのみです");
                            return;
                        }
                        AnalysisFiles.Add(args[i]);
                    }
                }
                AnalysisMode = true;
            }
			MainWindow win = null;


			//while (true)
			//{
				StartTime = WinmmTimer.TickCount;
                //using (win = new MainWindow())
                //{
                    win = new MainWindow();
                    Application.Run(win);
				//	Content.AppContents.UnloadContents();
				//	if (!Reboot)
				//		break;

				//	RebootWinState = win.WindowState;
				//	if (win.WindowState == FormWindowState.Normal)
				//	{
				//		RebootClientY = win.ClientSize.Height;
				//		RebootLocation = win.Location;
				//	}
				//	else
				//	{
				//		RebootClientY = 0;
				//		RebootLocation = new Point();
				//	}
				//}
				////条件次第ではParserMediatorが空でnot状態で再起動になるcaseがexist
				//ParserMediator.ClearWarningList();
				//ParserMediator.Initialize(null);
				//GlobalStatic.Reset();
				////GC.Collect();
				//Reboot = false;
				//ConfigData.Instance.LoadConfig();
			//}
		}

		/// <summary>
		/// 実linefileのdirectory.最after \を付けたstring
		/// </summary>
		public static string ExeDir { get; private set; }
		public static string CsvDir { get; private set; }
		public static string ErbDir { get; private set; }
		public static string DebugDir { get; private set; }
		public static string DatDir { get; private set; }
		public static string ContentDir { get; private set; }
		public static string ExeName { get; private set; }

		public static bool Reboot = false;
		//public static int RebootClientX = 0;
		public static int RebootClientY = 0;
        public static FormWindowState RebootWinState = FormWindowState.Normal;
		public static Point RebootLocation;

        public static bool AnalysisMode = false;
        public static List<string> AnalysisFiles = null;

		public static bool debugMode = false;
		public static bool DebugMode { get { return debugMode; } }


		public static uint StartTime { get; private set; }

	}
}