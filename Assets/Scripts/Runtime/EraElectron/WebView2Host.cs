// Windows-only: WebView2 embedded host.
// Requires Microsoft.Web.WebView2.Core.dll and WebView2Loader.dll in Assets/Plugins/WebView2/.
// The WebView2 runtime ships with Windows 10/11; also installable via
// https://developer.microsoft.com/microsoft-edge/webview2/
//
// Threading model:
//   A dedicated background STA thread runs a WinForms application loop.
//   WebView2 must be created and driven from that thread.
//   Unity's main thread communicates via Invoke(delegate).
//
// Bridge protocol (JS → C#):
//   JS:  window.chrome.webview.postMessage(JSON.stringify({id, method, args}))
//   C#:  WebMessageReceived → dispatch to IEraNativeBridge → ExecuteScriptAsync resolve
//
// Security:
//   - Serves game files from EreLocalFileServer (loopback only).
//   - No file:// access; CSP applied to game origin.
//   - Navigating away from the game origin is blocked.

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using UnityEngine;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// Windows embedded WebView2 host.
    /// Runs the browser in a dedicated STA thread alongside the Unity main thread.
    /// </summary>
    public sealed class WebView2Host : IEraElectronHost
    {
        // ------------------------------------------------------------------ //
        //  IEraElectronHost                                                    //
        // ------------------------------------------------------------------ //

        public EraElectronHostMode HostMode    => EraElectronHostMode.Embedded;
        public HostCapabilities    Capabilities => _caps ?? _defaultCaps;

        // ------------------------------------------------------------------ //
        //  State                                                               //
        // ------------------------------------------------------------------ //

        HostCapabilities  _caps;
        IEraNativeBridge  _bridge;
        GameDescriptor    _game;
        string            _startUrl;

        CoreWebView2Environment    _env;
        CoreWebView2Controller     _controller;
        CoreWebView2               _webView;

        Thread  _staThread;
        string  _dataDirPath;          // captured on Unity main thread before STA start
        System.Windows.Forms.Form _hostForm;
        volatile bool _webViewReady;
        TaskCompletionSource<bool> _initTcs;
        TaskCompletionSource<bool> _loadTcs;

        // Pending JS-to-C# calls (from WV2 thread → resolved on same thread)
        readonly ConcurrentQueue<(int id, string method, string argsJson, bool isAsync)> _pendingCalls
            = new ConcurrentQueue<(int, string, string, bool)>();

        static readonly HostCapabilities _defaultCaps = new HostCapabilities
        {
            ChromiumEngine   = true,
            WebWorkers       = true,
            Audio            = true,
            NativeFilePicker = false,
            ChromeVersion    = 120,
            Note             = "Windows WebView2 (Chromium edge)",
        };

        // ------------------------------------------------------------------ //
        //  IEraElectronHost: Init                                              //
        // ------------------------------------------------------------------ //

        public async Task InitializeAsync(
            GameDescriptor    game,
            IEraNativeBridge  bridge,
            CancellationToken cancellationToken = default)
        {
            _game      = game;
            _bridge    = bridge;
            _initTcs   = new TaskCompletionSource<bool>();

            cancellationToken.ThrowIfCancellationRequested();

            // Capture Unity main-thread-only path BEFORE starting the STA thread.
            _dataDirPath = System.IO.Path.Combine(
                UnityEngine.Application.temporaryCachePath, "WebView2Data");

            // Start STA thread that owns WebView2
            _staThread = new Thread(StaThreadProc) { IsBackground = true, Name = "WebView2STA" };
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.Start();

            // Wait for the WV2 environment to be ready (or fail)
            await _initTcs.Task;

            _caps = new HostCapabilities
            {
                ChromiumEngine   = true,
                WebWorkers       = true,
                Audio            = true,
                NativeFilePicker = false,
                ChromeVersion    = 120,
                Note             = $"WebView2 env ready, gameRoot={game.GameRoot}",
            };

            UnityEngine.Debug.Log("[WebView2Host] Initialized.");
        }

        // ------------------------------------------------------------------ //
        //  IEraElectronHost: Load game                                         //
        // ------------------------------------------------------------------ //

        public async Task LoadGameAsync(string gameOriginUrl, CancellationToken cancellationToken = default)
        {
            if (_env == null)
                throw new InvalidOperationException("[WebView2Host] Not initialized.");

            _startUrl = gameOriginUrl + "/index.html";
            _loadTcs  = new TaskCompletionSource<bool>();

            // Create controller on the STA thread (requires parent HWND)
            _hostForm.Invoke(new Action(CreateController));

            await _loadTcs.Task;
            UnityEngine.Debug.Log("[WebView2Host] Game JS loaded.");
        }

        public void Show()
        {
            _hostForm?.Invoke(new Action(() =>
            {
                if (_controller != null)
                    _controller.IsVisible = true;
            }));
        }

        public void Hide()
        {
            _hostForm?.Invoke(new Action(() =>
            {
                if (_controller != null)
                    _controller.IsVisible = false;
            }));
        }

        public async Task StopAsync()
        {
            _hostForm?.Invoke(new Action(() =>
            {
                try { _controller?.Close(); }   catch { }
                try { _hostForm?.Close(); }     catch { }
            }));
            await Task.Delay(200);
        }

        public async Task<string> EvaluateJsAsync(string js)
        {
            if (_webView == null) return "null";
            var tcs = new TaskCompletionSource<string>();
            _hostForm.Invoke(new Action(async () =>
            {
                try   { tcs.SetResult(await _webView.ExecuteScriptAsync(js)); }
                catch { tcs.SetResult("null"); }
            }));
            return await tcs.Task;
        }

        public void Dispose()
        {
            try { _hostForm?.Invoke(new Action(() => _hostForm?.Close())); } catch { }
        }

        // ------------------------------------------------------------------ //
        //  STA thread                                                          //
        // ------------------------------------------------------------------ //

        void StaThreadProc()
        {
            // WinForms message loop required for WebView2
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            _hostForm = new System.Windows.Forms.Form
            {
                Text        = "uEmuera WebView2",
                ShowInTaskbar = false,
                WindowState = System.Windows.Forms.FormWindowState.Normal,
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None,
                BackColor   = System.Drawing.Color.Black,
            };

            _hostForm.Load += async (s, e) =>
            {
                try
                {
                    System.IO.Directory.CreateDirectory(_dataDirPath);
                    _env = await CoreWebView2Environment.CreateAsync(null, _dataDirPath);
                    _initTcs.TrySetResult(true);
                    UnityEngine.Debug.Log("[WebView2Host] CoreWebView2Environment ready.");
                }
                catch (Exception ex)
                {
                    _initTcs.TrySetException(ex);
                    _hostForm.Close();
                }
            };

            System.Windows.Forms.Application.Run(_hostForm);
        }

        // ------------------------------------------------------------------ //
        //  Create WebView2 controller (called on STA thread after init)        //
        // ------------------------------------------------------------------ //

        async void CreateController()
        {
            try
            {
                // Get Unity main window HWND
                IntPtr parentHwnd = GetUnityWindowHwnd();

                if (parentHwnd == IntPtr.Zero)
                {
                    // Fallback: use the host form as parent
                    _hostForm.Show();
                    _hostForm.BringToFront();
                    parentHwnd = _hostForm.Handle;
                }
                else
                {
                    // Position our form over the Unity window
                    PositionOverUnityWindow();
                    _hostForm.Show();
                }

                _controller = await _env.CreateCoreWebView2ControllerAsync(
                    _hostForm.Handle);

                _webView = _controller.CoreWebView2;

                // Configure
                _webView.Settings.AreDefaultContextMenusEnabled = false;
                _webView.Settings.IsStatusBarEnabled = false;
                _webView.Settings.AreDevToolsEnabled = true; // useful for development
                _webView.Settings.IsZoomControlEnabled = false;

                // Bridge: JS → C# via postMessage
                _webView.WebMessageReceived += OnWebMessageReceived;

                // Navigation guard: only allow the game origin
                _webView.NavigationStarting += OnNavigationStarting;

                // DOM content loaded: inject bridge
                _webView.DOMContentLoaded += OnDomContentLoaded;

                // Set bounds to fill the host form
                _controller.Bounds = new System.Drawing.Rectangle(
                    0, 0, _hostForm.ClientSize.Width, _hostForm.ClientSize.Height);
                _controller.IsVisible = true;

                // Navigate to the file server
                _webView.Navigate(_startUrl ?? "about:blank");

                _webViewReady = true;
                UnityEngine.Debug.Log($"[WebView2Host] Controller created, navigating to {_startUrl}");
            }
            catch (Exception ex)
            {
                _loadTcs.TrySetException(ex);
            }
        }

        // ------------------------------------------------------------------ //
        //  Events                                                              //
        // ------------------------------------------------------------------ //

        void OnDomContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            // Navigation completed — game JS is running
            _loadTcs?.TrySetResult(true);
        }

        void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (_startUrl == null) return;
            // Allow only our game origin
            Uri origin = new Uri(_startUrl);
            Uri nav    = new Uri(e.Uri);
            if (nav.Host != origin.Host || nav.Port != origin.Port)
            {
                e.Cancel = true;
                UnityEngine.Debug.LogWarning($"[WebView2Host] Blocked navigation to external URL: {e.Uri}");
            }
        }

        void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Expected format: {"id": N, "method": "...", "args": "...", "async": bool}
            string msg = e.TryGetWebMessageAsString();
            ProcessBridgeMessage(msg);
        }

        void ProcessBridgeMessage(string json)
        {
            if (string.IsNullOrEmpty(json) || _bridge == null) return;

            // Minimal JSON extraction (replace with proper parser when available)
            int    callId   = ExtractInt(json, "\"id\"");
            string method   = ExtractString(json, "\"method\"");
            string argsJson = ExtractString(json, "\"args\"");
            bool   isAsync  = json.Contains("\"async\":true") || json.Contains("\"async\": true");

            if (string.IsNullOrEmpty(method)) return;

            if (!isAsync)
            {
                try
                {
                    string result = _bridge.DispatchSync(method, argsJson);
                    ResolveJs(callId, result);
                }
                catch (Exception ex)
                {
                    RejectJs(callId, ex.Message);
                }
            }
            else
            {
                int asyncCallId = _bridge.BeginAsync(method, argsJson);
                _ = ResolveAsyncAsync(callId, asyncCallId);
            }
        }

        async System.Threading.Tasks.Task ResolveAsyncAsync(int jsCallId, int csharpCallId)
        {
            try
            {
                string result = await _bridge.AwaitAsync(csharpCallId);
                ResolveJs(jsCallId, result);
            }
            catch (Exception ex)
            {
                RejectJs(jsCallId, ex.Message);
            }
        }

        void ResolveJs(int callId, string resultJson)
        {
            string js = $"window._eraResolve({callId}, {resultJson ?? "null"});";
            _webView?.PostWebMessageAsString(js); // use ExecuteScript for reliability
            _hostForm?.BeginInvoke(new Action(async () =>
            {
                try { await _webView?.ExecuteScriptAsync(js); } catch { }
            }));
        }

        void RejectJs(int callId, string errorMsg)
        {
            string escaped = errorMsg?.Replace("\"", "\\\"").Replace("\n", "\\n") ?? "";
            string js = $"window._eraReject({callId}, \"{escaped}\");";
            _hostForm?.BeginInvoke(new Action(async () =>
            {
                try { await _webView?.ExecuteScriptAsync(js); } catch { }
            }));
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        void PositionOverUnityWindow()
        {
            IntPtr unityHwnd = GetUnityWindowHwnd();
            if (unityHwnd == IntPtr.Zero) return;

            RECT rect;
            if (GetClientRect(unityHwnd, out rect))
            {
                POINT topLeft = new POINT { X = 0, Y = 0 };
                ClientToScreen(unityHwnd, ref topLeft);
                _hostForm.Location = new System.Drawing.Point(topLeft.X, topLeft.Y);
                _hostForm.Size = new System.Drawing.Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
        }

        static IntPtr GetUnityWindowHwnd()
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            return proc.MainWindowHandle;
        }

        // Very simple JSON field extractors (no dependency on proper JSON parser)
        static int ExtractInt(string json, string key)
        {
            int ki = json.IndexOf(key, StringComparison.Ordinal);
            if (ki < 0) return -1;
            int ci = json.IndexOf(':', ki + key.Length);
            if (ci < 0) return -1;
            int s = ci + 1;
            while (s < json.Length && (json[s] == ' ' || json[s] == '\t')) s++;
            int e = s;
            while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '-')) e++;
            int v; int.TryParse(json.Substring(s, e - s), out v);
            return v;
        }

        static string ExtractString(string json, string key)
        {
            int ki = json.IndexOf(key, StringComparison.Ordinal);
            if (ki < 0) return null;
            int ci = json.IndexOf(':', ki + key.Length);
            if (ci < 0) return null;
            int q1 = json.IndexOf('"', ci + 1);
            if (q1 < 0) return null;
            int q2 = q1 + 1;
            while (q2 < json.Length && json[q2] != '"') {
                if (json[q2] == '\\') q2++;
                q2++;
            }
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        // ------------------------------------------------------------------ //
        //  P/Invoke                                                            //
        // ------------------------------------------------------------------ //

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
    }
}

#endif
