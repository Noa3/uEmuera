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
        SyncBridgeHostObject       _syncBridge;

        Thread  _staThread;
        string  _dataDirPath;   // captured on Unity main thread before STA start
        volatile bool _running;
        IntPtr        _nativeHwnd;
        TaskCompletionSource<bool> _initTcs;
        TaskCompletionSource<bool> _loadTcs;

        readonly ConcurrentQueue<Action> _callbacks = new ConcurrentQueue<Action>();
        static readonly ConcurrentDictionary<IntPtr, WebView2Host> HostsByWindow =
            new ConcurrentDictionary<IntPtr, WebView2Host>();
        static readonly WndProcDelegate WindowProcedure = WindowProc;

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
            if (UnityEngine.Application.isEditor)
                throw new NotSupportedException(
                    "In-process WebView2 is disabled in Unity Editor because native " +
                    "initialization can terminate the Editor. Use OfficialSidecar or " +
                    "a Windows standalone player.");

            _game      = game ?? throw new ArgumentNullException(nameof(game));
            _bridge    = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _initTcs   = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            cancellationToken.ThrowIfCancellationRequested();

            _dataDirPath = System.IO.Path.Combine(
                UnityEngine.Application.temporaryCachePath, "WebView2Data");

            _running = true;
            _staThread = new Thread(StaThreadProc) { IsBackground = true, Name = "WebView2STA" };
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.Start();

            using (cancellationToken.Register(() => _initTcs.TrySetCanceled(cancellationToken)))
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
            if (string.IsNullOrWhiteSpace(gameOriginUrl))
                throw new ArgumentException("Game origin URL is required.", nameof(gameOriginUrl));

            cancellationToken.ThrowIfCancellationRequested();
            _startUrl = gameOriginUrl.TrimEnd('/') + "/index.html";
            _loadTcs  = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (!PostCallback(CreateController))
                throw new InvalidOperationException(
                    "[WebView2Host] STA message window is not available.");

            using (cancellationToken.Register(() => _loadTcs.TrySetCanceled(cancellationToken)))
                await _loadTcs.Task;
            UnityEngine.Debug.Log("[WebView2Host] Game JS loaded.");
        }

        public void Show()
        {
            if (_controller != null)
                PostCallback(() =>
                {
                    try
                    {
                        ShowWindow(_nativeHwnd, 5);
                        _controller.IsVisible = true;
                    }
                    catch { }
                });
        }

        public void Hide()
        {
            if (_controller != null)
                PostCallback(() =>
                {
                    try
                    {
                        _controller.IsVisible = false;
                        ShowWindow(_nativeHwnd, 0);
                    }
                    catch { }
                });
        }

        public async Task StopAsync()
        {
            if (!_running && (_staThread == null || !_staThread.IsAlive))
                return;

            var stopTcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            if (!PostCallback(() =>
            {
                try
                {
                    if (_webView != null)
                    {
                        _webView.WebMessageReceived -= OnWebMessageReceived;
                        _webView.NavigationStarting -= OnNavigationStarting;
                        _webView.DOMContentLoaded -= OnDomContentLoaded;
                        try { _webView.RemoveHostObjectFromScript("eraNative"); } catch { }
                    }
                    _syncBridge = null;
                    _controller?.Close();
                }
                finally
                {
                    _controller = null;
                    _webView = null;
                    _running = false;
                    stopTcs.TrySetResult(true);
                }
            }))
            {
                _running = false;
                stopTcs.TrySetResult(true);
            }

            Task completed = await Task.WhenAny(stopTcs.Task, Task.Delay(2000));
            if (completed != stopTcs.Task)
                UnityEngine.Debug.LogWarning("[WebView2Host] Timed out while stopping the STA host.");

            if (_staThread != null && _staThread.IsAlive)
                _staThread.Join(2000);
            _staThread = null;
        }

        public async Task<string> EvaluateJsAsync(string js)
        {
            if (_webView == null || !_running) return "null";
            var tcs = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            if (!PostCallback(() =>
            {
                try
                {
                    _ = _webView.ExecuteScriptAsync(js).ContinueWith(task =>
                    {
                        try { tcs.TrySetResult(task.GetAwaiter().GetResult()); }
                        catch { tcs.TrySetResult("null"); }
                    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                catch { tcs.TrySetResult("null"); }
            }))
                return "null";
            return await tcs.Task;
        }

        public void Dispose()
        {
            try { StopAsync().GetAwaiter().GetResult(); }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[WebView2Host] Dispose failed: " + ex.Message);
                _running = false;
            }
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
                HostsByWindow[_nativeHwnd] = this;

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
                    HostsByWindow.TryRemove(_nativeHwnd, out _);
                    try { DestroyWindow(_nativeHwnd); } catch { }
                    _nativeHwnd = IntPtr.Zero;
                }
                _running = false;
            }
        }

        const string WC_CLASS = "uEmueraWV2Host";

        IntPtr CreateMessageWindow()
        {
            WNDCLASSEX wc = new WNDCLASSEX
            {
                cbSize        = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc   = Marshal.GetFunctionPointerForDelegate(WindowProcedure),
                lpszClassName = WC_CLASS,
                hInstance     = GetModuleHandle(null),
            };
            RegisterClassEx(ref wc);

            return CreateWindowEx(
                0, WC_CLASS, "uEmuera EraElectron",
                0x00CF0000, 100, 100, 1280, 720,
                IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        }

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

        void CreateController()
        {
            try
            {
                var controllerTask = _env.CreateCoreWebView2ControllerAsync(_nativeHwnd);
                RunMessageLoopUntil(() => controllerTask.IsCompleted);
                _controller = controllerTask.GetAwaiter().GetResult();
                _webView    = _controller.CoreWebView2;

                _syncBridge = new SyncBridgeHostObject(_bridge);
                _webView.AddHostObjectToScript("eraNative", _syncBridge);

                _webView.Settings.AreDefaultContextMenusEnabled = false;
                _webView.Settings.IsStatusBarEnabled            = false;
                _webView.Settings.AreDevToolsEnabled            = true;
                _webView.Settings.IsZoomControlEnabled          = false;

                _webView.WebMessageReceived += OnWebMessageReceived;
                _webView.NavigationStarting += OnNavigationStarting;
                _webView.DOMContentLoaded += OnDomContentLoaded;

                ResizeToCoverUnityWindow();
                _controller.IsVisible = true;
                ShowWindow(_nativeHwnd, 5);

                _webView.Navigate(_startUrl ?? "about:blank");

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
                if (_controller == null || _nativeHwnd == IntPtr.Zero) return;

                RECT r;
                if (GetClientRect(_nativeHwnd, out r))
                {
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
            UnityEngine.Debug.Log("[WebView2Host] Loader DOM ready; waiting for game entry signal.");
        }

        void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (_startUrl == null) return;
            // Allow only our game origin
            Uri origin = new Uri(_startUrl);
            Uri nav    = new Uri(e.Uri);
            if (!string.Equals(nav.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(nav.Host, origin.Host, StringComparison.OrdinalIgnoreCase) ||
                nav.Port != origin.Port)
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
            if (string.IsNullOrWhiteSpace(json)) return;

            BridgeMessage message;
            try
            {
                message = JsonUtility.FromJson<BridgeMessage>(json);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[WebView2Host] Invalid bridge message: " + ex.Message);
                return;
            }

            if (message == null) return;
            if (string.Equals(message.type, "uemuera-ready", StringComparison.Ordinal))
            {
                _loadTcs?.TrySetResult(true);
                return;
            }
            if (string.Equals(message.type, "uemuera-load-error", StringComparison.Ordinal))
            {
                _loadTcs?.TrySetException(new InvalidOperationException(
                    message.error ?? "EraElectron game entry failed to load."));
                return;
            }
            if (_bridge == null || !message.isAsync || string.IsNullOrEmpty(message.method))
                return;

            try
            {
                int asyncCallId = _bridge.BeginAsync(message.method, message.args ?? "[]");
                _ = ResolveAsyncAsync(message.id, asyncCallId);
            }
            catch (Exception ex)
            {
                RejectJs(message.id, ex.Message);
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
            string js = $"window._eraResolve({callId}, {ToJsString(resultJson ?? "null")});";
            PostCallback(() =>
            {
                try { _ = _webView?.ExecuteScriptAsync(js); } catch { }
            });
        }

        void RejectJs(int callId, string errorMsg)
        {
            string js = $"window._eraReject({callId}, {ToJsString(errorMsg ?? "ERA call failed")});";
            PostCallback(() =>
            {
                try { _ = _webView?.ExecuteScriptAsync(js); } catch { }
            });
        }

        // ------------------------------------------------------------------ //
        //  Cross-thread callback via PostMessage custom WM                     //
        // ------------------------------------------------------------------ //

        const uint WM_APP_CALLBACK = 0x8001;

        /// <summary>Posts an action to run on the STA thread's message loop.</summary>
        bool PostCallback(Action action)
        {
            if (action == null || !_running || _nativeHwnd == IntPtr.Zero)
                return false;
            _callbacks.Enqueue(action);
            if (PostMessage(_nativeHwnd, WM_APP_CALLBACK, IntPtr.Zero, IntPtr.Zero))
                return true;

            _callbacks.TryDequeue(out _);
            return false;
        }

        static IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (HostsByWindow.TryGetValue(hwnd, out var host))
            {
                if (msg == WM_APP_CALLBACK)
                {
                    while (host._callbacks.TryDequeue(out var action))
                    {
                        try { action(); }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning(
                                "[WebView2Host] Callback error: " + ex.Message);
                        }
                    }
                    return IntPtr.Zero;
                }

                if (msg == 0x0005)
                {
                    host.ResizeToCoverUnityWindow();
                    return IntPtr.Zero;
                }
            }
            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.AutoDual)]
        public sealed class SyncBridgeHostObject
        {
            readonly IEraNativeBridge _nativeBridge;

            public SyncBridgeHostObject(IEraNativeBridge nativeBridge)
            {
                _nativeBridge = nativeBridge
                    ?? throw new ArgumentNullException(nameof(nativeBridge));
            }

            public string DispatchSync(string method, string argsJson)
            {
                return _nativeBridge.DispatchSync(method, argsJson);
            }
        }

        #pragma warning disable 0649
        [Serializable]
        sealed class BridgeMessage
        {
            public string type;
            public int id;
            public string method;
            public string args;
            public bool isAsync;
            public string error;
        }
        #pragma warning restore 0649

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        static IntPtr GetUnityWindowHwnd()
        {
            try { return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle; }
            catch { return IntPtr.Zero; }
        }

        static string ToJsString(string value)
        {
            if (value == null) return "\"\"";
            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\u2028", "\\u2028")
                .Replace("\u2029", "\\u2029") + "\"";
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

        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
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
