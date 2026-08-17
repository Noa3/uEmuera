using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Browser-side folder import for WebGL builds.
/// The browser supplies file bytes; no absolute path from the user's machine is
/// exposed to C# or passed to the game runtime.
/// </summary>
public static class WebGLGameImporter
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void UEmueraPickGameFiles(string receiver, string persistentRoot);
#endif

    public static void RequestFolder(string receiver)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UEmueraPickGameFiles(receiver, Application.persistentDataPath);
#else
        Debug.Log("WebGL folder import is only available in a WebGL player.");
#endif
    }
}
