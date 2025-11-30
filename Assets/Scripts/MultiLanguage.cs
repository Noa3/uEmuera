using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MultiLanguage
{
    public static readonly char[] splits = new char[] { '\xa', '\xd' };
    
    private static bool useUnityLocalization = true;
    private static Dictionary<string, string> language_map = new Dictionary<string, string>();
    public static string FirstWindowTitlebar = null;

    /// <summary>
    /// Enables or disables Unity Localization Package integration.
    /// Set to false to use legacy system only.
    /// </summary>
    public static bool UseUnityLocalization
    {
        get { return useUnityLocalization; }
        set { useUnityLocalization = value; }
    }

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

    public static bool SetLanguage()
    {
        var lang = PlayerPrefs.GetString("language", "");
        if(string.IsNullOrEmpty(lang))
        {
            lang = "zh_cn";
            PlayerPrefs.SetString("language", lang);
        }

        if (useUnityLocalization && LocalizationHelper.Instance != null)
        {
            LocalizationHelper.SetLanguageByCode(lang);
            LoadLegacyData(lang);
            return true;
        }

        SetLanguage(lang);
        return true;
    }

    public static void SetLanguage(string lang)
    {
        if (useUnityLocalization && LocalizationHelper.Instance != null)
        {
            LocalizationHelper.SetLanguageByCode(lang);
            LoadLegacyData(lang);
        }
        else
        {
            SetLanguageLegacy(lang);
        }

        PlayerPrefs.SetString("language", lang);
    }

    /// <summary>
    /// Legacy language switching method. Updates all UI elements directly.
    /// </summary>
    private static void SetLanguageLegacy(string lang)
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
                catch(System.Exception e)
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
            var text = obj.GetComponent<UnityEngine.UI.Text>();
            if(text == null)
                continue;
            text.text = value;

            if(string.CompareOrdinal(key, 0, "FirstWindow", 0, 11) == 0)
                FirstWindowTitlebar = value;

            float fv = 0;
            if(key.IndexOf("Menu1") >= 0)
                fv = menu1;
            else if(key.IndexOf("Menu2") >= 0)
                fv = menu2;
            if(fv <= 0.001f)
                continue;

            var rt = text.transform as RectTransform;
            rt.sizeDelta = new Vector2(fv - 52, rt.sizeDelta.y);
        }
    }

    /// <summary>
    /// Loads legacy data (bracket keys and special values) when using Unity Localization.
    /// </summary>
    private static void LoadLegacyData(string lang)
    {
        var list = Load(lang);
        if(list == null)
            return;

        language_map = new Dictionary<string, string>();

        for(int i = 0; i < list.Count; ++i)
        {
            var kv = list[i];
            var key = kv.Key;
            var value = kv.Value;
            
            if(key[0] == '[')
            {
                language_map[key] = value;
            }
            else if(string.CompareOrdinal(key, 0, "FirstWindow", 0, 11) == 0)
            {
                FirstWindowTitlebar = value;
            }
        }
    }

    /// <summary>
    /// Gets localized text by key. Supports both bracket keys ([Wait], [Exit]) and regular keys.
    /// </summary>
    public static string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
            return key;

        if (key[0] == '[')
        {
            if (language_map.TryGetValue(key, out string v))
                return v;
            
            if (useUnityLocalization && LocalizationHelper.Instance != null)
            {
                string result = LocalizationHelper.GetMessageString(key);
                if (result != key)
                    return result;
            }
            
            return key;
        }

        if (useUnityLocalization && LocalizationHelper.Instance != null)
        {
            string result = LocalizationHelper.GetUIString(key);
            if (result != key)
                return result;
        }

        if (language_map.TryGetValue(key, out string value))
            return value;

        return key;
    }

    static void SetMenuWidth(string menu, float v)
    {
        var o = GenericUtils.Get("Options.MenuPad."+menu);
        if(o == null)
            return;
        var rt = o.transform as RectTransform;
        rt.sizeDelta = new Vector2(v, rt.sizeDelta.y);
    }
}
