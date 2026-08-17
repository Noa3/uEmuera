using System.Collections.Generic;
using System.IO;
using System.Text;
using MinorShift.Emuera.GameProc;
using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="OnDemandErbCompiler"/> / <see cref="ErbOnDemand"/> —
    /// the interpreter-owned lazy ERB compiler (Phase 6 — Fast boot).
    ///
    /// <para>These exercise the lookup/pending/reporting logic and the compile-state
    /// transitions. The full compile path needs a live ErbLoader + Process, which
    /// EditMode cannot cheaply build; the runtime CALL/expression dispatch wiring is
    /// verified by PlayMode/integration instead.</para>
    /// </summary>
    [TestFixture]
    public class OnDemandErbCompilerTests
    {
        static string TempErb(string content)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_odm_" + Path.GetRandomFileName() + ".ERB");
            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        static List<KeyValuePair<string, string>> Pairs(params string[] files)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var f in files)
                pairs.Add(new KeyValuePair<string, string>(Path.GetFileName(f), f));
            return pairs;
        }

        static FunctionCatalog BuildCatalog(List<KeyValuePair<string, string>> files)
        {
            return FunctionCatalog.Build(files, Encoding.UTF8);
        }

        [SetUp]
        public void CleanupBefore()
        {
            OnDemandErbCompiler.Clear();
            FunctionCatalog.Clear();
        }

        [TearDown]
        public void CleanupAfter()
        {
            OnDemandErbCompiler.Clear();
            FunctionCatalog.Clear();
        }

        [Test]
        public void DeferredFile_ReportedPending_UntilExplicitCompiled()
        {
            string priPath = TempErb("@PRI_FUNC\nRETURN 0\n");
            string defPath = TempErb("@DEF_FUNC\nRETURN 0\n");
            try
            {
                var files = Pairs(priPath, defPath);
                var cat = BuildCatalog(files);
                var deferred  = new List<KeyValuePair<string, string>>
                    { new KeyValuePair<string, string>(Path.GetFileName(defPath), defPath) };
                var compiled  = new List<string> { priPath };

                // null loader: only the pending/known reporting is exercised here
                // (the compile path itself needs a live ErbLoader).
                OnDemandErbCompiler.Activate(null, null, cat, deferred, compiled);

                Assert.IsTrue(ErbOnDemand.IsFunctionPending("DEF_FUNC"),
                    "function declared in a deferred file must report as pending");
                Assert.IsFalse(ErbOnDemand.IsFunctionPending("PRI_FUNC"),
                    "function in a boot-compiled (priority) file must not be pending");
                Assert.IsTrue(ErbOnDemand.IsKnownFunction("DEF_FUNC"));
                Assert.IsTrue(ErbOnDemand.IsKnownFunction("PRI_FUNC"));
                Assert.IsFalse(ErbOnDemand.IsKnownFunction("DOES_NOT_EXIST"));
            }
            finally
            {
                File.Delete(priPath);
                File.Delete(defPath);
            }
        }

        [Test]
        public void Activate_MarksPriorityMetadata_AsCompiled()
        {
            string priPath = TempErb("@PRI_FUNC\nRETURN 0\n");
            string defPath = TempErb("@DEF_FUNC\nRETURN 0\n");
            try
            {
                var files = Pairs(priPath, defPath);
                var cat = BuildCatalog(files);
                var deferred = new List<KeyValuePair<string, string>>
                    { new KeyValuePair<string, string>(Path.GetFileName(defPath), defPath) };

                OnDemandErbCompiler.Activate(null, null, cat, deferred, new List<string> { priPath });

                Assert.AreEqual(FunctionCompileState.Compiled, cat.GetFirst("PRI_FUNC").State);
                Assert.AreEqual(FunctionCompileState.Catalogued, cat.GetFirst("DEF_FUNC").State);
            }
            finally
            {
                File.Delete(priPath);
                File.Delete(defPath);
            }
        }

        [Test]
        public void FailedCompile_MarksFailed_AndDecrementsRemaining()
        {
            string priPath = TempErb("@PRI_FUNC\nRETURN 0\n");
            string defPath = TempErb("@DEF_FUNC\nRETURN 0\n");
            try
            {
                var files = Pairs(priPath, defPath);
                var cat = BuildCatalog(files);
                var deferred = new List<KeyValuePair<string, string>>
                    { new KeyValuePair<string, string>(Path.GetFileName(defPath), defPath) };

                OnDemandErbCompiler.Activate(null, null, cat, deferred, new List<string> { priPath });
                Assert.AreEqual(1, OnDemandErbCompiler.Instance.RemainingFiles);

                // Null loader -> CompileFile catches the NRE and records a Failed state.
                ErbOnDemand.EnsureCompiled("DEF_FUNC");

                Assert.AreEqual(FunctionCompileState.Failed, cat.GetFirst("DEF_FUNC").State);
                Assert.AreEqual(0, OnDemandErbCompiler.Instance.RemainingFiles);
                Assert.IsFalse(ErbOnDemand.IsFunctionPending("DEF_FUNC"),
                    "a failed file must no longer report as pending");
            }
            finally
            {
                File.Delete(priPath);
                File.Delete(defPath);
            }
        }

        [Test]
        public void Clear_ResetsEverything()
        {
            string priPath = TempErb("@PRI_FUNC\nRETURN 0\n");
            string defPath = TempErb("@DEF_FUNC\nRETURN 0\n");
            try
            {
                var files = Pairs(priPath, defPath);
                var cat = BuildCatalog(files);
                var deferred = new List<KeyValuePair<string, string>>
                    { new KeyValuePair<string, string>(Path.GetFileName(defPath), defPath) };

                OnDemandErbCompiler.Activate(null, null, cat, deferred, new List<string> { priPath });
                Assert.IsNotNull(OnDemandErbCompiler.Instance);
                Assert.IsTrue(ErbOnDemand.IsFunctionPending("DEF_FUNC"));

                OnDemandErbCompiler.Clear();

                Assert.IsNull(OnDemandErbCompiler.Instance);
                Assert.IsFalse(ErbOnDemand.IsFunctionPending("DEF_FUNC"));
            }
            finally
            {
                File.Delete(priPath);
                File.Delete(defPath);
            }
        }
    }
}