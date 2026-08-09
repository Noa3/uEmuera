using System;
using System.Collections.Generic;
using System.Text;

namespace MinorShift.Emuera.GameData.Variable
{
	//Obfuscation attribute. Set (Exclude=true) when using enum.ToString() or enum.Parse().
	[global::System.Reflection.Obfuscation(Exclude=true)]
	internal enum VariableCode
	{
		__NULL__ = 0x00000000,
        __CAN_FORBID__ = 0x00010000,
		__INTEGER__ = 0x00020000,
		__STRING__ = 0x00040000,
		__ARRAY_1D__ = 0x00080000,
		__CHARACTER_DATA__ = 0x00100000,//first argument omittable; filled in with TARGET
		__UNCHANGEABLE__ = 0x00400000,//Unchangeable attribute
		__CALC__ = 0x00800000,//Calculated value
		__EXTENDED__ = 0x01000000,//Variables added by Emuera
		__LOCAL__ = 0x02000000,//Local variable.
		__GLOBAL__ = 0x04000000,//Global variable.
		__ARRAY_2D__ = 0x08000000,//2D array. Mutually exclusive with the character variable flag
		__SAVE_EXTENDED__ = 0x10000000,//Variables that should be saved by the extended save feature.
							//Setting this flag saves them automatically (in theory). Note that renaming prevents correct loading.
        __ARRAY_3D__ = 0x20000000,//3D array
        __CONSTANT__ = 0x40000000,//Fully constant data read from CSV; the ~NAME family falls under this

		__UPPERCASE__ = 0x7FFF0000,
		__LOWERCASE__ = 0x0000FFFF,

		__COUNT_SAVE_INTEGER__ = 0x00,//Actually all are arrays
		__COUNT_INTEGER__ = 0x00,
		//PALAMLV, EXPLV, RESULT, COUNT, TARGET, SELECTCOM cannot be set to forbidden
		DAY = 0x00 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Elapsed days.
		MONEY = 0x01 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Money
		ITEM = 0x02 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Number held
		FLAG = 0x03 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Flag
		TFLAG = 0x04 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Temporary flag
		UP = 0x05 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Increase value of the training parameter. index refers to PALAM.CSV.
		PALAMLV = 0x06 | __INTEGER__ | __ARRAY_1D__,//Boundary values for leveling the training parameter. Crossing them increases the number of jewels.
		EXPLV = 0x07 | __INTEGER__ | __ARRAY_1D__,//Boundary values for level dividing experience. Crossing them raises the training effect.
		EJAC = 0x08 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Temporary variable for the orgasm check.
		DOWN = 0x09 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Decrease value of the training parameter. index refers to PALAM.CSV
		RESULT = 0x0A | __INTEGER__ | __ARRAY_1D__,//Return value (numerical)
		COUNT = 0x0B | __INTEGER__ | __ARRAY_1D__,//Loop counter
		TARGET = 0x0C | __INTEGER__ | __ARRAY_1D__,//"Registration number" of the character being trained
		ASSI = 0x0D | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//"Registration number" of the assistant character
		MASTER = 0x0E | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//"Registration number" of the protagonist character. Usually 0
		NOITEM = 0x0F | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Does the item not exist? 1 when set to non-existent. GAMEBASE.CSV
		LOSEBASE = 0x10 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Decrease value of base parameters. Usually LOSEBASE:0 is stamina loss, LOSEBASE:1 is spirit loss.
		SELECTCOM = 0x11 | __INTEGER__ | __ARRAY_1D__,//Selected command. Same as in TRAIN.CSV
		ASSIPLAY = 0x12 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Is the assistant currently training? 1 = true, 0 = false
		PREVCOM = 0x13 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Previous command.
		NOTUSE_14 = 0x14 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Area where RAND was stored in eramaker.
		NOTUSE_15 = 0x15 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Area where CHARANUM was stored in eramaker.
		TIME = 0x16 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Time
		ITEMSALES = 0x17 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Is it on sale?
		PLAYER = 0x18 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Registration number of the character doing the training. Usually MASTER or ASSI
		NEXTCOM = 0x19 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Registration number of the character doing the training. Usually MASTER or ASSI
		PBAND = 0x1A | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Item number of the penis band
		BOUGHT = 0x1B | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//Item number purchased most recently
		NOTUSE_1C = 0x1C | __INTEGER__ | __ARRAY_1D__,//Unused area
		NOTUSE_1D = 0x1D | __INTEGER__ | __ARRAY_1D__,//Unused area
		A = 0x1E | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//General-purpose variable
        B = 0x1F | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        C = 0x20 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        D = 0x21 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        E = 0x22 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        F = 0x23 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        G = 0x24 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        H = 0x25 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        I = 0x26 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        J = 0x27 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        K = 0x28 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        L = 0x29 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        M = 0x2A | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        N = 0x2B | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        O = 0x2C | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        P = 0x2D | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        Q = 0x2E | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        R = 0x2F | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        S = 0x30 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        T = 0x31 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        U = 0x32 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        V = 0x33 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        W = 0x34 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        X = 0x35 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        Y = 0x36 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
        Z = 0x37 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,
		NOTUSE_38 = 0x38 | __INTEGER__ | __ARRAY_1D__,//Unused area
		NOTUSE_39 = 0x39 | __INTEGER__ | __ARRAY_1D__,//Unused area
		NOTUSE_3A = 0x3A | __INTEGER__ | __ARRAY_1D__,//Unused area
		NOTUSE_3B = 0x3B | __INTEGER__ | __ARRAY_1D__,//Unused area
		__COUNT_SAVE_INTEGER_ARRAY__ = 0x3C,

