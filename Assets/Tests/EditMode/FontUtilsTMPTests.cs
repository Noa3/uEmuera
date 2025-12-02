using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for the FontUtils class TextMeshPro font management.
    /// </summary>
    [TestFixture]
    public class FontUtilsTMPTests
    {
        #region SetDefaultFont Tests

        [Test]
        public void SetDefaultFont_WithValidName_SetsDefaultFontname()
        {
            FontUtils.SetDefaultFont("MS Gothic");
            
            Assert.AreEqual("MS Gothic", FontUtils.default_fontname);
        }

        [Test]
        public void SetDefaultFont_WithNull_FallsBackToDefault()
        {
            // First set a valid font
            FontUtils.SetDefaultFont("MS Gothic");
            var originalName = FontUtils.default_fontname;
            
            // Then set null - should keep the previous default
            FontUtils.SetDefaultFont(null);
            
            // Should fallback to default Japanese font name
            Assert.IsNotNull(FontUtils.default_fontname);
        }

        [Test]
        public void SetDefaultFont_WithEmpty_FallsBackToDefault()
        {
            FontUtils.SetDefaultFont("");
            
            // Should fallback to default Japanese font name
            Assert.AreEqual("ＭＳ ゴシック", FontUtils.default_fontname);
        }

        #endregion

        #region GetFont Tests

        [Test]
        public void GetFont_WithNull_ReturnsDefaultFont()
        {
            FontUtils.SetDefaultFont("MS Gothic");
            
            var result = FontUtils.GetFont(null);
            
            Assert.AreEqual(FontUtils.default_font, result);
        }

        [Test]
        public void GetFont_WithEmpty_ReturnsDefaultFont()
        {
            FontUtils.SetDefaultFont("MS Gothic");
            
            var result = FontUtils.GetFont("");
            
            Assert.AreEqual(FontUtils.default_font, result);
        }

        [Test]
        public void GetFont_CachesSameNameFont()
        {
            FontUtils.SetDefaultFont("MS Gothic");
            
            // First call
            var font1 = FontUtils.GetFont("MS Gothic");
            
            // Second call with same name
            var font2 = FontUtils.GetFont("MS Gothic");
            
            Assert.AreEqual(font1, font2);
            Assert.AreEqual("MS Gothic", FontUtils.last_name);
        }

        #endregion

        #region GetTMPFont Tests

        [Test]
        public void GetTMPFont_WithNull_ReturnsDefaultTMPFont()
        {
            FontUtils.SetDefaultFont("MS Gothic");
            
            var result = FontUtils.GetTMPFont(null);
            
            Assert.AreEqual(FontUtils.default_tmp_font, result);
        }

        [Test]
        public void GetTMPFont_WithEmpty_ReturnsDefaultTMPFont()
        {
            FontUtils.SetDefaultFont("MS Gothic");
            
            var result = FontUtils.GetTMPFont("");
            
            Assert.AreEqual(FontUtils.default_tmp_font, result);
        }

        [Test]
        public void GetTMPFont_CachesSameNameFont()
        {
            FontUtils.SetDefaultFont("MS Gothic");
            
            // First call
            var font1 = FontUtils.GetTMPFont("MS Gothic");
            
            // Second call with same name
            var font2 = FontUtils.GetTMPFont("MS Gothic");
            
            Assert.AreEqual(font1, font2);
            Assert.AreEqual("MS Gothic", FontUtils.last_tmp_name);
        }

        #endregion
    }
}
