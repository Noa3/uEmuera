using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for the uEmuera.Utils utility class.
    /// </summary>
    [TestFixture]
    public class UtilsTests
    {
        #region NormalizePath Tests

        [Test]
        public void NormalizePath_WithForwardSlashes_NormalizesPath()
        {
            string result = uEmuera.Utils.NormalizePath("folder1/folder2/file.txt");
            
            Assert.AreEqual("folder1/folder2/file.txt", result);
        }

        [Test]
        public void NormalizePath_WithBackSlashes_ConvertsToForwardSlashes()
        {
            string result = uEmuera.Utils.NormalizePath("folder1\\folder2\\file.txt");
            
            Assert.AreEqual("folder1/folder2/file.txt", result);
        }

        [Test]
        public void NormalizePath_WithMixedSlashes_NormalizesToForwardSlashes()
        {
            string result = uEmuera.Utils.NormalizePath("folder1/folder2\\file.txt");
            
            Assert.AreEqual("folder1/folder2/file.txt", result);
        }

        [Test]
        public void NormalizePath_SingleFile_ReturnsFile()
        {
            string result = uEmuera.Utils.NormalizePath("file.txt");
            
            Assert.AreEqual("file.txt", result);
        }

        [Test]
        public void NormalizePath_EmptyPath_ReturnsEmptyString()
        {
            string result = uEmuera.Utils.NormalizePath("");
            
            Assert.AreEqual("", result);
        }

        #endregion

        #region GetSuffix Tests

        [Test]
        public void GetSuffix_WithExtension_ReturnsExtension()
        {
            string result = uEmuera.Utils.GetSuffix("file.txt");
            
            Assert.AreEqual("txt", result);
        }

        [Test]
        public void GetSuffix_WithMultipleDots_ReturnsLastPart()
        {
            string result = uEmuera.Utils.GetSuffix("file.test.csv");
            
            Assert.AreEqual("csv", result);
        }

        [Test]
        public void GetSuffix_WithoutExtension_ReturnsFilename()
        {
            string result = uEmuera.Utils.GetSuffix("filename");
            
            Assert.AreEqual("filename", result);
        }

        #endregion

        #region GetDisplayLength Tests

        [Test]
        public void GetDisplayLength_AsciiCharacters_ReturnsHalfWidth()
        {
            // ASCII characters should be half-width
            float fontSize = 16;
            string text = "abc";
            
            int result = uEmuera.Utils.GetDisplayLength(text, fontSize);
            
            // 3 characters * (16 / 2) = 24
            Assert.AreEqual(24, result);
        }

        [Test]
        public void GetDisplayLength_EmptyString_ReturnsZero()
        {
            int result = uEmuera.Utils.GetDisplayLength("", 16);
            
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetDisplayLength_FullWidthCharacters_ReturnsFullWidth()
        {
            // Japanese characters should be full-width (not in half-size set)
            float fontSize = 16;
            string text = "あいう"; // 3 Japanese hiragana characters
            
            int result = uEmuera.Utils.GetDisplayLength(text, fontSize);
            
            // 3 characters * 16 = 48
            Assert.AreEqual(48, result);
        }

        [Test]
        public void GetDisplayLength_MixedWidth_CalculatesCorrectly()
        {
            float fontSize = 16;
            string text = "aあ"; // 1 ASCII + 1 Japanese
            
            int result = uEmuera.Utils.GetDisplayLength(text, fontSize);
            
            // (1 * 8) + (1 * 16) = 24
            Assert.AreEqual(24, result);
        }

        #endregion

        #region CheckHalfSize Tests

        [Test]
        public void CheckHalfSize_AsciiCharacter_ReturnsTrue()
        {
            Assert.IsTrue(uEmuera.Utils.CheckHalfSize('A'));
            Assert.IsTrue(uEmuera.Utils.CheckHalfSize('z'));
            Assert.IsTrue(uEmuera.Utils.CheckHalfSize('0'));
            Assert.IsTrue(uEmuera.Utils.CheckHalfSize(' '));
        }

        [Test]
        public void CheckHalfSize_SpecialHalfWidthChar_ReturnsTrue()
        {
            Assert.IsTrue(uEmuera.Utils.CheckHalfSize('~'));
        }

        [Test]
        public void CheckHalfSize_JapaneseCharacter_ReturnsFalse()
        {
            Assert.IsFalse(uEmuera.Utils.CheckHalfSize('あ'));
        }

        #endregion

        #region GetByteCount Tests

        [Test]
        public void GetByteCount_NullString_ReturnsZero()
        {
            int result = uEmuera.Utils.GetByteCount(null);
            
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetByteCount_EmptyString_ReturnsZero()
        {
            int result = uEmuera.Utils.GetByteCount("");
            
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetByteCount_AsciiString_CountsAsOne()
        {
            int result = uEmuera.Utils.GetByteCount("abc");
            
            Assert.AreEqual(3, result);
        }

        [Test]
        public void GetByteCount_FullWidthString_CountsAsTwo()
        {
            int result = uEmuera.Utils.GetByteCount("あ");
            
            Assert.AreEqual(2, result);
        }

        [Test]
        public void GetByteCount_MixedString_CountsCorrectly()
        {
            int result = uEmuera.Utils.GetByteCount("aあb");
            
            // 1 + 2 + 1 = 4
            Assert.AreEqual(4, result);
        }

        #endregion
    }
}
