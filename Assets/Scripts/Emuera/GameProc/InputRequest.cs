using System;
using System.Collections.Generic;
using System.Text;

namespace MinorShift.Emuera.GameProc
{
	enum InputType
	{
		EnterKey = 1,//Enter key or click
		AnyKey = 2,//any input is fine
		IntValue = 3,//integer value. whether OneInput is decided by another variable
		StrValue = 4,//string.
		Void = 5,//no input possible. can only wait → treated as if it didn't happen when skipping or in a macro

		//1823
		PrimitiveMouseKey = 11,

		// Emuera EM/EE Extensions - BINPUT types (button-only input)
		BIntValue = 21,//button-only integer input
		BStrValue = 22,//button-only string input
	}
	

	// 1819 addition one of the plans to loosen the coupling between input/display related, Data, and Process systems
	// want to put a cushion in between as much as possible. eventually on a separate thread

	//not sure throwing away the class each time is best. should it be reused?
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
					|| InputType == InputType.PrimitiveMouseKey
					|| InputType == InputType.BIntValue || InputType == InputType.BStrValue); 
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
