using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Manages Android storage permissions for accessing external storage.
/// Provides methods to check and request read/write permissions.
/// </summary>
public static class AndroidPermissionManager
{
    /// <summary>
    /// The external storage read permission string for Android.
    /// </summary>
    public const string ExternalStorageRead = "android.permission.READ_EXTERNAL_STORAGE";
    
    /// <summary>
    /// The external storage write permission string for Android.
    /// </summary>
    public const string ExternalStorageWrite = "android.permission.WRITE_EXTERNAL_STORAGE";
    
    /// <summary>
    /// Callback type for permission request results.
    /// </summary>
    /// <param name="granted">True if permission was granted, false otherwise.</param>
    public delegate void PermissionCallback(bool granted);
    
    /// <summary>
    /// Checks if the app has read permission for external storage.
    /// On non-Android platforms, this always returns true.
    /// </summary>
    /// <returns>True if the app has read permission.</returns>
    public static bool HasStorageReadPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(ExternalStorageRead);
#else
        return true;
#endif
    }
    
    /// <summary>
    /// Checks if the app has write permission for external storage.
    /// On non-Android platforms, this always returns true.
    /// </summary>
    /// <returns>True if the app has write permission.</returns>
    public static bool HasStorageWritePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(ExternalStorageWrite);
#else
        return true;
#endif
    }
    
    /// <summary>
    /// Checks if the app has both read and write permissions for external storage.
    /// On non-Android platforms, this always returns true.
    /// </summary>
    /// <returns>True if the app has both read and write permissions.</returns>
    public static bool HasStoragePermissions()
    {
        return HasStorageReadPermission() && HasStorageWritePermission();
    }
    
    /// <summary>
    /// Requests storage permissions from the user.
    /// On non-Android platforms, the callback is immediately invoked with true.
    /// </summary>
    /// <param name="callback">Callback invoked with the result of the permission request.</param>
    public static void RequestStoragePermissions(PermissionCallback callback)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (HasStoragePermissions())
        {
            callback?.Invoke(true);
            return;
        }
        
        // Create a callbacks object to handle permission results
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += (permission) =>
        {
            // Check if all required permissions are now granted
            if (HasStoragePermissions())
            {
                callback?.Invoke(true);
            }
        };
        callbacks.PermissionDenied += (permission) =>
        {
            callback?.Invoke(false);
        };
        callbacks.PermissionDeniedAndDontAskAgain += (permission) =>
        {
            callback?.Invoke(false);
        };
        
        // Request both read and write permissions
        string[] permissions = new string[] { ExternalStorageRead, ExternalStorageWrite };
        Permission.RequestUserPermissions(permissions, callbacks);
#else
        callback?.Invoke(true);
#endif
    }
    
    /// <summary>
    /// Requests storage permissions and waits for the result using a coroutine.
    /// Yields until the permission dialog is dismissed.
    /// </summary>
    /// <param name="resultCallback">Callback invoked with true if permissions were granted.</param>
    /// <returns>An IEnumerator for use with coroutines.</returns>
    public static IEnumerator RequestStoragePermissionsCoroutine(PermissionCallback resultCallback)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (HasStoragePermissions())
        {
            resultCallback?.Invoke(true);
            yield break;
        }
        
        bool? result = null;
        RequestStoragePermissions((granted) =>
        {
            result = granted;
        });
        
        // Wait for the callback to be invoked
        while (!result.HasValue)
        {
            yield return null;
        }
        
        resultCallback?.Invoke(result.Value);
#else
        resultCallback?.Invoke(true);
        yield break;
#endif
    }
    
    /// <summary>
    /// Checks if we should show a rationale for storage permissions.
    /// This is true when the user has previously denied the permission but hasn't selected "Don't ask again".
    /// On non-Android platforms, this always returns false.
    /// </summary>
    /// <returns>True if we should show a rationale for why permissions are needed.</returns>
    public static bool ShouldShowPermissionRationale()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Check if we should show rationale for read permission
        return Permission.ShouldShowRequestPermissionRationale(ExternalStorageRead) ||
               Permission.ShouldShowRequestPermissionRationale(ExternalStorageWrite);
#else
        return false;
#endif
    }
    
    /// <summary>
    /// Checks if the app is running on Android.
    /// </summary>
    /// <returns>True if running on Android, false otherwise.</returns>
    public static bool IsAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }
    
    /// <summary>
    /// Gets the Android SDK version (API level).
    /// Returns 0 on non-Android platforms.
    /// </summary>
    /// <returns>The Android SDK version, or 0 on non-Android platforms.</returns>
    public static int GetAndroidSDKVersion()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return version.GetStatic<int>("SDK_INT");
        }
#else
        return 0;
#endif
    }
    
    /// <summary>
    /// Checks if the device is running Android 10 (API 29) or higher,
    /// which uses scoped storage by default.
    /// </summary>
    /// <returns>True if running Android 10+ or not on Android.</returns>
    public static bool IsAndroid10OrHigher()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetAndroidSDKVersion() >= 29;
#else
        return false;
#endif
    }
    
    /// <summary>
    /// Checks if the device is running Android 11 (API 30) or higher,
    /// which enforces scoped storage more strictly.
    /// </summary>
    /// <returns>True if running Android 11+ or not on Android.</returns>
    public static bool IsAndroid11OrHigher()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetAndroidSDKVersion() >= 30;
#else
        return false;
#endif
    }
    
    /// <summary>
    /// Opens the app settings page where the user can manually grant permissions.
    /// This is useful when the user has selected "Don't ask again".
    /// </summary>
    public static void OpenAppSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent", 
                "android.settings.APPLICATION_DETAILS_SETTINGS"))
            using (var uri = new AndroidJavaClass("android.net.Uri"))
            using (var packageUri = uri.CallStatic<AndroidJavaObject>("parse", 
                "package:" + Application.identifier))
            {
                intent.Call<AndroidJavaObject>("setData", packageUri);
                currentActivity.Call("startActivity", intent);
            }
        }
        catch (Exception ex)
        {
            uEmuera.Logger.Exception(ex, "Failed to open app settings");
        }
#endif
    }
}
