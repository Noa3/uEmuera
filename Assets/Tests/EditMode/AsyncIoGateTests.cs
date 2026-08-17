using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="AsyncIoGate"/> — bounded, prioritized async file reads
    /// (Phase 6 — bounded image I/O concurrency + priority queue).
    ///
    /// <para>The gate is a static singleton shared across tests, so cleanup always
    /// waits for full idle (including the fire-and-forget workers) and deletes
    /// temp files with retry to avoid mid-read delete races.</para>
    /// </summary>
    [TestFixture]
    public class AsyncIoGateTests
    {
        string dir_;

        [SetUp]
        public void Setup()
        {
            dir_ = Path.Combine(Path.GetTempPath(), "uEmuera_io_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir_);
        }

        [TearDown]
        public void Cleanup()
        {
            WaitForIdle();
            try { if (Directory.Exists(dir_)) Directory.Delete(dir_, true); }
            catch { } // best-effort
        }

        string TempFile(int index)
        {
            var path = Path.Combine(dir_, index + ".bin");
            File.WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes("DATA-" + index));
            return path;
        }

        /// <summary>Block until every queued/active read is done (workers fully idle).</summary>
        static void WaitForIdle()
        {
            var deadline = System.DateTime.UtcNow.AddSeconds(15);
            while ((AsyncIoGate.PendingHigh + AsyncIoGate.PendingLow + AsyncIoGate.ActiveReads > 0)
                   && System.DateTime.UtcNow < deadline)
                System.Threading.Thread.Sleep(5);
        }

        static void WaitAll(List<Task<byte[]>> tasks)
        {
            Assert.IsTrue(Task.WaitAll(tasks.ToArray(), 15000),
                "all reads must complete within timeout");
        }

        [Test]
        public void ConcurrentReads_ReturnCorrectBytes()
        {
            var paths = new List<string>();
            var tasks = new List<Task<byte[]>>();
            try
            {
                for (int i = 0; i < 12; i++)
                {
                    string p = TempFile(i);
                    paths.Add(p);
                    tasks.Add(AsyncIoGate.ReadAllBytesAsync(p));
                }

                WaitAll(tasks);

                for (int i = 0; i < paths.Count; i++)
                    Assert.AreEqual("DATA-" + i, System.Text.Encoding.UTF8.GetString(tasks[i].Result));
            }
            finally
            {
                WaitForIdle();
            }
        }

        [Test]
        public void Concurrency_NeverExceeds_Cap()
        {
            var tasks = new List<Task<byte[]>>();
            try
            {
                for (int i = 0; i < 6; i++)
                    tasks.Add(AsyncIoGate.ReadAllBytesAsync(TempFile(i)));

                // activeReads is incremented synchronously at submit time, so right
                // after submission it can only rise toward the cap as workers start.
                Assert.GreaterOrEqual(AsyncIoGate.ActiveReads, 0);
                Assert.LessOrEqual(AsyncIoGate.ActiveReads, AsyncIoGate.MaxConcurrentReads,
                    "concurrent reads must never exceed the configured cap");

                WaitAll(tasks);
                WaitForIdle();
                Assert.AreEqual(0, AsyncIoGate.PendingHigh + AsyncIoGate.PendingLow,
                    "queues must drain once all reads complete");
            }
            finally
            {
                WaitForIdle();
            }
        }

        [Test]
        public void LowPriority_Reads_Complete()
        {
            var tasks = new List<Task<byte[]>>();
            try
            {
                for (int i = 0; i < 8; i++)
                    tasks.Add(AsyncIoGate.ReadAllBytesAsync(TempFile(i), lowPriority: true));

                WaitAll(tasks);

                for (int i = 0; i < tasks.Count; i++)
                    Assert.AreEqual("DATA-" + i, System.Text.Encoding.UTF8.GetString(tasks[i].Result));
            }
            finally
            {
                WaitForIdle();
            }
        }

        [Test]
        public void MixedPriority_AllComplete_CapHolds()
        {
            var tasks = new List<Task<byte[]>>();
            try
            {
                // Queue low-priority work that occupies the worker pool, then stack
                // high-priority work on top. Invariant: everything drains and
                // concurrency stays within the cap.
                for (int i = 0; i < 4; i++)
                    tasks.Add(AsyncIoGate.ReadAllBytesAsync(TempFile(i), lowPriority: true));
                for (int i = 4; i < 8; i++)
                    tasks.Add(AsyncIoGate.ReadAllBytesAsync(TempFile(i), lowPriority: false));

                Assert.LessOrEqual(AsyncIoGate.ActiveReads, AsyncIoGate.MaxConcurrentReads);

                WaitAll(tasks);
                WaitForIdle();
                Assert.AreEqual(0, AsyncIoGate.PendingHigh + AsyncIoGate.PendingLow);
                Assert.LessOrEqual(AsyncIoGate.ActiveReads, AsyncIoGate.MaxConcurrentReads);
            }
            finally
            {
                WaitForIdle();
            }
        }

        [Test]
        public void MissingFile_FaultsTask()
        {
            bool faultObserved = false;
            try
            {
                var task = AsyncIoGate.ReadAllBytesAsync(
                    Path.Combine(Path.GetTempPath(), "does_not_exist_" + System.Guid.NewGuid() + ".bin"));
                // Wait() rethrows the fault, which is what we want to observe.
                task.Wait(10000);
            }
            catch (System.AggregateException)
            {
                faultObserved = true;
            }
            Assert.IsTrue(faultObserved, "a missing file must fault the task");
        }
    }
}