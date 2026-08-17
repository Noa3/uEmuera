using System.Collections.Generic;

namespace MinorShift.Emuera.Compatibility
{
    /// <summary>
    /// Rendering capabilities exposed to the parity generator and render tests.
    /// The renderer remains platform-specific; these IDs describe observable surfaces.
    /// </summary>
    public static class RendererCapabilityRegistry
    {
        public static IReadOnlyCollection<string> RegisteredCapabilities { get; } = new[]
        {
            FeatureCapabilityIds.HtmlDiv,
            FeatureCapabilityIds.HtmlClearButton,
            FeatureCapabilityIds.HtmlImageSrcb,
            FeatureCapabilityIds.HtmlImageSrcm,
            FeatureCapabilityIds.HtmlPrintIsland,
            FeatureCapabilityIds.CbgSprite,
            FeatureCapabilityIds.CbgButtonMap,
            FeatureCapabilityIds.CbgOrdering,
            FeatureCapabilityIds.InputMouse,
            FeatureCapabilityIds.InputCoordinates,
        };
    }
}
