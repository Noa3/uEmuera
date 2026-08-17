using System.Threading;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Monotonically-increasing game session identifier (uEmuera Phase 6 #21–#22/#38).
    ///
    /// Rules:
    /// <list type="bullet">
    ///   <item>Call <see cref="Bump"/> at the start of every new game session (inside
    ///     the teardown coroutine of EmueraMain, before GlobalStatic.Reset and
    ///     SpriteManager.ForceClear are called).</item>
    ///   <item>Record the ID at the time an async request is created.</item>
    ///   <item>Discard the result if <see cref="IsValid"/> returns false when the
    ///     result arrives.</item>
    /// </list>
    /// This prevents:
    /// <list type="bullet">
    ///   <item>Old SpriteManager callbacks firing on a new game's GameObjects.</item>
    ///   <item>BackgroundErbLoader batches from an old session mutating a new session's
    ///     LabelDictionary.</item>
    ///   <item>srcb hover images arriving after a pointer-exit.</item>
    ///   <item>CBG image callbacks landing on reused pooled GameObjects.</item>
    /// </list>
    /// </summary>
    public static class GameSession
    {
        static volatile int current_ = 0;

        /// <summary>Current session ID. Valid IDs are &gt;= 1 (0 = pre-first-game).</summary>
        public static int Current => current_;

        /// <summary>
        /// Increments the session ID atomically. Call exactly once per game teardown,
        /// before any shared state is reset or reused.
        /// </summary>
        /// <returns>The new (now current) session ID.</returns>
        public static int Bump()
        {
            return Interlocked.Increment(ref current_);
        }

        /// <summary>
        /// Returns true when <paramref name="id"/> equals the current session.
        /// False means the caller belongs to a previous session and must not
        /// mutate live state.
        /// </summary>
        public static bool IsValid(int id) => id == current_;
    }
}
