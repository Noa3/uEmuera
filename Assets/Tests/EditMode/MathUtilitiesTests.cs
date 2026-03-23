using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for MathUtilities Burst-compiled functions.
    /// </summary>
    [TestFixture]
    public class MathUtilitiesTests
    {
        #region LimitPosition Tests

        [Test]
        public void LimitPosition_ContentSmallerThanDisplay_CentersHorizontally()
        {
            float2 position = new float2(100, 0);
            float2 displaySize = new float2(1000, 500);
            float2 contentSize = new float2(500, 1000);
            float offsetHeight = 0;

            var result = MathUtilities.LimitPosition(position, displaySize, contentSize, offsetHeight);

            Assert.AreEqual(0, result.x, "X should be centered at 0 when content is smaller");
        }

        [Test]
        public void LimitPosition_ContentLargerThanDisplay_ClampsHorizontally()
        {
            float2 position = new float2(100, 0);
            float2 displaySize = new float2(500, 500);
            float2 contentSize = new float2(1000, 1000);
            float offsetHeight = 0;

            var result = MathUtilities.LimitPosition(position, displaySize, contentSize, offsetHeight);

            Assert.AreEqual(0, result.x, "X should be clamped to 0 when trying to scroll right beyond content");
        }

        [Test]
        public void LimitPosition_NegativePosition_ClampsToMinimum()
        {
            float2 position = new float2(-600, 0);
            float2 displaySize = new float2(500, 500);
            float2 contentSize = new float2(1000, 1000);
            float offsetHeight = 0;

            var result = MathUtilities.LimitPosition(position, displaySize, contentSize, offsetHeight);

            Assert.AreEqual(-500, result.x, "X should be clamped to display width - content width");
        }

        [Test]
        public void LimitPosition_VerticalWithOffset_RespectsOffset()
        {
            float2 position = new float2(0, -100);
            float2 displaySize = new float2(500, 500);
            float2 contentSize = new float2(500, 1000);
            float offsetHeight = 100;

            var result = MathUtilities.LimitPosition(position, displaySize, contentSize, offsetHeight);

            Assert.GreaterOrEqual(result.y, offsetHeight, "Y should respect offset height");
        }

        #endregion

        #region ApplyDragDamping Tests

        [Test]
        public void ApplyDragDamping_ZeroVelocity_ReturnsZero()
        {
            float2 velocity = float2.zero;
            float dragAmount = 300f;
            float deltaTime = 0.016f;

            var result = MathUtilities.ApplyDragDamping(velocity, dragAmount, deltaTime);

            Assert.AreEqual(float2.zero, result);
        }

        [Test]
        public void ApplyDragDamping_ReducesVelocity()
        {
            float2 velocity = new float2(100, 0);
            float dragAmount = 300f;
            float deltaTime = 0.016f;

            var result = MathUtilities.ApplyDragDamping(velocity, dragAmount, deltaTime);

            Assert.Less(math.length(result), math.length(velocity), "Velocity should be reduced");
        }

        [Test]
        public void ApplyDragDamping_MaintainsDirection()
        {
            float2 velocity = new float2(100, 100);
            float dragAmount = 50f;
            float deltaTime = 0.016f;

            var result = MathUtilities.ApplyDragDamping(velocity, dragAmount, deltaTime);

            float2 velocityNorm = math.normalize(velocity);
            float2 resultNorm = math.normalize(result);
            
            Assert.AreEqual(velocityNorm.x, resultNorm.x, 0.01f, "Direction X should be maintained");
            Assert.AreEqual(velocityNorm.y, resultNorm.y, 0.01f, "Direction Y should be maintained");
        }

        #endregion

        #region Distance Tests

        [Test]
        public void Distance_SamePoint_ReturnsZero()
        {
            float2 a = new float2(100, 100);
            float2 b = new float2(100, 100);

            float distance = MathUtilities.Distance(a, b);

            Assert.AreEqual(0, distance, 0.001f);
        }

        [Test]
        public void Distance_UnitDistance_ReturnsOne()
        {
            float2 a = new float2(0, 0);
            float2 b = new float2(1, 0);

            float distance = MathUtilities.Distance(a, b);

            Assert.AreEqual(1, distance, 0.001f);
        }

        [Test]
        public void Distance_PythagoreanTriple_ReturnsCorrectValue()
        {
            float2 a = new float2(0, 0);
            float2 b = new float2(3, 4);

            float distance = MathUtilities.Distance(a, b);

            Assert.AreEqual(5, distance, 0.001f);
        }

        #endregion

        #region Clamp Tests

        [Test]
        public void Clamp_ValueWithinBounds_ReturnsValue()
        {
            float2 value = new float2(50, 50);
            float2 min = new float2(0, 0);
            float2 max = new float2(100, 100);

            var result = MathUtilities.Clamp(value, min, max);

            Assert.AreEqual(value, result);
        }

        [Test]
        public void Clamp_ValueBelowMin_ReturnsMin()
        {
            float2 value = new float2(-10, -10);
            float2 min = new float2(0, 0);
            float2 max = new float2(100, 100);

            var result = MathUtilities.Clamp(value, min, max);

            Assert.AreEqual(min, result);
        }

        [Test]
        public void Clamp_ValueAboveMax_ReturnsMax()
        {
            float2 value = new float2(150, 150);
            float2 min = new float2(0, 0);
            float2 max = new float2(100, 100);

            var result = MathUtilities.Clamp(value, min, max);

            Assert.AreEqual(max, result);
        }

        #endregion

        #region Lerp Tests

        [Test]
        public void Lerp_AtStart_ReturnsA()
        {
            float2 a = new float2(0, 0);
            float2 b = new float2(100, 100);

            var result = MathUtilities.Lerp(a, b, 0);

            Assert.AreEqual(a, result);
        }

        [Test]
        public void Lerp_AtEnd_ReturnsB()
        {
            float2 a = new float2(0, 0);
            float2 b = new float2(100, 100);

            var result = MathUtilities.Lerp(a, b, 1);

            Assert.AreEqual(b, result);
        }

        [Test]
        public void Lerp_Midpoint_ReturnsAverage()
        {
            float2 a = new float2(0, 0);
            float2 b = new float2(100, 100);

            var result = MathUtilities.Lerp(a, b, 0.5f);

            Assert.AreEqual(50, result.x, 0.001f);
            Assert.AreEqual(50, result.y, 0.001f);
        }

        #endregion
    }
}
