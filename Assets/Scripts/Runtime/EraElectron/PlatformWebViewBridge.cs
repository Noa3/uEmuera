using System;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
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
            EraElectronHostMode resolved = mode == EraElectronHostMode.Auto
                ? ResolvePlatformDefault()
                : mode;

            Debug.Log($"[PlatformWebViewBridge] Creating host: mode={mode} resolved={resolved} platform={UnityEngine.Application.platform}");

            switch (resolved)
            {
                case EraElectronHostMode.Embedded:
#if UNITY_EDITOR_WIN
                    Debug.LogError(
                        "[PlatformWebViewBridge] In-process WebView2 is disabled in Unity Editor " +
                        "because native initialization can terminate the Editor. Use OfficialSidecar " +
                        "or a Windows standalone player.");
                    return new NullEraElectronHost(EraElectronHostMode.Embedded,
                        "Embedded WebView2 is disabled in Unity Editor. " +
                        "Use OfficialSidecar or a Windows standalone player.");
#elif UNITY_STANDALONE_WIN
                    Debug.Log("[PlatformWebViewBridge] Creating Windows WebView2Host.");
                    return new WebView2Host();
#else
                    Debug.LogWarning("[PlatformWebViewBridge] Embedded WebView host not yet implemented on this platform. Returning NullHost.");
                    return new NullEraElectronHost(EraElectronHostMode.Embedded,
                        "Embedded WebView not yet implemented on this platform. " +
                        "See Docs/ADR/WEB_RUNTIME_HOST.md.");
#endif

                case EraElectronHostMode.OfficialSidecar:
                    return new OfficialSidecarHost();

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
#if UNITY_EDITOR_WIN
            return EraElectronHostMode.OfficialSidecar;
#elif UNITY_ANDROID
            return EraElectronHostMode.Embedded;
#elif UNITY_STANDALONE_WIN
            return EraElectronHostMode.Embedded;
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            return EraElectronHostMode.Embedded;
#else
            return EraElectronHostMode.Embedded;
#endif
        }
    }

    // ====================================================================== //
    //  OfficialSidecarHost                                                    //
    // ====================================================================== //

    /// <summary>
    /// Launches the official EraElectron executable in an isolated working
    /// directory whose <c>game</c> entry points at the detected game package.
    /// This is the compatibility path for source-form ERE packages: the official
    /// engine understands the CommonJS/bundle loading model and owns its renderer.
    /// </summary>
    public sealed class OfficialSidecarHost : IEraElectronHost
    {
        GameDescriptor _game;
        Process _process;
        string _sessionDirectory;

        static readonly HostCapabilities SidecarCapabilities = new HostCapabilities
        {
            ChromiumEngine = true,
            WebWorkers = true,
            Audio = true,
            NativeFilePicker = true,
            ChromeVersion = 120,
            Note = "Official EraElectron sidecar",
        };

        public EraElectronHostMode HostMode => EraElectronHostMode.OfficialSidecar;
        public HostCapabilities Capabilities => SidecarCapabilities;

        public static bool IsAvailable(GameDescriptor game)
        {
            return !string.IsNullOrEmpty(ResolveExecutablePath(game));
        }

        public Task InitializeAsync(
            GameDescriptor game,
            IEraNativeBridge bridge,
            CancellationToken cancellationToken = default)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            cancellationToken.ThrowIfCancellationRequested();

            string executable = ResolveExecutablePath(game);
            if (string.IsNullOrEmpty(executable))
                throw new NotSupportedException(
                    "Official EraElectron sidecar was not found. Configure " +
                    "GameSettings.EraElectronExecutablePath or " +
                    "UEMUERA_ERA_ELECTRON_EXE.");

            _sessionDirectory = Path.Combine(
                UnityEngine.Application.temporaryCachePath,
                "EraElectronSessions",
                (game.GameId ?? Guid.NewGuid().ToString("N")) + "_" +
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_sessionDirectory);
            CreateGameLink(Path.Combine(_sessionDirectory, "game"), game.GameRoot);

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = _sessionDirectory,
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            _process = Process.Start(startInfo);
            if (_process == null)
                throw new InvalidOperationException(
                    "Failed to start the EraElectron sidecar process.");
            return Task.CompletedTask;
        }

        public async Task LoadGameAsync(
            string gameOriginUrl,
            CancellationToken cancellationToken = default)
        {
            if (_process == null)
                throw new InvalidOperationException("Sidecar is not initialized.");

            const int timeoutMs = 15000;
            int elapsed = 0;
            while (!_process.HasExited && _process.MainWindowHandle == IntPtr.Zero && elapsed < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
                elapsed += 100;
                _process.Refresh();
            }

            if (_process.HasExited)
                throw new InvalidOperationException(
                    $"EraElectron sidecar exited with code {_process.ExitCode}.");
        }

        public void Show() => SetWindowVisibility(true);
        public void Hide() => SetWindowVisibility(false);

        public async Task StopAsync()
        {
            Process process = _process;
            _process = null;
            if (process != null)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(3000))
                            process.Kill();
                    }
                }
                catch { }
                finally
                {
                    process.Dispose();
                }
            }

            RemoveSessionDirectory();
            await Task.CompletedTask;
        }

        public Task<string> EvaluateJsAsync(string js)
        {
            throw new NotSupportedException(
                "JavaScript evaluation is owned by the official EraElectron process.");
        }

        public void Dispose()
        {
            try { StopAsync().GetAwaiter().GetResult(); }
            catch { RemoveSessionDirectory(); }
        }

        static string ResolveExecutablePath(GameDescriptor game)
        {
            var candidates = new System.Collections.Generic.List<string>();
            string configured = game?.UserSettings?.EraElectronExecutablePath;
            if (!string.IsNullOrEmpty(configured)) candidates.Add(configured);

            string environment = Environment.GetEnvironmentVariable("UEMUERA_ERA_ELECTRON_EXE");
            if (!string.IsNullOrEmpty(environment)) candidates.Add(environment);

            string dataPath = UnityEngine.Application.dataPath;
            candidates.Add(Path.Combine(UnityEngine.Application.streamingAssetsPath,
                "EraElectron", "ere.exe"));
            candidates.Add(Path.Combine(dataPath, "..", "EraElectron", "ere.exe"));
            candidates.Add(Path.Combine(dataPath, "..", "EraElectron", "era-electron.exe"));
            candidates.Add(Path.Combine(UnityEngine.Application.persistentDataPath,
                "EraElectron", "ere.exe"));

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate)) continue;
                string full = Path.GetFullPath(candidate);
                if (File.Exists(full)) return full;
            }
            return null;
        }

        static void CreateGameLink(string linkPath, string targetPath)
        {
            string target = Path.GetFullPath(targetPath);
            if (!Directory.Exists(target))
                throw new DirectoryNotFoundException("Game root not found: " + target);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/D /S /C mklink /J \"{linkPath}\" \"{target}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
#else
            var info = new ProcessStartInfo
            {
                FileName = "ln",
                Arguments = $"-s \"{target}\" \"{linkPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
#endif
            using (Process process = Process.Start(info))
            {
                if (process == null || !process.WaitForExit(5000) || process.ExitCode != 0)
                    throw new IOException("Could not create the sidecar game directory link.");
            }
        }

        void SetWindowVisibility(bool visible)
        {
            if (_process == null) return;
            try
            {
                _process.Refresh();
                IntPtr hwnd = _process.MainWindowHandle;
                if (hwnd != IntPtr.Zero)
                    ShowWindow(hwnd, visible ? 5 : 0);
            }
            catch { }
        }

        void RemoveSessionDirectory()
        {
            string directory = _sessionDirectory;
            _sessionDirectory = null;
            if (string.IsNullOrEmpty(directory)) return;
            try
            {
                string link = Path.Combine(directory, "game");
                if (Directory.Exists(link)) Directory.Delete(link);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
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
