using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace uEmuera.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for GenericUtils class which requires Unity runtime.
    /// These tests validate Unity-specific utilities like color conversion and coroutines.
    /// </summary>
    [TestFixture]
    public class GenericUtilsTests
    {
        #region ToUnityColor Tests

        [Test]
        public void ToUnityColor_ConvertsCorrectly()
        {
            var emueraColor = new uEmuera.Drawing.Color(128, 64, 32, 255);
            
            UnityEngine.Color unityColor = emueraColor.ToUnityColor();
            
            Assert.AreEqual(128 / 255f, unityColor.r, 0.01f);
            Assert.AreEqual(64 / 255f, unityColor.g, 0.01f);
            Assert.AreEqual(32 / 255f, unityColor.b, 0.01f);
            Assert.AreEqual(1f, unityColor.a, 0.01f);
        }

        [Test]
        public void ToUnityColor_Black_IsCorrect()
        {
            var emueraColor = uEmuera.Drawing.Color.Black;
            
            UnityEngine.Color unityColor = emueraColor.ToUnityColor();
            
            Assert.AreEqual(0f, unityColor.r, 0.01f);
            Assert.AreEqual(0f, unityColor.g, 0.01f);
            Assert.AreEqual(0f, unityColor.b, 0.01f);
        }

        [Test]
        public void ToUnityColor_White_IsCorrect()
        {
            var emueraColor = uEmuera.Drawing.Color.White;
            
            UnityEngine.Color unityColor = emueraColor.ToUnityColor();
            
            Assert.AreEqual(1f, unityColor.r, 0.01f);
            Assert.AreEqual(1f, unityColor.g, 0.01f);
            Assert.AreEqual(1f, unityColor.b, 0.01f);
        }

        #endregion

        #region GetColorCode Tests

        [Test]
        public void GetColorCode_ReturnsHexString()
        {
            var color = new uEmuera.Drawing.Color(255, 0, 0, 255);
            
            string colorCode = GenericUtils.GetColorCode(color);
            
            Assert.AreEqual("ff0000ff", colorCode);
        }

        [Test]
        public void GetColorCode_UnityColor_ReturnsHexString()
        {
            var color = new UnityEngine.Color(0f, 1f, 0f, 1f);
            
            string colorCode = GenericUtils.GetColorCode(color);
            
            Assert.AreEqual("00ff00ff", colorCode);
        }

        #endregion

        #region ToUnityRect Tests

        [Test]
        public void ToUnityRect_ConvertsCorrectly()
        {
            var emueraRect = new uEmuera.Drawing.Rectangle(10, 20, 100, 50);
            
            Rect unityRect = GenericUtils.ToUnityRect(emueraRect);
            
            Assert.AreEqual(10, unityRect.x);
            Assert.AreEqual(20, unityRect.y);
            Assert.AreEqual(100, unityRect.width);
            Assert.AreEqual(50, unityRect.height);
        }

        [Test]
        public void ToUnityRect_WithDimensions_ConvertsWithYFlip()
        {
            var emueraRect = new uEmuera.Drawing.Rectangle(10, 20, 100, 50);
            
            Rect unityRect = GenericUtils.ToUnityRect(emueraRect, 200, 200);
            
            Assert.AreEqual(10, unityRect.x);
            // Y is flipped: height - rectHeight - rectY = 200 - 50 - 20 = 130
            Assert.AreEqual(130, unityRect.y);
            Assert.AreEqual(100, unityRect.width);
            Assert.AreEqual(50, unityRect.height);
        }

        #endregion

        #region GetFilename Tests

        [Test]
        public void GetFilename_WithPath_ReturnsFilename()
        {
            string result = GenericUtils.GetFilename("FolderA/FolderB/Filename.txt");
            
            Assert.AreEqual("Filename.txt", result);
        }

        [Test]
        public void GetFilename_WithoutPath_ReturnsOriginal()
        {
            string result = GenericUtils.GetFilename("Filename.txt");
            
            Assert.AreEqual("Filename.txt", result);
        }

        [Test]
        public void GetFilename_EmptyString_ReturnsEmpty()
        {
            string result = GenericUtils.GetFilename("");
            
            Assert.AreEqual("", result);
        }

        #endregion

        #region Logger Tests

        [Test]
        public void Info_LogsMessage()
        {
            // This test verifies that the Info method doesn't throw
            Assert.DoesNotThrow(() => GenericUtils.Info("Test info message"));
        }

        [Test]
        public void Warn_LogsWarning()
        {
            // This test verifies that the Warn method doesn't throw
            Assert.DoesNotThrow(() => GenericUtils.Warn("Test warning message"));
        }

        [Test]
        public void Error_LogsError()
        {
            // This test verifies that the Error method doesn't throw
            LogAssert.ignoreFailingMessages = true;
            Assert.DoesNotThrow(() => GenericUtils.Error("Test error message"));
            LogAssert.ignoreFailingMessages = false;
        }

        #endregion
    }
}
