#if ENABLE_PIXEL_PERFECT

using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Manages Pixel Perfect Camera for crisp rendering of images and text.
/// Works in conjunction with PostProcessingManager to provide both crisp rendering
/// and optional CRT effects.
/// 
/// Note: Pixel Perfect rendering is optional. Define ENABLE_PIXEL_PERFECT to use this feature.
/// </summary>
public class PixelPerfectManager : MonoBehaviour
{
    /// <summary>
    /// PlayerPrefs key for storing pixel perfect enabled state.
    /// </summary>
    private const string PREF_PIXEL_PERFECT_ENABLED = "PixelPerfectEnabled";
    
    /// <summary>
    /// Singleton instance for accessing PixelPerfectManager from other scripts.
    /// </summary>
    public static PixelPerfectManager instance { get; private set; }
    
    /// <summary>
    /// The camera that will use pixel perfect rendering.
    /// </summary>
    [Tooltip("The camera that will use pixel perfect rendering")]
    public Camera targetCamera;
    
    /// <summary>
    /// The Pixel Perfect Camera component.
    /// </summary>
    private PixelPerfectCamera pixelPerfectCamera_;
    
    /// <summary>
    /// Whether pixel perfect rendering is currently enabled.
    /// </summary>
    private bool isEnabled_;
    
    /// <summary>
    /// Reference resolution width for pixel perfect calculations.
    /// Should match the game's design resolution.
    /// </summary>
    [Tooltip("Reference resolution width for pixel perfect calculations")]
    public int referenceResolutionWidth = 1920;
    
    /// <summary>
    /// Reference resolution height for pixel perfect calculations.
    /// Should match the game's design resolution.
    /// </summary>
    [Tooltip("Reference resolution height for pixel perfect calculations")]
    public int referenceResolutionHeight = 1080;
    
    /// <summary>
    /// Pixels per unit for sprite rendering.
    /// Higher values result in smaller sprites.
    /// </summary>
    [Tooltip("Pixels per unit for sprite rendering")]
    public int pixelsPerUnit = 100;
    
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
        
        // Initialize pixel perfect camera
        InitializePixelPerfect();
    }
    
    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    
    /// <summary>
    /// Initializes the pixel perfect camera system.
    /// </summary>
    void InitializePixelPerfect()
    {
        // Get or create camera reference
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera != null)
        {
            // Get or add PixelPerfectCamera component
            pixelPerfectCamera_ = targetCamera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera_ == null)
            {
                pixelPerfectCamera_ = targetCamera.gameObject.AddComponent<PixelPerfectCamera>();
                ConfigurePixelPerfectSettings();
            }
        }
        
        // Load saved preference (default to enabled for crisp visuals)
        isEnabled_ = PlayerPrefs.GetInt(PREF_PIXEL_PERFECT_ENABLED, 1) == 1;
        SetPixelPerfectEnabled(isEnabled_);
    }
    
    /// <summary>
    /// Configures the pixel perfect camera settings for optimal crisp rendering.
    /// </summary>
    void ConfigurePixelPerfectSettings()
    {
        if (pixelPerfectCamera_ == null)
            return;
        
        // Set reference resolution based on game design
        pixelPerfectCamera_.refResolutionX = referenceResolutionWidth;
        pixelPerfectCamera_.refResolutionY = referenceResolutionHeight;
        
        // Asset pixels per unit (should match sprites if using sprites)
        pixelPerfectCamera_.assetsPPU = pixelsPerUnit;
        
        // Enable crop frame for black bars if needed
        pixelPerfectCamera_.cropFrameX = false;
        pixelPerfectCamera_.cropFrameY = false;
        
        // Stretch fill maintains aspect ratio while filling screen
        pixelPerfectCamera_.stretchFill = true;
        
        // Upscale render texture - keeps things sharp
        pixelPerfectCamera_.upscaleRT = true;
        
        // Pixel snapping ensures UI elements align to pixel grid
        pixelPerfectCamera_.pixelSnapping = true;
        
        // Run in edit mode for preview
        pixelPerfectCamera_.runInEditMode = false;
    }
    
    /// <summary>
    /// Gets whether pixel perfect rendering is currently enabled.
    /// </summary>
    public bool IsEnabled()
    {
        return isEnabled_;
    }
    
    /// <summary>
    /// Sets whether pixel perfect rendering is enabled.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    public void SetPixelPerfectEnabled(bool enabled)
    {
        isEnabled_ = enabled;
        
        if (pixelPerfectCamera_ != null)
        {
            pixelPerfectCamera_.enabled = enabled;
        }
        
        // Save preference
        PlayerPrefs.SetInt(PREF_PIXEL_PERFECT_ENABLED, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Toggles pixel perfect rendering on/off.
    /// </summary>
    public void TogglePixelPerfect()
    {
        SetPixelPerfectEnabled(!isEnabled_);
    }
    
    /// <summary>
    /// Updates the reference resolution.
    /// Useful when screen resolution or orientation changes.
    /// </summary>
    /// <param name="width">New reference width.</param>
    /// <param name="height">New reference height.</param>
    public void UpdateReferenceResolution(int width, int height)
    {
        referenceResolutionWidth = width;
        referenceResolutionHeight = height;
        
        if (pixelPerfectCamera_ != null)
        {
            pixelPerfectCamera_.refResolutionX = width;
            pixelPerfectCamera_.refResolutionY = height;
        }
    }
    
    /// <summary>
    /// Gets the current pixel ratio (how many screen pixels per game pixel).
    /// Useful for UI scaling calculations.
    /// </summary>
    /// <returns>The pixel ratio.</returns>
    public int GetPixelRatio()
    {
        if (pixelPerfectCamera_ != null && pixelPerfectCamera_.enabled)
        {
            return pixelPerfectCamera_.pixelRatio;
        }
        return 1;
    }
}

#else

using UnityEngine;

/// <summary>
/// Pixel Perfect rendering disabled. Define ENABLE_PIXEL_PERFECT in your build settings to enable.
/// </summary>
public class PixelPerfectManager : MonoBehaviour
{
    public static PixelPerfectManager instance { get; private set; }
    
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
    public void SetPixelPerfectEnabled(bool enabled) { }
    public void TogglePixelPerfect() { }
    public void UpdateReferenceResolution(int width, int height) { }
    public int GetPixelRatio() => 1;
}

#endif
