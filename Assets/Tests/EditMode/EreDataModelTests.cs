using System.IO;
using NUnit.Framework;
using uEmuera.Runtime;
using uEmuera.Runtime.EraElectron;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="EreDataModel"/>, <see cref="VarAddress"/>,
    /// and <see cref="EraCsvParser"/> (Phase 8 — EraElectron data model).
    /// </summary>
    [TestFixture]
    public class EreDataModelTests
    {
        // ------------------------------------------------------------------ //
        //  VarAddress                                                          //
        // ------------------------------------------------------------------ //

        [Test]
        public void VarAddress_Global_TwoSegments()
        {
            VarAddress a;
            Assert.IsTrue(VarAddress.TryParse("flag:5", out a));
            Assert.AreEqual("flag", a.Table);
            Assert.AreEqual(-1,     a.CharIdx, "global address has no char index");
            Assert.AreEqual(5,      a.ArrayIdx);
            Assert.IsFalse(a.IsChara);
        }

        [Test]
        public void VarAddress_Chara_ThreeSegments()
        {
            VarAddress a;
            Assert.IsTrue(VarAddress.TryParse("abl:0:3", out a));
            Assert.AreEqual("abl", a.Table);
            Assert.AreEqual(0,     a.CharIdx);
            Assert.AreEqual(3,     a.ArrayIdx);
            Assert.IsTrue(a.IsChara);
        }

        [Test]
        public void VarAddress_TableName_LowercaseNormalized()
        {
            VarAddress a;
            Assert.IsTrue(VarAddress.TryParse("FLAG:10", out a));
            Assert.AreEqual("flag", a.Table, "table name must be lower-cased");
            Assert.AreEqual(10,     a.ArrayIdx);
        }

        [Test]
        public void VarAddress_Empty_ReturnsFalse()
        {
            VarAddress a;
            Assert.IsFalse(VarAddress.TryParse("",    out a));
            Assert.IsFalse(VarAddress.TryParse(null,  out a));
            Assert.IsFalse(VarAddress.TryParse("abl", out a), "single segment is invalid");
        }

        [Test]
        public void VarAddress_ToKey_Global()
        {
            VarAddress a;
            VarAddress.TryParse("flag:7", out a);
            Assert.AreEqual("flag:7", a.ToKey());
        }

        [Test]
        public void VarAddress_ToKey_Chara()
        {
            VarAddress a;
            VarAddress.TryParse("abl:2:5", out a);
            Assert.AreEqual("abl:2:5", a.ToKey());
        }

        // ------------------------------------------------------------------ //
        //  EreDataModel — get / set / add                                     //
        // ------------------------------------------------------------------ //

        EreDataModel _model;

        [SetUp]
        public void Setup() =>
            _model = EreDataModel.Create(BuildMinimalDescriptor());

        [TearDown]
        public void Teardown() => _model?.Dispose();

        [Test]
        public void Get_UnsetInt_ReturnsZero()
        {
            var v = _model.Get("flag:0");
            Assert.AreEqual(0L, v);
        }

        [Test]
        public void Set_Int_ThenGet_ReturnsValue()
        {
            _model.Set("flag:5", 42L);
            Assert.AreEqual(42L, _model.Get("flag:5"));
        }

        [Test]
        public void Set_Int_Double_ConvertedToLong()
        {
            _model.Set("flag:1", 3.14);
            Assert.AreEqual(3L, _model.Get("flag:1"));
        }

        [Test]
        public void Add_Int_AccumulatesCorrectly()
        {
            _model.Set("flag:2", 10L);
            _model.Add("flag:2", 5L);
            Assert.AreEqual(15L, _model.Get("flag:2"));
        }

        [Test]
        public void Add_Unset_StartsFromZero()
        {
            _model.Add("global:3", 7L);
            Assert.AreEqual(7L, _model.Get("global:3"));
        }

        [Test]
        public void Set_String_ThenGet_ReturnsValue()
        {
            _model.Set("callname:1", "TestChara");
            Assert.AreEqual("TestChara", _model.Get("callname:1"));
        }

        [Test]
        public void Get_UnsetString_ReturnsEmpty()
        {
            var v = _model.Get("callname:99");
            Assert.AreEqual("", v);
        }

        [Test]
        public void Add_String_Concatenates()
        {
            _model.Set("name:0", "Hello");
            _model.Add("name:0", " World");
            Assert.AreEqual("Hello World", _model.Get("name:0"));
        }

        [Test]
        public void Set_CharaTable_ThreeSegments()
        {
            _model.Set("abl:0:3", 100L);
            Assert.AreEqual(100L, _model.Get("abl:0:3"));
        }

        [Test]
        public void CharaTable_IsolatedPerCharacter()
        {
            _model.Set("abl:0:1", 50L);
            _model.Set("abl:1:1", 99L);
            Assert.AreEqual(50L, _model.Get("abl:0:1"));
            Assert.AreEqual(99L, _model.Get("abl:1:1"));
        }

        [Test]
        public void ResetAll_ClearsAllVariables()
        {
            _model.Set("flag:0", 1L);
            _model.Set("abl:0:1", 2L);
            _model.ResetAll();
            Assert.AreEqual(0L, _model.Get("flag:0"));
            Assert.AreEqual(0L, _model.Get("abl:0:1"));
        }

        [Test]
        public void AddCharacter_ThenIsInList()
        {
            Assert.IsTrue(_model.AddCharacter(42));
            Assert.Contains(42, (System.Collections.IList)_model.AddedCharacters);
        }

        [Test]
        public void AddCharacter_Duplicate_ReturnsFalse()
        {
            _model.AddCharacter(1);
            Assert.IsFalse(_model.AddCharacter(1));
            Assert.AreEqual(1, _model.AddedCharacters.Count);
        }

        [Test]
        public void RemoveCharacter_RemovesFromList()
        {
            _model.AddCharacter(5);
            _model.RemoveCharacter(5);
            Assert.IsFalse(
                ((System.Collections.IList)_model.AddedCharacters).Contains(5));
        }

        // ------------------------------------------------------------------ //
        //  EraCsvParser                                                        //
        // ------------------------------------------------------------------ //

        [Test]
        public void EraCsvParser_ParseIndexTable_BasicEntries()
        {
            string path = TempCsv(
                "; comment\n" +
                "0,Alpha\n" +
                "1,Beta\n" +
                "; another comment\n" +
                "5,Gamma\n");
            try
            {
                var result = new System.Collections.Generic.Dictionary<int, string>();
                foreach (var kv in EraCsvParser.ParseIndexTable(path))
                    result[kv.Key] = kv.Value;

                Assert.AreEqual("Alpha", result[0]);
                Assert.AreEqual("Beta",  result[1]);
                Assert.AreEqual("Gamma", result[5]);
                Assert.AreEqual(3, result.Count);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void EraCsvParser_ParseIndexTable_InlineComment()
        {
            string path = TempCsv("3,Delta ; this is a comment\n");
            try
            {
                var result = new System.Collections.Generic.Dictionary<int, string>();
                foreach (var kv in EraCsvParser.ParseIndexTable(path))
                    result[kv.Key] = kv.Value;
                Assert.AreEqual("Delta", result[3]);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void EraCsvParser_EmptyFile_NoEntries()
        {
            string path = TempCsv("");
            try
            {
                int count = 0;
                foreach (var _ in EraCsvParser.ParseIndexTable(path)) count++;
                Assert.AreEqual(0, count);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void EraCsvParser_ParseKeyValue_ReturnsFirstPipeSegment()
        {
            string path = TempCsv("Title,MyGame|Subtitle|Extra\n");
            try
            {
                string val = null;
                foreach (var kv in EraCsvParser.ParseKeyValue(path))
                    if (kv.Key == "Title") { val = kv.Value; break; }
                Assert.AreEqual("MyGame", val);
            }
            finally { File.Delete(path); }
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        static GameDescriptor BuildMinimalDescriptor() => new GameDescriptor
        {
            GameId      = "test-0000",
            Title       = "Test",
            RuntimeKind = RuntimeKind.EraElectron,
            GameRoot    = Path.GetTempPath(), // empty dir; no real CSV
            SaveNamespace = "test",
        };

        static string TempCsv(string content)
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "uEmuera_ere_" + System.IO.Path.GetRandomFileName() + ".csv");
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);
            return path;
        }
    }
}
