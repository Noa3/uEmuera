using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Manages on-demand rendering to reduce power consumption when the screen is static.
/// Rendering only occurs when:
/// - Mouse moves (if present)
/// - Touch input is detected
/// - Keyboard input is detected
/// - Content changes (text/color updates)
/// </summary>
public class OnDemandRenderManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static OnDemandRenderManager instance { get; private set; }

    /// <summary>
    /// The frame interval when idle (no input or changes).
    /// Higher values = lower frame rate when idle.
    /// </summary>
    [Tooltip("Frame interval when idle. Higher = lower frame rate when nothing is happening.")]
    [Range(1, 60)]
    public int idleFrameInterval = 30;

    /// <summary>
    /// The frame interval when active (input or changes detected).
    /// 1 = render every frame.
    /// </summary>
    [Tooltip("Frame interval when active. 1 = render every frame.")]
    [Range(1, 4)]
    public int activeFrameInterval = 1;

    /// <summary>
    /// Number of frames to keep rendering after the last input/change.
    /// This ensures smooth transitions and animations complete.
    /// </summary>
    [Tooltip("Number of frames to continue rendering after the last activity.")]
    [Range(1, 120)]
    public int activeFrameCount = 30;

    /// <summary>
    /// Minimum mouse movement threshold to trigger rendering.
    /// </summary>
    [Tooltip("Minimum mouse movement (in pixels) to trigger rendering.")]
    [Range(0.1f, 10f)]
    public float mouseMovementThreshold = 0.5f;

    /// <summary>
    /// Whether on-demand rendering is enabled.
    /// </summary>
    [Tooltip("Enable on-demand rendering to save power when screen is static.")]
    public bool enabled_ = true;

    // Tracking variables
    private Vector3 last_mouse_position_;
    private int frames_since_activity_;
    private bool content_dirty_;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Start()
    {
        last_mouse_position_ = Input.mousePosition;
        frames_since_activity_ = 0;
        content_dirty_ = true; // Start with rendering enabled
    }

    void Update()
    {
        if (!enabled_)
        {
            // When disabled, render every frame
            OnDemandRendering.renderFrameInterval = 1;
            return;
        }

        bool should_render = CheckForActivity();

        if (should_render)
        {
            frames_since_activity_ = 0;
            OnDemandRendering.renderFrameInterval = activeFrameInterval;
        }
        else
        {
            frames_since_activity_++;
            
            if (frames_since_activity_ >= activeFrameCount)
            {
                OnDemandRendering.renderFrameInterval = idleFrameInterval;
            }
        }
    }

    /// <summary>
    /// Checks for any activity that should trigger rendering.
    /// </summary>
    /// <returns>True if rendering should occur.</returns>
    private bool CheckForActivity()
    {
        // Check for content changes
        if (content_dirty_)
        {
            content_dirty_ = false;
            return true;
        }

        // Check for mouse movement
        if (CheckMouseMovement())
        {
            return true;
        }

        // Check for touch input
        if (Input.touchCount > 0)
        {
            return true;
        }

        // Check for any key press
        if (Input.anyKey || Input.anyKeyDown)
        {
            return true;
        }

        // Check for mouse buttons
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            return true;
        }

        // Check for scroll wheel
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f || 
            Mathf.Abs(Input.mouseScrollDelta.x) > 0.01f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the mouse has moved since the last frame.
    /// </summary>
    /// <returns>True if the mouse moved beyond the threshold.</returns>
    private bool CheckMouseMovement()
    {
        Vector3 current_mouse_pos = Input.mousePosition;
        float distance = Vector3.Distance(current_mouse_pos, last_mouse_position_);
        last_mouse_position_ = current_mouse_pos;

        return distance > mouseMovementThreshold;
    }

    /// <summary>
    /// Marks the content as dirty, triggering a render.
    /// Call this when text, colors, or other visual content changes.
    /// </summary>
    public void SetContentDirty()
    {
        content_dirty_ = true;
        frames_since_activity_ = 0;
        
        // Immediately set to active rendering for responsiveness
        OnDemandRendering.renderFrameInterval = activeFrameInterval;
    }

    /// <summary>
    /// Requests immediate rendering for the next few frames.
    /// Useful for ensuring animations or transitions complete smoothly.
    /// </summary>
    public void RequestRender()
    {
        frames_since_activity_ = 0;
        OnDemandRendering.renderFrameInterval = activeFrameInterval;
    }

    /// <summary>
    /// Requests rendering for a specific number of frames.
    /// </summary>
    /// <param name="frameCount">Number of frames to render.</param>
    public void RequestRenderFrames(int frameCount)
    {
        // Reset the counter to negative to extend active rendering
        frames_since_activity_ = -frameCount;
        OnDemandRendering.renderFrameInterval = activeFrameInterval;
    }
}
