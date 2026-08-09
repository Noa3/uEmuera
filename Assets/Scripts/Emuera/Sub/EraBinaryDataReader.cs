using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MinorShift.Emuera.Sub
{
    /// <summary>
    /// Binary data reader for Emuera save files.
    /// Provides efficient deserialization of game save data including
    /// integers, strings, arrays, and character data.
    /// </summary>
    #region Common reader/writer data
    public enum EraSaveFileType : byte
    {
		Normal = 0x00,
		Global = 0x01,
		Var = 0x02,
		CharVar = 0x03,
	}

	public enum EraSaveDataType : byte
	{
		Int = 0x00,
		IntArray = 0x01,
		IntArray2D = 0x02,
		IntArray3D = 0x03,
		Str = 0x10,
		StrArray = 0x11,
		StrArray2D = 0x12,
		StrArray3D = 0x13,
		//SOC = 0xFD,//start of character data
		Separator = 0xFD,//data separator
		EOC = 0xFE,//end of character data
		EOF = 0xFF,//end of file
	}

	static class Ebdb//magic-number bytes in EraBinaryData
	{
		public const byte Byte = 0xCF;
		public const byte Int16 = 0xD0;//the following 2 bytes form an Int16
		public const byte Int32 = 0xD1;//the following 4 bytes form an Int32
		public const byte Int64 = 0xD2;//the following 8 bytes form an Int64
		public const byte String = 0xD8;//a String follows immediately

		public const byte EoA1 = 0xE0;//data separator (one-dimensional
		public const byte EoA2 = 0xE1;//data separator (two-dimensional
		public const byte Zero = 0xF0;//number of consecutive zeros following
		public const byte ZeroA1 = 0xF1;//number of consecutive empty arrays following (one-dimensional
		public const byte ZeroA2 = 0xF2;//number of consecutive empty arrays following (two-dimensional
		public const byte EoD = 0xFF;//end of variable data
	}

	static class EraBDConst
	{
		//Header is copied from png
		public const UInt64 Header = 0x0A1A0A0D41524589UL;
		public const UInt32 Version1808 = 1808;
		public const UInt32 DataCount = 0;
	}
	#endregion

	/// <summary>
	/// Added in 1808: new data save format
	/// Made abstract in case the format changes in the future
	/// </summary>
	internal abstract class EraBinaryDataReader : IDisposable
	{
		private EraBinaryDataReader() {}
		
		protected EraBinaryDataReader(BinaryReader stream, int ver, UInt32[] buf)
		{
			reader = stream;
			version = ver;
			data = buf;
		}
		protected BinaryReader reader = null;
		protected readonly int version = 0;
		protected readonly UInt32[] data = null;

		public abstract int ReaderVersion { get; }
		/// <summary>
		/// Create a reader from a FileStream
		/// Returns null if the file is invalid, does not throw an exception
		/// </summary>
		/// <param name="fs"></param>
		/// <returns></returns>
		public static EraBinaryDataReader CreateReader(FileStream fs)
		{
			try
			{
				if ((fs == null) || (fs.Length < 16))
					return null;
				BinaryReader reader = new BinaryReader(fs, Encoding.Unicode);

				if (reader.ReadUInt64() != EraBDConst.Header)
					return null;
				int version = (int)reader.ReadUInt32();
				int datacount = (int)reader.ReadUInt32();
				UInt32[] data = new UInt32[datacount];
				for (int i = 0; i < datacount; i++)
					data[i] = reader.ReadUInt32();
				if (version == EraBDConst.Version1808)
					return new EraBinaryDataReader1808(reader, version, data);
				else
					return null;
			}
			catch
			{
				return null;
			}
		}

		public abstract EraSaveFileType ReadFileType();

		/// <summary>
		/// Special processing for system use, no compression
		/// </summary>
		/// <returns></returns>
		public abstract Int64 ReadInt64();

		public abstract string ReadString();
		public abstract Int64 ReadInt();
		public abstract void ReadIntArray(Int64[] refArray, bool needInit);
		public abstract void ReadIntArray2D(Int64[,] refArray, bool needInit);
		public abstract void ReadIntArray3D(Int64[, ,] refArray, bool needInit);
		public abstract void ReadStrArray(string[] refArray, bool needInit);
		public abstract void ReadStrArray2D(string[,] refArray, bool needInit);
		public abstract void ReadStrArray3D(string[, ,] refArray, bool needInit);
		public abstract KeyValuePair<string, EraSaveDataType> ReadVariableCode();
		#region IDisposable Members

		public void Dispose()
		{
			if (reader != null)
				reader.Close();
			reader = null;
		}

		#endregion
		public void Close()
		{
			Dispose();
		}

		private sealed class EraBinaryDataReader1808 : EraBinaryDataReader
		{
			public EraBinaryDataReader1808(BinaryReader stream, int ver, UInt32[] buf)
				: base(stream, ver, buf)
			{
			}

			//public bool EOF
			//{
			//    get
			//    {
			//        return (reader.BaseStream.Length == reader.BaseStream.Position);
			//    }
			//}

			public override int ReaderVersion { get { return 1808; } }
			public override EraSaveFileType ReadFileType()
			{
				byte type = reader.ReadByte();
				if (type >= 0 && type <= 3)
					return (EraSaveFileType)type;
				throw new FileEE("ファイルデータ型異常");
			}

			private Int64 m_ReadInt()
			{
				byte b = reader.ReadByte();
				if (b <= Ebdb.Byte)
					return b;
				if (b == Ebdb.Int16)
					return reader.ReadInt16();
				if (b == Ebdb.Int32)
					return reader.ReadInt32();
				if (b == Ebdb.Int64)
					return reader.ReadInt64();
				throw new FileEE("バイナリデータの異常");
			}

			public override Int64 ReadInt64()
			{
				return reader.ReadInt64();
			}

			public override KeyValuePair<string, EraSaveDataType> ReadVariableCode()
			{
				EraSaveDataType type = (EraSaveDataType)reader.ReadByte();
				if (type == EraSaveDataType.EOC || type == EraSaveDataType.EOF || type == EraSaveDataType.Separator)
					return new KeyValuePair<string, EraSaveDataType>(null, type);
				string key = reader.ReadString();
				return new KeyValuePair<string, EraSaveDataType>(key, type);
			}

			//non-array values use special processing
			public override Int64 ReadInt()
			{
				return m_ReadInt();
			}


			public override string ReadString()
			{
				return reader.ReadString();
			}

			public override void ReadIntArray(Int64[] refArray, bool needInit)
			{
				Int64[] oriArray = null;
				byte b;
				int x = 0;
				int saveLength0 = reader.ReadInt32();
				if (refArray == null)//discard the data; this should be a rare case
					refArray = new Int64[saveLength0];

				int length0 = refArray.Length;

				//when the saved data is larger; this should be a rare case
				if (length0 < saveLength0)
				{
                    oriArray = refArray;
                    //1818 fix: prevent overflow when sizes differ / allocate the array to the maximum and only work on the overlapping part
                    refArray = new Int64[Math.Max(length0, saveLength0)];

                    length0 = Math.Min(length0, saveLength0);
				}
				while (true)
				{
					b = reader.ReadByte();
					if (b == Ebdb.EoD)
						break;
					if (b == Ebdb.Zero)
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								refArray[x + i] = 0;
						x += cnt;
						continue;
					}
					if (b <= Ebdb.Byte)
						refArray[x] = b;
					else if (b == Ebdb.Int16)
						refArray[x] = reader.ReadInt16();
					else if (b == Ebdb.Int32)
						refArray[x] = reader.ReadInt32();
					else if (b == Ebdb.Int64)
						refArray[x] = reader.ReadInt64();
					else
						throw new FileEE("バイナリデータの異常");
					x++;
				}
				if (needInit)
					for (; x < length0; x++)
						refArray[x] = 0;
				if (oriArray != null)
				{
					for (x = 0; x < length0; x++)
						oriArray[x] = refArray[x];
				}
				return;
			}
			public override void ReadIntArray2D(Int64[,] refArray, bool needInit)
			{
				Int64[,] oriArray = null;
				byte b;
				int x = 0;
				int y = 0;
				int saveLength0 = reader.ReadInt32();
				int saveLength1 = reader.ReadInt32();
				if (refArray == null)
					refArray = new Int64[saveLength0, saveLength1];
				int length0 = refArray.GetLength(0);
				int length1 = refArray.GetLength(1);

				if (length0 < saveLength0 || length1 < saveLength1)
				{
                    oriArray = refArray;
                    //1818 fix: prevent overflow when sizes differ / allocate the array to the maximum and only work on the overlapping part
                    refArray = new Int64[Math.Max(length0, saveLength0), Math.Max(length1, saveLength1)];

                    length0 = Math.Min(length0, saveLength0);
                    length1 = Math.Min(length1, saveLength1);
				}

				while (true)
				{
					b = reader.ReadByte();
					if (b == Ebdb.EoD)
						break;
					if (b == Ebdb.ZeroA1)
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								for (y = 0; y < length1; y++)
									refArray[x + i, y] = 0;
						x += cnt;
						y = 0;
						continue;
					}
					if (b == Ebdb.EoA1)
					{
						if (needInit)
							for (; y < length1; y++)
								refArray[x, y] = 0;
						x++;
						y = 0;
						continue;
					}

					if (b == Ebdb.Zero)
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								refArray[x, y + i] = 0;
						y += cnt;
						continue;
					}
					if (b <= Ebdb.Byte)
						refArray[x, y] = b;
					else if (b == Ebdb.Int16)
						refArray[x, y] = reader.ReadInt16();
					else if (b == Ebdb.Int32)
						refArray[x, y] = reader.ReadInt32();
					else if (b == Ebdb.Int64)
						refArray[x, y] = reader.ReadInt64();
					else
						throw new FileEE("バイナリデータの異常");
					y++;
				}
				if (needInit)
				{
					for (; x < length0; x++)
					{
						for (; y < length1; y++)
							refArray[x, y] = 0;
						y = 0;
					}
				}
				if (oriArray != null)
				{
					for (x = 0; x < length0; x++)
						for (y = 0; y < length1; y++)
							oriArray[x, y] = refArray[x, y];
				}
				return;
			}
			/// <summary>
			/// 
			/// </summary>
			/// <param name="refArray">Data destination to write to. Pass null to discard it</param>
			/// <param name="needInit">Whether parts with no data need to be filled with 0</param>
			public override void ReadIntArray3D(Int64[, ,] refArray, bool needInit)
			{
				Int64[, ,] oriArray = null;
				byte b;
				int x = 0;
				int y = 0;
				int z = 0;
				int saveLength0 = reader.ReadInt32();
				int saveLength1 = reader.ReadInt32();
				int saveLength2 = reader.ReadInt32();
				if (refArray == null)
					refArray = new Int64[saveLength0, saveLength1, saveLength2];
				int length0 = refArray.GetLength(0);
				int length1 = refArray.GetLength(1);
				int length2 = refArray.GetLength(2);

				if (length0 < saveLength0 || length1 < saveLength1 || length2 < saveLength2)
				{
					oriArray = refArray;
                    //1818 fix: prevent overflow when sizes differ / allocate the array to the maximum and only work on the overlapping part
                    refArray = new Int64[Math.Max(length0, saveLength0), Math.Max(length1, saveLength1), Math.Max(length2, saveLength2)];

                    length0 = Math.Min(length0, saveLength0);
                    length1 = Math.Min(length1, saveLength1);
                    length2 = Math.Min(length2, saveLength2);
				}

				while (true)
				{
					b = reader.ReadByte();
					if (b == Ebdb.EoD)
						break;
					if (b == Ebdb.ZeroA2)//cnt consecutive empty matrices
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								for (y = 0; y < length1; y++)
									for (z = 0; z < length2; z++)
										refArray[x + i, y, z] = 0;
						x += cnt;
						y = 0;
						z = 0;
						continue;
					}
					if (b == Ebdb.EoA2)//end of matrix, or remaining elements are all 0
					{
						if (needInit)
						{
							for (; y < length1; y++)
							{
								for (; z < length2; z++)
									refArray[x, y, z] = 0;
								z = 0;
							}
						}
						x++;
						y = 0;
						z = 0;
						continue;
					}

					if (b == Ebdb.ZeroA1)//cnt consecutive empty columns
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								for (z = 0; z < length2; z++)
									refArray[x, y + i, z] = 0;
						y += cnt;
						z = 0;
						continue;
					}
					if (b == Ebdb.EoA1)//end of column, or all remaining are 0
					{
						if (needInit)
							for (; z < length2; z++)
								refArray[x, y, z] = 0;
						y++;
						z = 0;
						continue;
					}

					if (b == Ebdb.Zero)//cnt consecutive zeros
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								refArray[x, y, z + i] = 0;
						z += cnt;
						continue;
					}
					if (b <= Ebdb.Byte)
						refArray[x, y, z] = b;
					else if (b == Ebdb.Int16)
						refArray[x, y, z] = reader.ReadInt16();
					else if (b == Ebdb.Int32)
						refArray[x, y, z] = reader.ReadInt32();
					else if (b == Ebdb.Int64)
						refArray[x, y, z] = reader.ReadInt64();
					else
						throw new FileEE("バイナリデータの異常");
					z++;
				}
				if (needInit)
				{

					for (; x < length0; x++)
					{
						for (; y < length1; y++)
						{
							for (; z < length2; z++)
								refArray[x, y, z] = 0;
							z = 0;
						}
						y = 0;
					}
				}
				if (oriArray != null)
				{
					for (x = 0; x < length0; x++)
						for (y = 0; y < length1; y++)
							for (z = 0; z < length2; z++)
								oriArray[x, y, z] = refArray[x, y, z];
				}
				return;
			}
			public override void ReadStrArray(string[] refArray, bool needInit)
			{
				string[] oriArray = null;
				byte b;
				int x = 0;
				int saveLength0 = reader.ReadInt32();
				if (refArray == null)//discard the data; this should be a rare case
					refArray = new string[saveLength0];

				int length0 = refArray.Length;

				//when the saved data is larger; this should be a rare case
				if (length0 < saveLength0)
				{
                    oriArray = refArray;
                    //1818 fix: prevent overflow when sizes differ / allocate the array to the maximum and only work on the overlapping part
                    refArray = new string[Math.Max(length0, saveLength0)];

                    length0 = Math.Min(length0, saveLength0);
				}
				while (true)
				{
					b = reader.ReadByte();
					if (b == Ebdb.EoD)
						break;
					if (b == Ebdb.Zero)
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								refArray[x + i] = null;
						x += cnt;
						continue;
					}
					if (b == Ebdb.String)
						refArray[x] = ReadString();
					else
						throw new FileEE("バイナリデータの異常");
					x++;
				}
				if (needInit)
					for (; x < length0; x++)
						refArray[x] = null;
				if (oriArray != null)
				{
					for (x = 0; x < length0; x++)
						oriArray[x] = refArray[x];
				}
				return;
			}
			public override void ReadStrArray2D(string[,] refArray, bool needInit)
			{
				string[,] oriArray = null;
				byte b;
				int x = 0;
				int y = 0;
				int saveLength0 = reader.ReadInt32();
				int saveLength1 = reader.ReadInt32();
				if (refArray == null)
					refArray = new string[saveLength0, saveLength1];
				int length0 = refArray.GetLength(0);
				int length1 = refArray.GetLength(1);

				if (length0 < saveLength0 || length1 < saveLength1)
				{
                    oriArray = refArray;
                    //1818 fix: prevent overflow when sizes differ / allocate the array to the maximum and only work on the overlapping part
                    refArray = new string[Math.Max(length0, saveLength0), Math.Max(length1, saveLength1)];

                    length0 = Math.Min(length0, saveLength0);
                    length1 = Math.Min(length1, saveLength1);
				}

				while (true)
				{
					b = reader.ReadByte();
					if (b == Ebdb.EoD)
						break;
					if (b == Ebdb.ZeroA1)
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								for (y = 0; y < length1; y++)
									refArray[x + i, y] = null;
						x += cnt;
						y = 0;
						continue;
					}
					if (b == Ebdb.EoA1)
					{
						if (needInit)
							for (; y < length1; y++)
								refArray[x, y] = null;
						x++;
						y = 0;
						continue;
					}

					if (b == Ebdb.Zero)
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								refArray[x, y + i] = null;
						y += cnt;
						continue;
					}
					if (b == Ebdb.String)
						refArray[x, y] = ReadString();
					else
						throw new FileEE("バイナリデータの異常");
					y++;
				}
				if (needInit)
				{
					for (; x < length0; x++)
					{
						for (; y < length1; y++)
							refArray[x, y] = null;
						y = 0;
					}
				}
				if (oriArray != null)
				{
					for (x = 0; x < length0; x++)
						for (y = 0; y < length1; y++)
							oriArray[x, y] = refArray[x, y];
				}
				return;
			}
			public override void ReadStrArray3D(string[, ,] refArray, bool needInit)
			{
				string[, ,] oriArray = null;
				byte b;
				int x = 0;
				int y = 0;
				int z = 0;
				int saveLength0 = reader.ReadInt32();
				int saveLength1 = reader.ReadInt32();
				int saveLength2 = reader.ReadInt32();
				if (refArray == null)
					refArray = new string[saveLength0, saveLength1, saveLength2];
				int length0 = refArray.GetLength(0);
				int length1 = refArray.GetLength(1);
				int length2 = refArray.GetLength(2);

				if (length0 < saveLength0 || length1 < saveLength1 || length2 < saveLength2)
				{
                    oriArray = refArray;
                    //1818 fix: prevent overflow when sizes differ / allocate the array to the maximum and only work on the overlapping part
                    refArray = new string[Math.Max(length0, saveLength0), Math.Max(length1, saveLength1), Math.Max(length2, saveLength2)];

                    length0 = Math.Min(length0, saveLength0);
                    length1 = Math.Min(length1, saveLength1);
                    length2 = Math.Min(length2, saveLength2);
				}

				while (true)
				{
					b = reader.ReadByte();
					if (b == Ebdb.EoD)
						break;
					if (b == Ebdb.ZeroA2)//cnt consecutive empty matrices
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								for (y = 0; y < length1; y++)
									for (z = 0; z < length2; z++)
										refArray[x + i, y, z] = null;
						x += cnt;
						y = 0;
						z = 0;
						continue;
					}
					if (b == Ebdb.EoA2)//end of matrix, or remaining elements are all 0
					{
						if (needInit)
						{
							for (; y < length1; y++)
							{
								for (; z < length2; z++)
									refArray[x, y, z] = null;
								z = 0;
							}
						}
						x++;
						y = 0;
						z = 0;
						continue;
					}

					if (b == Ebdb.ZeroA1)//cnt consecutive empty columns
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								for (z = 0; z < length2; z++)
									refArray[x, y + i, z] = null;
						y += cnt;
						z = 0;
						continue;
					}
					if (b == Ebdb.EoA1)//end of column, or all remaining are 0
					{
						if (needInit)
							for (; z < length2; z++)
								refArray[x, y, z] = null;
						y++;
						z = 0;
						continue;
					}

					if (b == Ebdb.Zero)//cnt consecutive zeros
					{
						int cnt = (int)m_ReadInt();
						if (needInit)
							for (int i = 0; i < cnt; i++)
								refArray[x, y, z + i] = null;
						z += cnt;
						continue;
					}
					if (b == Ebdb.String)
						refArray[x, y, z] = ReadString();
					else
						throw new FileEE("バイナリデータの異常");
					z++;
				}
				if (needInit)
				{
					for (; x < length0; x++)
					{
						for (; y < length1; y++)
						{
							for (; z < length2; z++)
								refArray[x, y, z] = null;
							z = 0;
						}
						y = 0;
					}
				}
				if (oriArray != null)
				{
					for (x = 0; x < length0; x++)
						for (y = 0; y < length1; y++)
							for (z = 0; z < length2; z++)
								oriArray[x, y, z] = refArray[x, y, z];
				}
				return;
			}
		}
	}
}
