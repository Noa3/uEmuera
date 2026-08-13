using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
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
            // Game has ere/main.js — loader should reference it
            Assert.IsTrue(body.Contains("ere/main.js"),
                "Loader must point to ere/main.js when source layout detected");
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
}
