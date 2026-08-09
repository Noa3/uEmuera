using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MinorShift.Emuera.Sub
{
    /// <summary>
    /// Stream reader for ERA script files with UTF-8 encoding support.
    /// Handles line-by-line reading of ERB and ERH files with proper text encoding
    /// and optional file renaming support for loaded scripts.
    /// </summary>
    internal sealed class EraStreamReader : IDisposable
    {
		public EraStreamReader(bool useRename)
		{
			this.useRename = useRename;
		}

		string filepath;
		string filename;
        readonly bool useRename = false;
		int curNo = 0;
		int nextNo = 0;
		StreamReader reader;
		FileStream stream;

		public bool Open(string path)
		{
			return Open(path, Path.GetFileName(path));
		}

		public bool Open(string path, string name)
		{
			//we are not being that naughty
			//if (disposed)
			//    throw new ExeEE("tried to reuse a disposed object");
			//if ((reader != null) || (stream != null) || (filepath != null))
			//    throw new ExeEE("tried to reuse an in-use object for another purpose");
			filepath = path;
			filename = name;
			nextNo = 0;
			curNo = 0;
			try
			{
				stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				reader = new StreamReader(stream, Config.Encode);
			}
			catch
			{
				this.Dispose();
				return false;
			}
			return true;
		}

		public string ReadLine()
		{
			nextNo++;
			curNo = nextNo;
			return reader.ReadLine();
		}

		/// <summary>
		/// Reads the next enabled line. It references Config via LexicalAnalyzer, so do not use it until Config is complete.
		/// </summary>
		public StringStream ReadEnabledLine(bool disabled = false)
		{
			string line = null;
			StringStream st = new StringStream();
			curNo = nextNo;
			while (true)
			{
				line = reader.ReadLine();
				curNo++;
				nextNo++;
				if (line == null)
					return null;
				if (line.Length == 0)
					continue;

				if (useRename && (line.IndexOf("[[") >= 0) && (line.IndexOf("]]") >= 0))
				{
					foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
						line = line.Replace(pair.Key, pair.Value);
				}
                st.Set(line);
				LexicalAnalyzer.SkipWhiteSpace(st);
				if (st.EOS)
					continue;
				//disabled because this would misfire inside [SKIPSTART]-[SKIPEND]
				if (!disabled)
				{
					if (st.Current == '}')
						throw new CodeEE("予期しない行連結終端記号'}'が見つかりました", new ScriptPosition(filename, curNo));
					if (st.Current == '{')
					{
						if (line.Trim() != "{")
							throw new CodeEE("行連結始端記号'{'の行に'{'以外の文字を含めることはできません", new ScriptPosition(filename, curNo));
						break;
					}
				}
				return st;
			}
			//curNo is not incremented from here on (the starting-marker line becomes the line number)
			StringBuilder b = new StringBuilder();
			while (true)
			{
				line = reader.ReadLine();
				nextNo++;
				if (line == null)
				{
					throw new CodeEE("行連結始端記号'{'が使われましたが終端記号'}'が見つかりません", new ScriptPosition(filename, curNo));
				}

				if (useRename && (line.IndexOf("[[") >= 0) && (line.IndexOf("]]") >= 0))
				{
					foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
						line = line.Replace(pair.Key, pair.Value);
				}
				string test = line.TrimStart();
				if (test.Length > 0)
				{
					if (test[0] == '}')
					{
						if (test.Trim() != "}")
							throw new CodeEE("行連結終端記号'}'の行に'}'以外の文字を含めることはできません", new ScriptPosition(filename, nextNo));
						break;
					}
                    //A line-joining character must be a single character, otherwise FOR's numeric-variable processing misfires.
                    //{
                    //A}
                    //we do not care about hopeless code like the following
					if (test[0] == '{' && test.Length == 1)
						throw new CodeEE("予期しない行連結始端記号'{'が見つかりました", new ScriptPosition(filename, nextNo));
				}
				b.Append(line);
				b.Append(" ");
			}
			st.Set(b.ToString());
			LexicalAnalyzer.SkipWhiteSpace(st);
			return st;
		}

		/// <summary>
		/// Line number of the line read most recently
		/// </summary>
		public int LineNo
		{ get { return curNo; } }
		public string Filename
		{
			get
			{
				return filename;
			}
		}
		//public string Filepath
		//{
		//    get
		//    {
		//        return filepath;
		//    }
		//}

		public void Close() { this.Dispose(); }
		bool disposed = false;
		#region IDisposable Members

		public void Dispose()
		{
			if (disposed)
				return;
			if (reader != null)
				reader.Close();
			else if (stream != null)
				stream.Close();
			filepath = null;
			filename = null;
			reader = null;
			stream = null;
			disposed = true;
		}

		#endregion
	}
}
