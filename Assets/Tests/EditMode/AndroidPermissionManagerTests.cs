using NUnit.Framework;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for the AndroidPermissionManager class.
    /// These tests verify the behavior on non-Android platforms (Editor).
    /// </summary>
    [TestFixture]
    public class AndroidPermissionManagerTests
    {
        #region HasStoragePermission Tests (Non-Android)

        [Test]
        public void HasStorageReadPermission_OnNonAndroid_ReturnsTrue()
        {
            // On non-Android platforms, permissions are always granted
            bool result = AndroidPermissionManager.HasStorageReadPermission();
            
            Assert.IsTrue(result);
        }

        [Test]
        public void HasStorageWritePermission_OnNonAndroid_ReturnsTrue()
        {
            // On non-Android platforms, permissions are always granted
            bool result = AndroidPermissionManager.HasStorageWritePermission();
            
            Assert.IsTrue(result);
        }

        [Test]
        public void HasStoragePermissions_OnNonAndroid_ReturnsTrue()
        {
            // On non-Android platforms, permissions are always granted
            bool result = AndroidPermissionManager.HasStoragePermissions();
            
            Assert.IsTrue(result);
        }
        
        [Test]
        public void HasManageExternalStoragePermission_OnNonAndroid_ReturnsTrue()
        {
            // On non-Android platforms, permissions are always granted
            bool result = AndroidPermissionManager.HasManageExternalStoragePermission();
            
            Assert.IsTrue(result);
        }

        #endregion

        #region ShouldShowPermissionRationale Tests (Non-Android)

        [Test]
        public void ShouldShowPermissionRationale_OnNonAndroid_ReturnsFalse()
        {
            // On non-Android platforms, we never show rationale
            bool result = AndroidPermissionManager.ShouldShowPermissionRationale();
            
            Assert.IsFalse(result);
        }

        #endregion

        #region IsAndroid Tests

        [Test]
        public void IsAndroid_OnEditor_ReturnsFalse()
        {
            // In editor tests, IsAndroid should return false
            bool result = AndroidPermissionManager.IsAndroid();
            
            Assert.IsFalse(result);
        }

        #endregion

        #region GetAndroidSDKVersion Tests

        [Test]
        public void GetAndroidSDKVersion_OnNonAndroid_ReturnsZero()
        {
            // On non-Android platforms, SDK version is 0
            int result = AndroidPermissionManager.GetAndroidSDKVersion();
            
            Assert.AreEqual(0, result);
        }

        #endregion

        #region IsAndroid10OrHigher Tests

        [Test]
        public void IsAndroid10OrHigher_OnNonAndroid_ReturnsFalse()
        {
            // On non-Android platforms, this should return false
            bool result = AndroidPermissionManager.IsAndroid10OrHigher();
            
            Assert.IsFalse(result);
        }

        #endregion

        #region IsAndroid11OrHigher Tests

        [Test]
        public void IsAndroid11OrHigher_OnNonAndroid_ReturnsFalse()
        {
            // On non-Android platforms, this should return false
            bool result = AndroidPermissionManager.IsAndroid11OrHigher();
            
            Assert.IsFalse(result);
        }

        #endregion

        #region RequestStoragePermissions Tests (Non-Android)

        [Test]
        public void RequestStoragePermissions_OnNonAndroid_InvokesCallbackWithTrue()
        {
            bool? callbackResult = null;
            
            AndroidPermissionManager.RequestStoragePermissions((granted) =>
            {
                callbackResult = granted;
            });
            
            Assert.IsNotNull(callbackResult);
            Assert.IsTrue(callbackResult.Value);
        }

        [Test]
        public void RequestStoragePermissions_WithNullCallback_DoesNotThrow()
        {
            // Should not throw even with null callback
            Assert.DoesNotThrow(() =>
            {
                AndroidPermissionManager.RequestStoragePermissions(null);
            });
        }

        #endregion

        #region Constant Values Tests

        [Test]
        public void ExternalStorageRead_HasCorrectValue()
        {
            Assert.AreEqual("android.permission.READ_EXTERNAL_STORAGE", 
                AndroidPermissionManager.ExternalStorageRead);
        }

        [Test]
        public void ExternalStorageWrite_HasCorrectValue()
        {
            Assert.AreEqual("android.permission.WRITE_EXTERNAL_STORAGE", 
                AndroidPermissionManager.ExternalStorageWrite);
        }
        
        [Test]
        public void ManageExternalStorage_HasCorrectValue()
        {
            Assert.AreEqual("android.permission.MANAGE_EXTERNAL_STORAGE", 
                AndroidPermissionManager.ManageExternalStorage);
        }

        #endregion
    }
}
