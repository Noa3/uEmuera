using NUnit.Framework;
using uEmuera.Drawing;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for the Font class used in text rendering.
    /// </summary>
    [TestFixture]
    public class FontTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_Basic_SetsProperties()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.AreEqual("MS Gothic", font.FontFamily.Name);
            Assert.AreEqual(12f, font.Size);
            Assert.AreEqual(FontStyle.Regular, font.Style);
            Assert.AreEqual(GraphicsUnit.Pixel, font.Unit);
        }

        [Test]
        public void Constructor_WithGdiCharSet_SetsProperties()
        {
            var font = new Font("MS Gothic", 14f, FontStyle.Bold, GraphicsUnit.Point, 0);
            
            Assert.AreEqual("MS Gothic", font.FontFamily.Name);
            Assert.AreEqual(14f, font.Size);
            Assert.AreEqual(FontStyle.Bold, font.Style);
        }

        [Test]
        public void Constructor_WithAllParameters_SetsProperties()
        {
            var font = new Font("MS Gothic", 16f, FontStyle.Italic, GraphicsUnit.Display, 0, false);
            
            Assert.AreEqual("MS Gothic", font.FontFamily.Name);
            Assert.AreEqual(16f, font.Size);
            Assert.AreEqual(FontStyle.Italic, font.Style);
        }

        #endregion

        #region Monospaced Tests

        [Test]
        public void Monospaced_MSGothic_ReturnsTrue()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.IsTrue(font.Monospaced);
        }

        [Test]
        public void Monospaced_MSPGothic_ReturnsFalse()
        {
            var font = new Font("MS PGothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.IsFalse(font.Monospaced);
        }

        [Test]
        public void Monospaced_MSPGothicJapanese_ReturnsFalse()
        {
            var font = new Font("ＭＳ Ｐゴシック", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.IsFalse(font.Monospaced);
        }

        #endregion

        #region Style Property Tests

        [Test]
        public void Bold_WhenStyleIncludesBold_ReturnsTrue()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
            
            Assert.IsTrue(font.Bold);
        }

        [Test]
        public void Bold_WhenStyleDoesNotIncludeBold_ReturnsFalse()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.IsFalse(font.Bold);
        }

        [Test]
        public void Italic_WhenStyleIncludesItalic_ReturnsTrue()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Italic, GraphicsUnit.Pixel);
            
            Assert.IsTrue(font.Italic);
        }

        [Test]
        public void Italic_WhenStyleDoesNotIncludeItalic_ReturnsFalse()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.IsFalse(font.Italic);
        }

        [Test]
        public void Underline_WhenStyleIncludesUnderline_ReturnsTrue()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Underline, GraphicsUnit.Pixel);
            
            Assert.IsTrue(font.Underline);
        }

        [Test]
        public void Underline_WhenStyleDoesNotIncludeUnderline_ReturnsFalse()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.IsFalse(font.Underline);
        }

        [Test]
        public void CombinedStyles_AllPropertiesCorrect()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel);
            
            Assert.IsTrue(font.Bold);
            Assert.IsTrue(font.Italic);
            Assert.IsFalse(font.Underline);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_DoesNotThrow()
        {
            var font = new Font("MS Gothic", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            
            Assert.DoesNotThrow(() => font.Dispose());
        }

        #endregion
    }

    /// <summary>
    /// Tests for the FontFamily class.
    /// </summary>
    [TestFixture]
    public class FontFamilyTests
    {
        [Test]
        public void Constructor_SetsName()
        {
            var fontFamily = new FontFamily("Arial");
            
            Assert.AreEqual("Arial", fontFamily.Name);
        }
    }

    /// <summary>
    /// Tests for the FontStyle enum.
    /// </summary>
    [TestFixture]
    public class FontStyleTests
    {
        [Test]
        public void FontStyle_CanBeCombined()
        {
            var combinedStyle = FontStyle.Bold | FontStyle.Italic | FontStyle.Underline;
            
            Assert.IsTrue((combinedStyle & FontStyle.Bold) != 0);
            Assert.IsTrue((combinedStyle & FontStyle.Italic) != 0);
            Assert.IsTrue((combinedStyle & FontStyle.Underline) != 0);
            Assert.IsFalse((combinedStyle & FontStyle.Strikeout) != 0);
        }

        [Test]
        public void FontStyle_Regular_HasValueZero()
        {
            Assert.AreEqual(0, (int)FontStyle.Regular);
        }
    }
}
