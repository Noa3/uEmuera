using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates the Settings UI at runtime if it doesn't exist in the prefab.
/// This ensures the Settings menu is available even if the prefab wasn't updated in Unity Editor.
/// </summary>
public class SettingsUIBuilder : MonoBehaviour
{
    /// <summary>
    /// Creates the settings UI structure if it doesn't already exist.
    /// </summary>
    /// <param name="optionWindow">The OptionWindow component to build the settings for.</param>
    public static void EnsureSettingsUIExists(OptionWindow optionWindow)
    {
        if (optionWindow == null)
            return;
            
        // Check if settings_box already exists
        if (optionWindow.settings_box != null)
            return; // UI already exists, nothing to do
            
        // Find the parent (should be the Options root)
        Transform parent = optionWindow.transform;
        
        // Create the settings box similar to other dialog boxes
        CreateSettingsBox(optionWindow, parent);
    }
    
    /// <summary>
    /// Creates the settings box UI structure.
    /// </summary>
    static void CreateSettingsBox(OptionWindow optionWindow, Transform parent)
    {
        // Create main settings box container
        GameObject settingsBox = new GameObject("SettingsBox");
        settingsBox.transform.SetParent(parent, false);
        
        RectTransform boxRect = settingsBox.AddComponent<RectTransform>();
        boxRect.anchorMin = Vector2.zero;
        boxRect.anchorMax = Vector2.one;
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;
        
        // Add dark semi-transparent background
        Image boxBg = settingsBox.AddComponent<Image>();
        boxBg.color = new Color(0, 0, 0, 0.85f);
        
        // Create border panel (inner panel)
        GameObject border = new GameObject("border");
        border.transform.SetParent(settingsBox.transform, false);
        
        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(600, 400);
        
        Image borderBg = border.AddComponent<Image>();
        borderBg.color = UIStyleManager.DarkTheme.BackgroundMedium;
        
        // Create title bar
        GameObject titleBar = new GameObject("titlebar");
        titleBar.transform.SetParent(border.transform, false);
        
        RectTransform titleRect = titleBar.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, 0);
        titleRect.sizeDelta = new Vector2(0, 60);
        
        GameObject titleText = new GameObject("title");
        titleText.transform.SetParent(titleBar.transform, false);
        
        RectTransform titleTextRect = titleText.AddComponent<RectTransform>();
        titleTextRect.anchorMin = Vector2.zero;
        titleTextRect.anchorMax = Vector2.one;
        titleTextRect.offsetMin = new Vector2(20, 0);
        titleTextRect.offsetMax = new Vector2(-20, 0);
        
        Text title = titleText.AddComponent<Text>();
        title.text = "Settings";
        title.fontSize = 24;
        title.color = UIStyleManager.DarkTheme.TextPrimary;
        title.alignment = TextAnchor.MiddleCenter;
        title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        UIStyleManager.AddTextShadow(title);
        
