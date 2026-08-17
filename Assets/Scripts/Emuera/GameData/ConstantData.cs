using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Variable;

namespace MinorShift.Emuera.GameData
{
    /// <summary>
    /// Defines constant data structures for character attributes and game constants.
    /// Manages enumerations for character string data (NAME, CALLNAME, etc.)
    /// and integer data (BASE, ABL, TALENT, etc.) used throughout the game.
    /// </summary>
    //Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
    [global::System.Reflection.Obfuscation(Exclude = false)]
    internal enum CharacterStrData
    {
		NAME = 0,
		CALLNAME = 1,
		NICKNAME = 2,
		MASTERNAME = 3,
		CSTR = 4,
	}
	
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude = false)]
	internal enum CharacterIntData
	{
		BASE = 0,
		ABL = 1,
		TALENT = 2,
		MARK = 3,
		EXP = 4,
		RELATION = 5,
		CFLAG = 6,
		EQUIP = 7,
		JUEL = 8,
		
	}
	
	internal sealed class ConstantData
	{

		private const int ablIndex = (int)(VariableCode.ABLNAME & VariableCode.__LOWERCASE__);
		private const int expIndex = (int)(VariableCode.EXPNAME & VariableCode.__LOWERCASE__);
		private const int talentIndex = (int)(VariableCode.TALENTNAME & VariableCode.__LOWERCASE__);
		private const int paramIndex = (int)(VariableCode.PALAMNAME & VariableCode.__LOWERCASE__);
		private const int trainIndex = (int)(VariableCode.TRAINNAME & VariableCode.__LOWERCASE__);
		private const int markIndex = (int)(VariableCode.MARKNAME & VariableCode.__LOWERCASE__);
		private const int itemIndex = (int)(VariableCode.ITEMNAME & VariableCode.__LOWERCASE__);
		private const int baseIndex = (int)(VariableCode.BASENAME & VariableCode.__LOWERCASE__);
		private const int sourceIndex = (int)(VariableCode.SOURCENAME & VariableCode.__LOWERCASE__);
		private const int exIndex = (int)(VariableCode.EXNAME & VariableCode.__LOWERCASE__);
		private const int strIndex = (int)(VariableCode.__DUMMY_STR__ & VariableCode.__LOWERCASE__);
		private const int equipIndex = (int)(VariableCode.EQUIPNAME & VariableCode.__LOWERCASE__);
		private const int tequipIndex = (int)(VariableCode.TEQUIPNAME & VariableCode.__LOWERCASE__);
		private const int flagIndex = (int)(VariableCode.FLAGNAME & VariableCode.__LOWERCASE__);
		private const int tflagIndex = (int)(VariableCode.TFLAGNAME & VariableCode.__LOWERCASE__);
		private const int cflagIndex = (int)(VariableCode.CFLAGNAME & VariableCode.__LOWERCASE__);
		private const int tcvarIndex = (int)(VariableCode.TCVARNAME & VariableCode.__LOWERCASE__);
		private const int cstrIndex = (int)(VariableCode.CSTRNAME & VariableCode.__LOWERCASE__);
		private const int stainIndex = (int)(VariableCode.STAINNAME & VariableCode.__LOWERCASE__);
		private const int cdflag1Index = (int)(VariableCode.CDFLAGNAME1 & VariableCode.__LOWERCASE__);
		private const int cdflag2Index = (int)(VariableCode.CDFLAGNAME2 & VariableCode.__LOWERCASE__);
		private const int strnameIndex = (int)(VariableCode.STRNAME & VariableCode.__LOWERCASE__);
		private const int tstrnameIndex = (int)(VariableCode.TSTRNAME & VariableCode.__LOWERCASE__);
		private const int savestrnameIndex = (int)(VariableCode.SAVESTRNAME & VariableCode.__LOWERCASE__);
		private const int globalIndex = (int)(VariableCode.GLOBALNAME & VariableCode.__LOWERCASE__);
		private const int globalsIndex = (int)(VariableCode.GLOBALSNAME & VariableCode.__LOWERCASE__);
		private const int countNameCsv = (int)VariableCode.__COUNT_CSV_STRING_ARRAY_1D__;
		
		public int[] MaxDataList = new int[countNameCsv];
        readonly HashSet<VariableCode> changedCode = new HashSet<VariableCode>();
		
		public int[] VariableIntArrayLength;
		public int[] VariableStrArrayLength;
		public Int64[] VariableIntArray2DLength;
		public Int64[] VariableStrArray2DLength;
		public Int64[] VariableIntArray3DLength;
		public Int64[] VariableStrArray3DLength;
		public int[] CharacterIntArrayLength;
		public int[] CharacterStrArrayLength;
		public Int64[] CharacterIntArray2DLength;
		public Int64[] CharacterStrArray2DLength;

		//private readonly GameBase gamebase;
		private readonly string[][] names = new string[(int)VariableCode.__COUNT_CSV_STRING_ARRAY_1D__][];
		private readonly Dictionary<string, int>[] nameToIntDics = new Dictionary<string, int>[(int)VariableCode.__COUNT_CSV_STRING_ARRAY_1D__];
		private readonly Dictionary<string, int> relationDic = new Dictionary<string, int>();
		public string[] GetCsvNameList(VariableCode code)
		{
			return names[(int)(code & VariableCode.__LOWERCASE__)];
		}

		public Int64[] ItemPrice;
		
		private readonly List<CharacterTemplate> CharacterTmplList;
		private EmueraConsole output;
		
		public ConstantData()
		{
			//this.gamebase = gamebase;
			setDefaultArrayLength();

			CharacterTmplList = new List<CharacterTemplate>();
			useCompatiName = Config.CompatiCALLNAME;
		}

		readonly bool useCompatiName;

		private void setDefaultArrayLength()
		{
			MaxDataList[ablIndex] = 100;
			MaxDataList[talentIndex] = 1000;
			MaxDataList[expIndex] = 100;
			MaxDataList[markIndex] = 100;
			MaxDataList[trainIndex] = 1000;
			MaxDataList[paramIndex] = 200;
			MaxDataList[itemIndex] = 1000;
			MaxDataList[baseIndex] = 100;
			MaxDataList[sourceIndex] = 1000;
			MaxDataList[exIndex] = 100;
			MaxDataList[equipIndex] = 100;
			MaxDataList[tequipIndex] = 100;
			MaxDataList[flagIndex] = 10000;
			MaxDataList[tflagIndex] = 1000;
			MaxDataList[cflagIndex] = 1000;
			MaxDataList[tcvarIndex] = 100;
			MaxDataList[cstrIndex] = 100;
			MaxDataList[stainIndex] = 1000;
			MaxDataList[strIndex] = 20000;
			MaxDataList[cdflag1Index] = 1;
			MaxDataList[cdflag2Index] = 1;
			MaxDataList[strnameIndex] = 20000;
			MaxDataList[tstrnameIndex] = 100;
			MaxDataList[savestrnameIndex] = 100;
			MaxDataList[globalIndex] = 1000;
			MaxDataList[globalsIndex] = 100;

			VariableIntArrayLength = new int[(int)VariableCode.__COUNT_INTEGER_ARRAY__];
			VariableStrArrayLength = new int[(int)VariableCode.__COUNT_STRING_ARRAY__];
			VariableIntArray2DLength = new Int64[(int)VariableCode.__COUNT_INTEGER_ARRAY_2D__];
			VariableStrArray2DLength = new Int64[(int)VariableCode.__COUNT_STRING_ARRAY_2D__];
			VariableIntArray3DLength = new Int64[(int)VariableCode.__COUNT_INTEGER_ARRAY_3D__];
			VariableStrArray3DLength = new Int64[(int)VariableCode.__COUNT_STRING_ARRAY_3D__];
			CharacterIntArrayLength = new int[(int)VariableCode.__COUNT_CHARACTER_INTEGER_ARRAY__];
			CharacterStrArrayLength = new int[(int)VariableCode.__COUNT_CHARACTER_STRING_ARRAY__];
			CharacterIntArray2DLength = new Int64[(int)VariableCode.__COUNT_CHARACTER_INTEGER_ARRAY_2D__];
			CharacterStrArray2DLength = new Int64[(int)VariableCode.__COUNT_CHARACTER_STRING_ARRAY_2D__];
			for (int i = 0; i < VariableIntArrayLength.Length; i++)
				VariableIntArrayLength[i] = 1000;
			VariableIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.FLAG)] = 10000;
			VariableIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.ITEMPRICE)] = MaxDataList[itemIndex];

			VariableIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.RANDDATA)] = 625;

			for (int i = 0; i < VariableStrArrayLength.Length; i++)
				VariableStrArrayLength[i] = 100;
			VariableStrArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.STR)] = MaxDataList[strIndex];

			for (int i = 0; i < VariableIntArray2DLength.Length; i++)
				VariableIntArray2DLength[i] = (100L << 32) + 100L;
			for (int i = 0; i < VariableStrArray2DLength.Length; i++)
				VariableStrArray2DLength[i] = (100L << 32) + 100L;

			for (int i = 0; i < VariableIntArray3DLength.Length; i++)
				VariableIntArray3DLength[i] = (100L << 40) + (100L << 20) + 100L;
			for (int i = 0; i < VariableStrArray3DLength.Length; i++)
				VariableStrArray3DLength[i] = (100L << 40) + (100L << 20) + 100L;

			for (int i = 0; i < CharacterIntArrayLength.Length; i++)
				CharacterIntArrayLength[i] = 100;
			CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.TALENT)] = 1000;
			CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.CFLAG)] = 1000;
			CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)] = 200;
			CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.GOTJUEL)] = 200;

			for (int i = 0; i < CharacterStrArrayLength.Length; i++)
				CharacterStrArrayLength[i] = 100;

			for (int i = 0; i < CharacterIntArray2DLength.Length; i++)
				CharacterIntArray2DLength[i] = (1L << 32) + 1L;
			for (int i = 0; i < CharacterStrArray2DLength.Length; i++)
				CharacterStrArray2DLength[i] = (1L << 32) + 1L;
		}

		private void loadVariableSizeData(string csvPath, bool disp)
		{
			// Use case-insensitive file resolution for non-Windows systems
			string resolvedPath = uEmuera.Utils.ResolveExistingFilePath(csvPath);
			if (string.IsNullOrEmpty(resolvedPath))
				return;
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(resolvedPath))
			{
				output.PrintError(eReader.Filename + GameMessages.T(" failed to open"));
				return;
			}
			ScriptPosition position = null;
			if (disp)
				output.PrintSystemLine(eReader.Filename + GameMessages.T(" loading..."));
			try
			{
				StringStream st = null;
				while ((st = eReader.ReadEnabledLine()) != null)
				{
					position = new ScriptPosition(eReader.Filename, eReader.LineNo);
					changeVariableSizeData(st.Substring(), position);
				}
				position = new ScriptPosition(eReader.Filename, -1);
			}
			catch
			{
				uEmuera.Media.SystemSounds.Hand.Play();
				if (position != null)
					ParserMediator.Warn(GameMessages.T("An unexpected error occurred"), position, 3);
				else
					output.PrintError(GameMessages.T("An unexpected error occurred"));
				return;
			}
			finally
			{
				eReader.Close();
			}
			decideActualArraySize(position);
		}


		private void changeVariableSizeData(string line, ScriptPosition position)
		{
			string[] tokens = line.Split(',');
			if (tokens.Length < 2)
			{
				ParserMediator.Warn(GameMessages.T("\",\" is required"), position, 1);
				return;
			}
			string idtoken = tokens[0].Trim();
			VariableIdentifier id = VariableIdentifier.GetVariableId(idtoken);
			if (id == null)
			{
				ParserMediator.Warn(GameMessages.T("The first value could not be recognized as a variable name"), position, 1);
				return;
			}
			if ((!id.IsArray1D) && (!id.IsArray2D) && (!id.IsArray3D))
			{
				ParserMediator.Warn(GameMessages.T("The size of the non-array variable ") + id.ToString() + GameMessages.T(" cannot be changed"), position, 1);
				return;
			}
			if ((id.IsCalc) || (id.Code == VariableCode.RANDDATA))
			{
				ParserMediator.Warn(id.ToString() + GameMessages.T(" size cannot be changed"), position, 1);
				return;
			}
            int length2 = 0;
            int length3 = 0;
			if (!int.TryParse(tokens[1], out int length))
			{
				ParserMediator.Warn(GameMessages.T("The second value could not be recognized as an integer"), position, 1);
				return;
			}
            //1820a16 Variable forbidding: specify a negative value
			if (length <= 0)
			{
				if (length == 0)
				{
					ParserMediator.Warn(GameMessages.T("An array length of 0 cannot be specified (to forbid a variable, specify a negative array length)"), position, 2);
					return;
				}
				if(!id.CanForbid)
				{
					ParserMediator.Warn(GameMessages.T("A negative array length was specified for a variable that cannot be forbidden"), position, 2);
					return;
				}
                if (tokens.Length > 2 && tokens[2].Length > 0 && tokens[2].Trim().Length > 0 && char.IsDigit((tokens[2].Trim())[0]))
                {
                    ParserMediator.Warn(GameMessages.T("Unnecessary data in the 1D array size specification will be ignored"), position, 0);
                }
				length = 0;
				goto check1break;
			}
			if (id.IsArray1D)
			{
                if (tokens.Length > 2 && tokens[2].Length > 0 && tokens[2].Trim().Length > 0 && char.IsDigit((tokens[2].Trim())[0]))
                {
                    ParserMediator.Warn(GameMessages.T("Unnecessary data in the 1D array size specification will be ignored"), position, 0);
                }
				if (id.IsLocal && length < 1)
				{
					ParserMediator.Warn(GameMessages.T("A local variable size cannot be less than 1"), position, 1);
					return;
				}
				if (!id.IsLocal && length < 100)
				{
					ParserMediator.Warn(GameMessages.T("The size of a non-local 1D array cannot be less than 100"), position, 1);
					return;
				}
				if (length > 1000000)
				{
					ParserMediator.Warn(GameMessages.T("The size of a 1D array cannot exceed 1000000"), position, 1);
					return;
				}
			}
			else if (id.IsArray2D)
			{
				if (tokens.Length < 3)
				{
					ParserMediator.Warn(GameMessages.T("Two numbers are required to specify a 2D array size"), position, 1);
					return;
				}
                if (tokens.Length > 3 && tokens[3].Length > 0 && tokens[3].Trim().Length > 0 && char.IsDigit((tokens[3].Trim())[0]))
                {
                    ParserMediator.Warn(GameMessages.T("Unnecessary data in the 2D array size specification will be ignored"), position, 0);
                }
                if (!int.TryParse(tokens[2], out length2))
				{
					ParserMediator.Warn(GameMessages.T("The third value could not be recognized as an integer"), position, 1);
					return;
				}
				if ((length < 1) || (length2 < 1))
				{
					ParserMediator.Warn(GameMessages.T("The array size cannot be less than 1"), position, 1);
					return;
				}
				if ((length > 1000000) || (length2 > 1000000))
				{
					ParserMediator.Warn(GameMessages.T("The array size cannot exceed 1000000"), position, 1);
					return;
				}
				if (length * length2 > 1000000)
				{
					ParserMediator.Warn(GameMessages.T("A 2D array can have at most 1,000,000 elements"), position, 1);
					return;
				}
			}
			else if (id.IsArray3D)
			{
				if (tokens.Length < 4)
				{
					ParserMediator.Warn(GameMessages.T("Three numbers are required to specify a 3D array size"), position, 1);
					return;
				}
                if (tokens.Length > 4 && tokens[4].Length > 0 && tokens[4].Trim().Length > 0 && char.IsDigit((tokens[4].Trim())[0]))
                {
                    ParserMediator.Warn(GameMessages.T("Unnecessary data in the 3D array size specification will be ignored"), position, 0);
                }
                if (!int.TryParse(tokens[2], out length2))
				{
					ParserMediator.Warn(GameMessages.T("The third value could not be recognized as an integer"), position, 1);
					return;
				}
				if (!int.TryParse(tokens[3], out length3))
				{
					ParserMediator.Warn(GameMessages.T("The fourth value could not be recognized as an integer"), position, 1);
					return;
				}
				if ((length < 1) || (length2 < 1) || (length3 < 1))
				{
					ParserMediator.Warn(GameMessages.T("The array size cannot be less than 1"), position, 1);
					return;
				}
				//1802 For size-saving reasons, exceeding 2^20 causes bugs
				if ((length > 1000000) || (length2 > 1000000) || (length3 > 1000000))
				{
					ParserMediator.Warn(GameMessages.T("The array size cannot exceed 1000000"), position, 1);
					return;
				}
				if (length * length2 * length3 > 10000000)
				{
					ParserMediator.Warn(GameMessages.T("A 3D array can have at most 10,000,000 elements"), position, 1);
					return;
				}
			}
check1break:
			switch (id.Code)
			{
				//1753a PALAM having different specs is itself a problem, so all variable/name-array count syncs were backed out
				//Basically just reverted to the old behavior
				case VariableCode.ITEMNAME:
				case VariableCode.ITEMPRICE:
					VariableIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.ITEMPRICE)] = length;
					MaxDataList[itemIndex] = length;
					break;
				case VariableCode.STR:
					VariableStrArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.STR)] = length;
					MaxDataList[strIndex] = length;
					break;
				case VariableCode.ABLNAME:
				case VariableCode.TALENTNAME:
				case VariableCode.EXPNAME:
				case VariableCode.MARKNAME:
				case VariableCode.PALAMNAME:
				case VariableCode.TRAINNAME:
				case VariableCode.BASENAME:
				case VariableCode.SOURCENAME:
				case VariableCode.EXNAME:
				case VariableCode.EQUIPNAME:
				case VariableCode.TEQUIPNAME:
				case VariableCode.FLAGNAME:
				case VariableCode.TFLAGNAME:
				case VariableCode.CFLAGNAME:
				case VariableCode.TCVARNAME:
				case VariableCode.CSTRNAME:
				case VariableCode.STAINNAME:
				case VariableCode.CDFLAGNAME1:
				case VariableCode.CDFLAGNAME2:
				case VariableCode.TSTRNAME:
				case VariableCode.SAVESTRNAME:
				case VariableCode.STRNAME:
				case VariableCode.GLOBALNAME:
				case VariableCode.GLOBALSNAME:
					MaxDataList[(int)(id.Code & VariableCode.__LOWERCASE__)] = length;
					break;
				default:
					{
						if (id.IsCharacterData)
						{
							if (id.IsArray2D)
							{
								Int64 length64 = (((Int64)length) << 32) + ((Int64)length2);
								if (id.IsInteger)
									CharacterIntArray2DLength[id.CodeInt] = length64;
								else if (id.IsString)
									CharacterStrArray2DLength[id.CodeInt] = length64;
							}
							else
							{
								if (id.IsInteger)
									CharacterIntArrayLength[id.CodeInt] = length;
								else if (id.IsString)
									CharacterStrArrayLength[id.CodeInt] = length;
							}
						}
						else if (id.IsArray2D)
						{
							Int64 length64 = (((Int64)length) << 32) + ((Int64)length2);
							if (id.IsInteger)
								VariableIntArray2DLength[id.CodeInt] = length64;
							else if (id.IsString)
								VariableStrArray2DLength[id.CodeInt] = length64;
						}
						else if (id.IsArray3D)
						{
							//Int64 length3d = ((Int64)length << 32) + ((Int64)length2 << 16) + (Int64)length3;
							Int64 length3d = ((Int64)length << 40) + ((Int64)length2 << 20) + (Int64)length3;
							if (id.IsInteger)
								VariableIntArray3DLength[id.CodeInt] = length3d;
							else
								VariableStrArray3DLength[id.CodeInt] = length3d;
						}
						else
						{
							if (id.IsInteger)
								VariableIntArrayLength[id.CodeInt] = length;
							else if (id.IsString)
								VariableStrArrayLength[id.CodeInt] = length;
						}
					}
					break;
			}
			//1803beta004 Make duplicate definitions a warning target
			if (!changedCode.Add(id.Code))
				ParserMediator.Warn(id.Code.ToString() + GameMessages.T(" element count is already defined (will be overwritten)"), position, 1);
		}

		private void _decideActualArraySize_sub(VariableCode mainCode, VariableCode nameCode, int[] arraylength, ScriptPosition position)
		{
			int nameIndex = (int)(nameCode & VariableCode.__LOWERCASE__);
			int mainLengthIndex = (int)(mainCode & VariableCode.__LOWERCASE__);
			if (changedCode.Contains(nameCode) && changedCode.Contains(mainCode))
			{
				if (MaxDataList[nameIndex] != arraylength[mainLengthIndex])
				{
					int i = Math.Max(MaxDataList[nameIndex], arraylength[mainLengthIndex]);
					arraylength[mainLengthIndex] = i;
					MaxDataList[nameIndex] = i;
					//1803beta004 Treat as inappropriate specification: warning level 1
					if (MaxDataList[nameIndex] == 0 || arraylength[mainLengthIndex] == 0)
						ParserMediator.Warn(mainCode.ToString() + GameMessages.T(" and ") + nameCode.ToString() + GameMessages.T(" have different forbid settings (the forbidding will be lifted)"), position, 1);
					else
						ParserMediator.Warn(mainCode.ToString() + GameMessages.T(" and ") + nameCode.ToString() + GameMessages.T(" have different element counts (will match the larger one)"), position, 1);
				}
			}
			else if (changedCode.Contains(nameCode) && !changedCode.Contains(mainCode))
				arraylength[mainLengthIndex] = MaxDataList[nameIndex];
			else if (!changedCode.Contains(nameCode) && changedCode.Contains(mainCode))
				MaxDataList[nameIndex] = arraylength[mainLengthIndex];
		}
		
		private void decideActualArraySize(ScriptPosition position)
		{
			_decideActualArraySize_sub(VariableCode.ABL, VariableCode.ABLNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.TALENT, VariableCode.TALENTNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.EXP, VariableCode.EXPNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.MARK, VariableCode.MARKNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.BASE, VariableCode.BASENAME, CharacterIntArrayLength, position);
            _decideActualArraySize_sub(VariableCode.SOURCE, VariableCode.SOURCENAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.EX, VariableCode.EXNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.EQUIP, VariableCode.EQUIPNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.TEQUIP, VariableCode.TEQUIPNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.FLAG, VariableCode.FLAGNAME, VariableIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.TFLAG, VariableCode.TFLAGNAME, VariableIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.CFLAG, VariableCode.CFLAGNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.TCVAR, VariableCode.TCVARNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.CSTR, VariableCode.CSTRNAME, CharacterStrArrayLength, position);
			_decideActualArraySize_sub(VariableCode.STAIN, VariableCode.STAINNAME, CharacterIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.STR, VariableCode.STRNAME, VariableStrArrayLength, position);
			_decideActualArraySize_sub(VariableCode.TSTR, VariableCode.TSTRNAME, VariableStrArrayLength, position);
			_decideActualArraySize_sub(VariableCode.SAVESTR, VariableCode.SAVESTRNAME, VariableStrArrayLength, position);
			_decideActualArraySize_sub(VariableCode.GLOBAL, VariableCode.GLOBALNAME, VariableIntArrayLength, position);
			_decideActualArraySize_sub(VariableCode.GLOBALS, VariableCode.GLOBALSNAME, VariableStrArrayLength, position);


			//PALAM (including JUEL)
			//If either PALAM or JUEL changed, take the larger one
			if (changedCode.Contains(VariableCode.PALAM) || changedCode.Contains(VariableCode.JUEL))
			{
				int palamJuelMax = Math.Max(CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.PALAM)]
						, CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)]);
				//If PALAMNAME changed, compare with it and adopt the larger one
				if(changedCode.Contains(VariableCode.PALAMNAME))
				{
					if (MaxDataList[paramIndex] != palamJuelMax)
					{
						int i = Math.Max(MaxDataList[paramIndex], palamJuelMax);
						MaxDataList[paramIndex] = i;
						if(CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.PALAM)] == palamJuelMax)
							CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.PALAM)] = i;
						if(CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)] == palamJuelMax)
							CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)] = i;
						//1803beta004 Treat as inappropriate specification: warning level 1
						ParserMediator.Warn(GameMessages.T("The element counts of PALAM, JUEL, and PALAMNAME are inconsistent"), position, 1);
					}
				}
				else//If PALAMNAME is not specified, match PALAMNAME to the larger one
					MaxDataList[paramIndex] = palamJuelMax;
			}
			//When PALAM and JUEL are unchanged but PALAMNAME changed
			else if (changedCode.Contains(VariableCode.PALAMNAME))
			{
				//Match PALAM to the specified PALAMNAME
				CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.PALAM)] = MaxDataList[paramIndex];
				//If the specified PALAMNAME is smaller than JUEL, warn and match to JUEL
				if (MaxDataList[paramIndex] < CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)])
				{
					ParserMediator.Warn(GameMessages.T("PALAMNAME has fewer elements than JUEL (will match JUEL)"), position, 1);
					MaxDataList[paramIndex] = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)];
				}
			}
			//CDFLAG
			//If either is changed, treat both as changed
			bool cdflagNameLengthChanged = changedCode.Contains(VariableCode.CDFLAGNAME1) || changedCode.Contains(VariableCode.CDFLAGNAME2);
			int mainLengthIndex = (int)(VariableCode.__LOWERCASE__ & VariableCode.CDFLAG);
			Int64 length64 = CharacterIntArray2DLength[mainLengthIndex];
			int length1 = (int)(length64 >> 32);
			int length2 = (int)(length64 & 0x7FFFFFFF);
			if (changedCode.Contains(VariableCode.CDFLAG) && cdflagNameLengthChanged)
			{
				//Too troublesome to adjust, so throw
				if ((length1 != MaxDataList[cdflag1Index]) || (length2 != MaxDataList[cdflag2Index]))
					throw new CodeEE(GameMessages.T("The element counts of CDFLAG, CDFLAGNAME1, and CDFLAGNAME2 do not match"), position);
			}
			else if (cdflagNameLengthChanged && !changedCode.Contains(VariableCode.CDFLAG))
			{
				length1 = MaxDataList[cdflag1Index];
				length2 = MaxDataList[cdflag2Index];
				if (length1 * length2 > 1000000)
				{
					//Too troublesome to adjust, so throw
					throw new CodeEE(GameMessages.T("CDFLAG has too many elements (the product of the CDFLAGNAME1 and CDFLAGNAME2 element counts exceeds 1,000,000)"), position);
				}
				CharacterIntArray2DLength[mainLengthIndex] = (((Int64)length1) << 32) + ((Int64)length2);
			}
			else if (!cdflagNameLengthChanged && changedCode.Contains(VariableCode.CDFLAG))
			{
				MaxDataList[cdflag1Index] = length1;
				MaxDataList[cdflag2Index] = length2;
			}
			//No longer used, so discard the data
			changedCode.Clear();
		}


		public void LoadData(string csvDir, EmueraConsole console, bool disp)
		{
			output = console;
			loadVariableSizeData(csvDir + "VariableSize.CSV", disp);
			for(int i = 0; i< countNameCsv;i++)
			{
				names[i] = new string[MaxDataList[i]];
				nameToIntDics[i] = new Dictionary<string, int>();
			}
			ItemPrice = new Int64[MaxDataList[itemIndex]];
			loadDataTo(csvDir + "ABL.CSV", ablIndex, null, disp);
			loadDataTo(csvDir + "EXP.CSV", expIndex, null, disp);
			loadDataTo(csvDir + "TALENT.CSV", talentIndex, null, disp);
			loadDataTo(csvDir + "PALAM.CSV", paramIndex, null, disp);
			loadDataTo(csvDir + "TRAIN.CSV", trainIndex, null, disp);
			loadDataTo(csvDir + "MARK.CSV", markIndex, null, disp);
			loadDataTo(csvDir + "ITEM.CSV", itemIndex, ItemPrice, disp);
			loadDataTo(csvDir + "BASE.CSV", baseIndex, null, disp);
			loadDataTo(csvDir + "SOURCE.CSV", sourceIndex, null, disp);
			loadDataTo(csvDir + "EX.CSV", exIndex, null, disp);
			loadDataTo(csvDir + "STR.CSV", strIndex, null, disp);
			loadDataTo(csvDir + "EQUIP.CSV", equipIndex, null, disp);
			loadDataTo(csvDir + "TEQUIP.CSV", tequipIndex, null, disp);
			loadDataTo(csvDir + "FLAG.CSV", flagIndex, null, disp);
			loadDataTo(csvDir + "TFLAG.CSV", tflagIndex, null, disp);
			loadDataTo(csvDir + "CFLAG.CSV", cflagIndex, null, disp);
			loadDataTo(csvDir + "TCVAR.CSV", tcvarIndex, null, disp);
			loadDataTo(csvDir + "CSTR.CSV", cstrIndex, null, disp);
			loadDataTo(csvDir + "STAIN.CSV", stainIndex, null, disp);
			loadDataTo(csvDir + "CDFLAG1.CSV", cdflag1Index, null, disp);
			loadDataTo(csvDir + "CDFLAG2.CSV", cdflag2Index, null, disp);
			
			loadDataTo(csvDir + "STRNAME.CSV", strnameIndex, null, disp);
			loadDataTo(csvDir + "TSTR.CSV", tstrnameIndex, null, disp);
			loadDataTo(csvDir + "SAVESTR.CSV", savestrnameIndex, null, disp);
			loadDataTo(csvDir + "GLOBAL.CSV", globalIndex, null, disp);
			loadDataTo(csvDir + "GLOBALS.CSV", globalsIndex, null, disp);
			//Create the reverse lookup dictionary
			for (int i = 0; i < names.Length; i++)
			{
				if (i == 10)//Str needs no reverse lookup
					continue;
				string[] nameArray = names[i];
				for (int j = 0; j < nameArray.Length; j++)
				{
					if (!string.IsNullOrEmpty(nameArray[j]) && !nameToIntDics[i].ContainsKey(nameArray[j]))
						nameToIntDics[i].Add(nameArray[j], j);
				}
			}
			//if (!Program.AnalysisMode)
			loadCharacterData(csvDir, disp);

			//Create the reverse lookup dictionary 2 (RELATION)
			for (int i = 0; i < CharacterTmplList.Count; i++)
			{
				CharacterTemplate tmpl = CharacterTmplList[i];
				if (!string.IsNullOrEmpty(tmpl.Name) && !relationDic.ContainsKey(tmpl.Name))
					relationDic.Add(tmpl.Name, (int)tmpl.No);
				if (!string.IsNullOrEmpty(tmpl.Callname) && !relationDic.ContainsKey(tmpl.Callname))
                    relationDic.Add(tmpl.Callname, (int)tmpl.No);
				if (!string.IsNullOrEmpty(tmpl.Nickname) && !relationDic.ContainsKey(tmpl.Nickname))
                    relationDic.Add(tmpl.Nickname, (int)tmpl.No);
			}
		}

		public bool isDefined(VariableCode varCode, string str)
		{
			if (string.IsNullOrEmpty(str))
				return false;
            Dictionary<string, int> dic;
            if (varCode == VariableCode.CDFLAG)
            {
                dic = GetKeywordDictionary(out _, VariableCode.CDFLAGNAME1, -1);
                if ((dic == null) || (!dic.ContainsKey(str)))
                    dic = GetKeywordDictionary(out _, VariableCode.CDFLAGNAME2, -1);
                if (dic == null)
                    return false;
                return dic.ContainsKey(str);
            }
            dic = GetKeywordDictionary(out _, varCode, -1);
			if (dic == null)
				return false;
			return dic.ContainsKey(str);
		}

        
		/// <summary>
		/// Reverse lookup: given an integer value, return the keyword name for that index.
		/// Used by ERDNAME. varname is the base variable name (e.g. "ABL" or "ABLNAME").
		/// </summary>
		public bool TryIntegerToKeyword(out string ret, long value, string varname)
		{
			ret = "";
			// Map base variable name or *NAME variant to the correct VariableCode for the NAME array
			VariableCode code = VariableCode.__NULL__;
			string upper = (varname ?? "").ToUpperInvariant().TrimEnd('@', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			switch (upper)
			{
				case "ABL": case "ABLNAME": code = VariableCode.ABLNAME; break;
				case "EXP": case "EXPNAME": code = VariableCode.EXPNAME; break;
				case "TALENT": case "TALENTNAME": code = VariableCode.TALENTNAME; break;
				case "PALAM": case "PALAMNAME": code = VariableCode.PALAMNAME; break;
				case "TRAIN": case "TRAINNAME": code = VariableCode.TRAINNAME; break;
				case "MARK": case "MARKNAME": code = VariableCode.MARKNAME; break;
				case "ITEM": case "ITEMNAME": code = VariableCode.ITEMNAME; break;
				case "BASE": case "BASENAME": code = VariableCode.BASENAME; break;
				case "SOURCE": case "SOURCENAME": code = VariableCode.SOURCENAME; break;
				case "EX": case "EXNAME": code = VariableCode.EXNAME; break;
				case "EQUIP": case "EQUIPNAME": code = VariableCode.EQUIPNAME; break;
				case "TEQUIP": case "TEQUIPNAME": code = VariableCode.TEQUIPNAME; break;
				case "FLAG": case "FLAGNAME": code = VariableCode.FLAGNAME; break;
				case "TFLAG": case "TFLAGNAME": code = VariableCode.TFLAGNAME; break;
				case "CFLAG": case "CFLAGNAME": code = VariableCode.CFLAGNAME; break;
				default: return false;
			}
			Dictionary<string, int> dic;
			try { dic = GetKeywordDictionary(out _, code, -1); }
			catch { return false; }
			if (dic == null) return false;
			foreach (var kv in dic)
				if (kv.Value == (int)value) { ret = kv.Key; return true; }
			return false;
		}

		public bool TryKeywordToInteger(out int ret, VariableCode code, string key, int index)
        {
            ret = 0;
            if (string.IsNullOrEmpty(key))
                return false;
            Dictionary<string, int> dic;
            try
            {
                dic = GetKeywordDictionary(out string errPos, code, index);
				if (dic == null)
					return false;
            }
            catch { return false; }
            return (dic.TryGetValue(key, out ret));
        }

		public int KeywordToInteger(VariableCode code, string key, int index)
		{
			if (string.IsNullOrEmpty(key))
				throw new CodeEE(GameMessages.T("The keyword cannot be empty"));
            Dictionary<string, int> dic = GetKeywordDictionary(out string errPos, code, index);
            if (dic.TryGetValue(key, out int ret))
                return ret;
            if (errPos == null)
				throw new CodeEE(GameMessages.T("The elements of array variable ") + code.ToString() + GameMessages.T(" cannot be specified by string"));
			else
				throw new CodeEE(errPos + GameMessages.T(" does not contain a definition of \"") + key + GameMessages.T("\""));
		}

		public Dictionary<string, int> GetKeywordDictionary(out string errPos, VariableCode code, int index)
		{
			errPos = null;
			int allowIndex = -1;
			Dictionary<string, int> ret = null;
			switch (code)
			{
				case VariableCode.ABL:
					ret = nameToIntDics[ablIndex];//AblName;
					errPos = "abl.csv";
					allowIndex = 1;
					break;
				case VariableCode.EXP:
					ret = nameToIntDics[expIndex];//ExpName;
					errPos = "exp.csv";
					allowIndex = 1;
					break;
				case VariableCode.TALENT:
					ret = nameToIntDics[talentIndex];//TalentName;
					errPos = "talent.csv";
					allowIndex = 1;
					break;
				case VariableCode.UP:
				case VariableCode.DOWN:
					ret = nameToIntDics[paramIndex];//ParamName 1;
					errPos = "palam.csv";
					allowIndex = 0;
					break;
				case VariableCode.PALAM:
				case VariableCode.JUEL:
				case VariableCode.GOTJUEL:
				case VariableCode.CUP:
				case VariableCode.CDOWN:
					ret = nameToIntDics[paramIndex];//ParamName 2;
					errPos = "palam.csv";
					allowIndex = 1;
					break;

				case VariableCode.TRAINNAME:
					ret = nameToIntDics[trainIndex];//TrainName;
					errPos = "train.csv";
					allowIndex = 0;
					break;
				case VariableCode.MARK:
					ret = nameToIntDics[markIndex];//MarkName;
					errPos = "mark.csv";
					allowIndex = 1;
					break;
				case VariableCode.ITEM:
				case VariableCode.ITEMSALES:
				case VariableCode.ITEMPRICE:
					ret = nameToIntDics[itemIndex];//ItemName;
					errPos = "Item.csv";
					allowIndex = 0;
					break;
				case VariableCode.LOSEBASE:
					ret = nameToIntDics[baseIndex];//BaseName;
					errPos = "base.csv";
					allowIndex = 0;
					break;
				case VariableCode.BASE:
				case VariableCode.MAXBASE:
				case VariableCode.DOWNBASE:
					ret = nameToIntDics[baseIndex];//BaseName;
					errPos = "base.csv";
					allowIndex = 1;
					break;
				case VariableCode.SOURCE:
					ret = nameToIntDics[sourceIndex];//SourceName;
					errPos = "source.csv";
					allowIndex = 1;
					break;
				case VariableCode.EX:
				case VariableCode.NOWEX:
					ret = nameToIntDics[exIndex];//ExName;
					errPos = "ex.csv";
					allowIndex = 1;
					break;


				case VariableCode.EQUIP:
					ret = nameToIntDics[equipIndex];//EquipName;
					errPos = "equip.csv";
					allowIndex = 1;
					break;
				case VariableCode.TEQUIP:
					ret = nameToIntDics[tequipIndex];//TequipName;
					errPos = "tequip.csv";
					allowIndex = 1;
					break;
				case VariableCode.FLAG:
					ret = nameToIntDics[flagIndex];//FlagName;
					errPos = "flag.csv";
					allowIndex = 0;
					break;
				case VariableCode.TFLAG:
					ret = nameToIntDics[tflagIndex];//TFlagName;
					errPos = "tflag.csv";
					allowIndex = 0;
					break;
				case VariableCode.CFLAG:
					ret = nameToIntDics[cflagIndex];//CFlagName;
					errPos = "cflag.csv";
					allowIndex = 1;
					break;
				case VariableCode.TCVAR:
					ret = nameToIntDics[tcvarIndex];//TCVarName;
					errPos = "tcvar.csv";
					allowIndex = 1;
					break;
				case VariableCode.CSTR:
					ret = nameToIntDics[cstrIndex];//CStrName;
					errPos = "cstr.csv";
					allowIndex = 1;
					break;

				case VariableCode.STAIN:
					ret = nameToIntDics[stainIndex];//StainName;
					errPos = "stain.csv";
					allowIndex = 1;
					break;
				case VariableCode.CDFLAGNAME1:
					ret = nameToIntDics[cdflag1Index];
					errPos = "cdflag1.csv";
					allowIndex = 0;
					break;
				case VariableCode.CDFLAGNAME2:
					ret = nameToIntDics[cdflag2Index];
					errPos = "cdflag2.csv";
					allowIndex = 0;
					break;
				case VariableCode.CDFLAG:
				{
					if (index == 1)
					{
						ret = nameToIntDics[cdflag1Index];//CDFlagName1
						errPos = "cdflag1.csv";
					}
					else if (index == 2)
					{
						ret = nameToIntDics[cdflag2Index];//CDFlagName2
						errPos = "cdflag2.csv";
					}
					else if (index >= 0)
						throw new CodeEE(GameMessages.T("Array variable ") + code.ToString() + GameMessages.T(": element number ") + (index + 1).ToString() + GameMessages.T(" cannot be specified by string"));
					else
						throw new CodeEE(GameMessages.T("Use CDFLAGNAME1 or CDFLAGNAME2 to get CDFLAG elements"));
					return ret;
				}
				case VariableCode.STR:
					ret = nameToIntDics[strnameIndex];
					errPos = "strname.csv";
					allowIndex = 0;
					break;
				case VariableCode.TSTR:
					ret = nameToIntDics[tstrnameIndex];
					errPos = "tstr.csv";
					allowIndex = 0;
					break;
				case VariableCode.SAVESTR:
					ret = nameToIntDics[savestrnameIndex];
					errPos = "savestr.csv";
					allowIndex = 0;
					break;
				case VariableCode.GLOBAL:
					ret = nameToIntDics[globalIndex];
					errPos = "global.csv";
					allowIndex = 0;
					break;
				case VariableCode.GLOBALS:
					ret = nameToIntDics[globalsIndex];
					errPos = "globals.csv";
					allowIndex = 0;
					break;
				case VariableCode.RELATION:
					ret = relationDic;
					errPos = "chara*.csv";
					allowIndex = 1;
					break;
				case VariableCode.NAME:
					ret = relationDic;
					errPos = "chara*.csv";
					allowIndex = -1;
					break;

			}
			if (index < 0)
				return ret;
			if (ret == null)
				throw new CodeEE(GameMessages.T("The elements of array variable ") + code.ToString() + GameMessages.T(" cannot be specified by string"));
			if ((index != allowIndex))
			{
				if (allowIndex < 0)//GETNUM only
					throw new CodeEE(GameMessages.T("The elements of array variable ") + code.ToString() + GameMessages.T(" cannot be specified by string"));
				throw new CodeEE(GameMessages.T("Array variable ") + code.ToString() + GameMessages.T(": element number ") + (index + 1).ToString() + GameMessages.T(" cannot be specified by string"));
			}
			return ret;
		}

		public CharacterTemplate GetCharacterTemplate(Int64 index)
		{
			//foreach (CharacterTemplate chara in CharacterTmplList)
			//{
			//	if (chara.No == index)
			//		return chara;
			//}
			//return null;

            int high = CharacterTmplList.Count - 1;
            int low = 0;
            int mid = 0;
            CharacterTemplate ct = null;
            while(low <= high)
            {
                mid = (low + high) / 2;
                ct = CharacterTmplList[mid];
                var k = ct.No;
                if(k > index)
                    high = mid - 1;
                else if(k < index)
                    low = mid + 1;
                else
                {
                    return ct;
                }
            }
            return null;
		}
		
		public CharacterTemplate GetCharacterTemplate_UseSp(Int64 index, bool sp)
		{
            //foreach (CharacterTemplate chara in CharacterTmplList)
            //{
            //	if (chara.No != index)
            //		continue;
            //	if (Config.CompatiSPChara && sp != chara.IsSpchara)
            //		continue;
            //	return chara;
            //}
            //return null;

            if(!Config.CompatiSPChara)
            {
                return GetCharacterTemplate(index);
            }
            int count = CharacterTmplList.Count;
            int high = count - 1;
            int low = 0;
            int mid = 0;
            bool found = false;
            CharacterTemplate ct = null;
            while(low <= high)
            {
                mid = (low + high) / 2;
                ct = CharacterTmplList[mid];
                var k = ct.No;
                if(k > index)
                    high = mid - 1;
                else if(k < index)
                    low = mid + 1;
                else
                {
                    found = true;
                    break;
                }
            }
            if(!found)
                return null;
            if(ct.IsSpchara == sp)
                return ct;
            for(var i = mid - 1; i >= 0; --i)
            {
                ct = CharacterTmplList[i];
                if(ct.No != index)
                    break;
                if(ct.IsSpchara == sp)
                    return ct;
            }
            for(var i = mid + 1; i < count; ++i)
            {
                ct = CharacterTmplList[i];
                if(ct.No != index)
                    break;
                if(ct.IsSpchara == sp)
                    return ct;
            }
            return null;
        }

		public CharacterTemplate GetCharacterTemplateFromCsvNo(Int64 index)
		{
            //foreach (CharacterTemplate chara in CharacterTmplList)
            //{
            //	if (chara.csvNo != index)
            //		continue;
            //	return chara;
            //}
            //return null;
            return GetCharacterTemplate(index);

        }

		public CharacterTemplate GetPseudoChara()
		{
			return new CharacterTemplate(0, this);
		}

		//private CharacterData dummyChara = null;
		//public CharacterData DummyChara
		//{
		//    get { if (dummyChara == null) dummyChara = new CharacterData(GlobalStatic.VEvaluator.Constant, GetPseudoChara(),varData); return dummyChara; }
		//    set { dummyChara = value; }
		//}

		private void loadCharacterData(string csvDir, bool disp)
		{
			if (!Directory.Exists(csvDir))
				return;
			List<KeyValuePair<string, string>> csvPaths = Config.GetFiles(csvDir, "CHARA*.CSV");
			for (int i = 0; i < csvPaths.Count; i++)
				loadCharacterDataFile(csvPaths[i].Value, csvPaths[i].Key, disp);
#if(UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            csvPaths = Config.GetFiles(csvDir, "Chara*.CSV");
            for(int i = 0; i < csvPaths.Count; i++)
                loadCharacterDataFile(csvPaths[i].Value, csvPaths[i].Key, disp);
            csvPaths = Config.GetFiles(csvDir, "CHARA*.csv");
            for(int i = 0; i < csvPaths.Count; i++)
                loadCharacterDataFile(csvPaths[i].Value, csvPaths[i].Key, disp);
            csvPaths = Config.GetFiles(csvDir, "Chara*.csv");
            for(int i = 0; i < csvPaths.Count; i++)
                loadCharacterDataFile(csvPaths[i].Value, csvPaths[i].Key, disp);
#endif
            SortCharacterTmplList();

            var count = CharacterTmplList.Count;
            CharacterTemplate tmpl = null;
            if(useCompatiName)
			{
                for(int i=0; i<count; ++i)
                {
                    tmpl = CharacterTmplList[i];
                    if(string.IsNullOrEmpty(tmpl.Callname))
                        tmpl.Callname = tmpl.Name;
                }
			}
            for(int i = 0; i < count; ++i)
            {
                tmpl = CharacterTmplList[i];
                tmpl.SetSpFlag();
            }
			Dictionary<Int64, CharacterTemplate> nList = new Dictionary<Int64, CharacterTemplate>();
			Dictionary<Int64, CharacterTemplate> spList = new Dictionary<Int64, CharacterTemplate>();
            for(int i = 0; i < count; ++i)
            {
                tmpl = CharacterTmplList[i];
                Dictionary<Int64, CharacterTemplate>  targetList = nList;
				if(Config.CompatiSPChara && tmpl.IsSpchara)
				{
					targetList = spList;
				}
                CharacterTemplate ct = null;
                if (targetList.TryGetValue(tmpl.No, out ct))
				{
					if (!Config.CompatiSPChara && (tmpl.IsSpchara!= ct.IsSpchara))
						ParserMediator.Warn(GameMessages.T("Character number ") + tmpl.No.ToString() + GameMessages.T(" is defined multiple times (to define it as an SP character, turn ON the compatibility option \"Use SP characters\")"), null, 1);
					else
						ParserMediator.Warn(GameMessages.T("Character number ") + tmpl.No.ToString() + GameMessages.T(" is defined multiple times"), null, 1);
				}
				else
					targetList.Add(tmpl.No, tmpl);
			}
		}

		private void loadCharacterDataFile(string csvPath, string csvName, bool disp)
		{
			CharacterTemplate tmpl = null;
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(csvPath, csvName))
			{
				output.PrintError(eReader.Filename + GameMessages.T(" failed to open"));
				return;
			}
			ScriptPosition position = null;
			if (disp)
				output.PrintSystemLine(eReader.Filename + GameMessages.T(" loading..."));
			try
			{
				Int64 index = -1;
				StringStream st = null;
				while ((st = eReader.ReadEnabledLine()) != null)
				{
					position = new ScriptPosition(eReader.Filename, eReader.LineNo);
					string[] tokens = st.Substring().Split(',');
					if (tokens.Length < 2)
					{
						ParserMediator.Warn(GameMessages.T("\",\" is required"), position, 1);
						continue;
					}
					if (tokens[0].Length == 0)
					{
						ParserMediator.Warn(GameMessages.T("\",\" is at the beginning"), position, 1);
						continue;
					}
					if ((tokens[0].Equals("NO", Config.SCVariable))
						|| (tokens[0].Equals("番号", Config.SCVariable)))
					{
						if (tmpl != null)
						{
							ParserMediator.Warn(GameMessages.T("The number was defined twice"), position, 1);
							continue;
						}
						if (!Int64.TryParse(tokens[1].TrimEnd(), out index))
						{
							ParserMediator.Warn(tokens[1] + GameMessages.T(" could not be converted to an integer"), position, 1);
							continue;
						}
						tmpl = new CharacterTemplate(index, this);
						string no = eReader.Filename.ToUpper();
						no = no.Substring(no.IndexOf("CHARA") + 5);
						StringBuilder sb = new StringBuilder();
						StringStream ss = new StringStream(no);
						while (!ss.EOS && char.IsDigit(ss.Current))
						{
							sb.Append(ss.Current);
							ss.ShiftNext();
						}
						if (sb.Length > 0)
							tmpl.csvNo = Convert.ToInt64(sb.ToString());
						else
							tmpl.csvNo = 0;
							//tmpl.csvNo = index;
						CharacterTmplList.Add(tmpl);
						continue;
					}
					if (tmpl == null)
					{
						ParserMediator.Warn(GameMessages.T("Other data started before the number was defined"), position, 1);
						continue;
					}
					toCharacterTemplate(position, tmpl, tokens);
				}
			}
			catch
			{
				uEmuera.Media.SystemSounds.Hand.Play();
				if (position != null)
					ParserMediator.Warn(GameMessages.T("An unexpected error occurred"), position, 3);
				else
					output.PrintError(GameMessages.T("An unexpected error occurred"));
				return;
			}
			finally
			{
				eReader.Dispose();
			}
		}

        private void SortCharacterTmplList()
        {
            CharacterTmplList.Sort((l, r) =>
            {
                return (int)(l.No - r.No);
            });
        }

		private bool tryToInt64(string str, out Int64 p)
		{
			p = -1;
			if (string.IsNullOrEmpty(str))
				return false;
			StringStream st = new StringStream(str);
			int sign = 1;
			if (st.Current == '+')
				st.ShiftNext();
			else if (st.Current == '-')
			{
				sign = -1;
				st.ShiftNext();
			}
			//1803beta005 char.IsDigit picks up full-width digits too, so...
			//if (!char.IsDigit(st.Current))
			// return false;
			switch (st.Current)
			{
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					break;
				default:
					return false;
			}
			try
			{
				p = LexicalAnalyzer.ReadInt64(st, false);
				p *= sign;
			}
			catch
			{
				return false;
			}
			return true;
		}

		private void toCharacterTemplate(ScriptPosition position, CharacterTemplate chara, string[] tokens)
		{
			if (chara == null)
				return;
			int length;
            Dictionary<int, Int64> intArray = null;
            Dictionary<int, string> strArray = null;
			Dictionary<string, int> namearray;

			string errPos = null;
			string varname = tokens[0].ToUpper();
			switch (varname)
			{
				case "NAME":
				case "名前":
					chara.Name = tokens[1];
					return;
				case "CALLNAME":
				case "呼び名":
					chara.Callname = tokens[1];
					return;
				case "NICKNAME":
				case "あだ名":
					chara.Nickname = tokens[1];
					return;
				case "MASTERNAME":
				case "主人の呼び方":
					chara.Mastername = tokens[1];
					return;
				case "MARK":
				case "刻印":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.MARK)];
					intArray = chara.Mark;
					namearray = nameToIntDics[markIndex];
					errPos = "mark.csv";
					break;
				case "EXP":
				case "経験":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.EXP)];
					intArray = chara.Exp;
					namearray = nameToIntDics[expIndex];//ExpName;
					errPos = "exp.csv";
					break;
				case "ABL":
				case "能力":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.ABL)];
					intArray = chara.Abl;
					namearray = nameToIntDics[ablIndex];//AblName;
					errPos = "abl.csv";
					break;
				case "BASE":
				case "基礎":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.MAXBASE)];
					intArray = chara.Maxbase;
					namearray = nameToIntDics[baseIndex];//BaseName;
					errPos = "base.csv";
					break;
				case "TALENT":
				case "素質":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.TALENT)];
					intArray = chara.Talent;
					namearray = nameToIntDics[talentIndex];//TalentName;
					errPos = "talent.csv";
					break;
				case "RELATION":
				case "相性":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.RELATION)];
					intArray = chara.Relation;
					namearray = null;
					break;
				case "CFLAG":
				case "フラグ":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.CFLAG)];
					intArray = chara.CFlag;
					namearray = nameToIntDics[cflagIndex];//CFlagName;
					errPos = "cflag.csv";
					break;
				case "EQUIP":
				case "装着物":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.EQUIP)];
					intArray = chara.Equip;
					namearray = nameToIntDics[equipIndex];//EquipName;
					errPos = "equip.csv";
					break;
				case "JUEL":
				case "珠":
					length = CharacterIntArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)];
					intArray = chara.Juel;
					namearray = nameToIntDics[paramIndex];//ParamName;
					errPos = "palam.csv";
					break;
				case "CSTR":
					length = CharacterStrArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.CSTR)];
					strArray = chara.CStr;
					namearray = nameToIntDics[cstrIndex];//CStrName;
					errPos = "cstr.csv";
					break;
				default:
					ParserMediator.Warn(string.Format(GameMessages.UnrecognizedIdentifier, tokens[0]), position, 1);
					return;
			}
			if (length < 0)
			{
				ParserMediator.Warn(GameMessages.T("Program error"), position, 3);
				return;
			}
			if (length == 0)
			{
				ParserMediator.Warn(varname + GameMessages.T(" is a variable with forbidding set"), position, 2);
				return;
			}
			bool p1isNumeric = tryToInt64(tokens[1].TrimEnd(), out long p1);
			if (p1isNumeric && ((p1 < 0) || (p1 >= length)))
			{
				ParserMediator.Warn(p1.ToString() + GameMessages.T(" is outside the array range"), position, 1);
				return;
			}
			int index = (int)p1;
			if ((!p1isNumeric) && (namearray != null))
			{
				if (!namearray.TryGetValue(tokens[1], out index))
				{
					ParserMediator.Warn(errPos + GameMessages.T(" does not contain a definition of \"") + tokens[1] + GameMessages.T("\""), position, 1);
					//ParserMediator.Warn("\"" + tokens[1] + "\" is an uninterpretable identifier", position, 1);
					return;
				}
				else if (index >= length)
				{
					ParserMediator.Warn(GameMessages.T("\"") + tokens[1] + GameMessages.T("\" is outside the array range"), position, 1);
					return;
				}
			}

			if ((index < 0) || (index >= length))
			{
				if (p1isNumeric)
					ParserMediator.Warn(index.ToString() + GameMessages.T(" is outside the array range"), position, 1);
				else if (tokens[1].Length == 0)
					ParserMediator.Warn(GameMessages.T("The second identifier is missing"), position, 1);
				else
					ParserMediator.Warn(string.Format(GameMessages.UnrecognizedIdentifier, tokens[1]), position, 1);
				return;
			}
			if (strArray != null)
			{
				if (tokens.Length < 3)
					ParserMediator.Warn(GameMessages.T("The third identifier is missing"), position, 1);
				if (strArray.ContainsKey(index))
					ParserMediator.Warn(varname + GameMessages.T(" element number ") + index.ToString() + GameMessages.T(" is already defined (will be overwritten)"), position, 1);
				strArray[index] = tokens[2];
			}
			else
			{
				if ((tokens.Length < 3) || !tryToInt64(tokens[2], out long p2))
					p2 = 1;
				if (intArray.ContainsKey(index))
					ParserMediator.Warn(varname + GameMessages.T(" element number ") + index.ToString() + GameMessages.T(" is already defined (will be overwritten)"), position, 1);
				intArray[index] = p2;
			}
		}


		private void loadDataTo(string csvPath, int targetIndex, Int64[] targetI, bool disp)
		{

			// Use case-insensitive file resolution for non-Windows systems
			string resolvedPath = uEmuera.Utils.ResolveExistingFilePath(csvPath);
			if (string.IsNullOrEmpty(resolvedPath))
				return;
			string[] target = names[targetIndex];
            HashSet<int> defined = new HashSet<int>();
			EraStreamReader eReader = new EraStreamReader(false);
			if (!eReader.Open(resolvedPath))
			{
				output.PrintError(eReader.Filename + GameMessages.T(" failed to open"));
				return;
			}
			ScriptPosition position = null;

			if (disp || Program.AnalysisMode)
				output.PrintSystemLine(eReader.Filename + GameMessages.T(" loading..."));
			try
			{
				StringStream st = null;
				while ((st = eReader.ReadEnabledLine()) != null)
				{
					position = new ScriptPosition(eReader.Filename, eReader.LineNo);
					string[] tokens = st.Substring().Split(',');
					if (tokens.Length < 2)
					{
						ParserMediator.Warn(GameMessages.T("\",\" is required"), position, 1);
						continue;
					}
                    if (!Int32.TryParse(tokens[0], out int index))
                    {
                        ParserMediator.Warn(GameMessages.T("The first value could not be converted to an integer"), position, 1);
                        continue;
                    }
                    if (target.Length == 0)
					{
						ParserMediator.Warn(GameMessages.T("This is a name array with forbidding set"), position, 2);
						break;
					}
					if ((index < 0) || (target.Length <= index))
					{
						ParserMediator.Warn(index.ToString() + GameMessages.T(" is outside the array range"), position, 1);
						continue;
					}
                    if (!defined.Add(index))
                        ParserMediator.Warn(index.ToString() + GameMessages.T("-th element is already defined (will be overwritten with the new value)"), position, 1);
					target[index] = tokens[1];
					if ((targetI != null) && (tokens.Length >= 3))
					{

                        if (!Int64.TryParse(tokens[2].TrimEnd(), out long price))
                        {
                            ParserMediator.Warn(GameMessages.T("Could not read the price"), position, 1);
                            continue;
                        }

                        targetI[index] = price;
					}
				}
			}
			catch
			{
				uEmuera.Media.SystemSounds.Hand.Play();
				if (position != null)
					ParserMediator.Warn(GameMessages.T("An unexpected error occurred"), position, 3);
				else
					output.PrintError(GameMessages.T("An unexpected error occurred"));
				return;
			}
			finally
			{
				eReader.Close();
			}


		}
	}

	internal sealed class CharacterTemplate
	{
        readonly int[] arraySize;
        readonly int cstrSize;

		public string Name;
		public string Callname;
		public string Nickname;
		public string Mastername;
		public readonly Int64 No;
		public readonly Dictionary<Int32, Int64> Maxbase = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> Mark = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> Exp = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> Abl = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> Talent = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> Relation = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> CFlag = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> Equip = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, Int64> Juel = new Dictionary<Int32, Int64>();
		public readonly Dictionary<Int32, string> CStr = new Dictionary<Int32, string>();
		public Int64 csvNo;
		public bool IsSpchara { get; private set; }
		
		public CharacterTemplate(Int64 index, ConstantData constant)
		{
			arraySize = constant.CharacterIntArrayLength;
			cstrSize = constant.CharacterStrArrayLength[(int)(VariableCode.__LOWERCASE__ & VariableCode.CSTR)];
			No = index;
		}
		public int ArrayStrLength(CharacterStrData type)
		{
			switch (type)
			{
				case CharacterStrData.CSTR:
					return cstrSize;
				default:
					throw new CodeEE(GameMessages.T("Referenced a nonexistent key"));
			}
		}

		public int ArrayLength(CharacterIntData type)
		{
			switch (type)
			{
				case CharacterIntData.BASE:
					{
						int size = arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.BASE)];
						int maxSize = arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.MAXBASE)];
						return size > maxSize ? size : maxSize;
					}
				case CharacterIntData.MARK:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.MARK)];
				case CharacterIntData.ABL:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.ABL)];
				case CharacterIntData.EXP:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.EXP)];
				case CharacterIntData.RELATION:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.RELATION)];
				case CharacterIntData.TALENT:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.TALENT)];
				case CharacterIntData.CFLAG:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.CFLAG)];
				case CharacterIntData.EQUIP:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.EQUIP)];
				case CharacterIntData.JUEL:
					return arraySize[(int)(VariableCode.__LOWERCASE__ & VariableCode.JUEL)];
				default:
					throw new CodeEE(GameMessages.T("Referenced a nonexistent key"));
			}
		}

		internal void SetSpFlag()
		{
			//bool res;
			if (CFlag.ContainsKey(0) && CFlag[0] != 0L)
				IsSpchara = true;
		}
	}
}
