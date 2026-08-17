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
		The starting point of the code.
		MainWindow is created here,
		MainWindow creates Process,
		and Process creates GameBase, ConstantData and Variable.
		
		
		Process handles loading and executing *.ERB and other processing,
		MainWindow handles input/output,
		ConstantData handles the storage of constants,
		and Variable handles the management of variables.
		 
		That was the plan, but the boundaries became blurred during remodeling.
		 
		EmueraConsole was added later, and it took charge of the input/output.
        
        1750 DebugConsole added
         Debug cannot be fully separated, so a part of it is also handled by EmueraConsole
		
		TODO: 1819 Want to at least separate the MainWindow & Console input/display group from the Process & Data processing group
		*/
		/// <summary>
		/// The application's main entry point.
		/// </summary>
		//[STAThread]
		public static void Main(string[] args)
		{
			// Register CodePagesEncodingProvider so Encoding.GetEncoding(932) / CP932 / Shift-JIS
			// works on .NET Core and .NET 5+ runtimes (not included by default).
			// On Unity's Mono runtime, I18N.CJK.dll provides CP932 support natively, so this
			// is a no-op there. Uses reflection to avoid a compile-time package dependency.
			try
			{
				// Try fully-qualified assembly name first (NuGet package on .NET Core)
				var t = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages")
				         ?? Type.GetType("System.Text.CodePagesEncodingProvider");
				var prop = t?.GetProperty("Instance",
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				var provider = prop?.GetValue(null) as System.Text.EncodingProvider;
				if (provider != null)
					System.Text.Encoding.RegisterProvider(provider);
			}
			catch { }

			ExeDir = Sys.ExeDir;
#if UEMUERA_DEBUG
			//debugMode = true;

			//Code for testing by assigning a local path to ExeDir.
			//A trailing \ is required at the end of a local path.
			//If a local path is written, remove it before distribution.
			ExeDir = @"";
			
#endif
			CsvDir = ExeDir + "csv/";
			if (!Directory.Exists(CsvDir)){
				CsvDir = ExeDir + "CSV/";
				if (!Directory.Exists(CsvDir)){
					CsvDir = ExeDir + "Csv/";
				}
			}
			ErbDir = ExeDir + "erb/";
			if (!Directory.Exists(ErbDir)){
				ErbDir = ExeDir + "ERB/";
				if (!Directory.Exists(ErbDir)){
					ErbDir = ExeDir + "Erb/";
				}
			}
			DebugDir = ExeDir + "debug/";
			if (!Directory.Exists(DebugDir)){
				DebugDir = ExeDir + "DEBUG/";
				if (!Directory.Exists(DebugDir)){
					DebugDir = ExeDir + "Debug/";
				}
			}
			DatDir = ExeDir + "dat/";
			if (!Directory.Exists(DatDir)){
				DatDir = ExeDir + "DAT/";
				if (!Directory.Exists(DatDir)){
					DatDir = ExeDir + "Dat/";
				}
			}
			ContentDir = ExeDir + "resources/";
			if (!Directory.Exists(ContentDir)){
				ContentDir = ExeDir + "RESOURCES/";
				if (!Directory.Exists(ContentDir)){
					ContentDir = ExeDir + "Resources/";
				}
			}
			//For error output
			//1815 Removed because .exe apparently trips an NG word on the Touhou board
			//ExeName = Path.GetFileNameWithoutExtension(Sys.ExeName);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			ConfigData.Instance.LoadConfig();
            //Forbid and forbid double startup
			//if ((!Config.AllowMultipleInstances) && (Sys.PrevInstance()))
			//{
			//	MessageBox.Show("To allow multiple instances, edit emuera.config", "Already running");
			//	return;
			//}
			if (!Directory.Exists(CsvDir))
			{
				MessageBox.Show("\"" + CsvDir + GameMessages.T("\" csv folder not found"), GameMessages.T("Folder not found"));
				return;
			}
			if (!Directory.Exists(ErbDir))
			{
				MessageBox.Show("\"" + ErbDir + GameMessages.T("\" erb folder not found"), GameMessages.T("Folder not found"));
				return;
			}
            int argsStart = 0;
            if ((args.Length > 0)&&(args[0].Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase)))
            {
                argsStart = 1;//skip the first one (-DEBUG) when in debug mode and parsing mode
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
						MessageBox.Show(GameMessages.T("Failed to create debug folder"), GameMessages.T("Folder not found"));
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
                        MessageBox.Show(GameMessages.T("The specified file or folder does not exist"));
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
                            MessageBox.Show(GameMessages.T("Only ERB files can be dropped"));
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
				////Depending on the conditions, it may restart with a non-empty ParserMediator
				//ParserMediator.ClearWarningList();
				//ParserMediator.Initialize(null);
				//GlobalStatic.Reset();
				////GC.Collect();
				//Reboot = false;
				//ConfigData.Instance.LoadConfig();
			//}
		}

		/// <summary>
		/// Directory of the executable file. A string ending with \
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