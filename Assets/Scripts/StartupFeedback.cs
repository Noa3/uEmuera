using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Startup progress helper:
/// - Persistent messages: do not auto-hide; always replace previous (no overlap)
/// - Final message: show for a short time, then hide
/// Thread-safe: background threads should call Post; main thread can call Step/Flush.
/// </summary>
public static class StartupFeedback
{
    private static readonly ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
    private static int sequence = 0;
    private static UnityEngine.Coroutine hideCoroutine;

    // Background/thread-safe enqueue (persistent)
    public static void Post(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        Queue.Enqueue(message);
        Debug.Log("[Startup] " + message);
    }

    // Show a persistent step message (no auto-hide). New step replaces previous.
    public static void Step(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        Debug.Log("[Startup] " + message);
        var content = EmueraContent.instance;
        if (content == null)
            return;
        var ow = content.option_window;
        if (ow == null)
            return;

        // Cancel any scheduled hide to keep message visible
        if (hideCoroutine != null)
        {
            GenericUtils.StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        // Ensure only current message is shown; overlay is single-instance
        ow.ShowInProgress(true);
        ow.SetInProgressMessage(message);
        sequence++;
    }

    // Show final step for a limited duration (default 3 seconds) then hide
    public static void Final(string message, float seconds = 3f)
    {
        if (string.IsNullOrEmpty(message))
            return;
        var content = EmueraContent.instance;
        if (content == null)
            return;
        var ow = content.option_window;
        if (ow == null)
            return;

        // Replace any current message
        Step(message);

        int mySeq = sequence;
        hideCoroutine = GenericUtils.StartCoroutine(HideLater(mySeq, seconds));
    }

    // Drain queued background messages as persistent steps
    public static void Flush()
    {
        if (Queue.IsEmpty)
            return;
        while (Queue.TryDequeue(out var msg))
        {
            Step(msg);
        }
    }

    private static System.Collections.IEnumerator HideLater(int mySeq, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        hideCoroutine = null;
        if (mySeq == sequence)
        {
            var content = EmueraContent.instance;
            if (content == null)
                yield break;
            var ow = content.option_window;
            if (ow == null)
                yield break;
            ow.ShowInProgress(false);
        }
    }
}
