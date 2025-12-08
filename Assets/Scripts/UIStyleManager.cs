using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages UI styling for a dark, professional ERA game theme.
/// Applies consistent colors, gradients, and effects to UI elements.
/// </summary>
public class UIStyleManager : MonoBehaviour
{
    /// <summary>
    /// Dark theme color palette for ERA-style UI.
    /// </summary>
    public static class DarkTheme
    {
        // Background colors - very dark with slight blue tint
        public static readonly Color BackgroundDark = new Color(0.08f, 0.08f, 0.10f, 1f);      // #141418
        public static readonly Color BackgroundMedium = new Color(0.12f, 0.12f, 0.15f, 1f);    // #1F1F26
        public static readonly Color BackgroundLight = new Color(0.16f, 0.16f, 0.20f, 1f);     // #292933
        
        // Accent colors - subtle blue-gray
        public static readonly Color AccentPrimary = new Color(0.25f, 0.35f, 0.50f, 1f);       // #405580
        public static readonly Color AccentSecondary = new Color(0.30f, 0.42f, 0.58f, 1f);     // #4D6B94
        
        // Text colors
        public static readonly Color TextPrimary = new Color(0.85f, 0.85f, 0.87f, 1f);         // #D9D9DD
        public static readonly Color TextSecondary = new Color(0.60f, 0.60f, 0.65f, 1f);       // #9999A6
        public static readonly Color TextDisabled = new Color(0.40f, 0.40f, 0.45f, 1f);        // #666672
        
        // Button colors
        public static readonly Color ButtonNormal = new Color(0.18f, 0.18f, 0.22f, 1f);        // #2E2E38
        public static readonly Color ButtonHighlight = new Color(0.22f, 0.28f, 0.38f, 1f);     // #384760
        public static readonly Color ButtonPressed = new Color(0.15f, 0.20f, 0.30f, 1f);       // #26334D
        
        // Border and separator
        public static readonly Color Border = new Color(0.25f, 0.25f, 0.30f, 1f);              // #40404D
        public static readonly Color Separator = new Color(0.20f, 0.20f, 0.25f, 1f);           // #333340
        
        // Special highlights
        public static readonly Color GlowSoft = new Color(0.40f, 0.55f, 0.75f, 0.2f);          // Soft blue glow
    }
    
    /// <summary>
    /// Applies dark theme styling to a GameObject and its children.
    /// </summary>
    /// <param name="root">The root GameObject to style.</param>
    public static void ApplyDarkTheme(GameObject root)
    {
        if (root == null)
            return;
            
        // Style all Images
        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var image in images)
        {
            if (image.name.Contains("Background") || image.name.Contains("background"))
            {
                image.color = DarkTheme.BackgroundDark;
            }
            else if (image.name.Contains("Panel") || image.name.Contains("panel"))
            {
                image.color = DarkTheme.BackgroundMedium;
            }
            else if (image.name.Contains("Border") || image.name.Contains("border"))
            {
                image.color = DarkTheme.Border;
            }
        }
        
        // Style all Texts
        var texts = root.GetComponentsInChildren<Text>(true);
        foreach (var text in texts)
        {
            // Keep existing colors for content, but adjust UI labels
            if (text.name.Contains("Title") || text.name.Contains("title") || 
                text.name.Contains("Label") || text.name.Contains("label"))
            {
                text.color = DarkTheme.TextPrimary;
            }
        }
        
        // Style all Buttons
        var buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            var colors = button.colors;
            colors.normalColor = DarkTheme.ButtonNormal;
            colors.highlightedColor = DarkTheme.ButtonHighlight;
            colors.pressedColor = DarkTheme.ButtonPressed;
            colors.selectedColor = DarkTheme.ButtonHighlight;
            colors.disabledColor = new Color(DarkTheme.ButtonNormal.r, DarkTheme.ButtonNormal.g, 
                                            DarkTheme.ButtonNormal.b, 0.5f);
            button.colors = colors;
        }
    }
    
    /// <summary>
    /// Applies a subtle gradient effect to an Image component.
    /// </summary>
    /// <param name="image">The Image to apply gradient to.</param>
    /// <param name="topColor">Top gradient color.</param>
    /// <param name="bottomColor">Bottom gradient color.</param>
    public static void ApplyGradient(Image image, Color topColor, Color bottomColor)
    {
        if (image == null)
            return;
            
        // Unity UI doesn't support gradients directly without a shader
        // For now, just apply the average color
        // In a full implementation, you'd use a custom shader material
        Color avgColor = Color.Lerp(topColor, bottomColor, 0.5f);
        image.color = avgColor;
    }
    
    /// <summary>
    /// Creates a subtle shadow effect for text.
    /// </summary>
    /// <param name="text">The Text component to add shadow to.</param>
    public static void AddTextShadow(Text text)
    {
        if (text == null)
            return;
            
        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }
        
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }
    
    /// <summary>
    /// Adds an outline effect to text for better readability.
    /// </summary>
    /// <param name="text">The Text component to add outline to.</param>
    public static void AddTextOutline(Text text)
    {
        if (text == null)
            return;
            
        var outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }
        
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1f, 1f);
    }
}
