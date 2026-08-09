using NUnit.Framework;
using uEmuera.Drawing;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for the Color struct used throughout the uEmuera rendering system.
    /// </summary>
    [TestFixture]
    public class ColorTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_RGB_SetsCorrectValues()
        {
            var color = new Color(128, 64, 32);
            
            Assert.AreEqual(128, color.R);
            Assert.AreEqual(64, color.G);
            Assert.AreEqual(32, color.B);
            Assert.AreEqual(255, color.A);
        }

        [Test]
        public void Constructor_RGBA_SetsCorrectValues()
        {
            var color = new Color(100, 150, 200, 128);
            
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
            Assert.AreEqual(128, color.A);
        }

        [Test]
        public void Constructor_FloatValues_SetsCorrectValues()
        {
            var color = new Color(0.5f, 0.25f, 0.75f, 1.0f);
            
            Assert.AreEqual(127, color.R);
            Assert.AreEqual(63, color.G);
            Assert.AreEqual(191, color.B);
            Assert.AreEqual(255, color.A);
        }

        #endregion

        #region FromArgb Tests

        [Test]
        public void FromArgb_RGB_CreatesCorrectColor()
        {
            var color = Color.FromArgb(255, 128, 64);
            
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(128, color.G);
            Assert.AreEqual(64, color.B);
            Assert.AreEqual(255, color.A);
        }

        [Test]
        public void FromArgb_ARGB_CreatesCorrectColor()
        {
            var color = Color.FromArgb(128, 255, 100, 50);
            
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(100, color.G);
            Assert.AreEqual(50, color.B);
            Assert.AreEqual(128, color.A);
        }

        [Test]
        public void FromArgb_Integer_ParsesCorrectly()
        {
            // ARGB: 0xFF00FF00 = opaque green
            var color = Color.FromArgb(unchecked((int)0xFF00FF00));
            
            Assert.AreEqual(0, color.R);
            Assert.AreEqual(255, color.G);
            Assert.AreEqual(0, color.B);
            Assert.AreEqual(255, color.A);
        }

        #endregion

        #region ToArgb Tests

        [Test]
        public void ToArgb_ReturnsCorrectIntegerValue()
        {
            var color = Color.FromArgb(255, 0, 255, 0);
            
            int argb = color.ToArgb();
            
            Assert.AreEqual(unchecked((int)0xFF00FF00), argb);
        }

        [Test]
        public void ToArgb_RoundTrip_PreservesColor()
        {
            var original = Color.FromArgb(100, 150, 200, 50);
            int argb = original.ToArgb();
            var restored = Color.FromArgb(argb);
            
            Assert.AreEqual(original.R, restored.R);
            Assert.AreEqual(original.G, restored.G);
            Assert.AreEqual(original.B, restored.B);
            Assert.AreEqual(original.A, restored.A);
        }

        #endregion

        #region ToRGBA Tests

        [Test]
        public void ToRGBA_ReturnsCorrectIntegerValue()
        {
            var color = Color.FromArgb(255, 255, 0, 0);
            
            int rgba = color.ToRGBA();
            
            // R=255, G=0, B=0, A=255 -> 0xFF0000FF
            Assert.AreEqual(unchecked((int)0xFF0000FF), rgba);
        }

        #endregion

        #region Equality Tests

        [Test]
        public void Equality_SameColors_ReturnsTrue()
        {
            var color1 = new Color(100, 150, 200, 128);
            var color2 = new Color(100, 150, 200, 128);
            
            Assert.IsTrue(color1 == color2);
            Assert.AreEqual(color1, color2);
        }

        [Test]
        public void Equality_DifferentColors_ReturnsFalse()
        {
            var color1 = new Color(100, 150, 200, 128);
            var color2 = new Color(100, 150, 200, 129);
            
            Assert.IsFalse(color1 == color2);
            Assert.IsTrue(color1 != color2);
        }

        [Test]
        public void Equals_WithNonColorObject_ReturnsFalse()
        {
            var color = new Color(100, 150, 200, 128);
            
            Assert.IsFalse(color.Equals("not a color"));
        }

        #endregion

        #region Predefined Colors Tests

        [Test]
        public void Black_IsCorrect()
        {
            Assert.AreEqual(0, Color.Black.R);
            Assert.AreEqual(0, Color.Black.G);
            Assert.AreEqual(0, Color.Black.B);
            Assert.AreEqual(255, Color.Black.A);
        }

        [Test]
        public void White_IsCorrect()
        {
            Assert.AreEqual(255, Color.White.R);
            Assert.AreEqual(255, Color.White.G);
            Assert.AreEqual(255, Color.White.B);
            Assert.AreEqual(255, Color.White.A);
        }

        [Test]
        public void Red_IsCorrect()
        {
            Assert.AreEqual(255, Color.Red.R);
            Assert.AreEqual(0, Color.Red.G);
            Assert.AreEqual(0, Color.Red.B);
        }

        [Test]
        public void Green_IsCorrect()
        {
            Assert.AreEqual(0, Color.Green.R);
            Assert.AreEqual(128, Color.Green.G);
            Assert.AreEqual(0, Color.Green.B);
        }

        [Test]
        public void Blue_IsCorrect()
        {
            Assert.AreEqual(0, Color.Blue.R);
            Assert.AreEqual(0, Color.Blue.G);
            Assert.AreEqual(255, Color.Blue.B);
        }

        [Test]
        public void Gray_EqualsGrey()
        {
            Assert.AreEqual(Color.Gray, Color.Grey);
        }

        [Test]
        public void Transparent_HasZeroAlpha()
        {
            Assert.AreEqual(0, Color.Transparent.A);
        }

        #endregion

        #region FromName Tests

        [Test]
        public void FromName_Black_ReturnsBlack()
        {
            var color = Color.FromName("Black");
            Assert.AreEqual(Color.Black, color);
        }

        [Test]
        public void FromName_White_ReturnsWhite()
        {
            var color = Color.FromName("White");
            Assert.AreEqual(Color.White, color);
        }

        [Test]
        public void FromName_Red_ReturnsRed()
        {
            var color = Color.FromName("Red");
            Assert.AreEqual(Color.Red, color);
        }

        [Test]
        public void FromName_Green_ReturnsGreen()
        {
            var color = Color.FromName("Green");
            Assert.AreEqual(Color.Green, color);
        }

        [Test]
        public void FromName_Blue_ReturnsBlue()
        {
            var color = Color.FromName("Blue");
            Assert.AreEqual(Color.Blue, color);
        }

        [Test]
        public void FromName_Gray_ReturnsGray()
        {
            var color = Color.FromName("Gray");
            Assert.AreEqual(Color.Gray, color);
        }

        [Test]
        public void FromName_Grey_ReturnsGrey()
        {
            var color = Color.FromName("Grey");
            Assert.AreEqual(Color.Grey, color);
        }

        [Test]
        public void FromName_Unknown_ReturnsBlack()
        {
            var color = Color.FromName("UnknownColor");
            Assert.AreEqual(Color.Black, color);
        }

        [Test]
        public void FromName_Yellow_ReturnsYellow()
        {
            var color = Color.FromName("Yellow");
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(255, color.G);
            Assert.AreEqual(0, color.B);
        }

        [Test]
        public void FromName_CaseInsensitive_ReturnsSameColor()
        {
            Assert.AreEqual(Color.FromName("Yellow"), Color.FromName("yellow"));
            Assert.AreEqual(Color.FromName("Yellow"), Color.FromName("YELLOW"));
            Assert.AreEqual(Color.FromName("DeepSkyBlue"), Color.FromName("deepskyblue"));
        }

        [Test]
        public void FromName_ExtendedKnownColors_Resolve()
        {
            // .NET known-color values, matching reference Emuera behavior
            var orange = Color.FromName("Orange");
            Assert.AreEqual(255, orange.R);
            Assert.AreEqual(165, orange.G);
            Assert.AreEqual(0, orange.B);

            var silver = Color.FromName("Silver");
            Assert.AreEqual(192, silver.R);
            Assert.AreEqual(192, silver.G);
            Assert.AreEqual(192, silver.B);

            var navy = Color.FromName("Navy");
            Assert.AreEqual(0, navy.R);
            Assert.AreEqual(0, navy.G);
            Assert.AreEqual(128, navy.B);

            var fuchsia = Color.FromName("Fuchsia");
            Assert.AreEqual(255, fuchsia.R);
            Assert.AreEqual(0, fuchsia.G);
            Assert.AreEqual(255, fuchsia.B);
        }

        [Test]
        public void FromName_AllReturnOpaque()
        {
            // Only Transparent is allowed to have zero alpha; every other known
            // color must be opaque or HtmlManager.stringToColorInt32 would treat it
            // as an invalid color name.
            Assert.AreEqual(255, Color.FromName("Yellow").A);
            Assert.AreEqual(255, Color.FromName("DeepSkyBlue").A);
            Assert.AreEqual(255, Color.FromName("Crimson").A);
            Assert.AreEqual(0, Color.FromName("Transparent").A);
        }

        [Test]
        public void FromName_Red_StillMatchesConstant()
        {
            Assert.AreEqual(Color.Red, Color.FromName("Red"));
        }

        #endregion
    }
}
