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
		__CHARACTER_DATA__ = 0x00100000,//第一argumentを省略possible.TARGETで補う
		__UNCHANGEABLE__ = 0x00400000,//変更不可attribute
		__CALC__ = 0x00800000,//calculatevalue
		__EXTENDED__ = 0x01000000,//Emueraでaddしたvariable
		__LOCAL__ = 0x02000000,//ローカルvariable.
		__GLOBAL__ = 0x04000000,//グローバルvariable.
		__ARRAY_2D__ = 0x08000000,//二次originalarray.charactervariableフラグと排他
		__SAVE_EXTENDED__ = 0x10000000,//拡張save機能によってsavedoべきvariable.
							//このフラグを立てておけば勝手にsavebe done(はず).nameを変えると正常にloadできなくなるのでcaution.
        __ARRAY_3D__ = 0x20000000,//三次originalarray
        __CONSTANT__ = 0x40000000,//完全定数CSVfrom読み込まれる～NAME系がこれに該当

		__UPPERCASE__ = 0x7FFF0000,
		__LOWERCASE__ = 0x0000FFFF,

		__COUNT_SAVE_INTEGER__ = 0x00,//実はallarray
		__COUNT_INTEGER__ = 0x00,
		//PALAMLV, EXPLV, RESULT, COUNT, TARGET, SELECTCOMはprohibitedsetting不可
		DAY = 0x00 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//経過日数.
		MONEY = 0x01 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//金
		ITEM = 0x02 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//所持数
		FLAG = 0x03 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//フラグ
		TFLAG = 0x04 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//一whenフラグ
		UP = 0x05 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータのabove昇value.indexはPALAM.CSVのthing.
		PALAMLV = 0x06 | __INTEGER__ | __ARRAY_1D__,//調教insideパラメータのレベルreasonの境界value.境界valueを越えると珠の数が多くなる.
		EXPLV = 0x07 | __INTEGER__ | __ARRAY_1D__,//経験のレベルreasonの境界value.境界valueを越えると調教の効果がaboveがる.
		EJAC = 0x08 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//射精チェックのbecauseの一whenvariable.
		DOWN = 0x09 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータの減少value.indexはPALAM.CSVのthing
		RESULT = 0x0A | __INTEGER__ | __ARRAY_1D__,//戻りvalue(numeric)
		COUNT = 0x0B | __INTEGER__ | __ARRAY_1D__,//繰り返しカウンター
		TARGET = 0x0C | __INTEGER__ | __ARRAY_1D__,//調教insideのキャラの"登録番号"
		ASSI = 0x0D | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//助手のキャラの"登録番号"
		MASTER = 0x0E | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//主人公のキャラの"登録番号".通常0
		NOITEM = 0x0F | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//アイテムがdoes not existか？does not existsettingなら１．GAMEBASE.CSV
		LOSEBASE = 0x10 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//基礎パラメータの減少value.通常はLOSEBASE:0が体力の消耗,LOSEBASE:1が気力の消耗.
		SELECTCOM = 0x11 | __INTEGER__ | __ARRAY_1D__,//選択されたcommand.TRAIN.CSVのthingとsame
		ASSIPLAY = 0x12 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//助手current調教しているか？1 = true, 0 = false
		PREVCOM = 0x13 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//前回のcommand.
		NOTUSE_14 = 0x14 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//eramakerではRANDが格納されている領域.
		NOTUSE_15 = 0x15 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//eramakerではCHARANUMが格納されている領域.
		TIME = 0x16 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//when刻
		ITEMSALES = 0x17 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//売っているか？
		PLAYER = 0x18 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//調教している人間のキャラの登録番号.通常はMASTERかASSI
		NEXTCOM = 0x19 | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//調教している人間のキャラの登録番号.通常はMASTERかASSI
		PBAND = 0x1A | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//ペニスバンドのアイテム番号
		BOUGHT = 0x1B | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//直before 購入したアイテム番号
		NOTUSE_1C = 0x1C | __INTEGER__ | __ARRAY_1D__,//未use領域
		NOTUSE_1D = 0x1D | __INTEGER__ | __ARRAY_1D__,//未use領域
		A = 0x1E | __INTEGER__ | __ARRAY_1D__ | __CAN_FORBID__,//汎forvariable
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
		NOTUSE_38 = 0x38 | __INTEGER__ | __ARRAY_1D__,//未use領域
		NOTUSE_39 = 0x39 | __INTEGER__ | __ARRAY_1D__,//未use領域
		NOTUSE_3A = 0x3A | __INTEGER__ | __ARRAY_1D__,//未use領域
		NOTUSE_3B = 0x3B | __INTEGER__ | __ARRAY_1D__,//未use領域
		__COUNT_SAVE_INTEGER_ARRAY__ = 0x3C,

		ITEMPRICE = 0x3C | __INTEGER__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CAN_FORBID__,//アイテム価格
		LOCAL = 0x3D | __INTEGER__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__,//ローカルvariable
		ARG = 0x3E | __INTEGER__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__,//functionのargumentfor
		GLOBAL = 0x3F | __INTEGER__ | __ARRAY_1D__ | __GLOBAL__ | __EXTENDED__ | __CAN_FORBID__,//グローバルnumerictypevariable
		RANDDATA = 0x40 | __INTEGER__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__,//グローバルnumerictypevariable
		__COUNT_INTEGER_ARRAY__ = 0x41,


		SAVESTR = 0x00 | __STRING__ | __ARRAY_1D__ | __CAN_FORBID__,//stringdata.保存be done
		__COUNT_SAVE_STRING_ARRAY__ = 0x01,


		//RESULTSはprohibitedsetting不可
		STR = 0x01 | __STRING__ | __ARRAY_1D__ | __CAN_FORBID__,//stringdata.STR.CSV.書き換えpossible.
		RESULTS = 0x02 | __STRING__ | __ARRAY_1D__,//実はこいつもarray
		LOCALS = 0x03 | __STRING__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__, //ローカルstringvariable
		ARGS = 0x04 | __STRING__ | __ARRAY_1D__ | __LOCAL__ | __EXTENDED__ | __CAN_FORBID__,//functionのargumentfor
		GLOBALS = 0x05 | __STRING__ | __ARRAY_1D__ | __GLOBAL__ | __EXTENDED__ | __CAN_FORBID__, //グローバルstringvariable
		TSTR = 0x06 | __STRING__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,

		__COUNT_STRING_ARRAY__ = 0x07,



		SAVEDATA_TEXT = 0x00 | __STRING__ | __EXTENDED__, //savewhenにつかうstring.PUTFORMでaddcanやつ
		__COUNT_SAVE_STRING__ = 0x00,
		__COUNT_STRING__ = 0x01,






		ISASSI = 0x00 | __INTEGER__ | __CHARACTER_DATA__,//助手か？1 = ture, 0 = false
		NO = 0x01 | __INTEGER__ | __CHARACTER_DATA__,//キャラ番号

		__COUNT_SAVE_CHARACTER_INTEGER__ = 0x02,//こいつらはarrayではnotらしい.
		__COUNT_CHARACTER_INTEGER__ = 0x02,

		BASE = 0x00 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//基礎パラメータ.
		MAXBASE = 0x01 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//基礎パラメータの最大value.
		ABL = 0x02 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//能力.ABL.CSV
		TALENT = 0x03 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//素質.TALENT.CSV
		EXP = 0x04 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//経験.EXP.CSV
		MARK = 0x05 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//刻印.MARK.CSV
		PALAM = 0x06 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータ.PALAM.CSV
		SOURCE = 0x07 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータ.直previous commandで発生した調教ソース.
		EX = 0x08 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータ.この調教inside,どこで何回絶頂したか.
		CFLAG = 0x09 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//フラグ.
		JUEL = 0x0A | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//珠.PALAM.CSV
		RELATION = 0x0B | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//関係.indexは登録番号ではなくキャラ番号
		EQUIP = 0x0C | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//未usevariable
		TEQUIP = 0x0D | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータ.アイテムをuseinsideか.ITEM.CSV
		STAIN = 0x0E | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__,//調教insideパラメータ.汚れ
		GOTJUEL = 0x0F | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータ.今回獲得した珠.PALAM.CSV
		NOWEX = 0x10 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __CAN_FORBID__,//調教insideパラメータ.直previous commandでどこで何回絶頂したか.
        DOWNBASE = 0x11 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__, //調教insideパラメータ.LOSEBASEのcharactervariable版
        CUP = 0x12 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//調教insideパラメータ.UPのcharactervariable版
        CDOWN = 0x13 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//調教insideパラメータ.DOWNのcharactervariable版
        TCVAR = 0x14 | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//charactervariableでの一whenvariable


		__COUNT_SAVE_CHARACTER_INTEGER_ARRAY__ = 0x11,
		__COUNT_CHARACTER_INTEGER_ARRAY__ = 0x54,

		NAME = 0x00 | __STRING__ | __CHARACTER_DATA__,//name//登録番号で呼び出す
		CALLNAME = 0x01 | __STRING__ | __CHARACTER_DATA__,//呼び名
		NICKNAME = 0x02 | __STRING__ | __CHARACTER_DATA__ | __SAVE_EXTENDED__ | __EXTENDED__,//あだ名
		MASTERNAME = 0x03 | __STRING__ | __CHARACTER_DATA__ | __SAVE_EXTENDED__ | __EXTENDED__,//あだ名

		__COUNT_SAVE_CHARACTER_STRING__ = 0x02,
		__COUNT_CHARACTER_STRING__ = 0x04,

		CSTR = 0x00 | __STRING__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __SAVE_EXTENDED__ | __EXTENDED__ | __CAN_FORBID__,//characterforstringarray

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

		//CALCなvariableについては番号順はどうでもいい.
		//1803beta004 ～～NAME系については番号順をConstantDataがusedoので重要
		
		RAND = 0x00 | __INTEGER__ | __ARRAY_1D__ | __CALC__ | __UNCHANGEABLE__,//乱数.０～argument-1untilのvalueを返す.
		CHARANUM = 0x01 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__,//character数.character登録数を返す.

		ABLNAME = 0x00 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//能力.ABL.CSV//csvfrom読まれるdataは保存されnot.変更不可
		EXPNAME = 0x01 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//経験.EXP.CSV
		TALENTNAME = 0x02 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//素質.TALENT.CSV
		PALAMNAME = 0x03 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//能力.PALAM.CSV
		TRAINNAME = 0x04 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//調教名.TRAIN.CSV
		MARKNAME = 0x05 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//刻印.MARK.CSV
		ITEMNAME = 0x06 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __CONSTANT__ | __CAN_FORBID__,//アイテム.ITEM.CSV
		BASENAME = 0x07 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//基礎能力名.BASE.CSV
		SOURCENAME = 0x08 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//調教ソース名.SOURCE.CSV
		EXNAME = 0x09 | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//絶頂名.EX.CSV
		__DUMMY_STR__ = 0x0A | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__,
		EQUIPNAME = 0x0B | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//装着物名.EQUIP.CSV
		TEQUIPNAME = 0x0C | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//調教when装着物名.TEQUIP.CSV
		FLAGNAME = 0x0D | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//フラグ名.FLAG.CSV
		TFLAGNAME = 0x0E | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//一whenフラグ名.TFLAG.CSV
		CFLAGNAME = 0x0F | __STRING__ | __ARRAY_1D__ | __UNCHANGEABLE__ | __EXTENDED__ | __CONSTANT__ | __CAN_FORBID__,//characterフラグ名.CFLAG.CSV
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


		GAMEBASE_AUTHER = 0x04 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//stringtype.作者.綴りを間違えていたが互換性のbecause残す.
		GAMEBASE_AUTHOR = 0x00 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//stringtype.作者
		GAMEBASE_INFO = 0x01 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//stringtype.addinfo
		GAMEBASE_YEAR = 0x02 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//stringtype.製作年
		GAMEBASE_TITLE = 0x03 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//stringtype.タイトル
		WINDOW_TITLE = 0x05 | __STRING__ | __CALC__ | __EXTENDED__,//stringtype.ウインドウのタイトル.変更possible.
		//アンダースコア2つで囲まれたvariableをaddしたらVariableTokenに特別なprocessが必要.
		__FILE__ = 0x06 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//current実lineinsideのfile名
		__FUNCTION__ = 0x07 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//current実lineinsideのfunction名
        MONEYLABEL = 0x08 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//お金のラベル
        DRAWLINESTR = 0x09 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//DRAWLINEの描画string
        EMUERA_VERSION = 0x0A | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__, //Emeuraのバージョン

		LASTLOAD_TEXT = 0x05 | __STRING__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.

		GAMEBASE_GAMECODE = 0x00 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.コード
		GAMEBASE_VERSION = 0x01 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.バージョン
		GAMEBASE_ALLOWVERSION = 0x02 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.バージョン違い認める
		GAMEBASE_DEFAULTCHARA = 0x03 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.最初fromいるキャラ
		GAMEBASE_NOITEM = 0x04 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.アイテムなし

		LASTLOAD_VERSION = 0x05 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.
		LASTLOAD_NO = 0x06 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//numerictype.
		__LINE__ = 0x07 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//current実lineinsideのline番号
		LINECOUNT = 0x08 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//描画したlineの総数.CLEARで減少
        ISTIMEOUT = 0x0B | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//TINPUT系等がTIMEOUTしたか？

        __INT_MAX__ = 0x09 | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Int64最大value
        __INT_MIN__ = 0x0A | __INTEGER__ | __CALC__ | __UNCHANGEABLE__ | __EXTENDED__,//Int64最小value

		CVAR = 0xFC | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __EXTENDED__,//ユーザーdefinitionvariable
		CVARS = 0xFC | __STRING__ | __CHARACTER_DATA__ | __ARRAY_1D__ | __EXTENDED__,//ユーザーdefinitionvariable
		CVAR2D = 0xFC | __INTEGER__ | __CHARACTER_DATA__ | __ARRAY_2D__ | __EXTENDED__,//ユーザーdefinitionvariable
		CVARS2D = 0xFC | __STRING__ | __CHARACTER_DATA__ | __ARRAY_2D__ | __EXTENDED__,//ユーザーdefinitionvariable
		//CVAR3D = 0xFC | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,//ユーザーdefinitionvariable
		//CVARS3D = 0xFC | __STRING__ | __ARRAY_3D__ | __EXTENDED__,//ユーザーdefinitionvariable
		REF = 0xFD | __INTEGER__ | __ARRAY_1D__ | __EXTENDED__,//参照type
		REFS = 0xFD | __STRING__ | __ARRAY_1D__ | __EXTENDED__,
		REF2D = 0xFD | __INTEGER__ | __ARRAY_2D__ | __EXTENDED__,
		REFS2D = 0xFD | __STRING__ | __ARRAY_2D__ | __EXTENDED__,
		REF3D = 0xFD | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,
		REFS3D = 0xFD | __STRING__ | __ARRAY_3D__ | __EXTENDED__,
		VAR = 0xFE | __INTEGER__ | __ARRAY_1D__ | __EXTENDED__,//ユーザーdefinitionvariable 1808 プライベートvariableと広域variableを区別しnot
		VARS = 0xFE | __STRING__ | __ARRAY_1D__ | __EXTENDED__,//ユーザーdefinitionvariable
		VAR2D = 0xFE | __INTEGER__ | __ARRAY_2D__ | __EXTENDED__,//ユーザーdefinitionvariable
		VARS2D = 0xFE | __STRING__ | __ARRAY_2D__ | __EXTENDED__,//ユーザーdefinitionvariable
		VAR3D = 0xFE | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,//ユーザーdefinitionvariable
		VARS3D = 0xFE | __STRING__ | __ARRAY_3D__ | __EXTENDED__,//ユーザーdefinitionvariable
		//PRIVATE = 0xFF | __INTEGER__ | __ARRAY_1D__ | __EXTENDED__,//プライベートvariable
		//PRIVATES = 0xFF | __STRING__ | __ARRAY_1D__ | __EXTENDED__,//プライベートvariable
		//PRIVATE2D = 0xFF | __INTEGER__ | __ARRAY_2D__ | __EXTENDED__,//プライベートvariable
		//PRIVATES2D = 0xFF | __STRING__ | __ARRAY_2D__ | __EXTENDED__,//プライベートvariable
		//PRIVATE3D = 0xFF | __INTEGER__ | __ARRAY_3D__ | __EXTENDED__,//プライベートvariable
		//PRIVATES3D = 0xFF | __STRING__ | __ARRAY_3D__ | __EXTENDED__,//プライベートvariable
	}
}

