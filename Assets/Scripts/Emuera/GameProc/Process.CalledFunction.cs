using System;
using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.Sub;
using MinorShift.Emuera.GameData;
using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Function;
using MinorShift.Emuera.GameData.Variable;

namespace MinorShift.Emuera.GameProc
{

	internal sealed class UserDefinedFunctionArgument
	{
		public UserDefinedFunctionArgument(IOperandTerm[] srcArgs, VariableTerm[] destArgs)
		{
			Arguments = srcArgs;
			TransporterInt = new Int64[Arguments.Length];
			TransporterStr = new string[Arguments.Length];
			TransporterRef = new Array[Arguments.Length];
			isRef = new bool[Arguments.Length];
			for (int i = 0; i < Arguments.Length; i++)
			{
				isRef[i] = destArgs[i].Identifier.IsReference;
			}
		}
		public readonly IOperandTerm[] Arguments;
		public readonly Int64[] TransporterInt;
		public readonly string[] TransporterStr;
		public readonly Array[] TransporterRef;
		public readonly bool[] isRef;
		public void SetTransporter(ExpressionMediator exm)
		{
			for (int i = 0; i < Arguments.Length; i++)
			{
				if (Arguments[i] == null)
					continue;
				if (isRef[i])
				{
					VariableTerm vTerm = (VariableTerm)Arguments[i];
					if (vTerm.Identifier.IsCharacterData)
					{
						Int64 charaNo = vTerm.GetElementInt(0, exm);
						if ((charaNo < 0) || (charaNo >= GlobalStatic.VariableData.CharacterList.Count))
							throw new CodeEE("characterarrayvariable" + vTerm.Identifier.Name + "の第１argument(" + charaNo.ToString() + ")はキャラ登録番号の範囲outです");
						TransporterRef[i] = (Array)vTerm.Identifier.GetArrayChara((int)charaNo);
					}
					else
						TransporterRef[i] = (Array)vTerm.Identifier.GetArray();

				}
				else if (Arguments[i].GetOperandType() == typeof(Int64))
					TransporterInt[i] = Arguments[i].GetIntValue(exm);
				else
					TransporterStr[i] = Arguments[i].GetStrValue(exm);
			}
		}
		public UserDefinedFunctionArgument Restructure(ExpressionMediator exm)
		{
			for (int i = 0; i < Arguments.Length; i++)
			{
				if (Arguments[i] == null)
					continue;
				if(isRef[i])
					Arguments[i].Restructure(exm);
				else
					Arguments[i] = Arguments[i].Restructure(exm);
			}
			return this;
		}
	}

	/// <summary>
	/// currentcallinsideのfunction
	/// eventfunctionを除いて実lineduring in部状態は変化しnotので使いまわしても良い
	/// </summary>
	internal sealed class CalledFunction
	{
		private CalledFunction(string label) { FunctionName = label; }
		public static CalledFunction CallEventFunction(Process parent, string label, LogicalLine retAddress)
		{
			CalledFunction called = new CalledFunction(label);
			//List<FunctionLabelLine> newLabelList = new List<FunctionLabelLine>();
			called.Finished = false;
			called.eventLabelList = parent.LabelDictionary.GetEventLabels(label);
			if (called.eventLabelList == null)
			{
				FunctionLabelLine line = parent.LabelDictionary.GetNonEventLabel(label);
				if (line != null)
				{
					throw new CodeEE("eventfunctionでnotfunction@" + label + "(" + line.Position.Filename + ":" + line.Position.LineNo + "line )に対しEVENTcallがlineわれました");
				}
				return null;
			}
			called.counter = -1;
			called.group = 0;
			called.ShiftNext();
			called.TopLabel = called.CurrentLabel;
			called.returnAddress = retAddress;
			called.IsEvent = true;
			return called;
		}

		public static CalledFunction CallFunction(Process parent, string label, LogicalLine retAddress)
		{
			CalledFunction called = new CalledFunction(label);
			called.Finished = false;
			FunctionLabelLine labelline = parent.LabelDictionary.GetNonEventLabel(label);
			if (labelline == null)
			{
				if (parent.LabelDictionary.GetEventLabels(label) != null)
				{
					throw new CodeEE("eventfunction@" + label + "に対し通常のCALLがlineわれました(このErrorは互換性option"" + Config.GetConfigName(ConfigCode.CompatiCallEvent) + ""にthan無視できます)");
				}
				return null;
			}
            else if (labelline.IsMethod)
            {
                throw new CodeEE("#FUCNTION(S)がdefinitionされたfunction@" + labelline.LabelName + "(" + labelline.Position.Filename + ":" + labelline.Position.LineNo.ToString() + "line )に対し通常のCALLがlineわれました");
            }
			called.TopLabel = labelline;
			called.CurrentLabel = labelline;
			called.returnAddress = retAddress;
			called.IsEvent = false;
            return called;
		}

