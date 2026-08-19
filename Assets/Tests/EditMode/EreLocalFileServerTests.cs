using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using uEmuera.Runtime;
using uEmuera.Runtime.EraElectron;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="EreLocalFileServer"/>.
    /// Starts a real loopback server and makes HTTP requests against it.
    /// </summary>
    [TestFixture]
    public class EreLocalFileServerTests
    {
        string _tempDir;
        EreLocalFileServer _server;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_ServerTests_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDir);

            // Create test game structure
            Directory.CreateDirectory(Path.Combine(_tempDir, "ere"));
            File.WriteAllText(Path.Combine(_tempDir, "ere", "main.js"),
                "// game entry", Encoding.UTF8);
            File.WriteAllText(Path.Combine(_tempDir, "ere", "era-electron.js"),
                "// sdk stub", Encoding.UTF8);
            File.WriteAllText(Path.Combine(_tempDir, "era.bundle.js"),
                "// compiled sdk", Encoding.UTF8);
            File.WriteAllText(Path.Combine(_tempDir, "main.bundle.js"),
                "// compiled game", Encoding.UTF8);
            File.WriteAllText(Path.Combine(_tempDir, ".ere-min-version"), "2200");
            Directory.CreateDirectory(Path.Combine(_tempDir, "csv"));
            File.WriteAllText(Path.Combine(_tempDir, "csv", "GameBase.csv"),
                "ID,NAME\r\n0,TestGame", Encoding.UTF8);

            _server = new EreLocalFileServer(_tempDir, "/* bridge */");
            _server.Start();
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Dispose();
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        // ------------------------------------------------------------------ //
        //  Server lifecycle                                                     //
        // ------------------------------------------------------------------ //

        [Test]
        public void Start_PortIsPositive()
        {
            Assert.Greater(_server.Port, 0);
        }

        [Test]
        public void Start_IsRunning()
        {
            Assert.IsTrue(_server.IsRunning);
        }

        [Test]
        public void BaseUrl_ContainsLocalhostAndPort()
        {
            Assert.IsTrue(_server.BaseUrl.StartsWith("http://127.0.0.1:"));
            Assert.IsTrue(_server.BaseUrl.Contains(_server.Port.ToString()));
        }

        [Test]
        public void Token_IsNonEmpty()
        {
            Assert.IsNotEmpty(_server.Token);
            Assert.AreEqual(32, _server.Token.Length, "Guid N format is 32 hex chars");
        }

        [Test]
        public void Stop_SetsIsRunningFalse()
        {
            _server.Stop();
            Assert.IsFalse(_server.IsRunning);
        }

        // ------------------------------------------------------------------ //
        //  HTTP responses                                                       //
        // ------------------------------------------------------------------ //

        [Test]
        public void Get_BridgeJs_Returns200AndBridgeContent()
        {
            string url = $"{_server.BaseUrl}/bridge.js";
            string body = GetString(url);
            Assert.IsNotNull(body);
            Assert.IsTrue(body.Contains("/* bridge */"),
                "bridge.js must return the injected bootstrap JS");
        }

        [Test]
        public void Get_GameFile_Returns200()
        {
            string url = $"{_server.BaseUrl}/game/ere/main.js";
            string body = GetString(url);
            Assert.IsNotNull(body);
            Assert.IsTrue(body.Contains("// game entry"));
        }

        [Test]
        public void Get_GameCsv_Returns200()
        {
            string url = $"{_server.BaseUrl}/game/csv/GameBase.csv";
            string body = GetString(url);
            Assert.IsNotNull(body);
            Assert.IsTrue(body.Contains("TestGame"));
        }

        [Test]
        public void Get_MissingFile_Returns404()
        {
            string url = $"{_server.BaseUrl}/game/notexist.js";
            int code = GetStatusCode(url);
            Assert.AreEqual(404, code);
        }

        [Test]
        public void Get_WithoutToken_Returns403()
        {
            string url = $"http://127.0.0.1:{_server.Port}/game/ere/main.js";
            int code = GetStatusCode(url);
            Assert.AreEqual(403, code, "Request without session token must be rejected");
        }

        [Test]
        public void Get_WrongToken_Returns403()
        {
            string url = $"http://127.0.0.1:{_server.Port}/deadbeef/game/ere/main.js";
            int code = GetStatusCode(url);
            Assert.AreEqual(403, code, "Wrong token must be rejected");
        }

        [Test]
        public void Get_PathTraversal_Returns403()
        {
            // Attempt to escape game root via ../
            string url = $"{_server.BaseUrl}/game/../../../etc/passwd";
            int code = GetStatusCode(url);
            Assert.AreEqual(403, code, "Path traversal must be blocked");
        }

        [Test]
        public void Get_IndexHtml_Returns200WithHtml()
        {
            string url = $"{_server.BaseUrl}/index.html";
            string body = GetString(url);
            Assert.IsNotNull(body);
            Assert.IsTrue(body.Contains("<!DOCTYPE html") || body.Contains("<!doctype html"),
                "index.html must return HTML loader page");
            Assert.IsTrue(body.Contains("bridge.js"),
                "Loader page must inject bridge.js");
        }

        [Test]
        public void Get_IndexHtml_LoaderRefersToGameEntry()
        {
            string url = $"{_server.BaseUrl}/index.html";
            string body = GetString(url);
            Assert.IsTrue(body.Contains("main.bundle.js"),
                "Loader must point to the compiled main bundle.");
        }

        [Test]
        public void Get_IndexHtml_LoadsSdkBeforeBridgeBeforeGameAndSignalsReady()
        {
            string body = GetString($"{_server.BaseUrl}/index.html");
            int sdk = body.IndexOf("era.bundle.js", StringComparison.Ordinal);
            int bridge = body.IndexOf("bridge.js", StringComparison.Ordinal);
            int game = body.IndexOf("main.bundle.js", StringComparison.Ordinal);
            Assert.That(sdk, Is.GreaterThanOrEqualTo(0));
            Assert.That(bridge, Is.GreaterThan(sdk));
            Assert.That(game, Is.GreaterThan(bridge));
            Assert.IsTrue(body.Contains("uemuera-ready"));
            Assert.IsTrue(body.Contains("run(window.game)"),
                "Loader must invoke the compiled game entry point.");
        }

        [Test]
        public void Get_IndexHtml_SourceOnlyPackageLoadsCommonJsEntry()
        {
            File.Delete(Path.Combine(_tempDir, "era.bundle.js"));
            File.Delete(Path.Combine(_tempDir, "main.bundle.js"));

            string body = GetString($"{_server.BaseUrl}/index.html");

            Assert.IsTrue(body.Contains("game/ere/main.js"));
            Assert.IsTrue(body.Contains("window.require"));
            Assert.IsTrue(body.Contains("#/era-electron"));
            Assert.IsTrue(body.Contains("run(window.module.exports)"));
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        static string GetString(string url)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 3000;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new System.IO.StreamReader(resp.GetResponseStream()))
                    return sr.ReadToEnd();
            }
            catch (WebException ex) when (ex.Response != null)
            {
                return null; // non-200 status
            }
        }

        static int GetStatusCode(string url)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 3000;
                using (var resp = (HttpWebResponse)req.GetResponse())
                    return (int)resp.StatusCode;
            }
            catch (WebException ex) when (ex.Response != null)
            {
                return (int)((HttpWebResponse)ex.Response).StatusCode;
            }
        }
    }

    [TestFixture]
    public class EraElectronRuntimeTests
    {
        string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_RuntimeTests_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, ".ere-min-version"), "4.8.0");
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Test]
        public async Task StartAsync_Success_TransitionsToRunningAndShowsHost()
        {
            var host = new RecordingHost();
            using (var runtime = new EraElectronRuntime(_ => host))
            {
                await runtime.InitializeAsync(BuildDescriptor(), new RuntimeContext());
                await runtime.StartAsync();

                Assert.AreEqual(RuntimeState.Running, runtime.State);
                Assert.IsTrue(host.InitializeCalled);
                Assert.IsTrue(host.LoadCalled);
                Assert.IsTrue(host.ShowCalled);
                Assert.IsNotEmpty(host.OriginUrl);
            }
        }

        [Test]
        public void StartAsync_LoadFailure_FaultsAndCleansUpHostAndServer()
        {
            var host = new RecordingHost { LoadException = new InvalidOperationException("load failed") };
            using (var runtime = new EraElectronRuntime(_ => host))
            {
                runtime.InitializeAsync(BuildDescriptor(), new RuntimeContext()).GetAwaiter().GetResult();

                Assert.Throws<InvalidOperationException>(
                    () => runtime.StartAsync().GetAwaiter().GetResult());
                Assert.AreEqual(RuntimeState.Faulted, runtime.State);
                Assert.IsTrue(host.StopCalled, "A partially started host must be stopped on launch failure.");
                Assert.IsTrue(host.DisposeCalled, "A partially started host must be disposed on launch failure.");
                Assert.IsNull(runtime.GetDiagnostics().SavePath,
                    "The loopback server must be released after launch failure.");
            }
        }

        [Test]
        public void InitializeAsync_MissingGameDirectory_Faults()
        {
            var missing = BuildDescriptor();
            missing.GameRoot = Path.Combine(_tempDir, "missing");
            using (var runtime = new EraElectronRuntime(_ => new RecordingHost()))
            {
                Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
                    await runtime.InitializeAsync(missing, new RuntimeContext()));
                Assert.AreEqual(RuntimeState.Faulted, runtime.State);
            }
        }

        [Test]
        public async Task Dispatcher_AsyncCallCompletesWithoutMainThreadDispatcher()
        {
            using (var model = EreDataModel.Create(BuildDescriptor()))
            {
                var dispatcher = new EreApiDispatcher(model, new RuntimeContext());
                int callId = dispatcher.BeginAsync("delay", "[0]");
                Assert.AreEqual("null", await dispatcher.AwaitAsync(callId));
            }
        }

        [Test]
        public void Dispatcher_ParsesAndSerializesJsonArguments()
        {
            using (var model = EreDataModel.Create(BuildDescriptor()))
            {
                var dispatcher = new EreApiDispatcher(model, new RuntimeContext());
                Assert.AreEqual("42", dispatcher.DispatchSync("set", "[\"flag:5\",42]"));
                Assert.AreEqual("42", dispatcher.DispatchSync("get", "[\"flag:5\"]"));
                Assert.AreEqual("\"A,B\"",
                    dispatcher.DispatchSync("set", "[\"callname:1:0\",\"A,B\"]"));
            }
        }

        GameDescriptor BuildDescriptor() => new GameDescriptor
        {
            GameId = "ere-test",
            Title = "ERE Test",
            Version = "1.0",
            RuntimeKind = RuntimeKind.EraElectron,
            GameRoot = _tempDir,
            RequiredRuntimeVersion = "4.8.0",
        };

        sealed class RecordingHost : IEraElectronHost
        {
            public EraElectronHostMode HostMode => EraElectronHostMode.Embedded;
            public HostCapabilities Capabilities { get; } = new HostCapabilities
            {
                ChromiumEngine = true,
                WebWorkers = true,
                Audio = true,
            };

            public bool InitializeCalled { get; private set; }
            public bool LoadCalled { get; private set; }
            public bool ShowCalled { get; private set; }
            public bool StopCalled { get; private set; }
            public bool DisposeCalled { get; private set; }
            public string OriginUrl { get; private set; }
            public Exception LoadException { get; set; }

            public Task InitializeAsync(
                GameDescriptor game,
                IEraNativeBridge bridge,
                CancellationToken cancellationToken = default)
            {
                InitializeCalled = true;
                return Task.CompletedTask;
            }

            public Task LoadGameAsync(
                string gameOriginUrl,
                CancellationToken cancellationToken = default)
            {
                LoadCalled = true;
                OriginUrl = gameOriginUrl;
                if (LoadException != null)
                    throw LoadException;
                return Task.CompletedTask;
            }

            public void Show() => ShowCalled = true;
            public void Hide() { }

            public Task StopAsync()
            {
                StopCalled = true;
                return Task.CompletedTask;
            }

            public Task<string> EvaluateJsAsync(string js) => Task.FromResult("null");

            public void Dispose() => DisposeCalled = true;
        }
    }
}
