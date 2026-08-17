using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Bounded asynchronous file-read gate for image loading (uEmuera Phase 6 — bounded
/// image I/O concurrency + priority queue).
///
/// <para>Without this gate, <c>SpriteManager.Loading</c> spawned one unbounded
/// <c>Task.Run(File.ReadAllBytes)</c> per image. During a large screen switch a game
/// can queue hundreds of reads at once; each one consumes a thread-pool thread and
/// issues random-access I/O, which stalls meaningful progress on HDD/network storage
/// and raises token/memory pressure. The gate caps concurrency at
/// <see cref="MaxConcurrentReads"/> and runs all reads through a small worker pool
/// (one worker per concurrent slot).</para>
///
/// <para>Priority: two FIFO queues (high / low). Workers always drain the high queue
/// before touching the low queue, so preloading (low) can never delay a user-facing
/// <c>GetSprite</c> (high). Low-priority items still make progress whenever no
/// high-priority work is waiting.</para>
///
/// <para>Thread-safe for any caller. Designed to be awaited from a coroutine that
/// polls <c>Task.IsCompleted</c> each frame (same pattern the SpriteManager already
/// used), keeping texture creation on the Unity main thread.</para>
/// </summary>
internal static class AsyncIoGate
{
    /// <summary>
    /// Maximum number of files read concurrently. 2 keeps image loading fast while
    /// avoiding I/O thrash (random-read latency is typically the real bottleneck,
    /// not CPU).
    /// </summary>
    public const int MaxConcurrentReads = 2;

    static readonly object gateLock = new object();
    static readonly Queue<ReadItem> highQueue = new Queue<ReadItem>();
    static readonly Queue<ReadItem> lowQueue  = new Queue<ReadItem>();
    static int activeReads;

    sealed class ReadItem
    {
        public readonly string Path;
        public readonly TaskCompletionSource<byte[]> Completion;
        public ReadItem(string path)
        {
            Path = path;
            Completion = new TaskCompletionSource<byte[]>();
        }
    }

    // ---- stats (thread-safe) -------------------------------------------
    public static int PendingHigh { get { lock (gateLock) return highQueue.Count; } }
    public static int PendingLow  { get { lock (gateLock) return lowQueue.Count; } }
    public static int ActiveReads { get { lock (gateLock) return activeReads; } }

    /// <summary>
    /// Enqueues an async file read. High-priority items (user-facing sprite loads)
    /// are always dequeued before low-priority ones (preload).
    /// </summary>
    public static Task<byte[]> ReadAllBytesAsync(string path, bool lowPriority = false)
    {
        var item = new ReadItem(path);
        lock (gateLock)
        {
            if (lowPriority) lowQueue.Enqueue(item);
            else highQueue.Enqueue(item);
            if (activeReads < MaxConcurrentReads)
            {
                activeReads++;
                ThreadPool.QueueUserWorkItem(Worker);
            }
        }
        return item.Completion.Task;
    }

    static void Worker(object state)
    {
        while (true)
        {
            ReadItem item;
            lock (gateLock)
            {
                if (highQueue.Count > 0) item = highQueue.Dequeue();
                else if (lowQueue.Count > 0) item = lowQueue.Dequeue();
                else { activeReads--; return; }
            }

            byte[] result = null;
            try
            {
                result = File.ReadAllBytes(item.Path);
            }
            catch (Exception ex)
            {
                item.Completion.TrySetException(ex);
                continue;
            }
            item.Completion.TrySetResult(result);
        }
    }
}