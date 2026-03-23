using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Tests for PostProcessingManager configuration and stub behavior.
/// The full feature is gated behind ENABLE_POST_PROCESSING; these tests verify
/// the stub API contract that is always compiled in.
/// </summary>
public class PostProcessingManagerTests
{
    [Test]
    public void PostProcessingManager_StubIsDisabledByDefault()
    {
        // The stub implementation (compiled when ENABLE_POST_PROCESSING is not defined)
        // must always report IsEnabled() == false so that UI toggles reflect actual state.
        var go = new GameObject("TestPPM");
        var manager = go.AddComponent<PostProcessingManager>();

        Assert.IsFalse(manager.IsEnabled(), "Stub PostProcessingManager must report disabled");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PostProcessingManager_SetEnabled_DoesNotThrow()
    {
        var go = new GameObject("TestPPM2");
        var manager = go.AddComponent<PostProcessingManager>();

        Assert.DoesNotThrow(() => manager.SetPostProcessingEnabled(true));
        Assert.DoesNotThrow(() => manager.SetPostProcessingEnabled(false));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void PostProcessingManager_Toggle_DoesNotThrow()
    {
        var go = new GameObject("TestPPM3");
        var manager = go.AddComponent<PostProcessingManager>();

        Assert.DoesNotThrow(() => manager.TogglePostProcessing());

        Object.DestroyImmediate(go);
    }
}
