using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace uEmuera.Async
{
    /// <summary>
    /// Modern async utilities for Unity operations.
    /// Provides async/await patterns for common Unity tasks.
    /// </summary>
    public static class AsyncUtilities
    {
        /// <summary>
        /// Waits for a specified number of seconds asynchronously.
        /// </summary>
        /// <param name="seconds">The number of seconds to wait.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes after the specified time.</returns>
        public static async Task WaitForSeconds(float seconds, CancellationToken cancellationToken = default)
        {
            float elapsedTime = 0;
            while (elapsedTime < seconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                elapsedTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Waits for a specified number of frames asynchronously.
        /// </summary>
        /// <param name="frameCount">The number of frames to wait.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes after the specified frames.</returns>
        public static async Task WaitForFrames(int frameCount, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < frameCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        /// <summary>
        /// Waits until a condition is true asynchronously.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="timeout">Optional timeout in seconds (0 = no timeout).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes when the condition is true, or throws TimeoutException.</returns>
        public static async Task WaitUntil(Func<bool> condition, float timeout = 0, CancellationToken cancellationToken = default)
        {
            float elapsedTime = 0;
            while (!condition())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (timeout > 0 && elapsedTime >= timeout)
                {
                    throw new TimeoutException($"WaitUntil timed out after {timeout} seconds");
                }

                await Task.Yield();
                elapsedTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Waits while a condition is true asynchronously.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="timeout">Optional timeout in seconds (0 = no timeout).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes when the condition is false, or throws TimeoutException.</returns>
        public static async Task WaitWhile(Func<bool> condition, float timeout = 0, CancellationToken cancellationToken = default)
        {
            await WaitUntil(() => !condition(), timeout, cancellationToken);
        }

        /// <summary>
        /// Runs an action on the main thread asynchronously.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes when the action has been executed.</returns>
        public static async Task RunOnMainThread(Action action, CancellationToken cancellationToken = default)
        {
            if (SynchronizationContext.Current != null)
            {
                // Already on main thread
                action();
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            
            void Execute()
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            // Schedule on main thread via Unity's dispatcher
            UnityMainThreadDispatcher.Enqueue(Execute);
            
            await tcs.Task;
        }

        /// <summary>
        /// Runs a function on the main thread asynchronously and returns its result.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="func">The function to run.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that completes with the function result.</returns>
        public static async Task<T> RunOnMainThread<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            if (SynchronizationContext.Current != null)
            {
                // Already on main thread
                return func();
            }

            var tcs = new TaskCompletionSource<T>();
            
            void Execute()
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    T result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            UnityMainThreadDispatcher.Enqueue(Execute);
            
            return await tcs.Task;
        }

        /// <summary>
        /// Retries an async operation with exponential backoff.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="operation">The operation to retry.</param>
        /// <param name="maxRetries">Maximum number of retries.</param>
        /// <param name="initialDelaySeconds">Initial delay between retries in seconds.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        public static async Task<T> RetryWithBackoff<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            float initialDelaySeconds = 0.5f,
            CancellationToken cancellationToken = default)
        {
            int retryCount = 0;
            float delay = initialDelaySeconds;

            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (retryCount < maxRetries)
                {
                    retryCount++;
                    Debug.LogWarning($"Operation failed (attempt {retryCount}/{maxRetries}): {ex.Message}. Retrying in {delay}s...");
                    
                    await WaitForSeconds(delay, cancellationToken);
                    
                    // Exponential backoff
                    delay *= 2;
                }
            }
        }
    }

    /// <summary>
    /// Simple main thread dispatcher for Unity.
    /// Ensures actions are executed on the main Unity thread.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher instance_;
        private static readonly ConcurrentQueue<Action> executionQueue_ = new();

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (instance_ == null)
                {
                    var go = new GameObject("UnityMainThreadDispatcher");
                    instance_ = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return instance_;
            }
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void Enqueue(Action action)
        {
            // Ensure instance exists
            _ = Instance;

            executionQueue_.Enqueue(action);
        }

        void Update()
        {
            while (executionQueue_.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error executing main thread action: {ex}");
                }
            }
        }
    }
}