		public static CalledFunction CreateCalledFunctionMethod(FunctionLabelLine labelline, string label)
		{
			CalledFunction called = new CalledFunction(label);
			called.TopLabel = labelline;
			called.CurrentLabel = labelline;
			called.returnAddress = null;
			called.IsEvent = false;
			return called;
		}
		
		
		static FunctionMethod tostrMethod = null;
		/// <summary>
		/// 1803beta005 予めargumentの数を合わせて規定valueを代入しておく
        /// 1806+v6.99 式insidefunctionのargumentに無効な#DIMvariableを与えているcaseにExceptionになるのを修正
		/// 1808beta009 REFtypeに対応
		/// </summary>
		public UserDefinedFunctionArgument ConvertArg(IOperandTerm[] srcArgs, out string errMes)
		{
			errMes = null;
            if (TopLabel.IsError)
            {
                errMes = TopLabel.ErrMes;
                return null;
            }
            FunctionLabelLine func = TopLabel;
            IOperandTerm[] convertedArg = new IOperandTerm[func.Arg.Length];
			if(convertedArg.Length < srcArgs.Length)
			{
				errMes = "argumentの数がfunction\"@" + func.LabelName + "\"にsettingされた数を超えています";
				return null;
			}
			IOperandTerm term;
			VariableTerm destArg;
			//bool isString = false;
			for (int i = 0; i < func.Arg.Length; i++)
			{
				term = (i < srcArgs.Length) ? srcArgs[i] : null;
				destArg = func.Arg[i];
				//isString = destArg.IsString;
				if (destArg.Identifier.IsReference)//参照渡しのcase
				{
					if (term == null)
					{
						errMes = "\"@" + func.LabelName + "\"の" + (i + 1).ToString() + "番目のargumentは参照渡しのbecause省略できません";
						return null;
					}
					VariableTerm vTerm = term as VariableTerm;
					if (vTerm == null || vTerm.Identifier.Dimension == 0)
					{
						errMes = "\"@" + func.LabelName + "\"の" + (i + 1).ToString() + "番目のargumentは参照渡しのbecauseのarrayvariableでなければなりません";
						return null;
					}
					//TODO 1810alpha007 キャラtypeを認めるかどうかはっきりしたい 今のところ認めnot方向
					//typeチェック
					if (!((ReferenceToken)destArg.Identifier).MatchType(vTerm.Identifier, false, out errMes))
					{
						errMes = "\"@" + func.LabelName + "\"の" + (i + 1).ToString() + "番目のargument:" + errMes;
						return null;
					}
				}
				else if (term == null)//argumentが省略されたとき
				{
					term = func.Def[i];//デフォルトvalueを代入
					//1808beta001 デフォルトvalueがnotcaseはErrorにdo
					//一応逃がす
					if (term == null && !Config.CompatiFuncArgOptional)
					{
						errMes = "\"@" + func.LabelName + "\"の" + (i + 1).ToString() + "番目のargumentは省略できません(このwarningは互換性option"" + Config.GetConfigName(ConfigCode.CompatiFuncArgOptional) + ""にthan無視できます)";
						return null;
					}
				}
				else if (term.GetOperandType() != destArg.GetOperandType())
				{
					if (term.GetOperandType() == typeof(string))
					{
						errMes = "\"@" + func.LabelName + "\"の" + (i + 1).ToString() + "番目のargumentをstringtypefromintegertypeにconvertできません";
						return null;
					}
					else
					{
						if (!Config.CompatiFuncArgAutoConvert)
						{
							errMes = "\"@" + func.LabelName + "\"の" + (i + 1).ToString() + "番目のargumentをintegertypefromstringtypeにconvertできません(このwarningは互換性option"" + Config.GetConfigName(ConfigCode.CompatiFuncArgAutoConvert) + ""にthan無視できます)";
							return null;
						}
						if (tostrMethod == null)
							tostrMethod = FunctionMethodCreator.GetMethodList()["TOSTR"];
						term = new FunctionMethodTerm(tostrMethod, new IOperandTerm[] { term });
					}
				}
				convertedArg[i] = term;
			}
			return new UserDefinedFunctionArgument(convertedArg, func.Arg);
		}

		public LogicalLine CallLabel(Process parent, string label)
		{
			return parent.LabelDictionary.GetLabelDollar(label, this.CurrentLabel);
		}

        public void updateRetAddress(LogicalLine line)
        {
            returnAddress = line;
        }

		public CalledFunction Clone()
		{
			CalledFunction called = new CalledFunction(this.FunctionName);
			called.eventLabelList = this.eventLabelList;
			called.CurrentLabel = this.CurrentLabel;
			called.TopLabel = this.TopLabel;
			called.group = this.group;
			called.IsEvent = this.IsEvent;

			called.counter = this.counter;
			called.returnAddress = this.returnAddress;
			return called;
		}

		List<FunctionLabelLine>[] eventLabelList;
		public FunctionLabelLine CurrentLabel { get; private set; }
		public FunctionLabelLine TopLabel { get; private set; }
		int counter = -1;
		int group = 0;
		LogicalLine returnAddress;
		public readonly string FunctionName = "";
		public bool IsJump { get; set; }
		public bool Finished { get; private set; }
		public LogicalLine ReturnAddress
		{
			get { return returnAddress; }
		}
		public bool IsEvent{get; private set;}

		public bool HasSingleFlag
		{
			get
			{
				if (CurrentLabel == null)
					return false;
				return CurrentLabel.IsSingle;
			}
		}


		#region eventfunction専for
		public void ShiftNext()
		{
			while (true)
			{
				counter++;
				if (eventLabelList[group].Count > counter)
				{
					CurrentLabel = (eventLabelList[group])[counter];
					return;
				}
				group++;
				counter = -1;
				if (group >= 4)
				{
					CurrentLabel = null;
					return;
				}
			}
		}

		public void ShiftNextGroup()
		{
			counter = -1;
			group++;
            if (group >= 4)
            {
                CurrentLabel = null;
                return;
            }
			ShiftNext();
		}

        public void FinishEvent()
        {
            group = 4;
            counter = -1;
            CurrentLabel = null;
            return;
        }

        public bool IsOnly
        {
            get { return CurrentLabel.IsOnly; }
        }
		#endregion
	}
}
