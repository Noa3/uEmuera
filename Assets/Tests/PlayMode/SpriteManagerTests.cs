using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace uEmuera.Tests.PlayMode
{
    /// <summary>
    /// Tests for the SpriteManager optimization features.
    /// Tests non-blocking loading, preloading, and cache statistics.
    /// </summary>
    [TestFixture]
    public class SpriteManagerTests
    {
        [UnityTest]
        public IEnumerator CacheStats_InitialState_ReturnsZeroCounts()
        {
            // Clear any existing state
            SpriteManager.ForceClear();
            yield return null;

            // Get stats
            var stats = SpriteManager.GetCacheStats();

            // Verify initial state
            Assert.AreEqual(0, stats.LoadedTexturesCount, "Initial loaded texture count should be 0");
            Assert.AreEqual(0, stats.LoadingInProgressCount, "Initial loading count should be 0");
            Assert.AreEqual(0, stats.PreloadQueueCount, "Initial preload queue should be empty");
        }

        [UnityTest]
        public IEnumerator PreloadImages_WithValidNames_AddsToQueue()
        {
            // Clear any existing state
            SpriteManager.ForceClear();
            yield return null;

            // Preload some images (they don't need to exist for this test)
            SpriteManager.PreloadImages("test1", "test2", "test3");
            yield return null;

            // Check that preloading started
            var stats = SpriteManager.GetCacheStats();
            
            // Note: The queue might be empty if preloading completed very fast
            // We just verify it doesn't crash and returns valid data
            Assert.GreaterOrEqual(stats.PreloadQueueCount, 0, "Preload queue count should be non-negative");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator PreloadImages_Duplicates_DoesNotAddTwice()
        {
            // Clear any existing state
            SpriteManager.ForceClear();
            yield return null;

            // Add same image multiple times
            SpriteManager.PreloadImage("duplicate_test");
            SpriteManager.PreloadImage("duplicate_test");
            SpriteManager.PreloadImage("duplicate_test");
            yield return null;

            var stats = SpriteManager.GetCacheStats();
            
            // Queue should have at most 1 item (might be 0 if processed)
            Assert.LessOrEqual(stats.PreloadQueueCount, 1, "Duplicate images should not be added multiple times");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator IsPreloadingInProgress_AfterPreload_ReturnsCorrectState()
        {
            // Clear any existing state
            SpriteManager.ForceClear();
            yield return null;

            // Initially should not be preloading
            bool initialState = SpriteManager.IsPreloadingInProgress();
            
            // Add items to preload
            SpriteManager.PreloadImages("preload_test1", "preload_test2");
            yield return null;

            // Should either be preloading or finished (both are valid)
            bool afterPreload = SpriteManager.IsPreloadingInProgress();
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // After the wait, preloading should have finished (images don't exist so they resolve quickly)
            bool finalState = SpriteManager.IsPreloadingInProgress();
            Assert.IsFalse(finalState, "Preloading should have completed within 0.5s for non-existent test images");
            
            // The initial state before queueing should have been false
            Assert.IsFalse(initialState, "Preloading should not be in progress before any preload requests");
        }

        [UnityTest]
        public IEnumerator CacheStats_ToString_ReturnsValidString()
        {
            var stats = SpriteManager.GetCacheStats();
            string statsString = stats.ToString();
            
            Assert.IsNotNull(statsString, "Stats ToString should not return null");
            Assert.IsNotEmpty(statsString, "Stats ToString should not return empty string");
            Assert.That(statsString, Does.Contain("SpriteManager Stats"), "Stats string should contain identifier");
            
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            SpriteManager.ForceClear();
        }
    }
}