        // Create close button
        GameObject closeBtn = CreateButton("close", border.transform, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-10, -10), new Vector2(80, 40), "Close", OnCloseClicked);
        optionWindow.settings_close = closeBtn;
        
        // Create post-processing toggle
        GameObject ppToggle = CreateToggle("postprocessing", border.transform, 
            new Vector2(0.5f, 0.6f), new Vector2(500, 60),
            "CRT Monitor Effect", true);
        optionWindow.settings_postprocessing_toggle = ppToggle;
        optionWindow.settings_postprocessing_icon = ppToggle.transform.Find("icon")?.gameObject;
        optionWindow.settings_postprocessing_text = ppToggle.transform.Find("statetext")?.GetComponent<Text>();
        
        // Create pixel perfect toggle
        GameObject pixelToggle = CreateToggle("pixelperfect", border.transform,
            new Vector2(0.5f, 0.4f), new Vector2(500, 60),
            "Pixel Perfect Rendering", true);
        optionWindow.settings_pixelperfect_toggle = pixelToggle;
        optionWindow.settings_pixelperfect_icon = pixelToggle.transform.Find("icon")?.gameObject;
        optionWindow.settings_pixelperfect_text = pixelToggle.transform.Find("statetext")?.GetComponent<Text>();
        
        // Assign the settings box to the option window
        optionWindow.settings_box = settingsBox;
        
        // Apply dark theme
        UIStyleManager.ApplyDarkTheme(settingsBox);
        
        // Hide by default
        settingsBox.SetActive(false);
        
        void OnCloseClicked()
        {
            settingsBox.SetActive(false);
        }
    }
    
    /// <summary>
    /// Creates a button UI element.
    /// </summary>
    static GameObject CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, string text, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btn = new GameObject(name);
        btn.transform.SetParent(parent, false);
        
        RectTransform rect = btn.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        
        Image img = btn.AddComponent<Image>();
        img.color = UIStyleManager.DarkTheme.ButtonNormal;
        
        Button button = btn.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = UIStyleManager.DarkTheme.ButtonNormal;
        colors.highlightedColor = UIStyleManager.DarkTheme.ButtonHighlight;
        colors.pressedColor = UIStyleManager.DarkTheme.ButtonPressed;
        button.colors = colors;
        
        if (onClick != null)
            button.onClick.AddListener(onClick);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btn.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = text;
        btnText.fontSize = 18;
        btnText.color = UIStyleManager.DarkTheme.TextPrimary;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        return btn;
    }
    
    /// <summary>
    /// Creates a toggle button UI element.
    /// </summary>
    static GameObject CreateToggle(string name, Transform parent, Vector2 anchor, Vector2 sizeDelta,
        string labelText, bool showIcon)
    {
        GameObject toggle = new GameObject(name);
        toggle.transform.SetParent(parent, false);
        
        RectTransform rect = toggle.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        
        Image bg = toggle.AddComponent<Image>();
        bg.color = UIStyleManager.DarkTheme.ButtonNormal;
        
        Button btn = toggle.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = UIStyleManager.DarkTheme.ButtonNormal;
        colors.highlightedColor = UIStyleManager.DarkTheme.ButtonHighlight;
        colors.pressedColor = UIStyleManager.DarkTheme.ButtonPressed;
        btn.colors = colors;
        
        // Create label
        GameObject label = new GameObject("label");
        label.transform.SetParent(toggle.transform, false);
        
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0.7f, 1);
        labelRect.offsetMin = new Vector2(15, 0);
        labelRect.offsetMax = new Vector2(0, 0);
        
        Text labelComp = label.AddComponent<Text>();
        labelComp.text = labelText;
        labelComp.fontSize = 18;
        labelComp.color = UIStyleManager.DarkTheme.TextPrimary;
        labelComp.alignment = TextAnchor.MiddleLeft;
        labelComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        // Create state text (ON/OFF)
        GameObject stateText = new GameObject("statetext");
        stateText.transform.SetParent(toggle.transform, false);
        
        RectTransform stateRect = stateText.AddComponent<RectTransform>();
        stateRect.anchorMin = new Vector2(0.7f, 0);
        stateRect.anchorMax = new Vector2(1, 1);
        stateRect.offsetMin = new Vector2(0, 0);
        stateRect.offsetMax = new Vector2(-15, 0);
        
        Text stateComp = stateText.AddComponent<Text>();
        stateComp.text = "ON";
        stateComp.fontSize = 18;
        stateComp.color = UIStyleManager.DarkTheme.AccentPrimary;
        stateComp.alignment = TextAnchor.MiddleRight;
        stateComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        stateComp.fontStyle = FontStyle.Bold;
        
        // Create icon (checkmark)
        if (showIcon)
        {
            GameObject icon = new GameObject("icon");
            icon.transform.SetParent(toggle.transform, false);
            
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.85f, 0.5f);
            iconRect.anchorMax = new Vector2(0.85f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(20, 20);
            
            Text iconText = icon.AddComponent<Text>();
            iconText.text = "✓";
            iconText.fontSize = 24;
            iconText.color = UIStyleManager.DarkTheme.AccentSecondary;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            iconText.fontStyle = FontStyle.Bold;
        }
        
        return toggle;
    }
}
