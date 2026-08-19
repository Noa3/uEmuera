using System;
using System.Collections.Generic;

namespace uEmuera.Runtime
{
    /// <summary>
    /// Runtime-independent description of a detected game package.
    /// Contains only plain data — no Emuera- or EraElectron-specific objects.
    /// Created by a detector; passed to <see cref="IGameRuntime.InitializeAsync"/>.
    /// </summary>
    [Serializable]
    public sealed class GameDescriptor
    {
        /// <summary>
        /// Stable identifier derived from the game root path (SHA256 prefix).
        /// Used as a save namespace and library key.
        /// </summary>
        public string GameId { get; set; }

        /// <summary>Display title from game metadata, or folder name if unavailable.</summary>
        public string Title { get; set; }

        /// <summary>Game version string from metadata, or empty string if not specified.</summary>
        public string Version { get; set; }

        /// <summary>Which runtime this game requires.</summary>
        public RuntimeKind RuntimeKind { get; set; }

        /// <summary>Absolute path to the root directory that contains the game package.</summary>
        public string GameRoot { get; set; }

        /// <summary>
        /// Runtime-specific entry point, relative to <see cref="GameRoot"/>.
        /// Emuera: null (entry is the ERB sub-directory).
        /// EraElectron: relative path to the JS bundle (e.g. "main.js").
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// Runtime-specific configuration file path, relative to <see cref="GameRoot"/>.
        /// Emuera: "emuera.config". EraElectron: "ere.config.json" or equivalent.
        /// </summary>
        public string ConfigurationPath { get; set; }

        /// <summary>Absolute path to an icon image, or null if none found.</summary>
        public string IconPath { get; set; }

        /// <summary>Primary language code detected from game metadata (e.g. "ja", "zh-hans").</summary>
        public string Language { get; set; }

        /// <summary>
        /// Minimum runtime version string required by this game, or null.
        /// Interpreted by the matching <see cref="IGameRuntime"/> implementation.
        /// </summary>
        public string RequiredRuntimeVersion { get; set; }

        /// <summary>
        /// Additional directories mounted on top of the game package.
        /// Used for resource packs, mods, and user patches.
        /// Mount order is determined by <see cref="ResourceMount.Priority"/>.
        /// </summary>
        public List<ResourceMount> ResourceMounts { get; set; } = new List<ResourceMount>();

        /// <summary>
        /// Save-data namespace prefix, derived from <see cref="GameId"/>.
        /// Prevents save collisions between different games or runtime families.
        /// </summary>
        public string SaveNamespace { get; set; }

        /// <summary>
        /// Feature IDs found during detection (informational; runtime may re-scan).
        /// </summary>
        public List<string> DetectedFeatures { get; set; } = new List<string>();

        /// <summary>Detector confidence and evidence from the last directory scan.</summary>
        public DetectionResult DetectionResult { get; set; }

        /// <summary>
        /// Per-game user overrides (host mode, boot strategy, permissions, etc.).
        /// Null until the user modifies a setting for this game.
        /// </summary>
        public GameSettings UserSettings { get; set; }

        /// <summary>Last time this game was launched, UTC. Null if never launched.</summary>
        public DateTime? LastPlayed { get; set; }

        public override string ToString() =>
            $"Game({RuntimeKind}, \"{Title}\", v{Version}, root={GameRoot})";
    }

    // ------------------------------------------------------------------ //
    //  Supporting types                                                    //
    // ------------------------------------------------------------------ //

    /// <summary>An additional resource directory overlaid on the base game package.</summary>
    [Serializable]
    public sealed class ResourceMount
    {
        /// <summary>Absolute path to the mounted directory or archive root.</summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Describes what the mount provides.
        /// Well-known values: "ResourcePack", "Mod", "UserPatch".
        /// </summary>
        public string MountKind { get; set; }

        /// <summary>
        /// Resolution priority. Higher values win in overlay lookup.
        /// Convention: base game = 0, resource pack = 10, mod = 20, user patch = 100.
        /// </summary>
        public int Priority { get; set; }

        public override string ToString() =>
            $"Mount({MountKind}, pri={Priority}, {SourcePath})";
    }

    /// <summary>Detection confidence and evidence produced by an <see cref="Detection.IGameDetector"/>.</summary>
    [Serializable]
    public sealed class DetectionResult
    {
        /// <summary>How confident the detector is in its RuntimeKind assignment.</summary>
        public DetectionConfidence Confidence { get; set; }

        /// <summary>Machine-readable evidence keys that led to this decision.</summary>
        public List<string> Evidence { get; set; } = new List<string>();

        /// <summary>Human-readable warnings (e.g. ambiguous indicators, provisional heuristics).</summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// When the directory contained indicators for more than one runtime,
        /// the alternative RuntimeKind is stored here so the launcher can ask the user.
        /// </summary>
        public RuntimeKind? AmbiguousAlternative { get; set; }
    }

    public enum DetectionConfidence
    {
        Unknown = 0,
        Low     = 1,
        Medium  = 2,
        High    = 3,
        Certain = 4,
    }

    /// <summary>Per-game user preferences stored in the game library.</summary>
    [Serializable]
    public sealed class GameSettings
    {
        /// <summary>Force a specific runtime instead of the detected one.</summary>
        public RuntimeKind? RuntimeOverride { get; set; }

        /// <summary>
        /// EraElectron host mode override.
        /// "Auto", "Embedded", or "OfficialSidecar".
        /// Null = use global default.
        /// </summary>
        public string EraElectronHostMode { get; set; }

        /// <summary>
        /// Optional absolute path to the official EraElectron executable used by
        /// OfficialSidecar mode. Null uses the configured environment/default paths.
        /// </summary>
        public string EraElectronExecutablePath { get; set; }

        /// <summary>
        /// Emuera boot strategy override.
        /// Null = use global default (currently Auto→Safe).
        /// </summary>
        public string EmueraBootStrategy { get; set; }

        /// <summary>Network access policy: "Allow", "Ask", or "Block". Null = global default.</summary>
        public string NetworkPolicy { get; set; }

        /// <summary>Display font scale multiplier (1.0 = normal).</summary>
        public float FontScale { get; set; } = 1.0f;
    }
}