		ITEMPRICE = 0x3C | __INTEGER__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CAN_FORBID__,//Item price
		LOCAL = 0x3D | __INTEGER__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__,//Local variable
		ARG = 0x3E | __INTEGER__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__,//For function arguments
		GLOBAL = 0x3F | __INTEGER__ | __ARRAY_1D__ | __GLOBAL__ | __EXTENDED__ | __CAN_FORBID__,//Global numerical variable
		RANDDATA = 0x40 | __INTEGER__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__,//Global numerical variable
		__COUNT_INTEGER_ARRAY__ = 0x41,


		SAVESTR = 0x00 | __STRING__ | __ARRAY_1D__ | __CAN_FORBID__,//String data. Saved
		__COUNT_SAVE_STRING_ARRAY__ = 0x01,


		//RESULTS cannot be set to forbidden
		STR = 0x01 | __STRING__ | __ARRAY_1D__ | __CAN_FORBID__,//String data. STR.CSV. Rewritable.
		RESULTS = 0x02 | __STRING__ | __ARRAY_1D__,//In fact this is also an array
		LOCALS = 0x03 | __STRING__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__, //Local string variable
		ARGS = 0x04 | __STRING__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__,//For function arguments
		GLOBALS = 0x05 | __STRING__ | __ARRAY_1D__ | __GLOBAL__ | __EXTENDED__ | __CAN_FORBID__, //Global string variable
		TSTR = 0x06 | __STRING__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,

		__COUNT_STRING_ARRAY__ = 0x07,



		SAVEDATA_TEXT = 0x00 | __STRING__ | __EXTENDED__, //String used at save time. The kind that can be added via PUTFORM
		__COUNT_SAVE_STRING__ = 0x00,
		__COUNT_STRING__ = 0x01,






		ISASSI = 0x00 | __INTEGER__ | __CHARACTER_DATA__,//Is assistant? 1 = true, 0 = false
		NO = 0x01 | __INTEGER__ | __CHARACTER_DATA__,//Character number

		__COUNT_SAVE_CHARACTER_INTEGER__ = 0x02,//These apparently are not arrays.
		__COUNT_CHARACTER_INTEGER__ = 0x02,

		BASE = 0x00 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Base parameters.
		MAXBASE = 0x01 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Maximum value of base parameters.
		ABL = 0x02 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Abilities. ABL.CSV
		TALENT = 0x03 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Talents. TALENT.CSV
		EXP = 0x04 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Experience. EXP.CSV
		MARK = 0x05 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Marks. MARK.CSV
		PALAM = 0x06 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Training parameter. PALAM.CSV
		SOURCE = 0x07 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Training parameter. Training source produced by the immediately preceding command.
		EX = 0x08 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Training parameter. Where and how many times climaxed during this training.
		CFLAG = 0x09 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Flag.
		JUEL = 0x0A | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Jewels. PALAM.CSV
		RELATION = 0x0B | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Relationship. index is character number, not registration number
		EQUIP = 0x0C | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Unused variable
		TEQUIP = 0x0D | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Training parameter. Whether an item is in use. ITEM.CSV
		STAIN = 0x0E | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__,//Training parameter. Soiling
		GOTJUEL = 0x0F | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Training parameter. Jewels gained this time. PALAM.CSV
		NOWEX = 0x10 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//Training parameter. Where and how many times climaxed in the immediately preceding command.
        DOWNBASE = 0x11 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__, //Training parameter. Character variable version of LOSEBASE
        CUP = 0x12 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//Training parameter. Character variable version of UP
        CDOWN = 0x13 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//Training parameter. Character variable version of DOWN
        TCVAR = 0x14 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//Temporary variable for character variables


