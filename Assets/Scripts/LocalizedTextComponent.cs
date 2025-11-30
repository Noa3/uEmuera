using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

/// <summary>
/// Component that automatically updates UI Text when language changes.
/// Can be attached to GameObjects with Text components.
/// Supports both Unity Localization Package and legacy MultiLanguage system.
/// </summary>
[RequireComponent(typeof(Text))]
public class LocalizedTextComponent : MonoBehaviour
{
    [Header("Localization Settings")]
    [Tooltip("Table name (UI or Messages)")]
    public string tableName = "UI";

    [Tooltip("Key for localized string")]
    public string key = "";

    [Header("Legacy Support")]
    [Tooltip("Use legacy MultiLanguage.GetText() for keys starting with [")]
    public bool useLegacyForBracketKeys = true;

    [Header("Size Adjustment")]
    [Tooltip("Adjust RectTransform width based on language (for menus)")]
    public bool adjustWidth = false;

    [Tooltip("Reference width name (Menu1, Menu2, etc.)")]
    public string widthReference = "";

    private Text textComponent;
    private RectTransform rectTransform;
    private bool isInitialized = false;

    void Awake()
    {
        textComponent = GetComponent<Text>();
        rectTransform = transform as RectTransform;
    }

    void Start()
    {
        Initialize();
    }

    void OnEnable()
    {
        if (isInitialized)
        {
            UpdateText();
        }
        LocalizationHelper.OnLanguageChanged += UpdateText;
    }

    void OnDisable()
    {
        LocalizationHelper.OnLanguageChanged -= UpdateText;
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (string.IsNullOrEmpty(key))
        {
            key = DeriveKeyFromHierarchy();
        }

        isInitialized = true;
        UpdateText();
    }

    /// <summary>
    /// Derives localization key from GameObject hierarchy.
    /// Example: Options/MenuPad/Menu1/resolution -> Options.MenuPad.Menu1.resolution.Text
    /// </summary>
    private string DeriveKeyFromHierarchy()
    {
        string path = GetHierarchyPath();
        
        if (!string.IsNullOrEmpty(path))
        {
            return path + ".Text";
        }

        return gameObject.name;
    }

    private string GetHierarchyPath()
    {
        string path = gameObject.name;
        Transform parent = transform.parent;

        while (parent != null)
        {
            if (parent.name.StartsWith("Options") || 
                parent.name.StartsWith("FirstWindow") ||
                parent.name.StartsWith("EmueraMain"))
            {
                path = parent.name + "." + path;
                break;
            }

            path = parent.name + "." + path;
            parent = parent.parent;
        }

        return path;
    }

    /// <summary>
    /// Updates the text component with localized string.
    /// </summary>
    public void UpdateText()
    {
        if (textComponent == null || string.IsNullOrEmpty(key))
            return;

        string localizedText = GetLocalizedText();
        
        if (!string.IsNullOrEmpty(localizedText))
        {
            textComponent.text = localizedText;

            if (adjustWidth && !string.IsNullOrEmpty(widthReference))
            {
                AdjustWidth();
            }
        }
    }

    private string GetLocalizedText()
    {
        if (useLegacyForBracketKeys && key.StartsWith("[") && key.EndsWith("]"))
        {
            return MultiLanguage.GetText(key);
        }

        string result = string.Empty;

        if (tableName == "UI")
        {
            result = LocalizationHelper.GetUIString(key);
        }
        else if (tableName == "Messages")
        {
            result = LocalizationHelper.GetMessageString(key);
        }
        else
        {
            result = LocalizationHelper.GetLocalizedString(tableName, key);
        }

        if (result == key)
        {
            result = MultiLanguage.GetText(key);
        }

        return result;
    }

    private void AdjustWidth()
    {
        if (rectTransform == null)
            return;

        float menuWidth = GetMenuWidth(widthReference);
        if (menuWidth > 0)
        {
            rectTransform.sizeDelta = new Vector2(menuWidth - 52, rectTransform.sizeDelta.y);
        }
    }

    private float GetMenuWidth(string menuName)
    {
        string widthKey = $"<{menuName}>";
        string widthString = LocalizationHelper.GetLocalizedString("UI", widthKey);
        
        if (float.TryParse(widthString, out float width))
        {
            return width;
        }

        return 0f;
    }

    /// <summary>
    /// Manually set the localization key.
    /// </summary>
    public void SetKey(string newKey)
    {
        key = newKey;
        UpdateText();
    }

    /// <summary>
    /// Manually set the table name.
    /// </summary>
    public void SetTableName(string newTableName)
    {
        tableName = newTableName;
        UpdateText();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying && isInitialized)
        {
            UpdateText();
        }
    }
#endif
}
