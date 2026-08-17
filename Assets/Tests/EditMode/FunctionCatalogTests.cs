using System.Collections.Generic;
using System.Text;
using MinorShift.Emuera.GameProc;
using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="FunctionCatalog"/> — ERB metadata scan
    /// (Phase 6 #11-#12 / #8).
    /// </summary>
    [TestFixture]
    public class FunctionCatalogTests
    {
        // ---- tiny in-memory ERB builder -------------------------------------

        /// Helper: write a temp ERB file with given content, return its path.
        static string TempErb(string content)
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "uEmuera_test_" + System.IO.Path.GetRandomFileName() + ".ERB");
            System.IO.File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        static FunctionCatalog BuildFromText(string erbText)
        {
            var path = TempErb(erbText);
            var files = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(
                    System.IO.Path.GetFileName(path), path)
            };
            var cat = FunctionCatalog.Build(files, Encoding.UTF8);
            System.IO.File.Delete(path); // cleanup
            return cat;
        }

        // ---- tests ----------------------------------------------------------

        [Test]
        public void SimpleNormalFunction_Detected()
        {
            var cat = BuildFromText("@MY_FUNC\nPRINTL HELLO\n");
            Assert.IsTrue(cat.FunctionExists("MY_FUNC"));
            Assert.AreEqual(FunctionReturnKind.Void, cat.GetReturnKind("MY_FUNC"));
            Assert.AreEqual(1, cat.ExistFunctionValue("MY_FUNC"));
        }

        [Test]
        public void HashFunction_DetectedAsInt64()
        {
            var cat = BuildFromText("@GET_VALUE\n#FUNCTION\nRETURNF 42\n");
            Assert.IsTrue(cat.FunctionExists("GET_VALUE"));
            Assert.AreEqual(FunctionReturnKind.Int64, cat.GetReturnKind("GET_VALUE"));
            Assert.AreEqual(2, cat.ExistFunctionValue("GET_VALUE"));
            Assert.AreEqual(typeof(long), cat.GetClrReturnType("GET_VALUE"));
        }

        [Test]
        public void HashFunctions_DetectedAsString()
        {
            var cat = BuildFromText("@GET_TITLE\n#FUNCTIONS\nRETURNF \"HELLO\"\n");
            Assert.IsTrue(cat.FunctionExists("GET_TITLE"));
            Assert.AreEqual(FunctionReturnKind.String, cat.GetReturnKind("GET_TITLE"));
            Assert.AreEqual(3, cat.ExistFunctionValue("GET_TITLE"));
            Assert.AreEqual(typeof(string), cat.GetClrReturnType("GET_TITLE"));
        }

        [Test]
        public void EventFlags_ParsedCorrectly()
        {
            var cat = BuildFromText("@EVENTFIRST\n#PRI\nPRINTL PRI\n@EVENTFIRST\n#LATER\nPRINTL LATER\n");
            Assert.IsTrue(cat.FunctionExists("EVENTFIRST"));
            var all = cat.GetAll("EVENTFIRST");
            Assert.AreEqual(2, all.Count);
            Assert.IsTrue(all[0].IsPri,  "First declaration should be #PRI");
            Assert.IsTrue(all[1].IsLater, "Second declaration should be #LATER");
        }

        [Test]
        public void MissingFunction_Returns0()
        {
            var cat = BuildFromText("@REAL_FUNC\n");
            Assert.IsFalse(cat.FunctionExists("GHOST_FUNC"));
            Assert.AreEqual(0, cat.ExistFunctionValue("GHOST_FUNC"));
        }

        [Test]
        public void MultipleFiles_AllIndexed()
        {
            string p1 = TempErb("@FUNC_A\n");
            string p2 = TempErb("@FUNC_B\n#FUNCTION\nRETURNF 0\n");
            var files = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a.ERB", p1),
                new KeyValuePair<string, string>("b.ERB", p2),
            };
            var cat = FunctionCatalog.Build(files, Encoding.UTF8);
            System.IO.File.Delete(p1);
            System.IO.File.Delete(p2);

            Assert.IsTrue(cat.FunctionExists("FUNC_A"));
            Assert.IsTrue(cat.FunctionExists("FUNC_B"));
            Assert.AreEqual(FunctionReturnKind.Void,  cat.GetReturnKind("FUNC_A"));
            Assert.AreEqual(FunctionReturnKind.Int64, cat.GetReturnKind("FUNC_B"));
            Assert.AreEqual(2, cat.Count);
        }

        [Test]
        public void HashFunctionsMustNotMatchHashFunction()
        {
            // #FUNCTIONS should be detected as String, NOT confused with #FUNCTION.
            var cat = BuildFromText("@FOO\n#FUNCTIONS\nRETURNF \"x\"\n@BAR\n#FUNCTION\nRETURNF 0\n");
            Assert.AreEqual(FunctionReturnKind.String, cat.GetReturnKind("FOO"), "#FUNCTIONS → String");
            Assert.AreEqual(FunctionReturnKind.Int64,  cat.GetReturnKind("BAR"), "#FUNCTION → Int64");
        }

        [Test]
        public void GotoLabel_NotIndexedAsFunction()
        {
            // @@LABEL is a goto target, not a function
            var cat = BuildFromText("@REAL_FUNC\n@@NOT_A_FUNC\n");
            Assert.IsTrue(cat.FunctionExists("REAL_FUNC"));
            Assert.IsFalse(cat.FunctionExists("NOT_A_FUNC"), "@@LABEL must not appear in catalog");
        }

        [Test]
        public void LineNumber_RecordedCorrectly()
        {
            var cat = BuildFromText("@FIRST\n@SECOND\n");
            var m1 = cat.GetFirst("FIRST");
            var m2 = cat.GetFirst("SECOND");
            Assert.IsNotNull(m1);
            Assert.IsNotNull(m2);
            Assert.AreEqual(1, m1.LineNumber, "FIRST should be on line 1");
            Assert.AreEqual(2, m2.LineNumber, "SECOND should be on line 2");
        }

        [Test]
        public void Clear_RemovesInstance()
        {
            BuildFromText("@FOO\n");
            Assert.IsNotNull(FunctionCatalog.Instance);
            FunctionCatalog.Clear();
            Assert.IsNull(FunctionCatalog.Instance);
        }

        [Test]
        public void EmptyFile_HandledGracefully()
        {
            var cat = BuildFromText("");
            Assert.IsNotNull(cat);
            Assert.IsTrue(cat.IsReady);
            Assert.AreEqual(0, cat.Count);
        }

        [Test]
        public void CommentLines_Ignored()
        {
            var cat = BuildFromText(";@NOT_A_FUNC\n@REAL\n");
            Assert.IsFalse(cat.FunctionExists("NOT_A_FUNC"));
            Assert.IsTrue(cat.FunctionExists("REAL"));
        }
    }
}
