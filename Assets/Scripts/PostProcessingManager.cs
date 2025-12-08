#if ENABLE_POST_PROCESSING

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Manages post-processing effects for the game view.
/// Provides a CRT/old monitor effect with configurable settings.
/// Uses Post Processing Stack v3 API.
/// 
/// Note: Post Processing is optional. Define ENABLE_POST_PROCESSING to use this feature.
/// </summary>
public class PostProcessingManager : MonoBehaviour
{
    /// <summary>
    /// PlayerPrefs key for storing post-processing enabled state.
    /// </summary>
    private const string PREF_POST_PROCESSING_ENABLED = "PostProcessingEnabled";
    
    /// <summary>
    /// Singleton instance for accessing PostProcessingManager from other scripts.
    /// </summary>
    public static PostProcessingManager instance { get; private set; }
    
    /// <summary>
    /// The post-processing volume component.
    /// </summary>
    [Tooltip("The post-processing volume component")]
    public Volume postProcessVolume;
    
    /// <summary>
    /// The camera that will use post-processing.
    /// </summary>
    [Tooltip("The camera that will use post-processing")]
    public Camera targetCamera;
    
    /// <summary>
    /// Whether post-processing is currently enabled.
    /// </summary>
    private bool isEnabled_;
    
    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize post-processing
        InitializePostProcessing();
    }
    
    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    
    /// <summary>
    /// Initializes the post-processing system.
    /// Creates the volume if needed and loads saved settings.
    /// </summary>
    void InitializePostProcessing()
    {
        // Get camera reference
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // Create post-process volume if not assigned
        if (postProcessVolume == null)
        {
            GameObject volumeObj = new GameObject("PostProcessVolume");
            volumeObj.transform.SetParent(transform);
            
            postProcessVolume = volumeObj.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
            postProcessVolume.priority = 1;
            
            // Create and configure the profile
            ConfigureCRTEffect();
        }
        
        // Load saved preference
        isEnabled_ = PlayerPrefs.GetInt(PREF_POST_PROCESSING_ENABLED, 0) == 1;
        SetPostProcessingEnabled(isEnabled_);
    }
    
    /// <summary>
    /// Configures the CRT/old monitor effect profile.
    /// </summary>
    void ConfigureCRTEffect()
    {
        if (postProcessVolume == null)
            return;
        
        // Create a new profile
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        postProcessVolume.profile = profile;
        
        // In Post Processing v3, effects are added as settings components
        // Note: Not all v2 effects are available in v3. The API is different.
        // For v3, use the URP/HDRP built-in effects or implement custom ones.
        
        // Add Bloom effect if available
        try
        {
            var bloom = profile.Add<Bloom>(false);
            bloom.intensity.value = 0.5f;
            bloom.threshold.value = 0.9f;
            bloom.active = true;
        }
        catch
        {
            Debug.LogWarning("Bloom effect not available in current render pipeline");
        }
    }
    
    /// <summary>
    /// Gets whether post-processing is currently enabled.
    /// </summary>
    public bool IsEnabled()
    {
        return isEnabled_;
    }
    
    /// <summary>
    /// Sets whether post-processing is enabled.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    public void SetPostProcessingEnabled(bool enabled)
    {
        isEnabled_ = enabled;
        
        if (postProcessVolume != null)
        {
            postProcessVolume.enabled = enabled;
        }
        
        // Save preference
        PlayerPrefs.SetInt(PREF_POST_PROCESSING_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Toggles post-processing on/off.
    /// </summary>
    public void TogglePostProcessing()
    {
        SetPostProcessingEnabled(!isEnabled_);
    }
}

#else

using UnityEngine;

/// <summary>
/// Post-processing disabled. Define ENABLE_POST_PROCESSING in your build settings to enable.
/// </summary>
public class PostProcessingManager : MonoBehaviour
{
    public static PostProcessingManager instance { get; private set; }
    
    public Camera targetCamera { get; set; }
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    
    public bool IsEnabled() => false;
    public void SetPostProcessingEnabled(bool enabled) { }
    public void TogglePostProcessing() { }
}

#endif
