using System;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace uEmuera
{
    /// <summary>
    /// High-performance mathematical utilities using Unity.Mathematics and Burst compilation.
    /// </summary>
    public static class MathUtilities
    {
        /// <summary>
        /// Converts a uEmuera.Drawing.Color to UnityEngine.Color using Burst-compiled SIMD operations.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The Unity color.</returns>
        [BurstCompile]
        public static UnityEngine.Color ToUnityColor(float4 rgba)
        {
            return new UnityEngine.Color(rgba.x, rgba.y, rgba.z, rgba.w);
        }

        /// <summary>
        /// Clamps a float2 value between min and max bounds.
        /// </summary>
        [BurstCompile]
        public static float2 Clamp(float2 value, float2 min, float2 max)
        {
            return math.clamp(value, min, max);
        }

        /// <summary>
        /// Linearly interpolates between two float2 values.
        /// </summary>
        [BurstCompile]
        public static float2 Lerp(float2 a, float2 b, float t)
        {
            return math.lerp(a, b, t);
        }

        /// <summary>
        /// Calculates the distance between two float2 points.
        /// </summary>
        [BurstCompile]
        public static float Distance(float2 a, float2 b)
        {
            return math.distance(a, b);
        }

        /// <summary>
        /// Applies drag damping to a velocity vector.
        /// </summary>
        [BurstCompile]
        public static float2 ApplyDragDamping(float2 velocity, float dragAmount, float deltaTime)
        {
            float magnitude = math.length(velocity);
            if (magnitude <= 0)
                return float2.zero;
            
            float newMagnitude = math.max(0, magnitude - dragAmount * deltaTime);
            return velocity * (newMagnitude / magnitude);
        }

        /// <summary>
        /// Limits a position within display bounds.
        /// </summary>
        [BurstCompile]
        public static float2 LimitPosition(float2 position, float2 displaySize, float2 contentSize, float offsetHeight)
        {
            float2 result = position;
            
            // Horizontal bounds
            if (contentSize.x > displaySize.x)
            {
                result.x = math.clamp(result.x, displaySize.x - contentSize.x, 0);
            }
            else
            {
                result.x = 0;
            }

            // Vertical bounds
            float validHeight = contentSize.y - offsetHeight;
            if (offsetHeight > 0 && validHeight < displaySize.y)
            {
                result.y = offsetHeight;
            }
            else
            {
                float displayDelta = contentSize.y - displaySize.y;
                if (contentSize.y <= displaySize.y)
                {
                    result.y = displayDelta;
                }
                else
                {
                    result.y = math.clamp(result.y, offsetHeight, displayDelta);
                }
            }

            return result;
        }

        /// <summary>
        /// Performs a binary search to find a line index by line number.
        /// NOTE: This is a placeholder for future implementation when line storage
        /// is converted to NativeArray. Currently not used.
        /// </summary>
        [System.Obsolete("Not yet implemented - requires NativeArray-based line storage")]
        [BurstCompile]
        public static int BinarySearchLineNo(int lineNo, int beginIndex, int endIndex, int maxLogCount)
        {
            // This requires access to console_lines_ array converted to NativeArray
            // Left as placeholder for future optimization when line storage is refactored
            throw new NotImplementedException("Binary search requires NativeArray-based line storage");
        }
    }
}
