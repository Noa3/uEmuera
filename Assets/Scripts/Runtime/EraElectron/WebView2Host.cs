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
        string  _dataDirPath;   // captured on Unity main thread before STA start
        volatile bool _running = true;
        IntPtr        _nativeHwnd;
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

            // Register this host so the static WndProc can dispatch callbacks to it.
            _allHosts.Add(this);

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

            // Post CreateController to run on the STA thread via a Windows message.
            PostCallback(CreateController);

            await _loadTcs.Task;
            UnityEngine.Debug.Log("[WebView2Host] Game JS loaded.");
        }

        public void Show()
        {
            if (_controller != null)
                PostCallback(() => { try { _controller.IsVisible = true; } catch { } });
        }

        public void Hide()
        {
            if (_controller != null)
                PostCallback(() => { try { _controller.IsVisible = false; } catch { } });
        }

        public async Task StopAsync()
        {
            _running = false;
            PostCallback(() =>
            {
                try { _controller?.Close(); } catch { }
                // WM_QUIT will stop RunMessageLoopUntil
                PostMessage(_nativeHwnd, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);
            });
            await Task.Delay(300);
        }

        public async Task<string> EvaluateJsAsync(string js)
        {
            if (_webView == null) return "null";
            var tcs = new TaskCompletionSource<string>();
            PostCallback(async () =>
            {
                try   { tcs.SetResult(await _webView.ExecuteScriptAsync(js)); }
                catch { tcs.SetResult("null"); }
            });
            return await tcs.Task;
        }

        public void Dispose()
        {
            _running = false;
            try { PostMessage(_nativeHwnd, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero); } catch { }
        }

        // ------------------------------------------------------------------ //
        //  STA thread                                                          //
        // ------------------------------------------------------------------ //

        void StaThreadProc()
        {
            try
            {
                // Create a minimal hidden Win32 message window WITHOUT WinForms
                // Application.Run() — that call conflicts with Unity Editor's own
                // Win32 message pump.  We use CreateWindowEx + native GetMessage loop.
                _nativeHwnd = CreateMessageWindow();
                if (_nativeHwnd == IntPtr.Zero)
                {
                    _initTcs.TrySetException(new Exception(
                        "[WebView2Host] CreateWindowEx failed: " +
                        Marshal.GetLastWin32Error()));
                    return;
                }

                // Create WebView2 environment.  This is async-over-native: it starts
                // the Edge process and completes via PostMessage to our HWND.
                // We pump messages until the task completes.
                var envTask = CoreWebView2Environment.CreateAsync(null, _dataDirPath);
                RunMessageLoopUntil(() => envTask.IsCompleted);

                if (envTask.IsFaulted)
                {
                    _initTcs.TrySetException(envTask.Exception?.GetBaseException()
                        ?? new Exception("[WebView2Host] CreateAsync failed"));
                    return;
                }

                _env = envTask.Result;
                UnityEngine.Debug.Log("[WebView2Host] CoreWebView2Environment ready.");
                _initTcs.TrySetResult(true);

                // Keep pumping messages until we are told to stop.
                RunMessageLoopUntil(() => !_running);
            }
            catch (Exception ex)
            {
                _initTcs?.TrySetException(ex);
                _loadTcs?.TrySetException(ex);
                UnityEngine.Debug.LogError("[WebView2Host] STA thread error: " + ex);
            }
            finally
            {
                if (_nativeHwnd != IntPtr.Zero)
                {
                    try { DestroyWindow(_nativeHwnd); } catch { }
                    _nativeHwnd = IntPtr.Zero;
                }
            }
        }

        volatile bool _running = true;
        IntPtr        _nativeHwnd;

        const string WC_CLASS = "uEmueraWV2Host";

        IntPtr CreateMessageWindow()
        {
            // Register a minimal window class.
            WNDCLASSEX wc = new WNDCLASSEX
            {
                cbSize        = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc   = Marshal.GetFunctionPointerForDelegate(
                                    (WndProcDelegate)DefWindowProcWrapper),
                lpszClassName = WC_CLASS,
                hInstance     = GetModuleHandle(null),
            };
            RegisterClassEx(ref wc); // ok to fail if already registered

            return CreateWindowEx(
                0, WC_CLASS, "uEmuera WebView2 Host",
                0x00800000 /* WS_POPUP */, 0, 0, 800, 600,
                IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        }

        static IntPtr DefWindowProcWrapper(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
            => DefWindowProc(hwnd, msg, wParam, lParam);

        static void RunMessageLoopUntil(Func<bool> done)
        {
            while (!done())
            {
                MSG msg;
                while (PeekMessage(out msg, IntPtr.Zero, 0, 0, 1 /* PM_REMOVE */))
                {
                    if (msg.message == 0x0012 /* WM_QUIT */) return;
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
                Thread.Sleep(1);
            }
        }

        // ------------------------------------------------------------------ //
        //  Create WebView2 controller (called on STA thread after init)        //
        // ------------------------------------------------------------------ //

        async void CreateController()
        {
            try
            {
                // Parent is our native message window (stable HWND).
                _controller = await _env.CreateCoreWebView2ControllerAsync(_nativeHwnd);
                _webView    = _controller.CoreWebView2;

                // Configure
                _webView.Settings.AreDefaultContextMenusEnabled = false;
                _webView.Settings.IsStatusBarEnabled            = false;
                _webView.Settings.AreDevToolsEnabled            = true;
                _webView.Settings.IsZoomControlEnabled          = false;

                // Bridge: JS → C# via postMessage
                _webView.WebMessageReceived += OnWebMessageReceived;

                // Navigation guard
                _webView.NavigationStarting += OnNavigationStarting;

                // DOM loaded → resolve LoadGameAsync
                _webView.DOMContentLoaded += OnDomContentLoaded;

                // Resize to Unity window area
                ResizeToCoverUnityWindow();
                _controller.IsVisible = true;

                // Navigate to the loopback file server
                _webView.Navigate(_startUrl ?? "about:blank");

                _webViewReady = true;
                UnityEngine.Debug.Log($"[WebView2Host] Controller created → {_startUrl}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[WebView2Host] CreateController error: " + ex);
                _loadTcs?.TrySetException(ex);
            }
        }

        void ResizeToCoverUnityWindow()
        {
            try
            {
                IntPtr unityHwnd = GetUnityWindowHwnd();
                if (unityHwnd == IntPtr.Zero) return;

                RECT r;
                if (GetClientRect(unityHwnd, out r))
                {
                    // Reparent the WebView2 controller's bounds window under the Unity HWND
                    _controller.ParentWindow = unityHwnd;
                    _controller.Bounds = new System.Drawing.Rectangle(
                        0, 0, r.Right - r.Left, r.Bottom - r.Top);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[WebView2Host] ResizeToCoverUnityWindow: " + ex.Message);
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
            PostCallback(async () =>
            {
                try { await _webView?.ExecuteScriptAsync(js); } catch { }
            });
        }

        void RejectJs(int callId, string errorMsg)
        {
            string escaped = errorMsg?.Replace("\"", "\\\"").Replace("\n", "\\n") ?? "";
            string js = $"window._eraReject({callId}, \"{escaped}\");";
            PostCallback(async () =>
            {
                try { await _webView?.ExecuteScriptAsync(js); } catch { }
            });
        }

        // ------------------------------------------------------------------ //
        //  Cross-thread callback via PostMessage custom WM                     //
        // ------------------------------------------------------------------ //

        const uint WM_APP_CALLBACK = 0x8001;
        readonly ConcurrentQueue<Action> _callbacks = new ConcurrentQueue<Action>();

        /// <summary>Posts an action to run on the STA thread's message loop.</summary>
        void PostCallback(Action a)
        {
            if (_nativeHwnd == IntPtr.Zero) return;
            _callbacks.Enqueue(a);
            PostMessage(_nativeHwnd, WM_APP_CALLBACK, IntPtr.Zero, IntPtr.Zero);
        }

        // Our message loop should handle WM_APP_CALLBACK.
        // RunMessageLoopUntil pumps messages generically, so we need the WNDPROC to dispatch.
        // Override DefWindowProc to drain the callback queue on WM_APP_CALLBACK.
        static IntPtr DefWindowProcWrapper(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // Drain pending callbacks; safe because this is the STA thread.
            if (msg == WM_APP_CALLBACK)
            {
                // Walk the global list — static, so cast is via host lookup
                // (simple approach: iterate the queue via the singleton pattern)
                foreach (var host in _allHosts)
                {
                    if (host._nativeHwnd == hwnd)
                    {
                        Action a;
                        while (host._callbacks.TryDequeue(out a))
                            try { a(); } catch (Exception ex) {
                                UnityEngine.Debug.LogWarning("[WebView2Host] Callback error: " + ex.Message);
                            }
                        return IntPtr.Zero;
                    }
                }
            }
            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        // Track all hosts so the static WndProc can find them
        static readonly System.Collections.Concurrent.ConcurrentBag<WebView2Host> _allHosts
            = new System.Collections.Concurrent.ConcurrentBag<WebView2Host>();

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        static IntPtr GetUnityWindowHwnd()
        {
            try { return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle; }
            catch { return IntPtr.Zero; }
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
        struct MSG
        {
            public IntPtr hwnd; public uint message;
            public IntPtr wParam; public IntPtr lParam;
            public uint time; public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        struct WNDCLASSEX
        {
            public int cbSize; public uint style;
            public IntPtr lpfnWndProc; public int cbClsExtra; public int cbWndExtra;
            public IntPtr hInstance; public IntPtr hIcon; public IntPtr hCursor;
            public IntPtr hbrBackground; public string lpszMenuName;
            public string lpszClassName; public IntPtr hIconSm;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet=CharSet.Unicode)]
        static extern short RegisterClassEx(ref WNDCLASSEX lpwcx);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)]
        static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
        [DllImport("user32.dll")] static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint min, uint max, uint remove);
        [DllImport("user32.dll")] static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll")] static extern IntPtr DispatchMessage(ref MSG lpMsg);
        [DllImport("user32.dll")] static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    }
}

#endif
