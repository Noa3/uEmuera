using NUnit.Framework;
using System.IO;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for the StringStream class used for parsing strings character by character.
    /// This is a core class used in the Emuera scripting engine.
    /// </summary>
    [TestFixture]
    public class StringStreamTests
    {
        #region Constructor Tests
        
        [Test]
        public void Constructor_WithValidString_SetsRowString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("test");
            
            Assert.AreEqual("test", stream.RowString);
        }

        [Test]
        public void Constructor_WithNullString_SetsEmptyString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream(null);
            
            Assert.AreEqual("", stream.RowString);
        }

        [Test]
        public void Constructor_WithEmptyString_SetsEmptyString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("");
            
            Assert.AreEqual("", stream.RowString);
        }

        [Test]
        public void DefaultConstructor_CreatesEmptyStream()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream();
            
            Assert.AreEqual(null, stream.RowString);
        }

        #endregion

        #region Set Method Tests

        [Test]
        public void Set_WithValidString_UpdatesRowString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream();
            stream.Set("hello");
            
            Assert.AreEqual("hello", stream.RowString);
        }

        [Test]
        public void Set_WithNull_SetsEmptyString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("initial");
            stream.Set(null);
            
            Assert.AreEqual("", stream.RowString);
        }

        [Test]
        public void Set_ResetsCurrentPosition()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("test");
            stream.ShiftNext();
            stream.ShiftNext();
            stream.Set("new");
            
            Assert.AreEqual(0, stream.CurrentPosition);
        }

        #endregion

        #region Current Property Tests

        [Test]
        public void Current_AtStart_ReturnsFirstCharacter()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("abc");
            
            Assert.AreEqual('a', stream.Current);
        }

        [Test]
        public void Current_AtEnd_ReturnsEndOfString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("a");
            stream.ShiftNext();
            
            Assert.AreEqual(MinorShift.Emuera.Sub.StringStream.EndOfString, stream.Current);
        }

        [Test]
        public void Current_OnEmptyString_ReturnsEndOfString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("");
            
            Assert.AreEqual(MinorShift.Emuera.Sub.StringStream.EndOfString, stream.Current);
        }

        #endregion

        #region Next Property Tests

        [Test]
        public void Next_WithNextCharacter_ReturnsNextCharacter()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("ab");
            
            Assert.AreEqual('b', stream.Next);
        }

        [Test]
        public void Next_AtLastCharacter_ReturnsEndOfString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("a");
            
            Assert.AreEqual(MinorShift.Emuera.Sub.StringStream.EndOfString, stream.Next);
        }

        #endregion

        #region EOS Property Tests

        [Test]
        public void EOS_AtStart_ReturnsFalse()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("test");
            
            Assert.IsFalse(stream.EOS);
        }

        [Test]
        public void EOS_AfterReachingEnd_ReturnsTrue()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("ab");
            stream.ShiftNext();
            stream.ShiftNext();
            
            Assert.IsTrue(stream.EOS);
        }

        [Test]
        public void EOS_OnEmptyString_ReturnsTrue()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("");
            
            Assert.IsTrue(stream.EOS);
        }

        #endregion

        #region ShiftNext and Navigation Tests

        [Test]
        public void ShiftNext_IncrementsPosition()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("abc");
            stream.ShiftNext();
            
            Assert.AreEqual(1, stream.CurrentPosition);
            Assert.AreEqual('b', stream.Current);
        }

        [Test]
        public void Jump_SkipsMultipleCharacters()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("abcdef");
            stream.Jump(3);
            
            Assert.AreEqual(3, stream.CurrentPosition);
            Assert.AreEqual('d', stream.Current);
        }

        [Test]
        public void CurrentPosition_CanBeSetDirectly()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("abcdef");
            stream.CurrentPosition = 4;
            
            Assert.AreEqual('e', stream.Current);
        }

        #endregion

        #region Substring Tests

        [Test]
        public void Substring_FromCurrentPosition_ReturnsRemainingString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            stream.Jump(6);
            
            Assert.AreEqual("world", stream.Substring());
        }

        [Test]
        public void Substring_AtStart_ReturnsFullString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello");
            
            Assert.AreEqual("hello", stream.Substring());
        }

        [Test]
        public void Substring_AtEnd_ReturnsEmptyString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("a");
            stream.ShiftNext();
            
            Assert.AreEqual("", stream.Substring());
        }

        [Test]
        public void Substring_WithStartAndLength_ReturnsSubstring()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            
            Assert.AreEqual("world", stream.Substring(6, 5));
        }

        [Test]
        public void Substring_WithLengthExceedingEnd_TruncatesToEnd()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello");
            
            Assert.AreEqual("llo", stream.Substring(2, 100));
        }

        [Test]
        public void Substring_WithZeroLength_ReturnsEmptyString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello");
            
            Assert.AreEqual("", stream.Substring(0, 0));
        }

        #endregion

        #region Find Tests

        [Test]
        public void Find_StringExists_ReturnsRelativePosition()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            
            Assert.AreEqual(6, stream.Find("world"));
        }

        [Test]
        public void Find_StringNotFound_ReturnsNegative()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            
            Assert.Less(stream.Find("xyz"), 0);
        }

        [Test]
        public void Find_CharExists_ReturnsRelativePosition()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello");
            stream.ShiftNext();
            
            Assert.AreEqual(2, stream.Find('l'));
        }

        [Test]
        public void Find_CharNotFound_ReturnsNegative()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello");
            
            Assert.Less(stream.Find('z'), 0);
        }

        #endregion

        #region CurrentEqualTo Tests

        [Test]
        public void CurrentEqualTo_MatchingString_ReturnsTrue()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            
            Assert.IsTrue(stream.CurrentEqualTo("hello"));
        }

        [Test]
        public void CurrentEqualTo_NonMatchingString_ReturnsFalse()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            
            Assert.IsFalse(stream.CurrentEqualTo("world"));
        }

        [Test]
        public void CurrentEqualTo_StringTooLong_ReturnsFalse()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hi");
            
            Assert.IsFalse(stream.CurrentEqualTo("hello"));
        }

        [Test]
        public void CurrentEqualTo_WithIgnoreCase_MatchesCaseInsensitive()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("HELLO world");
            
            Assert.IsTrue(stream.CurrentEqualTo("hello", System.StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region TripleSymbol Tests

        [Test]
        public void TripleSymbol_WithThreeSameChars_ReturnsTrue()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("===test");
            
            Assert.IsTrue(stream.TripleSymbol());
        }

        [Test]
        public void TripleSymbol_WithDifferentChars_ReturnsFalse()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("=-=test");
            
            Assert.IsFalse(stream.TripleSymbol());
        }

        [Test]
        public void TripleSymbol_WithLessThanThreeChars_ReturnsFalse()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("==");
            
            Assert.IsFalse(stream.TripleSymbol());
        }

        #endregion

        #region Seek Tests

        [Test]
        public void Seek_FromBegin_SetsAbsolutePosition()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            stream.Seek(5, SeekOrigin.Begin);
            
            Assert.AreEqual(5, stream.CurrentPosition);
        }

        [Test]
        public void Seek_FromCurrent_MovesRelatively()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            stream.CurrentPosition = 3;
            stream.Seek(2, SeekOrigin.Current);
            
            Assert.AreEqual(5, stream.CurrentPosition);
        }

        [Test]
        public void Seek_FromEnd_MovesFromEnd()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            stream.Seek(-5, SeekOrigin.End);
            
            Assert.AreEqual(6, stream.CurrentPosition);
        }

        [Test]
        public void Seek_NegativeResult_ClampsToZero()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello");
            stream.Seek(-10, SeekOrigin.Begin);
            
            Assert.AreEqual(0, stream.CurrentPosition);
        }

        #endregion

        #region AppendString Tests

        [Test]
        public void AppendString_AppendsWithSpace()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello");
            stream.AppendString("world");
            
            Assert.AreEqual("hello world", stream.RowString);
        }

        #endregion

        #region Replace Tests

        [Test]
        public void Replace_ReplacesSubstring()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            stream.Replace(0, 5, "hi");
            
            Assert.AreEqual("hi world", stream.RowString);
        }

        [Test]
        public void Replace_ResetsPositionToStart()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            stream.CurrentPosition = 10;
            stream.Replace(0, 5, "hi");
            
            Assert.AreEqual(0, stream.CurrentPosition);
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_ReturnsFullString()
        {
            var stream = new MinorShift.Emuera.Sub.StringStream("hello world");
            stream.ShiftNext();
            
            Assert.AreEqual("hello world", stream.ToString());
        }

        #endregion
    }
}