		__COUNT_SAVE_CHARACTER_INTEGER_ARRAY__ = 0x11,
		__COUNT_CHARACTER_INTEGER_ARRAY__ = 0x54,

		NAME = 0x00 | __STRING__ | __CHARACTER_DATA__,//Name//Referenced by registration number
		CALLNAME = 0x01 | __STRING__ | __CHARACTER_DATA__,//Call name
		NICKNAME = 0x02 | __STRING__ | __CHARACTER_DATA__ | __SAVE_EXTENDED__ | __EXTENDED__,//Nickname
		MASTERNAME = 0x03 | __STRING__ | __CHARACTER_DATA__ | __SAVE_EXTENDED__ | __EXTENDED__,//Nickname

		__COUNT_SAVE_CHARACTER_STRING__ = 0x02,
		__COUNT_CHARACTER_STRING__ = 0x04,

		CSTR = 0x00 | __STRING__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//Character-data string array

		__COUNT_SAVE_CHARACTER_STRING_ARRAY__ = 0x00,
		__COUNT_CHARACTER_STRING_ARRAY__ = 0x01,

		CDFLAG = 0x00 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_2D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,

		__COUNT_CHARACTER_INTEGER_ARRAY_2D__ = 0x01,

		__COUNT_CHARACTER_STRING_ARRAY_2D__ = 0x00,


		DITEMTYPE = 0x00 | __INTEGER__ | __ARRAY_2D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
		DA = 0x01 | __INTEGER__ | __ARRAY_2D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
		DB = 0x02 | __INTEGER__ | __ARRAY_2D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
		DC = 0x03 | __INTEGER__ | __ARRAY_2D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
		DD = 0x04 | __INTEGER__ | __ARRAY_2D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
		DE = 0x05 | __INTEGER__ | __ARRAY_2D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
        __COUNT_INTEGER_ARRAY_2D__ = 0x06,

		__COUNT_STRING_ARRAY_2D__ = 0x00,

		TA = 0x00 | __INTEGER__ | __ARRAY_3D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
        TB = 0x01 | __INTEGER__ | __ARRAY_3D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,
        __COUNT_INTEGER_ARRAY_3D__ = 0x02,

        __COUNT_STRING_ARRAY_3D__ = 0x00,

		//For CALC variables the numeric order does not matter.
		//1803beta004 ～～ For the ~NAME family the order matters because ConstantData uses it
		
		RAND = 0x00 | __INTEGER__ | __ARRAY_1D__ | __CALC__ | __UNCHANGEABLE__,//Random number. Returns a value from 0 to argument-1.
		CHARANUM = 0x01 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__,//Character count. Returns the number of registered characters.

		ABLNAME = 0x00 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//Abilities. ABL.CSV//Data read from CSV is not saved. Unchangeable
		EXPNAME = 0x01 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//Experience. EXP.CSV
		TALENTNAME = 0x02 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//Talents. TALENT.CSV
		PALAMNAME = 0x03 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//Abilities. PALAM.CSV
		TRAINNAME = 0x04 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Training names. TRAIN.CSV
		MARKNAME = 0x05 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//Marks. MARK.CSV
		ITEMNAME = 0x06 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//Items. ITEM.CSV
		BASENAME = 0x07 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Base ability names. BASE.CSV
		SOURCENAME = 0x08 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Training source names. SOURCE.CSV
		EXNAME = 0x09 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Climax names. EX.CSV
		__DUMMY_STR__ = 0x0A | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__,
		EQUIPNAME = 0x0B | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Equipment names. EQUIP.CSV
		TEQUIPNAME = 0x0C | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Training-time equipment names. TEQUIP.CSV
		FLAGNAME = 0x0D | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Flag names. FLAG.CSV
		TFLAGNAME = 0x0E | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Temporary flag names. TFLAG.CSV
		CFLAGNAME = 0x0F | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//Character flag names. CFLAG.CSV
		TCVARNAME = 0x10 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		CSTRNAME = 0x11 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		STAINNAME = 0x12 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,

		CDFLAGNAME1 = 0x13 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		CDFLAGNAME2 = 0x14 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		STRNAME = 0x15 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		TSTRNAME = 0x16 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		SAVESTRNAME = 0x17 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		GLOBALNAME = 0x18 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,
		GLOBALSNAME = 0x19 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,

