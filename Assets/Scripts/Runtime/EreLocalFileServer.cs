using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// Minimal loopback HTTP file server that serves EraElectron game files.
    ///
    /// - Binds to 127.0.0.1:{random_port} on startup.
    /// - Serves static files from the game's root directory.
    /// - Injects EraElectronBridgeScript.Build() into index.html responses.
    /// - Blocks navigation to any non-loopback origin.
    ///
    /// Thread safety: single-threaded via SynchronizationContext; safe to call
    /// Start/Stop from any thread.
    /// </summary>
    public sealed class EreLocalFileServer : IDisposable
    {
        private HttpListener _listener;
        private Task _serveTask;
        private readonly string _gameRoot;
        private readonly string _bootstrapJs;
        private readonly object _lock = new object();
        private bool _disposed;
        private bool _running;

        /// <summary>
        /// The base URL (e.g. "http://127.0.0.1:54321") where the server is listening.
        /// Only valid after Start() succeeds.
        /// </summary>
        public string BaseUrl { get; private set; }

        public EreLocalFileServer(string gameRoot, string bootstrapJs)
        {
            _gameRoot = gameRoot ?? throw new ArgumentNullException(nameof(gameRoot));
            _bootstrapJs = bootstrapJs ?? throw new ArgumentNullException(nameof(bootstrapJs));

            if (!Directory.Exists(_gameRoot))
                throw new DirectoryNotFoundException($"Game root not found: {_gameRoot}");
        }

        /// <summary>
        /// Starts the loopback server. Returns the BaseUrl.
        /// Must be called once before any requests.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(EreLocalFileServer));
                if (_running)
                    return; // already started

                // Find a free port on loopback.
                int port = FindFreePort();
                BaseUrl = $"http://127.0.0.1:{port}/";

                _listener = new HttpListener();
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();

                _running = true;
                _serveTask = Task.Run(() => ServeLoop());
            }
        }

        /// <summary>
        /// Stops the server and waits for the serve loop to exit.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_running) return;
                _running = false;

                try { _listener?.Stop(); } catch { }
                try { _listener?.Close(); } catch { }
            }

            try { _serveTask?.Wait(1000); } catch { }
        }

        private async Task ServeLoop()
        {
            while (_running)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Listener stopped — normal exit.
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EreLocalFileServer] ServeLoop error: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Block non-loopback origins (security).
                string origin = request.Headers["Origin"];
                if (!string.IsNullOrEmpty(origin))
                {
                    try
                    {
                        var uri = new Uri(origin);
                        if (uri.Host != "127.0.0.1" && uri.Host != "localhost")
                        {
                            response.StatusCode = 403;
                            response.Close();
                            return;
                        }
                    }
                    catch { }
                }

                // Map URL to file path.
                string path = request.Url.AbsolutePath.TrimStart('/');
                if (string.IsNullOrEmpty(path))
                    path = "index.html";

                string fullPath = Path.Combine(_gameRoot, path);

                // Security: prevent path traversal.
                string fullDir = Directory.GetParent(fullPath)?.FullName;
                if (fullDir == null || !fullDir.StartsWith(_gameRoot, StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 403;
                    response.Close();
                    return;
                }

                if (!File.Exists(fullPath))
                {
                    // Try index.html fallback for SPA routes.
                    string indexPath = Path.Combine(_gameRoot, "index.html");
                    if (path != "index.html" && File.Exists(indexPath))
                        fullPath = indexPath;
                    else
                    {
                        response.StatusCode = 404;
                        response.Close();
                        return;
                    }
                }

                // Read file content.
                byte[] content;
                string ext = Path.GetExtension(fullPath).ToLowerInvariant();
                string contentType = GetContentType(ext);

                if (ext == ".html" || ext == ".htm")
                {
                    // Inject bootstrap JS into index.html.
                    string html = File.ReadAllText(fullPath, Encoding.UTF8);
                    if (Path.GetFileName(fullPath).Equals("index.html", StringComparison.OrdinalIgnoreCase))
                    {
                        // Inject before </head> or at start of <body>.
                        if (html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                            html = html.Replace("</head>", _bootstrapJs + "\n</head>", StringComparison.OrdinalIgnoreCase);
                        else if (html.Contains("<body", StringComparison.OrdinalIgnoreCase))
                            html = html.Replace("<body", _bootstrapJs + "\n<body", StringComparison.OrdinalIgnoreCase);
                        else
                            html = _bootstrapJs + "\n" + html;
                    }
                    content = Encoding.UTF8.GetBytes(html);
                }
                else
                {
                    content = File.ReadAllBytes(fullPath);
                }

                response.ContentType = contentType;
                response.ContentLength64 = content.Length;
                response.OutputStream.Write(content, 0, content.Length);
                response.OutputStream.Flush();
                response.Close();
            }
            catch (Exception ex)
            {
                try { context.Response.StatusCode = 500; context.Response.Close(); } catch { }
                UnityEngine.Debug.LogError($"[EreLocalFileServer] Request error: {ex.Message}");
            }
        }

        private static string GetContentType(string ext)
        {
            return ext switch
            {
                ".html" or ".htm" => "text/html; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                ".mp3" => "audio/mpeg",
                ".ogg" => "audio/ogg",
                ".wav" => "audio/wav",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                _ => "application/octet-stream",
            };
        }

        private static int FindFreePort()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:0/");
            listener.Start();
            int port = ((System.Net.IPEndPoint)listener.LocalUrls[0].Port).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _running = false;
            }

            Stop();
        }
    }
}
