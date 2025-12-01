//using System;
//using System.Runtime.InteropServices;
//using UnityEngine;

namespace MinorShift._Library
{
	/// <summary>
	/// wrapされたtimer.outfromは,このTickCountだけを呼び出す.
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
		/// 起動whenにBeginPeriod,endwhenにEndPeriodを呼び出すbecauseだけのインスタンス.
		/// staticなデストラクタがあればいらnotんだけど
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
		/// currentのフレームの描画に使うbecauseのミリ秒数
		/// </summary>
		public static uint CurrentFrameTime;
		/// <summary>
		/// フレーム描画start合図のwhenpointでのミリ秒を固定becauseのnumeric
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
