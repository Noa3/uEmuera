using System.IO;

namespace MinorShift._Library
{
	public static class Sys
	{
		static Sys()
		{}
        public static void SetWorkFolder(string folder)
        {
            _WorkFolder = folder;
        }
        public static string WorkFolder { get { return _WorkFolder; } }
        private static string _WorkFolder;

        public static void SetSourceFolder(string folder)
        {
            ExeDir = uEmuera.Utils.NormalizePath(_WorkFolder + "/" + folder + "/");
        }
        
		/// <summary>
		/// Path to the executable file
		/// </summary>
		//public static readonly string ExePath;

		/// <summary>
		/// Directory of the executable file. String ending with \
		/// </summary>
		public static string ExeDir { get; private set; }

		/// <summary>
		/// Name of the executable file. Without directory
		/// </summary>
		//public static readonly string ExeName;

		/// <summary>
		/// Prevent duplicate startup. Returns true if an exe with the same name is already running
		/// </summary>
		/// <returns></returns>
		public static bool PrevInstance()
		{
            //string thisProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            //if (System.Diagnostics.Process.GetProcessesByName(thisProcessName).Length > 1)
            //{
            //	return true;
            //}
            return false;
		}
	}
}

