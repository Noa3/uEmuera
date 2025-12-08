using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Manages post-processing effects for the game view.
/// Provides a CRT/old monitor effect with configurable settings.
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
    public PostProcessVolume postProcessVolume;
    
    /// <summary>
    /// The camera that will use post-processing.
    /// </summary>
    [Tooltip("The camera that will use post-processing")]
    public Camera targetCamera;
    
    /// <summary>
    /// The post-processing layer for the camera.
    /// </summary>
    private PostProcessLayer postProcessLayer_;
    
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
        // Get or add PostProcessLayer to camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera != null)
        {
            postProcessLayer_ = targetCamera.GetComponent<PostProcessLayer>();
            if (postProcessLayer_ == null)
            {
                postProcessLayer_ = targetCamera.gameObject.AddComponent<PostProcessLayer>();
                postProcessLayer_.volumeTrigger = targetCamera.transform;
                
                // Set layer mask to PostProcessing if it exists
                int ppLayer = LayerMask.NameToLayer("PostProcessing");
                if (ppLayer >= 0)
                {
                    postProcessLayer_.volumeLayer = 1 << ppLayer;
                }
                else
                {
                    // Fallback to default layer
                    postProcessLayer_.volumeLayer = LayerMask.GetMask("Default");
                }
            }
        }
        
        // Create post-process volume if not assigned
        if (postProcessVolume == null)
        {
            GameObject volumeObj = new GameObject("PostProcessVolume");
            volumeObj.transform.SetParent(transform);
            
            // Set layer to PostProcessing if it exists, otherwise use default
            int ppLayer = LayerMask.NameToLayer("PostProcessing");
            if (ppLayer >= 0)
            {
                volumeObj.layer = ppLayer;
            }
            
            postProcessVolume = volumeObj.AddComponent<PostProcessVolume>();
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
        PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        postProcessVolume.profile = profile;
        
        // Add Vignette for darker edges (CRT monitors had this)
        var vignette = profile.AddSettings<Vignette>();
        vignette.enabled.Override(true);
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.4f);
        vignette.roundness.Override(1f);
        
        // Add Chromatic Aberration for color separation at edges
        var chromaticAberration = profile.AddSettings<ChromaticAberration>();
        chromaticAberration.enabled.Override(true);
        chromaticAberration.intensity.Override(0.15f);
        
        // Add Grain for that old monitor noise
        var grain = profile.AddSettings<Grain>();
        grain.enabled.Override(true);
        grain.intensity.Override(0.25f);
        grain.size.Override(1.2f);
        grain.lumContrib.Override(0.8f);
        grain.colored.Override(false);
        
        // Add Color Grading for slight green/amber tint (old monitor feel)
        var colorGrading = profile.AddSettings<ColorGrading>();
        colorGrading.enabled.Override(true);
        colorGrading.temperature.Override(5f); // Slightly warm
        colorGrading.tint.Override(-5f); // Slightly green
        colorGrading.saturation.Override(-10f); // Slightly desaturated
        colorGrading.contrast.Override(10f); // Increase contrast
        
        // Add Bloom for slight glow (CRT phosphor glow)
        var bloom = profile.AddSettings<Bloom>();
        bloom.enabled.Override(true);
        bloom.intensity.Override(0.5f);
        bloom.threshold.Override(0.9f);
        bloom.softKnee.Override(0.5f);
        bloom.diffusion.Override(7f);
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
        
        if (postProcessLayer_ != null)
        {
            postProcessLayer_.enabled = enabled;
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
