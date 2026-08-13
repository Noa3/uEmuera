using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// Platform-aware factory and lifecycle coordinator for the EraElectron
    /// embedded WebView host.
    ///
    /// Determines which concrete <see cref="IEraElectronHost"/> to create based on
    /// <see cref="EraElectronHostMode"/> and the current Unity runtime platform:
    ///
    ///   Auto     → picks best available platform host
    ///   Embedded → requires PlatformWebViewHost (not yet built; falls back gracefully)
    ///   Sidecar  → launches official EraElectron executable (future)
    ///
    /// Current status: STUB.
    /// Only <see cref="NullEraElectronHost"/> is returned; it logs detailed info
    /// and throws <see cref="NotSupportedException"/> on StartAsync so the launcher
    /// shows an informative dialog instead of a silent black screen.
    ///
    /// To implement the real embedded host:
    ///   Windows:  wrap WebView2 (Microsoft.Web.WebView2) in PlatformWebViewHost
    ///   Android:  wrap android.webkit.WebView in an AndroidWebViewHost plugin
    ///   Linux:    wrap WebKitGTK or host-installed Chromium
    ///   All:      inject EraElectronBridgeScript.Build() before game JS loads
    ///
    /// See Docs/ADR/WEB_RUNTIME_HOST.md for the selection rationale.
    /// </summary>
    public static class PlatformWebViewBridge
    {
        /// <summary>
        /// Creates the best available <see cref="IEraElectronHost"/> for the current
        /// platform and the requested <paramref name="mode"/>.
        /// </summary>
        public static IEraElectronHost Create(EraElectronHostMode mode)
        {
            // Resolve Auto to the platform default.
            EraElectronHostMode resolved = mode == EraElectronHostMode.Auto
                ? ResolvePlatformDefault()
                : mode;

            Debug.Log($"[PlatformWebViewBridge] Creating host: mode={mode} resolved={resolved} platform={UnityEngine.Application.platform}");

            switch (resolved)
            {
                case EraElectronHostMode.Embedded:
                    // Return the real platform host where implemented.
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    Debug.Log("[PlatformWebViewBridge] Creating Windows WebView2Host.");
                    return new WebView2Host();
#else
                    Debug.LogWarning("[PlatformWebViewBridge] Embedded WebView host not yet implemented on this platform. Returning NullHost.");
                    return new NullEraElectronHost(EraElectronHostMode.Embedded,
                        "Embedded WebView not yet implemented on this platform. " +
                        "See Docs/ADR/WEB_RUNTIME_HOST.md.");
#endif

                case EraElectronHostMode.OfficialSidecar:
                    // TODO (Milestone 13): locate and launch official EraElectron sidecar.
                    Debug.LogWarning("[PlatformWebViewBridge] Official sidecar not yet implemented. Returning NullHost.");
                    return new NullEraElectronHost(EraElectronHostMode.OfficialSidecar,
                        "Official EraElectron sidecar not yet configured. " +
                        "Install the official EraElectron runtime and configure its path.");

                default:
                    return new NullEraElectronHost(resolved,
                        $"Unknown or unsupported EraElectronHostMode: {resolved}.");
            }
        }

        /// <summary>
        /// Reads the game's <c>.ere-min-version</c> file and returns the engine version
        /// string (e.g. "2200"), or an empty string if absent.
        /// </summary>
        public static string ReadEreMinVersion(GameDescriptor game)
        {
            if (game == null || string.IsNullOrEmpty(game.GameRoot)) return string.Empty;
            string path = Path.Combine(game.GameRoot, ".ere-min-version");
            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path, Encoding.UTF8).Trim();
            }
            catch { }
            return string.Empty;
        }

        // ------------------------------------------------------------------ //

        static EraElectronHostMode ResolvePlatformDefault()
        {
#if UNITY_ANDROID
            // Android WebView is the natural embedded choice.
            // Return Embedded (stub) — real AndroidWebViewHost to be added.
            return EraElectronHostMode.Embedded;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Windows: WebView2 is the embedded target.
            return EraElectronHostMode.Embedded;
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            // Linux: WebKitGTK or CEF.
            return EraElectronHostMode.Embedded;
#else
            // Unsupported platform — surface via NullHost.
            return EraElectronHostMode.Embedded;
#endif
        }
    }

    // ====================================================================== //
    //  NullEraElectronHost                                                    //
    // ====================================================================== //

    /// <summary>
    /// A no-op <see cref="IEraElectronHost"/> that records calls and surfaces a
    /// clear <see cref="NotSupportedException"/> on <see cref="LoadGameAsync"/> so
    /// that <c>EraElectronRuntime.StartAsync</c> can give the user an informative
    /// error instead of a silent black screen.
    ///
    /// Used while real platform hosts are pending implementation.
    /// </summary>
    public sealed class NullEraElectronHost : IEraElectronHost
    {
        readonly EraElectronHostMode _mode;
        readonly string              _reason;
        GameDescriptor               _game;

        public NullEraElectronHost(EraElectronHostMode mode, string reason)
        {
            _mode   = mode;
            _reason = reason ?? "WebView host not implemented.";
        }

        public EraElectronHostMode HostMode    => _mode;
        public HostCapabilities    Capabilities => _nullCaps;

        static readonly HostCapabilities _nullCaps = new HostCapabilities
        {
            ChromiumEngine   = false,
            WebWorkers       = false,
            Audio            = false,
            NativeFilePicker = false,
            ChromeVersion    = 0,
            Note             = "NullHost — no WebView available.",
            MissingCapabilities = new[] { "webview", "javascript", "webworkers", "audio" },
        };

        public Task InitializeAsync(
            GameDescriptor    game,
            IEraNativeBridge  bridge,
            CancellationToken cancellationToken = default)
        {
            _game = game;
            Debug.Log($"[NullEraElectronHost] InitializeAsync: game={game?.Title}, mode={_mode}");
            return Task.CompletedTask;
        }

        public Task LoadGameAsync(string gameOriginUrl, CancellationToken cancellationToken = default)
        {
            // Surface as NotSupportedException so EraElectronRuntime propagates
            // a user-readable message via LaunchEreGameCoroutine's error dialog.
            throw new NotSupportedException(
                $"EraElectron runtime not yet available.\n\n" +
                $"Game:   {_game?.Title ?? "?"}\n" +
                $"Engine: ≥ {_game?.RequiredRuntimeVersion ?? "?"}\n\n" +
                _reason + "\n\n" +
                "See Docs/ADR/WEB_RUNTIME_HOST.md for implementation status.");
        }

        public void Show()  => Debug.Log("[NullEraElectronHost] Show (no-op).");
        public void Hide()  => Debug.Log("[NullEraElectronHost] Hide (no-op).");

        public Task StopAsync()
        {
            Debug.Log("[NullEraElectronHost] StopAsync (no-op).");
            return Task.CompletedTask;
        }

        public Task<string> EvaluateJsAsync(string js)
        {
            Debug.LogWarning($"[NullEraElectronHost] EvaluateJsAsync called with no WebView: {js?.Substring(0, Math.Min(80, js?.Length ?? 0))}...");
            return Task.FromResult("null");
        }

        public void Dispose() { }
    }
}
