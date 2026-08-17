using System;
using System.Diagnostics;
using System.Threading;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Development-build guard: asserts that Emuera semantic state is only mutated
    /// from the single authoritative interpreter thread (uEmuera Phase 6 #76).
    ///
    /// Usage:
    /// <code>
    ///   // At interpreter-thread entry (EmueraThread.Work):
    ///   InterpreterThreadGuard.SetOwner();
    ///
    ///   // At every semantic-mutation site:
    ///   InterpreterThreadGuard.AssertOwner("LabelDictionary.AddLabel");
    /// </code>
    ///
    /// All methods are <see cref="ConditionalAttribute"/>-guarded on
    /// <c>UEMUERA_DEBUG</c> so they compile away in release builds.
    /// </summary>
    internal static class InterpreterThreadGuard
    {
        static volatile int ownerThreadId_ = -1;

        /// <summary>
        /// Records the calling thread as the authoritative interpreter thread.
        /// Must be called exactly once, at the start of the interpreter work loop.
        /// </summary>
        [Conditional("UEMUERA_DEBUG")]
        public static void SetOwner()
        {
            ownerThreadId_ = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Clears ownership (call on interpreter thread exit / game end).
        /// </summary>
        [Conditional("UEMUERA_DEBUG")]
        public static void ClearOwner()
        {
            ownerThreadId_ = -1;
        }

        /// <summary>
        /// Asserts that the calling thread is the recorded interpreter thread.
        /// Throws <see cref="InvalidOperationException"/> in debug builds if violated.
        /// No-op in release builds.
        /// </summary>
        /// <param name="context">Short description of the mutation site (for the error message).</param>
        [Conditional("UEMUERA_DEBUG")]
        public static void AssertOwner(string context = null)
        {
            int ownerId = ownerThreadId_;
            if (ownerId < 0)
                return; // owner not set yet — initialization phase, skip
            int callerThreadId = Thread.CurrentThread.ManagedThreadId;
            if (callerThreadId != ownerId)
            {
                string msg = string.Format(
                    "[InterpreterThreadGuard] THREAD VIOLATION — '{0}' called from thread {1}, expected interpreter thread {2}. " +
                    "See Docs/THREADING_MODEL.md.",
                    context ?? "(unspecified)", callerThreadId, ownerId);
                UnityEngine.Debug.LogError(msg);
#if UEMUERA_DEBUG
                throw new InvalidOperationException(msg);
#endif
            }
        }

        /// <summary>
        /// Returns true iff the calling thread is the recorded interpreter thread.
        /// Always returns true in release builds (guard is a no-op).
        /// </summary>
        public static bool IsOwnerThread
        {
            get
            {
#if UEMUERA_DEBUG
                int ownerId = ownerThreadId_;
                if (ownerId < 0) return true;
                return Thread.CurrentThread.ManagedThreadId == ownerId;
#else
                return true;
#endif
            }
        }
    }
}
