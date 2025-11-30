using System;
using System.Collections.Generic;
using System.Text;

namespace MinorShift.Emuera.GameProc
{
	enum InputType
	{
		EnterKey = 1,// Enter key or click
		AnyKey = 2,// Any input
		IntValue = 3,// Integer value. Whether OneInput or not is determined by a separate variable
		StrValue = 4,// String value.
		Void = 5,// Input disabled. Can only wait - becomes no-op during skip or macro mode

		// Added in version 1823
		PrimitiveMouseKey = 11,

	}
	

	// Added in version 1819: Part of a plan to reduce coupling between input/display systems and Data/Process systems
	// Goal is to add as much cushioning as possible between layers. Eventually for separate threading.

	// TODO: Consider whether to reuse or dispose this class each time
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
