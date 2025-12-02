using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Utility class for managing fonts and TextMeshPro font assets.
/// Provides methods to load and cache fonts by name.
/// </summary>
public static class FontUtils
{
    /// <summary>
    /// Mapping of font names to their resource paths.
    /// </summary>
    static readonly Dictionary<string, string> name_path_map = new Dictionary<string, string>
    {
        {"ＭＳ ゴシック", "MS Gothic"},
        {"MS Gothic", "MS Gothic"},
        {"ＭＳ Ｐゴシック", "MS PGothic"},
        {"MS PGothic", "MS PGothic"},
    };

    /// <summary>
    /// Sets the default font by name.
    /// </summary>
    /// <param name="fontname">The font name to set as default.</param>
    public static void SetDefaultFont(string fontname)
    {
        default_font = GetFont(fontname);
        if(default_font == null)
        {
            default_fontname = "ＭＳ ゴシック";
            default_font = GetFont(default_fontname);
        }
        else
        {
            default_fontname = fontname;
        }

        // Also set TMP font
        default_tmp_font = GetTMPFont(fontname);
        if(default_tmp_font == null)
        {
            default_tmp_font = GetTMPFont(default_fontname);
        }
    }

    /// <summary>
    /// Gets a Unity Font by name.
    /// </summary>
    /// <param name="name">The font name.</param>
    /// <returns>The loaded Font or null if not found.</returns>
    public static Font GetFont(string name)
    {
        if(string.IsNullOrEmpty(name))
            return default_font;

        if(name == last_name)
            return last_font;
        last_name = name;

        string path = null;
        name_path_map.TryGetValue(name, out path);

        return LoadFont(path);
    }
    
    /// <summary>
    /// Loads a Unity Font from resources.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <returns>The loaded Font.</returns>
    static Font LoadFont(string path)
    {
        if(string.IsNullOrEmpty(path))
            last_font = default_font;
        else if(!font_map.TryGetValue(path, out last_font))
        {
            last_font = Resources.Load<Font>("Fonts/" + path);
            if(last_font == null)
                last_font = default_font;
            font_map[path] = last_font;
        }
        return last_font;
    }

    /// <summary>
    /// Gets a TextMeshPro font asset by name.
    /// </summary>
    /// <param name="name">The font name.</param>
    /// <returns>The loaded TMP_FontAsset or null if not found.</returns>
    public static TMP_FontAsset GetTMPFont(string name)
    {
        if(string.IsNullOrEmpty(name))
            return default_tmp_font;

        if(name == last_tmp_name)
            return last_tmp_font;
        last_tmp_name = name;

        string path = null;
        name_path_map.TryGetValue(name, out path);

        return LoadTMPFont(path);
    }

    /// <summary>
    /// Loads a TextMeshPro font asset from resources.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <returns>The loaded TMP_FontAsset.</returns>
    static TMP_FontAsset LoadTMPFont(string path)
    {
        if(string.IsNullOrEmpty(path))
            last_tmp_font = default_tmp_font;
        else if(!tmp_font_map.TryGetValue(path, out last_tmp_font))
        {
            last_tmp_font = Resources.Load<TMP_FontAsset>("Fonts/" + path + " SDF");
            if(last_tmp_font == null)
                last_tmp_font = default_tmp_font;
            tmp_font_map[path] = last_tmp_font;
        }
        return last_tmp_font;
    }

    /// <summary>
    /// The last used font name for caching.
    /// </summary>
    public static string last_name = null;
    
    /// <summary>
    /// The last loaded Unity Font for caching.
    /// </summary>
    public static Font last_font = null;

    /// <summary>
    /// The last used TMP font name for caching.
    /// </summary>
    public static string last_tmp_name = null;
    
    /// <summary>
    /// The last loaded TMP font asset for caching.
    /// </summary>
    public static TMP_FontAsset last_tmp_font = null;

    /// <summary>
    /// Gets the default font name.
    /// </summary>
    public static string default_fontname { get; private set; }
    
    /// <summary>
    /// Gets the default Unity Font.
    /// </summary>
    public static Font default_font { get; private set; }
    
    /// <summary>
    /// Gets the default TextMeshPro font asset.
    /// </summary>
    public static TMP_FontAsset default_tmp_font { get; private set; }

    /// <summary>
    /// Cache for loaded Unity Fonts.
    /// </summary>
    static Dictionary<string, Font> font_map = new Dictionary<string, Font>();
    
    /// <summary>
    /// Cache for loaded TMP font assets.
    /// </summary>
    static Dictionary<string, TMP_FontAsset> tmp_font_map = new Dictionary<string, TMP_FontAsset>();
}
