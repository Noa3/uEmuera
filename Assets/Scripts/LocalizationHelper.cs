using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Helper class for Unity Localization Package integration.
/// Provides automatic language switching and string table management.
/// </summary>
public class LocalizationHelper : MonoBehaviour
{
    public static LocalizationHelper Instance { get; private set; }

    public const string UI_TABLE_NAME = "UI";
    public const string MESSAGES_TABLE_NAME = "Messages";

    public static event Action OnLanguageChanged;

    private static Dictionary<string, string> cachedStrings = new Dictionary<string, string>();
    private static bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeLocalization());
    }

    private IEnumerator InitializeLocalization()
    {
        yield return LocalizationSettings.InitializationOperation;
        
        isInitialized = true;
        
        string savedLanguage = PlayerPrefs.GetString("language", "");
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            SetLanguageByCode(savedLanguage);
        }

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        cachedStrings.Clear();
        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// Set language by language code (zh_cn, en_us, jp, default).
    /// Maps old language codes to Unity Locale identifiers.
    /// </summary>
    public static void SetLanguageByCode(string languageCode)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("LocalizationHelper not initialized yet");
            return;
        }

        string localeIdentifier = MapLanguageCodeToLocale(languageCode);
        
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == localeIdentifier)
            {
                LocalizationSettings.SelectedLocale = locale;
                PlayerPrefs.SetString("language", languageCode);
                return;
            }
        }

        Debug.LogWarning($"Locale not found for language code: {languageCode}");
    }

    /// <summary>
    /// Maps old language codes to Unity Locale identifiers.
    /// </summary>
    private static string MapLanguageCodeToLocale(string languageCode)
    {
        switch (languageCode.ToLower())
        {
            case "zh_cn":
            case "default":
                return "zh-Hans";
            case "en_us":
                return "en";
            case "jp":
                return "ja";
            case "de":
                return "de";
            default:
                return "zh-Hans";
        }
    }

    /// <summary>
    /// Get current language code in old format (zh_cn, en_us, jp).
    /// </summary>
    public static string GetCurrentLanguageCode()
    {
        if (!isInitialized || LocalizationSettings.SelectedLocale == null)
        {
            return PlayerPrefs.GetString("language", "zh_cn");
        }

        string localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
        switch (localeCode)
        {
            case "zh-Hans":
                return "zh_cn";
            case "en":
                return "en_us";
            case "ja":
                return "jp";
            case "de":
                return "de";
            default:
                return "zh_cn";
        }
    }

    /// <summary>
    /// Get localized string from table by key.
    /// Fallback: when not initialized, use MultiLanguage.GetText(key) silently to avoid log spam.
    /// </summary>
    public static string GetLocalizedString(string tableName, string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // If Unity Localization is not ready yet, use legacy MultiLanguage fallback without warning
        if (!isInitialized)
        {
            try
            {
                // Try legacy/global localization source to avoid noisy logs
                var fallback = MultiLanguage.GetText(key);
                return string.IsNullOrEmpty(fallback) ? key : fallback;
            }
            catch
            {
                return key;
            }
        }

        string cacheKey = $"{tableName}.{key}";
        if (cachedStrings.TryGetValue(cacheKey, out string cached))
        {
            return cached;
        }

        try
        {
            var stringTable = LocalizationSettings.StringDatabase.GetTable(tableName);
            if (stringTable != null)
            {
                var entry = stringTable.GetEntry(key);
                if (entry != null)
                {
                    string value = entry.GetLocalizedString();
                    cachedStrings[cacheKey] = value;
                    return value;
                }
            }
        }
        catch (Exception)
        {
            // Swallow and fallback to legacy
        }

        // Final fallback: legacy MultiLanguage, else key
        try
        {
            var fallback = MultiLanguage.GetText(key);
            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// Get localized string from UI table.
    /// </summary>
    public static string GetUIString(string key)
    {
        return GetLocalizedString(UI_TABLE_NAME, key);
    }

    /// <summary>
    /// Get localized string from Messages table (for dynamic messages like [Wait], [Exit], etc.).
    /// </summary>
    public static string GetMessageString(string key)
    {
        return GetLocalizedString(MESSAGES_TABLE_NAME, key);
    }

    /// <summary>
    /// Get localized string with async operation.
    /// </summary>
    public static void GetLocalizedStringAsync(string tableName, string key, Action<string> callback)
    {
        if (!isInitialized)
        {
            callback?.Invoke(key);
            return;
        }

        var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, key);
        operation.Completed += (op) =>
        {
            if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                callback?.Invoke(op.Result);
            }
            else
            {
                callback?.Invoke(key);
            }
        };
    }

    /// <summary>
    /// Get localized string from System table (for system messages like Loading, Error, etc.).
    /// </summary>
    public static string GetSystemString(string key)
    {
        return GetLocalizedString("System", key);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }
    }
}
