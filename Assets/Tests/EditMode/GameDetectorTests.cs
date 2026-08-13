using System.IO;
using NUnit.Framework;
using uEmuera.Runtime;
using uEmuera.Runtime.Detection;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Integration tests for <see cref="GameDetector"/>,
    /// <see cref="EraElectronGameDetector"/>, and <see cref="EmueraGameDetector"/>.
    ///
    /// Creates temporary directory fixtures that mirror real game layouts.
    /// Real EraUma layout verified against EraUma 3.0.00 (erauma-master, Aug 2026).
    /// </summary>
    [TestFixture]
    public class GameDetectorTests
    {
        string _tempRoot;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_DetectorTests_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempRoot, recursive: true); }
            catch { /* best-effort */ }
        }

        // ------------------------------------------------------------------ //
        //  Helper: fixture builders                                            //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates the minimal verified EraUma source layout:
        ///   .ere-min-version   — "2200"
        ///   ere/era-electron.js — SDK source
        ///   ere/main.js         — game entry
        /// </summary>
        string MakeEraUmaSourceLayout(string name = "erauma-test")
        {
            string dir = Path.Combine(_tempRoot, name);
            string ereDir = Path.Combine(dir, "ere");
            Directory.CreateDirectory(ereDir);
            File.WriteAllText(Path.Combine(dir,    ".ere-min-version"),   "2200");
            File.WriteAllText(Path.Combine(ereDir, "era-electron.js"),    "// SDK stub");
            File.WriteAllText(Path.Combine(ereDir, "main.js"),            "// game stub");
            File.WriteAllText(Path.Combine(dir,    "package.json"),
                "{ \"name\": \"erauma\", \"version\": \"3.0.00\" }");
            return dir;
        }

        /// <summary>
        /// Only .ere-min-version, no ere/ tree — might be a minimal distribution.
        /// </summary>
        string MakeMinVersionOnlyLayout(string name = "ere-minimal")
        {
            string dir = Path.Combine(_tempRoot, name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, ".ere-min-version"), "2000");
            return dir;
        }

        /// <summary>
        /// Only ere/era-electron.js (SDK source) and ere/main.js — no .ere-min-version.
        /// Represents a manually assembled or incomplete package.
        /// </summary>
        string MakeEreSourceNoBadgeLayout(string name = "ere-no-badge")
        {
            string dir = Path.Combine(_tempRoot, name);
            string ereDir = Path.Combine(dir, "ere");
            Directory.CreateDirectory(ereDir);
            File.WriteAllText(Path.Combine(ereDir, "era-electron.js"), "// SDK stub");
            File.WriteAllText(Path.Combine(ereDir, "main.js"),         "// game stub");
            return dir;
        }

        /// <summary>
        /// Only a root main.js — ambiguous, could be any JS project.
        /// </summary>
        string MakeRootMainJsOnlyLayout(string name = "js-only")
        {
            string dir = Path.Combine(_tempRoot, name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "main.js"), "// vanilla js project");
            return dir;
        }

        /// <summary>
        /// Standard Emuera layout: emuera.config + ERB/ subdirectory.
        /// </summary>
        string MakeEmueraLayout(string name = "emuera-game")
        {
            string dir = Path.Combine(_tempRoot, name);
            string erbDir = Path.Combine(dir, "ERB");
            Directory.CreateDirectory(erbDir);
            File.WriteAllText(Path.Combine(dir, "emuera.config"), "[General]");
            File.WriteAllText(Path.Combine(erbDir, "SYSTEM_TITLE.ERB"), "@SYSTEM_TITLE\n#CALL SHOW_TITLE");
            return dir;
        }

        /// <summary>
        /// Ambiguous: both ERB/ and .ere-min-version present.
        /// </summary>
        string MakeAmbiguousLayout(string name = "ambiguous-game")
        {
            string dir = MakeEmueraLayout(name);
            string ereDir = Path.Combine(dir, "ere");
            Directory.CreateDirectory(ereDir);
            File.WriteAllText(Path.Combine(dir,    ".ere-min-version"),  "2200");
            File.WriteAllText(Path.Combine(ereDir, "era-electron.js"),   "// SDK stub");
            File.WriteAllText(Path.Combine(ereDir, "main.js"),           "// game stub");
            return dir;
        }

        /// <summary>
        /// Empty directory — should not be detected as any runtime.
        /// </summary>
        string MakeEmptyLayout(string name = "empty-dir")
        {
            string dir = Path.Combine(_tempRoot, name);
            Directory.CreateDirectory(dir);
            return dir;
        }

        // ================================================================== //
        //  EraElectronGameDetector unit tests                                  //
        // ================================================================== //

        [Test]
        public void EreDetector_EraUmaSourceLayout_ReturnsCertain()
        {
            string dir = MakeEraUmaSourceLayout();
            var detector = new EraElectronGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

            var result = detector.TryDetect(dir, files);

            Assert.IsNotNull(result, "EraUma source layout must be detected");
            Assert.AreEqual(DetectionConfidence.Certain, result.Confidence);
            Assert.IsTrue(result.Evidence.Count >= 2, "Should have multiple evidence items");
            Assert.AreEqual(0, result.Warnings.Count,
                "No warnings expected for clean EraUma source layout");
        }

        [Test]
        public void EreDetector_EraUmaSourceLayout_BuildDescriptorUsesEreEntry()
        {
            string dir = MakeEraUmaSourceLayout();
            var detector = new EraElectronGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
            var result = detector.TryDetect(dir, files);

            var desc = detector.BuildDescriptor(dir, result);

            Assert.AreEqual(RuntimeKind.EraElectron, desc.RuntimeKind);
            Assert.AreEqual(Path.Combine("ere", "main.js"), desc.EntryPoint,
                "Entry point must be ere/main.js, not root main.js");
            Assert.AreEqual("2200", desc.RequiredRuntimeVersion,
                ".ere-min-version content must be read into RequiredRuntimeVersion");
            Assert.AreEqual("erauma", desc.Title,
                "Title must be read from package.json name field");
        }

        [Test]
        public void EreDetector_MinVersionOnly_ReturnsHigh()
        {
            string dir = MakeMinVersionOnlyLayout();
            var detector = new EraElectronGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

            var result = detector.TryDetect(dir, files);

            Assert.IsNotNull(result);
            Assert.AreEqual(DetectionConfidence.High, result.Confidence,
                ".ere-min-version alone yields High confidence");
        }

        [Test]
        public void EreDetector_SdkAndEntryNoMinVersion_ReturnsHigh()
        {
            // Both ere/era-electron.js and ere/main.js present; .ere-min-version absent.
            // Two independent strong source indicators → High confidence.
            // Still warns because official marker is missing.
            string dir = MakeEreSourceNoBadgeLayout();
            var detector = new EraElectronGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

            var result = detector.TryDetect(dir, files);

            Assert.IsNotNull(result);
            Assert.AreEqual(DetectionConfidence.High, result.Confidence,
                "SDK + game entry without .ere-min-version yields High (two independent ERE indicators)");
            Assert.IsTrue(result.Warnings.Count > 0, "Should warn about missing .ere-min-version");
        }

        [Test]
        public void EreDetector_RootMainJsOnly_ReturnsLow()
        {
            string dir = MakeRootMainJsOnlyLayout();
            var detector = new EraElectronGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

            var result = detector.TryDetect(dir, files);

            // Root main.js alone should return Low confidence (very generic)
            if (result != null)
                Assert.AreEqual(DetectionConfidence.Low, result.Confidence,
                    "A bare root main.js must not exceed Low confidence");
        }

        [Test]
        public void EreDetector_EmueraLayout_ReturnsNull()
        {
            string dir = MakeEmueraLayout();
            var detector = new EraElectronGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

            var result = detector.TryDetect(dir, files);

            Assert.IsNull(result, "Pure Emuera layout must not be detected as EraElectron");
        }

        [Test]
        public void EreDetector_EmptyDir_ReturnsNull()
        {
            string dir = MakeEmptyLayout();
            var detector = new EraElectronGameDetector();

            var result = detector.TryDetect(dir, new string[0]);

            Assert.IsNull(result);
        }

        // ================================================================== //
        //  EmueraGameDetector unit tests                                       //
        // ================================================================== //

        [Test]
        public void EmuDetector_ConfigAndErb_ReturnsCertain()
        {
            string dir = MakeEmueraLayout();
            var detector = new EmueraGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

            var result = detector.TryDetect(dir, files);

            Assert.IsNotNull(result);
            Assert.AreEqual(DetectionConfidence.Certain, result.Confidence);
            Assert.AreEqual(0, result.Warnings.Count);
        }

        [Test]
        public void EmuDetector_EraElectronLayout_ReturnsNull()
        {
            string dir = MakeEraUmaSourceLayout();
            var detector = new EmueraGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);

            var result = detector.TryDetect(dir, files);

            Assert.IsNull(result, "EraUma layout has no ERB/ and no emuera.config");
        }

        // ================================================================== //
        //  GameDetector integration tests                                      //
        // ================================================================== //

        [Test]
        public void GameDetector_EraUmaSourceLayout_DetectsEraElectron()
        {
            string dir = MakeEraUmaSourceLayout();
            var gd = GameDetector.CreateDefault();

            var desc = gd.Detect(dir);

            Assert.IsNotNull(desc, "EraUma source layout must be detected");
            Assert.AreEqual(RuntimeKind.EraElectron, desc.RuntimeKind);
            Assert.AreEqual(DetectionConfidence.Certain, desc.DetectionResult.Confidence);
        }

        [Test]
        public void GameDetector_EmueraLayout_DetectsEmuera()
        {
            string dir = MakeEmueraLayout();
            var gd = GameDetector.CreateDefault();

            var desc = gd.Detect(dir);

            Assert.IsNotNull(desc);
            Assert.AreEqual(RuntimeKind.Emuera, desc.RuntimeKind);
        }

        [Test]
        public void GameDetector_AmbiguousLayout_FlagsAmbiguity()
        {
            string dir = MakeAmbiguousLayout();
            var gd = GameDetector.CreateDefault();

            var desc = gd.Detect(dir);

            Assert.IsNotNull(desc, "Ambiguous layout must still produce a descriptor");
            // Higher confidence wins; both EraElectron (Certain) and Emuera (Certain) present
            // The ambiguous alternative must be set
            Assert.IsTrue(
                desc.DetectionResult.AmbiguousAlternative.HasValue ||
                desc.DetectionResult.Warnings.Count > 0,
                "Ambiguous layout must set AmbiguousAlternative or emit a warning");
        }

        [Test]
        public void GameDetector_UnknownDir_ReturnsNull()
        {
            string dir = MakeEmptyLayout();
            var gd = GameDetector.CreateDefault();

            var desc = gd.Detect(dir);

            Assert.IsNull(desc);
        }

        [Test]
        public void GameDetector_NonExistentDir_ReturnsNull()
        {
            string dir = Path.Combine(_tempRoot, "does_not_exist");
            var gd = GameDetector.CreateDefault();

            var desc = gd.Detect(dir);

            Assert.IsNull(desc);
        }

        [Test]
        public void GameDetector_DiscoverAll_FindsBothRuntimes()
        {
            // Place both game types directly under _tempRoot
            MakeEraUmaSourceLayout("ere-game");
            MakeEmueraLayout("emu-game");

            var gd = GameDetector.CreateDefault();
            var results = gd.DiscoverAll(_tempRoot);

            Assert.AreEqual(2, results.Count, "Should find exactly 2 games");
            bool foundEre  = false;
            bool foundEmu  = false;
            foreach (var d in results)
            {
                if (d.RuntimeKind == RuntimeKind.EraElectron) foundEre = true;
                if (d.RuntimeKind == RuntimeKind.Emuera)      foundEmu = true;
            }
            Assert.IsTrue(foundEre, "EraElectron game must be discovered");
            Assert.IsTrue(foundEmu, "Emuera game must be discovered");
        }

        // ------------------------------------------------------------------ //
        //  GameDescriptor field validation                                     //
        // ------------------------------------------------------------------ //

        [Test]
        public void EreDetector_BuildDescriptor_GameIdIsStable()
        {
            string dir = MakeEraUmaSourceLayout();
            var detector = new EraElectronGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
            var result = detector.TryDetect(dir, files);

            var desc1 = detector.BuildDescriptor(dir, result);
            var desc2 = detector.BuildDescriptor(dir, result);

            Assert.AreEqual(desc1.GameId, desc2.GameId,
                "GameId must be deterministic for the same path");
            Assert.IsTrue(desc1.GameId.StartsWith("ere-"),
                "EraElectron GameId must carry ere- prefix");
        }

        [Test]
        public void EmuDetector_BuildDescriptor_GameIdIsStable()
        {
            string dir = MakeEmueraLayout();
            var detector = new EmueraGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
            var result = detector.TryDetect(dir, files);

            var desc1 = detector.BuildDescriptor(dir, result);
            var desc2 = detector.BuildDescriptor(dir, result);

            Assert.AreEqual(desc1.GameId, desc2.GameId);
            Assert.IsTrue(desc1.GameId.StartsWith("emu-"));
        }

        [Test]
        public void EmuDetector_BuildDescriptor_SaveNamespaceEqualsGameId()
        {
            string dir = MakeEmueraLayout();
            var detector = new EmueraGameDetector();
            var files = Directory.GetFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
            var result = detector.TryDetect(dir, files);

            var desc = detector.BuildDescriptor(dir, result);

            Assert.AreEqual(desc.GameId, desc.SaveNamespace);
        }
    }
}
