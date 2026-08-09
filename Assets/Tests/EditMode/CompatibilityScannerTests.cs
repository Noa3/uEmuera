using System;
using System.IO;
using MinorShift.Emuera.Compatibility;
using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    public class CompatibilityScannerTests
    {
        [Test]
        public void Registries_PopulatedFromEngine()
        {
            // Ground truth must come from the engine itself, not a hard-coded list.
            Assert.Greater(CompatibilityScanner.InstructionCount, 100, "built-in instruction registry should be populated");
            Assert.Greater(CompatibilityScanner.MethodCount, 0, "built-in method registry should be populated");
            Assert.IsTrue(CompatibilityScanner.IsInstruction("PRINT"));
            Assert.IsTrue(CompatibilityScanner.IsInstruction("JUMP"));
            Assert.IsTrue(CompatibilityScanner.IsMethod("TOSTR"));
        }

        [Test]
        public void ReadStatementWord_BasicCases()
        {
            Assert.AreEqual("PRINT", CompatibilityScanner.ReadStatementWord("PRINT こんにちは"));
            Assert.AreEqual("CALL", CompatibilityScanner.ReadStatementWord("CALL @TEST_EVENT"));
            Assert.AreEqual("IF", CompatibilityScanner.ReadStatementWord("IF X == 1"));
            Assert.AreEqual("SIF", CompatibilityScanner.ReadStatementWord("SIF Y > 2"));
            Assert.AreEqual("DIM", CompatibilityScanner.ReadStatementWord("DIM A, 3"));
            Assert.IsNull(CompatibilityScanner.ReadStatementWord("@EVENT_FUNCTION"));
            Assert.IsNull(CompatibilityScanner.ReadStatementWord("; comment"));
            Assert.IsNull(CompatibilityScanner.ReadStatementWord(""));
            Assert.IsNull(CompatibilityScanner.ReadStatementWord("   "));
        }

        [Test]
        public void ReadTargetLabel_ExtractsAtLabels()
        {
            Assert.AreEqual("TEST_EVENT", CompatibilityScanner.ReadTargetLabel("@TEST_EVENT"));
            Assert.AreEqual("FOO", CompatibilityScanner.ReadTargetLabel(" @FOO (a, b)"));
            Assert.IsNull(CompatibilityScanner.ReadTargetLabel("123"));
            Assert.IsNull(CompatibilityScanner.ReadTargetLabel("@"));
            Assert.IsNull(CompatibilityScanner.ReadTargetLabel(null));
        }

        [Test]
        public void ReadIdentifierUses_DetectsMethodCalls()
        {
            var uses = CompatibilityScanner.ReadIdentifierUses("LOCALS:SETUP FOO(BAR) X");
            var list = new System.Collections.Generic.List<IdentifierUse>(uses);
            // Tokens: LOCALS ':' '.' SETUP FOO(BAR) X  — colon and dot are break chars,
            // so identifiers are: LOCALS SETUP FOO X and BAR inside parens.
            Assert.Contains("LOCALS", Names(list));
            Assert.Contains("SETUP", Names(list));
            Assert.Contains("FOO", Names(list));
            Assert.Contains("X", Names(list));
        }

        static string[] Names(System.Collections.Generic.List<IdentifierUse> uses)
        {
            var ret = new string[uses.Count];
            for (int i = 0; i < uses.Count; i++)
                ret[i] = uses[i].Name;
            return ret;
        }

        [Test]
        public void ScanDirectory_TinyFixture_Classifies()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ue_compat_scan");
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "MAIN.ERB"),
                    "PRINT Hello\n" +
                    "CALL @START\n" +
                    "SET X, 1\n" +
                    "@START\n" +
                    "PRINTL World\n" +
                    "RETURN\n");

                var report = CompatibilityScanner.ScanDirectory(dir);

                Assert.AreEqual(1, report.FilesScanned);
                Assert.AreEqual(0, report.FilesWithErrors);
                Assert.Greater(report.LogicalLinesSeen, 0);
                Assert.IsTrue(report.Instructions.Map.ContainsKey("PRINT"), "PRINT should be a known instruction");
                Assert.IsTrue(report.Instructions.Map.ContainsKey("CALL"), "CALL should be a known instruction");
                Assert.IsTrue(report.Instructions.Map.ContainsKey("SET"), "SET should be a known instruction");
                Assert.IsTrue(report.Instructions.Map.ContainsKey("RETURN"));
                Assert.IsTrue(report.CallTargets.Map.ContainsKey("START"), "CALL @START should record target START");
                Assert.IsTrue(report.FeatureAreas.Map.ContainsKey("Console"), "PRINT should be classified as Console feature");
                Assert.IsTrue(report.FeatureAreas.Map.ContainsKey("Control"), "non-console instructions should classify");
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    [Test]
        public void ScanCorpus_WhenEnvSet_ProducesReport()
        {
            // Production run against a real game, opt-in: set UEMUERA_CORPUS to the
            // game root (or a path to directory containing ERB/CSV files). When unset
            // the test is skipped so CI / batch runs stay hermetic.
            string corpus = Environment.GetEnvironmentVariable("UEMUERA_CORPUS");
            if (string.IsNullOrEmpty(corpus) || !Directory.Exists(corpus))
                Assert.Ignore("UEMUERA_CORPUS not set or missing");

            var report = CompatibilityScanner.ScanDirectory(corpus);

            Assert.Greater(report.FilesScanned, 0, "corpus should contain scan files");
            Assert.AreEqual(0, report.FilesWithErrors, "scan must not throw on any file");
            Assert.Greater(report.Instructions.Map.Count, 0);
            // Unknown tokens may legitimately be zero once every token resolves to a
            // registry entry or a user-defined #FUNCTION/#FUNCTIONS. The report is the
            // review artifact; the assertion guards regressions where scanning throws.
            Assert.AreEqual(0, report.FilesWithErrors);

            string reportPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                "uEmuera_CompatibilityReport.json");
            File.WriteAllText(reportPath, report.ToJson());
            TestContext.WriteLine("report: " + reportPath);

            string registryPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                "uEmuera_Registry.txt");
            var reg = new System.Text.StringBuilder();
            reg.AppendLine("#INSTRUCTIONS");
            foreach (string name in CompatibilityScanner.AllInstructions())
                reg.AppendLine(name);
            reg.AppendLine("#METHODS");
            foreach (string name in CompatibilityScanner.AllMethods())
                reg.AppendLine(name);
            File.WriteAllText(registryPath, reg.ToString());
            TestContext.WriteLine("registry: " + registryPath);
        }
    }
}