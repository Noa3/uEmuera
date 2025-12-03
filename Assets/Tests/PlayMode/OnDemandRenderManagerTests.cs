using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace uEmuera.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for OnDemandRenderManager class.
    /// Tests the on-demand rendering optimization functionality.
    /// </summary>
    [TestFixture]
    public class OnDemandRenderManagerTests
    {
        private GameObject test_game_object_;
        private OnDemandRenderManager render_manager_;

        [SetUp]
        public void Setup()
        {
            test_game_object_ = new GameObject("TestOnDemandRenderManager");
            render_manager_ = test_game_object_.AddComponent<OnDemandRenderManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (test_game_object_ != null)
            {
                Object.Destroy(test_game_object_);
            }
        }

        #region Singleton Tests

        [Test]
        public void Instance_AfterCreation_IsNotNull()
        {
            Assert.IsNotNull(OnDemandRenderManager.instance);
        }

        [Test]
        public void Instance_ReturnsSameInstance()
        {
            Assert.AreEqual(render_manager_, OnDemandRenderManager.instance);
        }

        #endregion

        #region SetContentDirty Tests

        [Test]
        public void SetContentDirty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => render_manager_.SetContentDirty());
        }

        [Test]
        public void SetContentDirty_CalledMultipleTimes_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                render_manager_.SetContentDirty();
                render_manager_.SetContentDirty();
                render_manager_.SetContentDirty();
            });
        }

        #endregion

        #region RequestRender Tests

        [Test]
        public void RequestRender_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => render_manager_.RequestRender());
        }

        [Test]
        public void RequestRenderFrames_WithPositiveValue_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => render_manager_.RequestRenderFrames(10));
        }

        [Test]
        public void RequestRenderFrames_WithZero_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => render_manager_.RequestRenderFrames(0));
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void DefaultValues_AreReasonable()
        {
            Assert.IsTrue(render_manager_.idleFrameInterval >= 1);
            Assert.IsTrue(render_manager_.idleFrameInterval <= 60);
            Assert.IsTrue(render_manager_.activeFrameInterval >= 1);
            Assert.IsTrue(render_manager_.activeFrameCount >= 1);
            Assert.IsTrue(render_manager_.mouseMovementThreshold > 0);
        }

        [Test]
        public void OnDemandEnabled_DefaultTrue()
        {
            Assert.IsTrue(render_manager_.OnDemandEnabled);
        }

        [Test]
        public void OnDemandEnabled_CanBeDisabled()
        {
            render_manager_.OnDemandEnabled = false;
            Assert.IsFalse(render_manager_.OnDemandEnabled);
        }

        #endregion

        #region Update Loop Tests

        [UnityTest]
        public IEnumerator Update_WhenDisabled_DoesNotThrow()
        {
            render_manager_.OnDemandEnabled = false;
            yield return null; // Wait one frame for Update to run
            // Test passes if no exception was thrown
        }

        [UnityTest]
        public IEnumerator Update_WhenEnabled_DoesNotThrow()
        {
            render_manager_.OnDemandEnabled = true;
            yield return null; // Wait one frame for Update to run
            // Test passes if no exception was thrown
        }

        [UnityTest]
        public IEnumerator SetContentDirty_TriggersActiveRendering()
        {
            render_manager_.OnDemandEnabled = true;
            render_manager_.SetContentDirty();
            yield return null; // Wait one frame for Update to run
            
            // Verify that frame interval is set to active (low value)
            Assert.AreEqual(render_manager_.activeFrameInterval, 
                UnityEngine.Rendering.OnDemandRendering.renderFrameInterval);
        }

        #endregion
    }
}
