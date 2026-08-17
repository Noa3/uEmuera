using System.Collections.Generic;

namespace uEmuera.Runtime.Detection
{
    /// <summary>
    /// Runtime-specific game directory inspector.
    /// Each runtime family implements this interface to recognize its own packages.
    /// Register implementations in <see cref="GameDetector.CreateDefault"/>.
    /// </summary>
    public interface IGameDetector
    {
        /// <summary>Which runtime this detector identifies.</summary>
        RuntimeKind Kind { get; }

        /// <summary>
        /// Inspect <paramref name="directory"/> using the provided top-level file list
        /// and return a <see cref="DetectionResult"/> if this detector recognizes the
        /// package, or null if it is clearly not this runtime.
        ///
        /// Must not throw; errors become warnings inside a returned Low-confidence result.
        /// </summary>
        DetectionResult TryDetect(string directory, IReadOnlyList<string> topLevelFiles);

        /// <summary>
        /// Build a full <see cref="GameDescriptor"/> from a directory already confirmed
        /// to be this runtime's package.  Called only when TryDetect returned non-null.
        /// </summary>
        GameDescriptor BuildDescriptor(string directory, DetectionResult result);
    }
}
