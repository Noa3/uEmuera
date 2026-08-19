using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace uEmuera.Runtime.EraElectron
{
    /// <summary>
    /// Lightweight loopback HTTP server that serves EraElectron game files
    /// to an embedded WebView over a private origin.
    ///
    /// Why a loopback server?
    ///   WebView security models (WebView2, Android WebView) restrict what
    ///   pages loaded from file:// can do — no Cross-Origin XHR, restricted
    ///   Service Workers, broken Web Workers in some configurations.
    ///   Serving from http://127.0.0.1:PORT/ gives a proper origin so the
    ///   game JS behaves exactly like in the reference browser.
    ///
    /// Security:
    ///   - Binds to 127.0.0.1 only (loopback; not accessible over LAN).
    ///   - Every request must include a session token in the path prefix.
    ///     Token is randomly generated per session and unknown to external code.
    ///   - Only files inside <see cref="GameRoot"/> are served.
    ///   - Path traversal is blocked (canonicalise and check prefix).
    ///   - Directory listing is disabled.
    ///   - Runs on a background thread; stops when Disposed.
    ///
    /// URL scheme:
    ///   http://127.0.0.1:{Port}/{Token}/game/{relative-path}
    ///   http://127.0.0.1:{Port}/{Token}/bridge.js   ← era.* bootstrap
    ///
    /// Intended use:
    ///   1. Create and Start() before opening the WebView.
    ///   2. Pass BaseUrl to the WebView as the page origin.
    ///   3. The WebView navigates to BaseUrl/game/main.js etc.
    ///   4. Stop()/Dispose() when the game session ends.
    /// </summary>
    public sealed class EreLocalFileServer : IDisposable
    {
        // ------------------------------------------------------------------ //
        //  Public surface                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Random loopback port the server is listening on.</summary>
        public int Port { get; private set; }

        /// <summary>Per-session token; must appear in every URL path.</summary>
        public string Token { get; private set; }

        /// <summary>Root game directory (only files inside this path are served).</summary>
        public string GameRoot { get; private set; }

        /// <summary>
        /// The base URL for this game session.
        /// WebView should navigate to <c>BaseUrl/game/ere/main.js</c> etc.
        /// </summary>
        public string BaseUrl => $"http://127.0.0.1:{Port}/{Token}";

        /// <summary>True while the server is running.</summary>
        public bool IsRunning => _running;

        // ------------------------------------------------------------------ //
        //  Private                                                             //
        // ------------------------------------------------------------------ //

        HttpListener _listener;
        Thread       _thread;
        volatile bool _running;
        string       _bridgeJs;

        static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            { ".js",   "text/javascript; charset=utf-8"  },
            { ".mjs",  "text/javascript; charset=utf-8"  },
            { ".json", "application/json; charset=utf-8" },
            { ".html", "text/html; charset=utf-8"        },
            { ".htm",  "text/html; charset=utf-8"        },
            { ".css",  "text/css; charset=utf-8"         },
            { ".png",  "image/png"                       },
            { ".jpg",  "image/jpeg"                      },
            { ".jpeg", "image/jpeg"                      },
            { ".gif",  "image/gif"                       },
            { ".webp", "image/webp"                      },
            { ".svg",  "image/svg+xml"                   },
            { ".mp3",  "audio/mpeg"                      },
            { ".ogg",  "audio/ogg"                       },
            { ".wav",  "audio/wav"                       },
            { ".csv",  "text/plain; charset=utf-8"       },
            { ".txt",  "text/plain; charset=utf-8"       },
        };

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                            //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a file server rooted at <paramref name="gameRoot"/>.
        /// Call <see cref="Start"/> to begin listening.
        /// </summary>
        public EreLocalFileServer(string gameRoot, string bridgeJs)
        {
            if (string.IsNullOrEmpty(gameRoot))
                throw new ArgumentNullException(nameof(gameRoot));
            string fullRoot = Path.GetFullPath(gameRoot);
            GameRoot = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (GameRoot.Length == 0)
                GameRoot = Path.DirectorySeparatorChar.ToString();
            else if (GameRoot.EndsWith(":", StringComparison.Ordinal))
                GameRoot += Path.DirectorySeparatorChar;
            _bridgeJs = bridgeJs ?? string.Empty;
            Token     = Guid.NewGuid().ToString("N");
        }

        /// <summary>Starts the HTTP listener on a random available loopback port.</summary>
        public void Start()
        {
            if (_running) return;

            Port      = FindFreePort();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _running = true;

            _thread = new Thread(ServeLoop) { IsBackground = true, Name = "EreFileServer" };
            _thread.Start();

            Debug.Log($"[EreLocalFileServer] Listening on {BaseUrl}  gameRoot={GameRoot}");
        }

        /// <summary>Stops the server gracefully.</summary>
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            try { _listener?.Stop(); }  catch { }
            try { _listener?.Close(); } catch { }
            _listener = null;
            Debug.Log("[EreLocalFileServer] Stopped.");
        }

        public void Dispose() => Stop();

        // ------------------------------------------------------------------ //
        //  Request loop                                                         //
        // ------------------------------------------------------------------ //

        void ServeLoop()
        {
            while (_running)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = _listener.GetContext();
                }
                catch
                {
                    break; // listener was stopped
                }

                // Handle on the same thread (requests are rare; game files are small-ish).
                // For high-throughput use, switch to a thread-pool dispatch.
                try   { HandleRequest(ctx); }
                catch (Exception ex) { Debug.LogWarning("[EreLocalFileServer] Request error: " + ex.Message); }
            }
        }

        void HandleRequest(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;

            try
            {
                string rawPath = req.Url?.AbsolutePath ?? "/";

                // Validate token prefix: /{Token}/...
                string tokenPrefix = "/" + Token;
                if (!rawPath.StartsWith(tokenPrefix, StringComparison.Ordinal))
                {
                    Respond(res, 403, "text/plain", "Forbidden");
                    return;
                }

                string subPath = rawPath.Substring(tokenPrefix.Length);
                if (subPath.StartsWith("/")) subPath = subPath.Substring(1);

                // Special: serve the era.* bridge JS
                if (string.Equals(subPath, "bridge.js", StringComparison.OrdinalIgnoreCase))
                {
                    Respond(res, 200, "text/javascript; charset=utf-8", _bridgeJs);
                    return;
                }

                // Special: root page — tiny loader that injects bridge then loads main
                if (subPath == "" || subPath == "index.html")
                {
                    string loader = BuildLoaderHtml();
                    Respond(res, 200, "text/html; charset=utf-8", loader);
                    return;
                }

                // Game files must start with game/
                if (!subPath.StartsWith("game/", StringComparison.OrdinalIgnoreCase))
                {
                    Respond(res, 404, "text/plain", "Not found");
                    return;
                }

                string relPath = subPath.Substring("game/".Length);

                // Canonicalise and block path traversal
                string fullPath = Path.GetFullPath(Path.Combine(GameRoot, relPath));
                string rootPrefix = GameRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                    ? GameRoot
                    : GameRoot + Path.DirectorySeparatorChar;
                if (!string.Equals(fullPath, GameRoot, StringComparison.OrdinalIgnoreCase) &&
                    !fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    Respond(res, 403, "text/plain", "Forbidden");
                    return;
                }

                if (!File.Exists(fullPath))
                {
                    Respond(res, 404, "text/plain", "Not found: " + relPath);
                    return;
                }

                string ext  = Path.GetExtension(fullPath);
                string mime = MimeTypes.TryGetValue(ext, out string m) ? m : "application/octet-stream";

                // Add CORS header so game JS (same-origin) can access all subresources
                res.Headers.Add("Access-Control-Allow-Origin", "*");
                res.Headers.Add("Cache-Control", "no-cache");

                byte[] data = File.ReadAllBytes(fullPath);
                res.StatusCode        = 200;
                res.ContentType       = mime;
                res.ContentLength64   = data.Length;
                res.OutputStream.Write(data, 0, data.Length);
                res.OutputStream.Close();
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Builds a minimal HTML loader page that:
        ///   1. Injects bridge.js (era.* surface)
        ///   2. Loads the game's main entry point
        ///
        /// The entry-point path is derived from GameRoot structure:
        ///   ere/main.js (source layout) → /game/ere/main.js
        ///   main.js     (bundle)         → /game/main.js
        /// </summary>
        string BuildLoaderHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head>");
            sb.Append("<meta charset=\"utf-8\">");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            sb.Append("<title>uEmuera EraElectron</title>");
            sb.Append("</head><body><div id=\"uemuera-load-error\"></div><script>\n");
            sb.Append("function post(v){if(window.chrome&&window.chrome.webview)window.chrome.webview.postMessage(JSON.stringify(v));}\n");
            sb.Append("function fail(e){var m=(e&&e.message)||String(e);document.getElementById('uemuera-load-error').textContent=m;post({type:'uemuera-load-error',error:m});}\n");
            sb.Append("function ready(){post({type:'uemuera-ready'});}\n");
            sb.Append("function run(entry){if(entry&&typeof entry.default==='function')entry=entry.default;if(typeof entry!=='function'){fail(new Error('ERE entry point did not export a function'));return;}Promise.resolve().then(function(){return entry();}).then(ready,fail);}\n");
            sb.Append("function load(src,next){var s=document.createElement('script');s.src=src;s.onload=next;s.onerror=function(){fail(new Error('Failed to load '+src));};document.head.appendChild(s);}\n");
            sb.Append("window.addEventListener('unhandledrejection',function(e){fail(e.reason);});\n");

            string eraUrl, mainUrl;
            if (!TryResolveBundleUrls(out eraUrl, out mainUrl))
            {
                string sourcePath = File.Exists(Path.Combine(GameRoot, "ere", "main.js"))
                    ? "ere/main.js"
                    : File.Exists(Path.Combine(GameRoot, "main.js")) ? "main.js" : null;
                if (sourcePath == null)
                {
                    sb.Append("fail(new Error('ERE package has no main entry point.'));\n");
                }
                else
                {
                    string sourceUrl = $"{BaseUrl}/game/{sourcePath}";
                    sb.Append($"load('{BaseUrl}/bridge.js',function(){{");
                    sb.Append("window.module={exports:{}};window.exports=window.module.exports;");
                    sb.Append("window.require=function(id){if(id==='#/era-electron')return window.era;throw new Error('Source module requires a compiled bundle: '+id);};");
                    sb.Append($"load('{sourceUrl}',function(){{run(window.module.exports);}});");
                    sb.Append("});\n");
                }
            }
            else
            {
                string staticUrl = ResolveOptionalBundleUrl("static.bundle.js");
                sb.Append($"load('{eraUrl}',function(){{load('{BaseUrl}/bridge.js',function(){{");
                if (!string.IsNullOrEmpty(staticUrl))
                    sb.Append($"load('{staticUrl}',function(){{");
                sb.Append($"load('{mainUrl}',function(){{run(window.game);}});");
                if (!string.IsNullOrEmpty(staticUrl)) sb.Append("});");
                sb.Append("});});\n");
            }
            sb.Append("</script></body></html>");
            return sb.ToString();
        }

        bool TryResolveBundleUrls(out string eraUrl, out string mainUrl)
        {
            string[][] candidates =
            {
                new[] { "era.bundle.js", "main.bundle.js" },
                new[] { "dist/era.bundle.js", "dist/main.bundle.js" },
            };
            foreach (var pair in candidates)
            {
                if (File.Exists(Path.Combine(GameRoot, pair[0])) &&
                    File.Exists(Path.Combine(GameRoot, pair[1])))
                {
                    eraUrl = $"{BaseUrl}/game/{pair[0]}";
                    mainUrl = $"{BaseUrl}/game/{pair[1]}";
                    return true;
                }
            }
            eraUrl = null;
            mainUrl = null;
            return false;
        }

        string ResolveOptionalBundleUrl(string fileName)
        {
            if (File.Exists(Path.Combine(GameRoot, fileName)))
                return $"{BaseUrl}/game/{fileName}";
            if (File.Exists(Path.Combine(GameRoot, "dist", fileName)))
                return $"{BaseUrl}/game/dist/{fileName}";
            return null;
        }

        static void Respond(HttpListenerResponse res, int code, string contentType, string body)
        {
            byte[] data = Encoding.UTF8.GetBytes(body);
            res.StatusCode      = code;
            res.ContentType     = contentType;
            res.ContentLength64 = data.Length;
            try
            {
                res.OutputStream.Write(data, 0, data.Length);
                res.OutputStream.Close();
            }
            catch { }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        static int FindFreePort()
        {
            // Let OS pick; extract the assigned port.
            var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
