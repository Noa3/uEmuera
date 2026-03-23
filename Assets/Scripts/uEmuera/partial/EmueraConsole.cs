using System;

namespace MinorShift.Emuera.GameView
{
    internal sealed partial class EmueraConsole : IDisposable
    {
        internal ConsoleDisplayLine GetDisplayLinesForuEmuera(int index)
        {
            if(index < 0 || index >= displayLineList.Count)
                return null;
            return displayLineList[index];
        }
        internal int GetDisplayLinesCount()
        {
            return displayLineList.Count;
        }
        internal bool IsInitializing
        {
            get { return state == ConsoleState.Initializing; }
        }
        internal int LastButtonGeneration
        {
            get { return lastButtonGeneration; }
        }
        internal bool IsWaitingInput
        {
            get { return state == ConsoleState.WaitInput; }
        }
        /// <summary>
        /// Returns true if the console is waiting for any value input (including button-only types).
        /// Non-button inputs should be blocked when this is true.
        /// </summary>
        internal bool IsWaitingInputSomething
        {
            get {
                return state == ConsoleState.WaitInput &&
                          inputReq != null &&
                          (inputReq.InputType == GameProc.InputType.IntValue || 
                          inputReq.InputType == GameProc.InputType.StrValue ||
                          inputReq.InputType == GameProc.InputType.BIntValue ||
                          inputReq.InputType == GameProc.InputType.BStrValue);
            }
        }
        /// <summary>
        /// Returns true if the console is waiting for button-only input (BINPUT/BINPUTS).
        /// Only button clicks should be accepted, background clicks should be ignored.
        /// </summary>
        internal bool IsWaitingButtonOnlyInput
        {
            get {
                return state == ConsoleState.WaitInput &&
                          inputReq != null &&
                          (inputReq.InputType == GameProc.InputType.BIntValue ||
                          inputReq.InputType == GameProc.InputType.BStrValue);
            }
        }
        internal GameProc.InputType InputType
        {
            get
            {
                if(inputReq == null)
                    return GameProc.InputType.Void;
                return inputReq.InputType;
            }
        }
    }
}
