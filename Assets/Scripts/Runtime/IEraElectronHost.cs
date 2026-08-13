using System;
using System.Threading;
using System.Threading.Tasks;

namespace uEmuera.Runtime
{
    /// <summary>
    /// Platform-specific web-runtime host.
    /// Each platform (Windows WebView2 / Android WebView / Linux / Sidecar)
    /// provides one implementation.
    ///
    /// An <see cref="EraElectronRuntime"/> owns exactly one host instance
    /// per game session.
    /// </summary>
    public interface IEraElectronHost : IDisposable
    {
        /// <summary>Which hosting strategy this instance uses.</summary>
        EraElectronHostMode HostMode { get; }

        /// <summary>
        /// Capability report produced during initialization.
        /// The runtime uses this to decide whether to fall back to sidecar.
        /// </summary>
        HostCapabilities Capabilities { get; }

        /// <summary>
        /// Prepare the host: create the WebView/process context, register the
        /// native bridge, configure the secure game-file origin.
        /// Does NOT load game bundles yet.
        /// </summary>
        Task InitializeAsync(
            GameDescriptor game,
            IEraNativeBridge bridge,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inject the era.* bridge and load the game bundles.
        /// <paramref name="gameOriginUrl"/> is the base URL of the
        /// <see cref="EreLocalFileServer"/> for this session
        /// (e.g. "http://127.0.0.1:PORT/TOKEN").
        /// Navigates to gameOriginUrl/index.html which loads bridge.js then game JS.
        /// Returns when the game's entry point has started executing.
        /// </summary>
        Task LoadGameAsync(string gameOriginUrl, CancellationToken cancellationToken = default);

        /// <summary>Show the web surface (make it visible to the player).</summary>
        void Show();

        /// <summary>Hide the web surface (return to launcher UI).</summary>
        void Hide();

        /// <summary>
        /// Stop the game JS (close WebView or terminate sidecar process).
        /// Called as part of <see cref="EraElectronRuntime.StopAsync"/>.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Evaluate arbitrary JavaScript in the game context (for debugging /
        /// bridge-side Promise resolution). Returns the JS result as a string.
        /// </summary>
        Task<string> EvaluateJsAsync(string js);
    }

    // ------------------------------------------------------------------ //
    //  Supporting types                                                    //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Capability snapshot from a host implementation.
    /// Produced during <see cref="IEraElectronHost.InitializeAsync"/>.
    /// </summary>
    public sealed class HostCapabilities
    {
        /// <summary>True if Chromium-level JS is available.</summary>
        public bool ChromiumEngine { get; set; }

        /// <summary>True if Web Workers are available in this context.</summary>
        public bool WebWorkers { get; set; }

        /// <summary>True if hardware audio playback is available.</summary>
        public bool Audio { get; set; }

        /// <summary>True if native file-picker / import is available.</summary>
        public bool NativeFilePicker { get; set; }

        /// <summary>Approximate Chrome-equivalent version, or 0 if unknown.</summary>
        public int ChromeVersion { get; set; }

        /// <summary>Human-readable note from the host about limitations.</summary>
        public string Note { get; set; }

        /// <summary>Missing capability IDs; used by Auto mode for sidecar fallback.</summary>
        public string[] MissingCapabilities { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// C# side of the ERA SDK bridge.
    /// The host calls these methods when game JS invokes an era.* API.
    /// All implementations live in EreApiDispatcher (created by EraElectronRuntime).
    /// </summary>
    public interface IEraNativeBridge
    {
        /// <summary>
        /// Dispatch a synchronous era.* call from JS.
        /// Returns a JSON-serialized result string, or null for void.
        /// Throws on error (host translates to JS exception).
        /// </summary>
        string DispatchSync(string method, string argsJson);

        /// <summary>
        /// Begin an async era.* call from JS.
        /// The host holds a JS Promise; uEmuera calls ResolveAsync when done.
        /// Returns an integer call-ID used to route the resolution.
        /// </summary>
        int BeginAsync(string method, string argsJson);

        /// <summary>
        /// Called by the host to retrieve the JSON result for a completed async call.
        /// The host resolves the pending JS Promise with this value.
        /// </summary>
        Task<string> AwaitAsync(int callId, CancellationToken cancellationToken = default);
    }
}
