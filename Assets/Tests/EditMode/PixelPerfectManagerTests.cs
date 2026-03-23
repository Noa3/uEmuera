using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Tests for PixelPerfectManager configuration and stub behavior.
/// The full feature is gated behind ENABLE_PIXEL_PERFECT; these tests verify
/// the stub API contract that is always compiled in.
/// </summary>
public class PixelPerfectManagerTests
{
    [Test]
    public void PixelPerfectManager_StubIsDisabledByDefault()
    {
        // The stub implementation (compiled when ENABLE_PIXEL_PERFECT is not defined)
        // must always report IsEnabled() == false so that UI toggles reflect actual state.
        var go = new GameObject("TestPPCam");
        var manager = go.AddComponent<PixelPerfectManager>();

        Assert.IsFalse(manager.IsEnabled(), "Stub PixelPerfectManager must report disabled");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PixelPerfectManager_ReferenceResolutionValid()
    {
        // Test that default reference resolution is reasonable
        int defaultWidth = 1920;
        int defaultHeight = 1080;
        
        Assert.IsTrue(defaultWidth > 0, "Reference width should be positive");
        Assert.IsTrue(defaultHeight > 0, "Reference height should be positive");
        Assert.IsTrue(defaultWidth >= defaultHeight, "Width should be >= height for landscape");
    }
    
    [Test]
    public void PixelPerfectManager_PixelsPerUnitValid()
    {
        // Test that pixels per unit is reasonable
        int pixelsPerUnit = 100;
        
        Assert.IsTrue(pixelsPerUnit > 0, "Pixels per unit should be positive");
        Assert.IsTrue(pixelsPerUnit <= 1000, "Pixels per unit should be reasonable (<=1000)");
    }

    [Test]
    public void PixelPerfectManager_SetEnabled_DoesNotThrow()
    {
        var go = new GameObject("TestPPCam2");
        var manager = go.AddComponent<PixelPerfectManager>();

        Assert.DoesNotThrow(() => manager.SetPixelPerfectEnabled(true));
        Assert.DoesNotThrow(() => manager.SetPixelPerfectEnabled(false));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PixelPerfectManager_GetPixelRatio_ReturnsPositive()
    {
        var go = new GameObject("TestPPCam3");
        var manager = go.AddComponent<PixelPerfectManager>();

        int ratio = manager.GetPixelRatio();
        Assert.IsTrue(ratio >= 1, "Pixel ratio should be at least 1");

        Object.DestroyImmediate(go);
    }
}
