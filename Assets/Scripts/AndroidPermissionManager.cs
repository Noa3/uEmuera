using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Manages Android storage permissions for accessing external storage.
/// Provides methods to check and request read/write permissions.
/// Handles both legacy storage permissions and Android 11+ All Files Access.
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
    /// The MANAGE_EXTERNAL_STORAGE permission for Android 11+ (API 30+).
    /// Required for broad access to external storage on newer Android versions.
    /// </summary>
    public const string ManageExternalStorage = "android.permission.MANAGE_EXTERNAL_STORAGE";
    
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
    /// Checks if the app has MANAGE_EXTERNAL_STORAGE permission (Android 11+).
    /// This grants access to all files on external storage.
    /// On non-Android platforms or Android below 11, this always returns true.
    /// </summary>
    /// <returns>True if the app has all files access permission.</returns>
    public static bool HasManageExternalStoragePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (GetAndroidSDKVersion() < 30)
            return true; // Not needed before Android 11
        
        try
        {
            using (var environment = new AndroidJavaClass("android.os.Environment"))
            {
                return environment.CallStatic<bool>("isExternalStorageManager");
            }
        }
        catch (Exception ex)
        {
            uEmuera.Logger.Exception(ex, "Failed to check MANAGE_EXTERNAL_STORAGE permission");
            return false;
        }
#else
        return true;
#endif
    }
    
    /// <summary>
    /// Checks if the app has appropriate storage permissions for the current Android version.
    /// For Android 11+, this checks for MANAGE_EXTERNAL_STORAGE.
    /// For older versions, this checks for READ/WRITE_EXTERNAL_STORAGE.
    /// On non-Android platforms, this always returns true.
    /// </summary>
    /// <returns>True if the app has the necessary storage permissions.</returns>
    public static bool HasStoragePermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (GetAndroidSDKVersion() >= 30)
        {
            // Android 11+ requires MANAGE_EXTERNAL_STORAGE for broad access
            return HasManageExternalStoragePermission();
        }
        else
        {
            // Legacy permissions for Android 10 and below
            return HasStorageReadPermission() && HasStorageWritePermission();
        }
#else
        return true;
#endif
    }
    
    /// <summary>
    /// Requests storage permissions from the user.
    /// For Android 11+, opens the All Files Access settings page.
    /// For older versions, uses the standard permission dialog.
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
        
        if (GetAndroidSDKVersion() >= 30)
        {
            // Android 11+ requires special handling - open the All Files Access settings
            RequestManageExternalStoragePermission();
            // We can't directly get a callback from the settings intent,
            // so the caller should check HasStoragePermissions() when resuming
            callback?.Invoke(false);
            return;
        }
        
        // Legacy permission request for Android 10 and below
        // Track permission responses to ensure callback is invoked once
        int pendingResponses = 2; // READ and WRITE permissions
        bool anyDenied = false;
        bool callbackInvoked = false;
        object lockObj = new object();
        
        System.Action checkAndInvokeCallback = () =>
        {
            lock (lockObj)
            {
                if (callbackInvoked)
                    return;
                    
                pendingResponses--;
                if (pendingResponses <= 0)
                {
                    callbackInvoked = true;
                    // Final check: are all permissions now granted?
                    bool allGranted = HasStoragePermissions();
                    callback?.Invoke(allGranted && !anyDenied);
                }
            }
        };
        
        // Create a callbacks object to handle permission results
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += (permission) =>
        {
            checkAndInvokeCallback();
        };
        callbacks.PermissionDenied += (permission) =>
        {
            lock (lockObj)
            {
                anyDenied = true;
            }
            checkAndInvokeCallback();
        };
        callbacks.PermissionDeniedAndDontAskAgain += (permission) =>
        {
            lock (lockObj)
            {
                anyDenied = true;
            }
            checkAndInvokeCallback();
        };
        
        // Request both read and write permissions
        string[] permissions = new string[] { ExternalStorageRead, ExternalStorageWrite };
        Permission.RequestUserPermissions(permissions, callbacks);
#else
        callback?.Invoke(true);
#endif
    }
    
    /// <summary>
    /// Opens the "All Files Access" settings page for Android 11+.
    /// This is required to get MANAGE_EXTERNAL_STORAGE permission.
    /// </summary>
    public static void RequestManageExternalStoragePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (GetAndroidSDKVersion() < 30)
            return; // Not applicable for older Android versions
        
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent",
                "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION"))
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
            uEmuera.Logger.Exception(ex, "Failed to open All Files Access settings");
            // Fallback to general storage settings if specific intent fails
            OpenStorageSettings();
        }
#endif
    }
    
    /// <summary>
    /// Opens the storage settings page (fallback for MANAGE_EXTERNAL_STORAGE).
    /// </summary>
    public static void OpenStorageSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent",
                "android.settings.MANAGE_ALL_FILES_ACCESS_PERMISSION"))
            {
                currentActivity.Call("startActivity", intent);
            }
        }
        catch (Exception)
        {
            // If that fails too, open app settings as last resort
            OpenAppSettings();
        }
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
        // For Android 11+, we always show rationale since it requires special handling
        if (GetAndroidSDKVersion() >= 30)
            return !HasManageExternalStoragePermission();
        
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
    /// <returns>True if running Android 10+, false otherwise (including non-Android platforms).</returns>
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
    /// <returns>True if running Android 11+, false otherwise (including non-Android platforms).</returns>
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
