using NUnit.Framework;
using uEmuera.Runtime.EraElectron;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="EraElectronBridgeScript"/>.
    /// Validates the generated JavaScript bootstrap without requiring a WebView.
    /// </summary>
    [TestFixture]
    public class EraElectronBridgeScriptTests
    {
        string _js;

        [SetUp]
        public void SetUp()
        {
            _js = EraElectronBridgeScript.Build("2200");
        }

        // ------------------------------------------------------------------ //
        //  Structure                                                            //
        // ------------------------------------------------------------------ //

        [Test]
        public void Build_ReturnsNonEmptyString()
        {
            Assert.IsNotNull(_js);
            Assert.Greater(_js.Length, 100, "Bootstrap should be non-trivial");
        }

        [Test]
        public void Build_ContainsIIFEWrapper()
        {
            Assert.IsTrue(_js.Contains("(function()"), "Must be wrapped in IIFE for scope safety");
            Assert.IsTrue(_js.Contains("})();"), "IIFE must close");
        }

        [Test]
        public void Build_DefinesEraBridgeStub()
        {
            Assert.IsTrue(_js.Contains("window._eraBridge"), "_eraBridge must be set on window");
            Assert.IsTrue(_js.Contains(".sync"), "_eraBridge must have sync method");
            Assert.IsTrue(_js.Contains(".beginAsync"), "_eraBridge must have beginAsync method");
            Assert.IsTrue(_js.Contains(".awaitAsync"), "_eraBridge must have awaitAsync method");
        }

        [Test]
        public void Build_DefinesEraObject()
        {
            Assert.IsTrue(_js.Contains("window.era="), "era object must be on window");
        }

        [Test]
        public void Build_ContainsVersionObject()
        {
            Assert.IsTrue(_js.Contains("version"), "era.version must exist");
            Assert.IsTrue(_js.Contains("\"2200\""), "engine version '2200' must appear in output");
            Assert.IsTrue(_js.Contains("engine:"), "era.version.engine key must be present");
            Assert.IsTrue(_js.Contains("sdk:"), "era.version.sdk key must be present");
        }

        [Test]
        public void Build_ContainsLoggerSubObject()
        {
            Assert.IsTrue(_js.Contains("logger:"), "era.logger sub-object must exist");
            Assert.IsTrue(_js.Contains("\"debug\"") || _js.Contains("debug:"),
                "era.logger.debug must exist");
            Assert.IsTrue(_js.Contains("\"error\"") || _js.Contains("error:"),
                "era.logger.error must exist");
        }

        // ------------------------------------------------------------------ //
        //  API coverage — top EraUma APIs must be wired                        //
        // ------------------------------------------------------------------ //

        [Test]
        public void Build_WiresPrintAndWait()
        {
            Assert.IsTrue(_js.Contains("printAndWait"),
                "printAndWait (21 359 calls in EraUma) must be in bootstrap");
        }

        [Test]
        public void Build_WiresGet()
        {
            Assert.IsTrue(_js.Contains("'get'") || _js.Contains("\"get\""),
                "era.get must be wired");
        }

        [Test]
        public void Build_WiresInput()
        {
            Assert.IsTrue(_js.Contains("input"),
                "era.input must be wired as async");
        }

        [Test]
        public void Build_WiresClear()
        {
            Assert.IsTrue(_js.Contains("clear"),
                "era.clear must be wired as async");
        }

        [Test]
        public void Build_WiresWaitAnyKey()
        {
            Assert.IsTrue(_js.Contains("waitAnyKey"),
                "era.waitAnyKey must be wired");
        }

        [Test]
        public void Build_WiresPlayMusic()
        {
            Assert.IsTrue(_js.Contains("playMusic"),
                "era.playMusic must be wired");
        }

        [Test]
        public void Build_WiresSaveData()
        {
            Assert.IsTrue(_js.Contains("saveData"),
                "era.saveData must be wired as async");
        }

        [Test]
        public void Build_WiresLoadData()
        {
            Assert.IsTrue(_js.Contains("loadData"),
                "era.loadData must be wired as async");
        }

        // ------------------------------------------------------------------ //
        //  Edge cases                                                           //
        // ------------------------------------------------------------------ //

        [Test]
        public void Build_NullEngineVersion_DoesNotCrash()
        {
            string js = EraElectronBridgeScript.Build(null);
            Assert.IsNotNull(js);
            Assert.IsTrue(js.Contains("window.era="));
        }

        [Test]
        public void Build_EmptyEngineVersion_StillValid()
        {
            string js = EraElectronBridgeScript.Build(string.Empty);
            Assert.IsNotNull(js);
            Assert.IsTrue(js.Contains("window._eraBridge"));
        }

        [Test]
        public void Build_EngineVersionWithSpecialChars_EscapedCorrectly()
        {
            // Engine versions should be safe strings, but test escaping anyway
            string js = EraElectronBridgeScript.Build("test\"version\\path");
            Assert.IsFalse(js.Contains("test\"version"),
                "Unescaped double-quote must not appear in output");
        }

        [Test]
        public void Build_WindowEraAliasedToWindowUnderscore()
        {
            // Games import era via #/era-electron which webpack resolves to window._era
            Assert.IsTrue(_js.Contains("window._era"), "window._era alias must exist for SDK compatibility");
        }

        [Test]
        public void Build_UEmueraSentinel_Set()
        {
            Assert.IsTrue(_js.Contains("_uEmuera:true"),
                "_uEmuera sentinel lets game JS detect uEmuera host if needed");
        }
    }
}