        __COUNT_CSV_STRING_ARRAY_1D__ = 0x1A,


		GAMEBASE_AUTHER = 0x04 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//String type. Author. Misspelled but kept for compatibility.
		GAMEBASE_AUTHOR = 0x00 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//String type. Author
		GAMEBASE_INFO = 0x01 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//String type. Additional information
		GAMEBASE_YEAR = 0x02 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//String type. Production year
		GAMEBASE_TITLE = 0x03 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//String type. Title
		WINDOW_TITLE = 0x05 | __STRING__ | __CALC__ | __EXTENDED__,//String type. Window title. Changeable.
		//Adding a variable enclosed by double underscores requires special handling in VariableToken.
		__FILE__ = 0x06 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Name of the file currently being executed
		__FUNCTION__ = 0x07 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Name of the function currently being executed
        MONEYLABEL = 0x08 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Label for money
        DRAWLINESTR = 0x09 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Draw string of DRAWLINE
        EMUERA_VERSION = 0x0A | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__, //Emuera version

		LASTLOAD_TEXT = 0x05 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type.

		GAMEBASE_GAMECODE = 0x00 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type. Code
		GAMEBASE_VERSION = 0x01 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type. Version
		GAMEBASE_ALLOWVERSION = 0x02 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type. Accept different versions
		GAMEBASE_DEFAULTCHARA = 0x03 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type. Character present from the start
		GAMEBASE_NOITEM = 0x04 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type. No item

		LASTLOAD_VERSION = 0x05 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type.
		LASTLOAD_NO = 0x06 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Numeric type.
		__LINE__ = 0x07 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Line number currently being executed
		LINECOUNT = 0x08 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Total number of drawn lines. Decreased by CLEAR
        ISTIMEOUT = 0x0B | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Whether TINPUT-family etc. timed out?

        __INT_MAX__ = 0x09 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Int64 maximum value
        __INT_MIN__ = 0x0A | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Int64 minimum value

		CVAR = 0xFC | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __EXTENDED__,//User-defined variable
		CVARS = 0xFC | __STRING__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __EXTENDED__,//User-defined variable
		CVAR2D = 0xFC | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_2D__ | __EXTENDED__,//User-defined variable
		CVARS2D = 0xFC | __STRING__ | __CHARACTER_DATA__ | __ARRAY_2D__ | __EXTENDED__,//User-defined variable
		//CVAR3D = 0xFC | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,//User-defined variable
		//CVARS3D = 0xFC | __STRING__ | __ARRAY_3D__ | __EXTENDED__,//User-defined variable
		REF = 0xFD | __INTEGER__ | __ARRAY_1D__ | __EXTENDED__,//Reference type
		REFS = 0xFD | __STRING__ | __ARRAY_1D__ | __EXTENDED__,
		REF2D = 0xFD | __INTEGER__ | __ARRAY_2D__ | __EXTENDED__,
		REFS2D = 0xFD | __STRING__ | __ARRAY_2D__ | __EXTENDED__,
		REF3D = 0xFD | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,
		REFS3D = 0xFD | __STRING__ | __ARRAY_3D__ | __EXTENDED__,
		VAR = 0xFE | __INTEGER__ | __ARRAY_1D__ | __EXTENDED__,//User-defined variable 1808 Does not distinguish private variables from wide-area variables
		VARS = 0xFE | __STRING__ | __ARRAY_1D__ | __EXTENDED__,//User-defined variable
		VAR2D = 0xFE | __INTEGER__ | __ARRAY_2D__ | __EXTENDED__,//User-defined variable
		VARS2D = 0xFE | __STRING__ | __ARRAY_2D__ | __EXTENDED__,//User-defined variable
		VAR3D = 0xFE | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,//User-defined variable
		VARS3D = 0xFE | __STRING__ | __ARRAY_3D__ | __EXTENDED__,//User-defined variable
		//PRIVATE = 0xFF | __INTEGER__ | __ARRAY_1D__ | __EXTENDED__,//Private variable
		//PRIVATES = 0xFF | __STRING__ | __ARRAY_1D__ | __EXTENDED__,//Private variable
		//PRIVATE2D = 0xFF | __INTEGER__ | __ARRAY_2D__ | __EXTENDED__,//Private variable
		//PRIVATES2D = 0xFF | __STRING__ | __ARRAY_2D__ | __EXTENDED__,//Private variable
		//PRIVATE3D = 0xFF | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,//Private variable
		//PRIVATES3D = 0xFF | __STRING__ | __ARRAY_3D__ | __EXTENDED__,//Private variable
	}
}

