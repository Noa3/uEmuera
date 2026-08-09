//using System;
//using System.Runtime.InteropServices;
//using UnityEngine;

namespace MinorShift._Library
{
	/// <summary>
	/// A wrapped timer. From outside, call only this TickCount.
	/// </summary>
	internal sealed class WinmmTimer
	{
		static WinmmTimer()
		{
			instance = new WinmmTimer();
		}
		private WinmmTimer()
		{
			//mm_BeginPeriod(1);
		}
		//~WinmmTimer()
		//{
		//	mm_EndPeriod(1);
		//}

		/// <summary>
		/// An instance used only to call BeginPeriod at startup and EndPeriod at shutdown.
		/// A static destructor would make this unnecessary, though.
		/// </summary>
		private static volatile WinmmTimer instance;

		public static uint TickCount
        {
            get
            {
                return (uint)(System.DateTime.Now.Ticks / 10000);
            }
        }
		/// <summary>
		/// The milliseconds to be used for rendering the current frame
		/// </summary>
		public static uint CurrentFrameTime;
		/// <summary>
		/// A value to pin the milliseconds at the moment the frame-render start signal fires
		/// </summary>
		public static void FrameStart() { CurrentFrameTime =TickCount; }

        //[DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        //private static extern uint mm_GetTime();
        //[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        //private static extern uint mm_BeginPeriod(uint uMilliseconds);
        //[DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        //private static extern uint mm_EndPeriod(uint uMilliseconds);
    }
}
