using UnityEngine;

/// <summary>
/// Adjusts a RectTransform to respect the device's safe area (notch, home indicator, etc.).
/// Attach this to a root panel that should avoid hardware obstructions on phones/tablets.
/// The component re-applies automatically whenever the safe area changes (foldable phones,
/// orientation changes, etc.).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rect_;
    Rect lastSafeArea_ = Rect.zero;
    Vector2Int lastScreenSize_ = Vector2Int.zero;

    void Awake()
    {
        rect_ = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        // Re-apply only when something changed (safe area, screen size).
        if (Screen.safeArea != lastSafeArea_ ||
            Screen.width  != lastScreenSize_.x ||
            Screen.height != lastScreenSize_.y)
        {
            Apply();
        }
    }

    void Apply()
    {
        Rect safe = Screen.safeArea;
        if (safe == lastSafeArea_ &&
            Screen.width  == lastScreenSize_.x &&
            Screen.height == lastScreenSize_.y)
            return;

        lastSafeArea_    = safe;
        lastScreenSize_  = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        // Convert from pixel coordinates to normalized anchor values.
        Vector2 anchorMin = new Vector2(safe.x / Screen.width,  safe.y / Screen.height);
        Vector2 anchorMax = new Vector2((safe.x + safe.width)  / Screen.width,
                                        (safe.y + safe.height) / Screen.height);

        rect_.anchorMin = anchorMin;
        rect_.anchorMax = anchorMax;
        rect_.offsetMin = Vector2.zero;
        rect_.offsetMax = Vector2.zero;
    }
}
