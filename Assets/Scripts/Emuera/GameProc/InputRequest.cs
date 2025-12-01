using System;
using System.Collections.Generic;
using System.Text;

namespace MinorShift.Emuera.GameProc
{
	enum InputType
	{
		EnterKey = 1,//Enterkeyかクリック
		AnyKey = 2,//なんでもいいfrominput
		IntValue = 3,//integervalue.OneInputかどうかは別のvariableで
		StrValue = 4,//string.
		Void = 5,//input不能.待つしかnot→スキップinsideorマクロinsideならなかったthisになる

		//1823
		PrimitiveMouseKey = 11,

	}
	

	// 1819add input-display系とData,Process系の結合を弱くしよう計画の一つ
	// canだけ間にクッションをおいていきたい.最終的には別スレッドに

	//classを毎回使い捨てるのはどうなんだろう 使いまわすべきか
	internal sealed class InputRequest
	{
		public InputRequest()
		{
			ID = LastRequestID++;
		}
		public readonly Int64 ID;
		public InputType InputType;
		public bool NeedValue
		{ 
			get 
			{ 
				return (InputType == InputType.IntValue || InputType == InputType.StrValue
					|| InputType == InputType.PrimitiveMouseKey); 
			} 
		}
		public bool OneInput = false;
		public bool StopMesskip = false;
		public bool IsSystemInput = false;

		public bool HasDefValue = false;
		public long DefIntValue;
		public string DefStrValue;

		public long Timelimit = -1;
		public bool DisplayTime;
		public string TimeUpMes;

		static Int64 LastRequestID = 0;
	}
}
