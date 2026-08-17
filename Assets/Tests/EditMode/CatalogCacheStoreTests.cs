using System.Collections.Generic;
using System.IO;
using System.Text;
using MinorShift.Emuera.GameProc;
using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="CatalogCacheStore"/> — the persistent binary cache for
    /// FunctionCatalog and GameResourceCatalog (Phase 6 — catalog cache).
    ///
    /// <para>All tests use uniquely-named temp files so the persistent cache can
    /// never accidentally match across runs.</para>
    /// </summary>
    [TestFixture]
    public class CatalogCacheStoreTests
    {
        static string TempErb(string content)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_ccs_" + Path.GetRandomFileName() + ".ERB");
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

        [TearDown]
        public void CleanupAfter()
        {
            FunctionCatalog.Clear();
            GameResourceCatalog.Clear();
        }

        [Test]
        public void FunctionCatalog_RoundTrips_ThroughCache()
        {
            string a = TempErb("@FUNC_A\nRETURN 1\n");
            string b = TempErb("@FUNC_B\n#FUNCTION\nRETURNF 5\n");
            try
            {
                var files = Pairs(a, b);
                var cat = FunctionCatalog.Build(files, Encoding.UTF8); // scans + saves

                FunctionCatalog loaded;
                bool ok = CatalogCacheStore.TryLoadFunctionCatalog(files, Encoding.UTF8, out loaded);

                Assert.IsTrue(ok, "cache must be valid immediately after save");
                Assert.IsNotNull(loaded);
                Assert.IsTrue(loaded.IsReady);
                Assert.IsTrue(loaded.FunctionExists("FUNC_A"));
                Assert.IsTrue(loaded.FunctionExists("FUNC_B"));
                Assert.AreEqual(FunctionReturnKind.Int64, loaded.GetReturnKind("FUNC_B"));
                Assert.AreNotSame(cat, loaded, "a cache load rebuilds a fresh catalog");
            }
            finally
            {
                File.Delete(a);
                File.Delete(b);
            }
        }

        [Test]
        public void FunctionCatalog_StaleCache_IsRejected()
        {
            string a = TempErb("@FUNC_A\nRETURN 1\n");
            string b = TempErb("@FUNC_B\nRETURN 2\n");
            try
            {
                var files = Pairs(a, b);
                FunctionCatalog.Build(files, Encoding.UTF8); // save

                // Invalidated: file b changes length + timestamp.
                File.WriteAllText(b, "@FUNC_B\nRETURN 2\nPRINTL CHANGED\n", Encoding.UTF8);

                FunctionCatalog loaded;
                bool ok = CatalogCacheStore.TryLoadFunctionCatalog(files, Encoding.UTF8, out loaded);
                Assert.IsFalse(ok, "edited file must invalidate the cache");
                Assert.IsNull(loaded);
            }
            finally
            {
                File.Delete(a);
                File.Delete(b);
            }
        }

        [Test]
        public void FunctionCatalog_EncodingChange_IsRejected()
        {
            string a = TempErb("@FUNC_A\nRETURN 1\n");
            try
            {
                var files = Pairs(a);
                FunctionCatalog.Build(files, Encoding.UTF8); // saved under UTF8 codepage

                FunctionCatalog loaded;
                bool ok = CatalogCacheStore.TryLoadFunctionCatalog(files, Encoding.UTF32, out loaded);
                Assert.IsFalse(ok, "different encoding codepage must invalidate the cache");
                Assert.IsNull(loaded);
            }
            finally
            {
                File.Delete(a);
            }
        }

        [Test]
        public void FunctionCatalog_DifferentFileSets_KeepIndependentCaches()
        {
            string first = TempErb("@FIRST_CACHE_FUNC\nRETURN 1\n");
            string second = TempErb("@SECOND_CACHE_FUNC\nRETURN 2\n");
            try
            {
                var firstFiles = Pairs(first);
                var secondFiles = Pairs(second);
                FunctionCatalog.Build(firstFiles, Encoding.UTF8);
                FunctionCatalog.Build(secondFiles, Encoding.UTF8);

                FunctionCatalog loadedFirst;
                FunctionCatalog loadedSecond;
                Assert.IsTrue(CatalogCacheStore.TryLoadFunctionCatalog(
                    firstFiles, Encoding.UTF8, out loadedFirst));
                Assert.IsTrue(CatalogCacheStore.TryLoadFunctionCatalog(
                    secondFiles, Encoding.UTF8, out loadedSecond));
                Assert.IsTrue(loadedFirst.FunctionExists("FIRST_CACHE_FUNC"));
                Assert.IsFalse(loadedFirst.FunctionExists("SECOND_CACHE_FUNC"));
                Assert.IsTrue(loadedSecond.FunctionExists("SECOND_CACHE_FUNC"));
                Assert.IsFalse(loadedSecond.FunctionExists("FIRST_CACHE_FUNC"));
            }
            finally
            {
                File.Delete(first);
                File.Delete(second);
            }
        }

        [Test]
        public void ResourceCatalog_RoundTrips_ThroughCache()
        {
            string dir = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_res_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            string img = Path.Combine(dir, "back.png");
            File.WriteAllBytes(img, new byte[] { 1, 2, 3, 4, 5 }); // dummy bytes
            try
            {
                var cat = GameResourceCatalog.Scan(dir); // scans + saves
                Assert.AreEqual(1, cat.Count);

                GameResourceCatalog loaded;
                bool ok = CatalogCacheStore.TryLoadResourceCatalog(dir, out loaded);

                Assert.IsTrue(ok, "resource cache must be valid immediately after save");
                Assert.IsNotNull(loaded);
                Assert.IsTrue(loaded.IsReady);
                Assert.AreEqual(1, loaded.Count);
                Assert.AreEqual(dir, loaded.RootDir);
                Assert.IsNotNull(loaded.TryResolve("back.png"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ResourceCatalog_StaleCache_IsRejected()
        {
            string dir = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_res_" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            string img = Path.Combine(dir, "back.png");
            File.WriteAllBytes(img, new byte[] { 1, 2, 3, 4, 5 });
            try
            {
                GameResourceCatalog.Scan(dir); // save
                File.WriteAllBytes(img, new byte[] { 9, 9, 9 }); // length change

                GameResourceCatalog loaded;
                bool ok = CatalogCacheStore.TryLoadResourceCatalog(dir, out loaded);
                Assert.IsFalse(ok, "edited resource must invalidate the cache");
                Assert.IsNull(loaded);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
