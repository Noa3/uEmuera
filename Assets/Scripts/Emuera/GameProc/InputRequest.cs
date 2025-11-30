using System;
using System.Collections.Generic;
using System.Text;

namespace MinorShift.Emuera.GameProc
{
	enum InputType
	{
		EnterKey = 1,  // Enter key or click / Enterキーかクリック
		AnyKey = 2,    // Any input / なんでもいいから入力
		IntValue = 3,  // Integer value. OneInput or not is in a separate variable / 整数値。OneInputかどうかは別の変数で
		StrValue = 4,  // String / 文字列。
		Void = 5,      // No input possible. Just wait → Skip or macro cancels it / 入力不能。待つしかない→スキップ中orマクロ中ならなかったことになる

		//1823
		PrimitiveMouseKey = 11,

	}
	

	// 1819 Added: Plan to weaken the coupling between input/display and Data/Process systems
	// Want to put cushioning in between as much as possible. Eventually separate threads
	// 1819追加 入力・表示系とData、Process系の結合を弱くしよう計画の一つ
	// できるだけ間にクッションをおいていきたい。最終的には別スレッドに

	// Should this class be disposable or reusable? / クラスを毎回使い捨てるのはどうなんだろう 使いまわすべきか
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
