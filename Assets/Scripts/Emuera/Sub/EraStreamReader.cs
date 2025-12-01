using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MinorShift.Emuera.Sub
{
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
			//そんなおline儀の悪いthisはしていnot
			//if (disposed)
			//    throw new ExeEE("破棄したobjectを再利forしようとした");
			//if ((reader != null) || (stream != null) || (filepath != null))
			//    throw new ExeEE("useinsideのobjectを別for途に再利forしようとした");
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
		/// next 有効なlineを読む.LexicalAnalyzer経由でConfigを参照doのでConfig完成untilつかわnotthis.
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
				//[SKIPSTART]～[SKIPEND]during ここが誤爆doので無効化
				if (!disabled)
				{
					if (st.Current == '}')
						throw new CodeEE("予期しnotline連結終端記号'}'が見つかりました", new ScriptPosition(filename, curNo));
					if (st.Current == '{')
					{
						if (line.Trim() != "{")
							throw new CodeEE("line連結始端記号'{'のlineに'{'以outの文字を含めるcannot be", new ScriptPosition(filename, curNo));
						break;
					}
				}
				return st;
			}
			//curNoはこのafter加算しnot(始端記号のlineをline番号とdo)
			StringBuilder b = new StringBuilder();
			while (true)
			{
				line = reader.ReadLine();
				nextNo++;
				if (line == null)
				{
					throw new CodeEE("line連結始端記号'{'が使われましたが終端記号'}'not found", new ScriptPosition(filename, curNo));
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
							throw new CodeEE("line連結終端記号'}'のlineに'}'以outの文字を含めるcannot be", new ScriptPosition(filename, nextNo));
						break;
					}
                    //line連結文字なら1字でnotとおかしい,called か,こうしnotとFORMのnumericvariableprocessが誤爆do.
                    //{
                    //A}
                    //みたいetcうしようもnotコードは知ったこっちゃnot
					if (test[0] == '{' && test.Length == 1)
						throw new CodeEE("予期しnotline連結始端記号'{'が見つかりました", new ScriptPosition(filename, nextNo));
				}
				b.Append(line);
				b.Append(" ");
			}
			st.Set(b.ToString());
			LexicalAnalyzer.SkipWhiteSpace(st);
			return st;
		}

		/// <summary>
		/// 直before 読んだlineのline番号
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
		#region IDisposable メンバ

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
