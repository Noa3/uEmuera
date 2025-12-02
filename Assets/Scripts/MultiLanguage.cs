using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Static utility class for managing multi-language support.
/// Handles loading and applying language translations to UI elements.
/// </summary>
public static class MultiLanguage
{
    /// <summary>
    /// Characters used to split lines in language files.
    /// </summary>
    public static readonly char[] splits = new char[] { '\xa', '\xd' };
    
    /// <summary>
    /// Loads a language file and returns key-value pairs.
    /// </summary>
    /// <param name="lang">The language code to load.</param>
    /// <returns>List of key-value pairs or null if not found.</returns>
    static List<KeyValuePair<string, string>> Load(string lang)
    {
        var text = Resources.Load<TextAsset>(string.Concat("Lang/", lang));
        if(text == null)
            return null;

        var lines = text.text.Split(splits);
        List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

        for(int i = 0; i < lines.Length; ++i)
        {
            var l = lines[i].Trim();
            if(string.IsNullOrEmpty(l) || 
                string.CompareOrdinal(l, 0, ";", 0, 1) == 0)
                continue;
            var s = l.IndexOf('=');
            var left = l.Substring(0, s).Trim();
            var right = l.Substring(s + 1).Trim();
            if(string.IsNullOrEmpty(right))
                continue;

            list.Add(new KeyValuePair<string, string>(left, right));
        }
        return list;
    }
    
    /// <summary>
    /// Sets the language from saved preferences.
    /// </summary>
    /// <returns>True if a language was set, false otherwise.</returns>
    public static bool SetLanguage()
    {
        var lang = PlayerPrefs.GetString("language", "");
        if(string.IsNullOrEmpty(lang))
            return false;
        SetLanguage(lang);
        return true;
    }
    
    /// <summary>
    /// Sets the language and applies translations to all UI elements.
    /// </summary>
    /// <param name="lang">The language code to set.</param>
    public static void SetLanguage(string lang)
    {
        var list = Load(lang);
        if(list == null)
            return;

        float menu1 = 0;
        float menu2 = 0;
        for(int i = 0; i < list.Count; ++i)
        {
            var kv = list[i];
            var key = kv.Key;
            var value = kv.Value;
            if(key[0] == '<')
            {
                try
                {
                    var mv = key.Substring(1, key.Length - 2);
                    var fv = float.Parse(value);
                    SetMenuWidth(mv, fv);

                    if(mv == "Menu1")
                        menu1 = fv;
                    else if(mv == "Menu2")
                        menu2 = fv;
                }
                catch(System.Exception)
                {
                }
            }
        }

        language_map = new Dictionary<string, string>();
        for(int i = 0; i < list.Count; ++i)
        {
            var kv = list[i];
            var key = kv.Key;
            var value = kv.Value;
            if(key[0] == '<')
                continue;
            else if(key[0] == '[')
            {
                language_map[key] = value;
            }
            var obj = GenericUtils.Get(key);
            if(obj == null)
                continue;
            
            // Try TextMeshProUGUI first, fallback to legacy Text
            var tmpText = obj.GetComponent<TextMeshProUGUI>();
            if(tmpText != null)
            {
                tmpText.text = value;
            }
            else
            {
                var text = obj.GetComponent<UnityEngine.UI.Text>();
                if(text != null)
                    text.text = value;
            }

            if(string.CompareOrdinal(key, 0, "FirstWindow", 0, 11) == 0)
                FirstWindowTitlebar = value;

            float fv = 0;
            if(key.IndexOf("Menu1") >= 0)
                fv = menu1;
            else if(key.IndexOf("Menu2") >= 0)
                fv = menu2;
            if(fv <= 0.001f)
                continue;

            var rt = obj.transform as RectTransform;
            rt.sizeDelta = new Vector2(fv - 52, rt.sizeDelta.y);
        }

        PlayerPrefs.SetString("language", lang);
    }
    
    /// <summary>
    /// Gets the localized text for a key.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <returns>The translated text or the key if not found.</returns>
    public static string GetText(string key)
    {
        string v = null;
        language_map.TryGetValue(key, out v);
        if(string.IsNullOrEmpty(v))
            return key;
        else
            return v;
    }
    
    /// <summary>
    /// Sets the width of a menu element.
    /// </summary>
    /// <param name="menu">The menu name.</param>
    /// <param name="v">The width value.</param>
    static void SetMenuWidth(string menu, float v)
    {
        var o = GenericUtils.Get("Options.MenuPad."+menu);
        if(o == null)
            return;
        var rt = o.transform as RectTransform;
        rt.sizeDelta = new Vector2(v, rt.sizeDelta.y);
    }
    
    /// <summary>
    /// Dictionary storing language translations.
    /// </summary>
    static Dictionary<string, string> language_map = new Dictionary<string, string>();
    
    /// <summary>
    /// Stores the localized title bar text for the first window.
    /// </summary>
    public static string FirstWindowTitlebar = null;
}
