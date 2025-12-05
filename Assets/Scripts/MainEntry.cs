using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using MinorShift.Emuera;
using MinorShift._Library;
using System.Text;

public class MainEntry : MonoBehaviour
{
    void Awake()
    {
        // Set target frame rate to match the screen's refresh rate
        // This prevents rendering faster than the display can show
        int screenRefreshRate = GetScreenRefreshRate();
        Application.targetFrameRate = screenRefreshRate;
        
        ResolutionHelper.Apply();
        
        // Initialize on-demand rendering manager
        InitializeOnDemandRendering();
    }
    
    /// <summary>
    /// Gets the screen's refresh rate in Hz.
    /// Falls back to 60 Hz if unable to determine.
    /// </summary>
    /// <returns>The screen refresh rate in Hz.</returns>
    int GetScreenRefreshRate()
    {
        // Try to get the current display's refresh rate
        Resolution currentResolution = Screen.currentResolution;
        int refreshRate = currentResolution.refreshRateRatio.numerator > 0 
            ? (int)(currentResolution.refreshRateRatio.value)
            : 60;
        
        // Ensure we have a reasonable value (at least 30, at most 240)
        if (refreshRate < 30)
            refreshRate = 60;
        else if (refreshRate > 240)
            refreshRate = 240;
            
        return refreshRate;
    }
    
    /// <summary>
    /// Initializes the on-demand rendering system.
    /// Creates the OnDemandRenderManager if it doesn't exist.
    /// </summary>
    void InitializeOnDemandRendering()
    {
        if (OnDemandRenderManager.instance == null)
        {
            var renderManagerObj = new GameObject("OnDemandRenderManager");
            renderManagerObj.AddComponent<OnDemandRenderManager>();
            DontDestroyOnLoad(renderManagerObj);
        }
    }

    void Start()
    {
        // Initialize logging system - always enabled for debugging
        InitializeLogging();
        
        LoadConfigMaps();
        if(!MultiLanguage.SetLanguage())
        {
            Object.FindFirstObjectByType<OptionWindow>().ShowLanguageBox();
        }
    }
    
    /// <summary>
    /// Initializes the logging system to output to Unity console.
    /// This is always enabled to help with debugging errors and warnings.
    /// </summary>
    void InitializeLogging()
    {
        uEmuera.Logger.info = GenericUtils.Info;
        uEmuera.Logger.warn = GenericUtils.Warn;
        uEmuera.Logger.error = GenericUtils.Error;
    }

#if UNITY_EDITOR
    public string era_path;
#endif

    void LoadConfigMaps()
    {
        char[] split = new char[] { '\x0d', '\x0a' };
        var shiftjis = Resources.Load<TextAsset>("Text/emuera_config_shiftjis");
        if(shiftjis == null)
            return;
        var utf8 = Resources.Load<TextAsset>("Text/emuera_config_utf8");
        if(utf8 == null)
            return;
        var utf8_cn = Resources.Load<TextAsset>("Text/emuera_config_utf8_zhcn");
        if(utf8_cn == null)
            return;

        //var jis_text = System.Text.Encoding.UTF8.GetString(shiftjis.bytes);
        //var jis_strs = jis_text.Split(split);

        var jis_bytes = shiftjis.bytes;
        var jis_md5_strs = GenericUtils.CalcMd5List(jis_bytes);

        var utf8_strs = utf8.text.Split(split);
        var utf8_str_list = new List<string>();
        foreach (var str in utf8_strs)
        {
            if (string.IsNullOrWhiteSpace(str))
                continue;
            utf8_str_list.Add(str);
        }

        var utf8cn_strs = utf8_cn.text.Split(split);
        var utf8cn_str_list = new List<string>();
        foreach (var str in utf8cn_strs)
        {
            if (string.IsNullOrWhiteSpace(str))
                continue;
            utf8cn_str_list.Add(str);
        }

        if (jis_md5_strs.Count != utf8cn_str_list.Count)
            return;

        Dictionary<string, string> jis_map = new Dictionary<string, string>();
        for(int i = 0; i < jis_md5_strs.Count; ++i)
        {
            jis_map[jis_md5_strs[i]] = utf8_str_list[i];
        }
        Dictionary<string, string> utf8cn_map = new Dictionary<string, string>();
        for(int i = 0; i < utf8cn_str_list.Count; ++i)
        {
            utf8cn_map[utf8cn_str_list[i]] = utf8_str_list[i];
        }
        uEmuera.Utils.SetSHIFTJIS_to_UTF8Dict(jis_map);
        uEmuera.Utils.SetUTF8ZHCN_to_UTF8Dict(utf8cn_map);
    }
}
