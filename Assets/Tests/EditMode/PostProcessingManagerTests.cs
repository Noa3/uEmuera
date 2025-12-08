using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Tests for PostProcessingManager functionality.
/// </summary>
public class PostProcessingManagerTests
{
    [Test]
    public void DarkTheme_ColorsAreValid()
    {
        // Test that all theme colors are properly defined
        Assert.IsTrue(UIStyleManager.DarkTheme.BackgroundDark.a > 0.9f, "Background should be opaque");
        Assert.IsTrue(UIStyleManager.DarkTheme.TextPrimary.r > 0.5f, "Primary text should be light");
        Assert.IsTrue(UIStyleManager.DarkTheme.ButtonNormal.r < 0.3f, "Buttons should be dark");
    }
    
    [Test]
    public void DarkTheme_ProperContrast()
    {
        // Test that there's good contrast between text and background
        Color bg = UIStyleManager.DarkTheme.BackgroundDark;
        Color text = UIStyleManager.DarkTheme.TextPrimary;
        
        // Calculate luminance difference (simplified)
        float bgLum = bg.r * 0.299f + bg.g * 0.587f + bg.b * 0.114f;
        float textLum = text.r * 0.299f + text.g * 0.587f + text.b * 0.114f;
        
        float contrast = Mathf.Abs(textLum - bgLum);
        Assert.IsTrue(contrast > 0.5f, "Text should have good contrast with background");
    }
    
    [Test]
    public void ApplyDarkTheme_HandlesNullGracefully()
    {
        // Should not throw exception with null parameter
        Assert.DoesNotThrow(() => UIStyleManager.ApplyDarkTheme(null));
    }
}
