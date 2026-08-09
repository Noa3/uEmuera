using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameProc;


namespace MinorShift.Emuera.GameData.Function
{
	internal static partial class FunctionMethodCreator
	{
		static FunctionMethodCreator()
		{
            methodList = new Dictionary<string, FunctionMethod>
            {
                //キャラクタデータ系
                ["GETCHARA"] = new GetcharaMethod(),
                ["GETSPCHARA"] = new GetspcharaMethod(),
                ["CSVNAME"] = new CsvStrDataMethod(CharacterStrData.NAME),
                ["CSVCALLNAME"] = new CsvStrDataMethod(CharacterStrData.CALLNAME),
                ["CSVNICKNAME"] = new CsvStrDataMethod(CharacterStrData.NICKNAME),
                ["CSVMASTERNAME"] = new CsvStrDataMethod(CharacterStrData.MASTERNAME),
                ["CSVCSTR"] = new CsvcstrMethod(),
                ["CSVBASE"] = new CsvDataMethod(CharacterIntData.BASE),
                ["CSVABL"] = new CsvDataMethod(CharacterIntData.ABL),
                ["CSVMARK"] = new CsvDataMethod(CharacterIntData.MARK),
                ["CSVEXP"] = new CsvDataMethod(CharacterIntData.EXP),
                ["CSVRELATION"] = new CsvDataMethod(CharacterIntData.RELATION),
                ["CSVTALENT"] = new CsvDataMethod(CharacterIntData.TALENT),
                ["CSVCFLAG"] = new CsvDataMethod(CharacterIntData.CFLAG),
                ["CSVEQUIP"] = new CsvDataMethod(CharacterIntData.EQUIP),
                ["CSVJUEL"] = new CsvDataMethod(CharacterIntData.JUEL),
                ["FINDCHARA"] = new FindcharaMethod(false),
                ["FINDLASTCHARA"] = new FindcharaMethod(true),
                ["EXISTCSV"] = new ExistCsvMethod(),

                //汎用処理系
                ["VARSIZE"] = new VarsizeMethod(),
                ["CHKFONT"] = new CheckfontMethod(),
                ["CHKDATA"] = new CheckdataMethod(EraSaveFileType.Normal),
                ["ISSKIP"] = new IsSkipMethod(),
                ["MOUSESKIP"] = new MesSkipMethod(true),
                ["MESSKIP"] = new MesSkipMethod(false),
                ["GETCOLOR"] = new GetColorMethod(false),
                ["GETDEFCOLOR"] = new GetColorMethod(true),
                ["GETFOCUSCOLOR"] = new GetFocusColorMethod(),
                ["GETBGCOLOR"] = new GetBGColorMethod(false),
                ["GETDEFBGCOLOR"] = new GetBGColorMethod(true),
                ["GETSTYLE"] = new GetStyleMethod(),
                ["GETFONT"] = new GetFontMethod(),
                ["BARSTR"] = new BarStringMethod(),
                ["CURRENTALIGN"] = new CurrentAlignMethod(),
                ["CURRENTREDRAW"] = new CurrentRedrawMethod(),
                ["COLOR_FROMNAME"] = new ColorFromNameMethod(),
                ["COLOR_FROMRGB"] = new ColorFromRGBMethod(),

                //TODO:1810
                //methodList["CHKVARDATA"] = new CheckdataStrMethod(EraSaveFileType.Var);
                ["CHKCHARADATA"] = new CheckdataStrMethod(EraSaveFileType.CharVar),
                //methodList["CHKGLOBALDATA"] = new CheckdataMethod(EraSaveFileType.Global);
                //methodList["FIND_VARDATA"] = new FindFilesMethod(EraSaveFileType.Var);
                ["FIND_CHARADATA"] = new FindFilesMethod(EraSaveFileType.CharVar),

                //定数取得
                ["MONEYSTR"] = new MoneyStrMethod(),
                ["PRINTCPERLINE"] = new GetPrintCPerLineMethod(),
                ["PRINTCLENGTH"] = new PrintCLengthMethod(),
                ["SAVENOS"] = new GetSaveNosMethod(),
                ["GETTIME"] = new GettimeMethod(),
                ["GETTIMES"] = new GettimesMethod(),
                ["GETMILLISECOND"] = new GetmsMethod(),
                ["GETSECOND"] = new GetSecondMethod(),

                //数学関数
                ["RAND"] = new RandMethod(),
                ["MIN"] = new MaxMethod(false),
                ["MAX"] = new MaxMethod(true),
                ["ABS"] = new AbsMethod(),
                ["POWER"] = new PowerMethod(),
                ["SQRT"] = new SqrtMethod(),
                ["CBRT"] = new CbrtMethod(),
                ["LOG"] = new LogMethod(),
                ["LOG10"] = new LogMethod(10.0d),
                ["EXPONENT"] = new ExpMethod(),
                ["SIGN"] = new SignMethod(),
                ["LIMIT"] = new GetLimitMethod(),

                //変数操作系
                ["SUMARRAY"] = new SumArrayMethod(),
                ["SUMCARRAY"] = new SumArrayMethod(true),
                ["MATCH"] = new MatchMethod(),
                ["CMATCH"] = new MatchMethod(true),
                ["GROUPMATCH"] = new GroupMatchMethod(),
                ["NOSAMES"] = new NosamesMethod(),
                ["ALLSAMES"] = new AllsamesMethod(),
                ["MAXARRAY"] = new MaxArrayMethod(),
                ["MAXCARRAY"] = new MaxArrayMethod(true),
                ["MINARRAY"] = new MaxArrayMethod(false, false),
                ["MINCARRAY"] = new MaxArrayMethod(true, false),
                ["GETBIT"] = new GetbitMethod(),
                ["GETNUM"] = new GetnumMethod(),
                ["GETPALAMLV"] = new GetPalamLVMethod(),
                ["GETEXPLV"] = new GetExpLVMethod(),
                ["FINDELEMENT"] = new FindElementMethod(false),
                ["FINDLASTELEMENT"] = new FindElementMethod(true),
                ["INRANGE"] = new InRangeMethod(),
                ["INRANGEARRAY"] = new InRangeArrayMethod(),
                ["INRANGECARRAY"] = new InRangeArrayMethod(true),
                ["GETNUMB"] = new GetnumMethod(),

                ["ARRAYMSORT"] = new ArrayMultiSortMethod(),

                //文字列操作系
                ["STRLENS"] = new StrlenMethod(),
                ["STRLENSU"] = new StrlenuMethod(),
                ["SUBSTRING"] = new SubstringMethod(),
                ["SUBSTRINGU"] = new SubstringuMethod(),
                ["STRFIND"] = new StrfindMethod(false),
                ["STRFINDU"] = new StrfindMethod(true),
                ["STRCOUNT"] = new StrCountMethod(),
                ["TOSTR"] = new ToStrMethod(),
                ["TOINT"] = new ToIntMethod(),
                ["TOUPPER"] = new StrChangeStyleMethod(StrFormType.Upper),
                ["TOLOWER"] = new StrChangeStyleMethod(StrFormType.Lower),
                ["TOHALF"] = new StrChangeStyleMethod(StrFormType.Half),
                ["TOFULL"] = new StrChangeStyleMethod(StrFormType.Full),
                ["LINEISEMPTY"] = new LineIsEmptyMethod(),
                ["REPLACE"] = new ReplaceMethod(),
                ["UNICODE"] = new UnicodeMethod(),
                ["UNICODEBYTE"] = new UnicodeByteMethod(),
                ["CONVERT"] = new ConvertIntMethod(),
                ["ISNUMERIC"] = new IsNumericMethod(),
                ["ESCAPE"] = new EscapeMethod(),
                ["ENCODETOUNI"] = new EncodeToUniMethod(),
                ["CHARATU"] = new CharAtMethod(),
                ["GETLINESTR"] = new GetLineStrMethod(),
                ["STRFORM"] = new StrFormMethod(),
                ["STRJOIN"] = new JoinMethod(),

                ["GETCONFIG"] = new GetConfigMethod(true),
                ["GETCONFIGS"] = new GetConfigMethod(false),

                //html系
                ["HTML_GETPRINTEDSTR"] = new HtmlGetPrintedStrMethod(),
                ["HTML_POPPRINTINGSTR"] = new HtmlPopPrintingStrMethod(),
                ["HTML_TOPLAINTEXT"] = new HtmlToPlainTextMethod(),
                ["HTML_ESCAPE"] = new HtmlEscapeMethod(),
                ["HTML_STRINGLINES"] = new HtmlStringLinesMethod(),
                ["ERDNAME"] = new ErdNameMethod(),


                //画像処理系
                ["SPRITECREATED"] = new SpriteStateMethod(),
                ["SPRITEWIDTH"] = new SpriteStateMethod(),
                ["SPRITEHEIGHT"] = new SpriteStateMethod(),
                ["SPRITEMOVE"] = new SpriteSetPosMethod(),
                ["SPRITESETPOS"] = new SpriteSetPosMethod(),
                ["SPRITEPOSX"] = new SpriteStateMethod(),
                ["SPRITEPOSY"] = new SpriteStateMethod(),
                ["SPRITEEXIST"] = new SpriteStateMethod(),

                ["CLIENTWIDTH"] = new ClientSizeMethod(),
                ["CLIENTHEIGHT"] = new ClientSizeMethod(),

                ["GETKEY"] = new GetKeyStateMethod(),
                ["GETKEYTRIGGERED"] = new GetKeyStateMethod(),
                ["MOUSEX"] = new MousePosMethod(),
                ["MOUSEY"] = new MousePosMethod(),
                ["ISACTIVE"] = new IsActiveMethod(),
                ["SAVETEXT"] = new SaveTextMethod(),
                ["LOADTEXT"] = new LoadTextMethod(),

                ["GCREATED"] = new GraphicsStateMethod(),// ("GCREATED");
                ["GWIDTH"] = new GraphicsStateMethod(),//("GWIDTH");
                ["GHEIGHT"] = new GraphicsStateMethod(),//("GHEIGHT");
                ["GGETCOLOR"] = new GraphicsGetColorMethod(),
                ["SPRITEGETCOLOR"] = new SpriteGetColorMethod(),

                ["GCREATE"] = new GraphicsCreateMethod(),
                ["GCREATEFROMFILE"] = new GraphicsCreateFromFileMethod(),
                ["GDISPOSE"] = new GraphicsDisposeMethod(),
                ["GCLEAR"] = new GraphicsClearMethod(),
                ["GFILLRECTANGLE"] = new GraphicsFillRectangleMethod(),
                ["GDRAWLINE"] = new GraphicsDrawLineMethod(),
                ["GDRAWSPRITE"] = new GraphicsDrawSpriteMethod(),
                ["GSETCOLOR"] = new GraphicsSetColorMethod(),
                ["GDRAWG"] = new GraphicsDrawGMethod(),
                ["GDRAWGWITHMASK"] = new GraphicsDrawGWithMaskMethod(),

                ["GSETBRUSH"] = new GraphicsSetBrushMethod(),
                ["GSETFONT"] = new GraphicsSetFontMethod(),
                ["GSETPEN"] = new GraphicsSetPenMethod(),

                ["SPRITECREATE"] = new SpriteCreateMethod(),
                ["SPRITEDISPOSE"] = new SpriteDisposeMethod(),
                ["SPRITEDISPOSEALL"] = new SpriteDisposeAllMethod(),

                ["CBGSETG"] = new CBGSetGraphicsMethod(),
                ["CBGSETSPRITE"] = new CBGSetCIMGMethod(),
                ["CBGCLEAR"] = new CBGClearMethod(),

                ["CBGCLEARBUTTON"] = new CBGClearButtonMethod(),
                ["CBGREMOVERANGE"] = new CBGRemoveRangeMethod(),
                ["CBGREMOVEBMAP"] = new CBGRemoveBMapMethod(),
                ["CBGSETBMAPG"] = new CBGSetBMapGMethod(),
                ["CBGSETBUTTONSPRITE"] = new CBGSETButtonSpriteMethod(),

                ["GSAVE"] = new GraphicsSaveMethod(),
                ["GLOAD"] = new GraphicsLoadMethod(),


                ["SPRITEANIMECREATE"] = new SpriteAnimeCreateMethod(),
                ["SPRITEANIMEADDFRAME"] = new SpriteAnimeAddFrameMethod(),
                ["SETANIMETIMER"] = new SetAnimeTimerMethod(),

                // Emuera EM/EE Extensions
                ["EXISTFUNCTION"] = new ExistFunctionMethod(),
                ["EXISTSOUND"] = new ExistSoundMethod(),
                ["FLOOR"] = new FloorMethod(),
                ["CEILING"] = new CeilingMethod(),
                ["ROUND"] = new RoundMethod(),

                // XML document commands
                ["XML_DOCUMENT"] = new XmlDocumentMethod(XmlDocumentMethod.Operation.Create),
                ["XML_RELEASE"] = new XmlDocumentMethod(XmlDocumentMethod.Operation.Release),
                ["XML_EXIST"] = new XmlDocumentMethod(XmlDocumentMethod.Operation.Check),
                ["XML_GET"] = new XmlGetMethod(),
                ["XML_GET_BYNAME"] = new XmlGetMethod(true),
                ["XML_SET"] = new XmlSetMethod(),
                ["XML_SET_BYNAME"] = new XmlSetMethod(true),
                ["XML_TOSTR"] = new XmlToStrMethod(),
                ["XML_ADDNODE"] = new XmlAddNodeMethod(XmlAddNodeMethod.Operation.Node),
                ["XML_ADDNODE_BYNAME"] = new XmlAddNodeMethod(XmlAddNodeMethod.Operation.Node, true),
                ["XML_REMOVENODE"] = new XmlRemoveNodeMethod(XmlRemoveNodeMethod.Operation.Node),
                ["XML_REMOVENODE_BYNAME"] = new XmlRemoveNodeMethod(XmlRemoveNodeMethod.Operation.Node, true),
                ["XML_ADDATTRIBUTE"] = new XmlAddNodeMethod(XmlAddNodeMethod.Operation.Attribute),
                ["XML_ADDATTRIBUTE_BYNAME"] = new XmlAddNodeMethod(XmlAddNodeMethod.Operation.Attribute, true),
                ["XML_REMOVEATTRIBUTE"] = new XmlRemoveNodeMethod(XmlRemoveNodeMethod.Operation.Attribute),
                ["XML_REMOVEATTRIBUTE_BYNAME"] = new XmlRemoveNodeMethod(XmlRemoveNodeMethod.Operation.Attribute, true),
                ["XML_REPLACE"] = new XmlReplaceMethod(),
                ["XML_REPLACE_BYNAME"] = new XmlReplaceMethod(true),
                ["MAP_CREATE"] = new MapCreateMethod(),
                ["MAP_EXIST"] = new MapExistMethod(),
                ["MAP_RELEASE"] = new MapReleaseMethod(),
                ["MAP_GET"] = new MapGetMethod(),
                ["MAP_HAS"] = new MapHasMethod(),
                ["MAP_SET"] = new MapSetMethod(),
                ["MAP_REMOVE"] = new MapRemoveMethod(),
                ["MAP_CLEAR"] = new MapClearMethod(),
                ["MAP_SIZE"] = new MapSizeMethod(),
                ["MAP_GETKEYS"] = new MapGetKeysMethod(),
                ["MAP_TOXML"] = new MapToXmlMethod(),
                ["MAP_FROMXML"] = new MapFromXmlMethod(),
                ["FLOWINPUT"] = new FlowInputMethod(),
                ["FLOWINPUTS"] = new FlowInputsMethod(),

                // DataTable commands (DT_*)
                ["DT_CREATE"] = new DtManageMethod(DtManageMethod.Op.Create),
                ["DT_EXIST"] = new DtManageMethod(DtManageMethod.Op.Check),
                ["DT_RELEASE"] = new DtManageMethod(DtManageMethod.Op.Release),
                ["DT_NOCASE"] = new DtManageMethod(DtManageMethod.Op.Case),
                ["DT_CLEAR"] = new DtManageMethod(DtManageMethod.Op.Clear),
                ["DT_ROW_COUNT"] = new DtManageMethod(DtManageMethod.Op.RowCount),
                ["DT_ROW_ADD"] = new DtManageMethod(DtManageMethod.Op.RowAdd),
                ["DT_COLUMN_ADD"] = new DtColMethod(DtColMethod.Op.Add),
                ["DT_COLUMN_NAMES"] = new DtColMethod(DtColMethod.Op.Names),
                ["DT_COLUMN_EXIST"] = new DtColMethod(DtColMethod.Op.Check),
                ["DT_COLUMN_REMOVE"] = new DtColMethod(DtColMethod.Op.Remove),
                ["DT_ROW_REMOVE"] = new DtRowOpMethod(DtRowOpMethod.Op.Remove),
                ["DT_GET"] = new DtRowOpMethod(DtRowOpMethod.Op.GetStr),
                ["DT_GETINT"] = new DtRowOpMethod(DtRowOpMethod.Op.GetInt),
                ["DT_SET"] = new DtRowOpMethod(DtRowOpMethod.Op.SetStr),
                ["DT_SETINT"] = new DtRowOpMethod(DtRowOpMethod.Op.SetInt),
                ["DT_FIND"] = new DtRowOpMethod(DtRowOpMethod.Op.Find),
                ["DT_SORT"] = new DtRowOpMethod(DtRowOpMethod.Op.Sort),
                ["DT_TOCSV"] = new DtRowOpMethod(DtRowOpMethod.Op.ToCsv),
                ["DT_TOXML"] = new DtRowOpMethod(DtRowOpMethod.Op.ToXml),

                // New built-in commands
                ["CLEARMEMORY"] = new ClearMemoryMethod(),
                ["EXISTFILE"] = new ExistFileMethod(),
                ["EXISTVAR"] = new ExistVarMethod(),
                ["ENUMFILES"] = new EnumFilesMethod(),
                ["DT_ROW_LENGTH"] = new DtRowLengthMethod(),
                ["DT_CELL_GET"] = new DtCellGetMethod(DtCellGetMethod.Op.GetInt),
                ["DT_CELL_GETS"] = new DtCellGetMethod(DtCellGetMethod.Op.GetStr),
                ["DT_CELL_ISNULL"] = new DtCellGetMethod(DtCellGetMethod.Op.IsNull),
                 ["DT_SELECT"] = new DtSelectMethod(),

                 // ERA standard built-ins
                 ["GETDOINGFUNCTION"] = new GetDoingFunctionMethod(),
                 ["HTML_STRINGLEN"] = new HtmlStringLenMethod(),
                 ["GETVAR"] = new GetVarMethod(),
                 ["GETVARS"] = new GetVarsMethod(),
             };


            //1823 自分の関数名を知っていた方が何かと便利なので覚えさせることにした
            foreach (var pair in methodList)
				pair.Value.SetMethodName(pair.Key);
        }

		private static readonly Dictionary<string, FunctionMethod> methodList;
		public static Dictionary<string, FunctionMethod> GetMethodList()
		{
			return methodList;
		}
	}
}