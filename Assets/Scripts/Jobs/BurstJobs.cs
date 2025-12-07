using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace uEmuera.Jobs
{
    /// <summary>
    /// Burst-compiled job for converting color values in batch.
    /// This provides significant performance improvements when processing many colors.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct ColorConversionJob : IJobParallelFor
    {
        /// <summary>
        /// Input colors as int ARGB values.
        /// </summary>
        [ReadOnly]
        public NativeArray<int> inputArgb;

        /// <summary>
        /// Output colors as float4 RGBA values.
        /// </summary>
        [WriteOnly]
        public NativeArray<float4> outputRgba;

        /// <summary>
        /// Executes the color conversion for a single index.
        /// </summary>
        /// <param name="index">The index to process.</param>
        public void Execute(int index)
        {
            int argb = inputArgb[index];
            
            // Extract components
            int a = (argb >> 24) & 0xFF;
            int r = (argb >> 16) & 0xFF;
            int g = (argb >> 8) & 0xFF;
            int b = argb & 0xFF;
            
            // Convert to float [0, 1]
            outputRgba[index] = new float4(
                r / 255.0f,
                g / 255.0f,
                b / 255.0f,
                a / 255.0f
            );
        }
    }

    /// <summary>
    /// Burst-compiled job for processing line positions in batch.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct LinePositionJob : IJobParallelFor
    {
        /// <summary>
        /// Base positions for each line (Y coordinate).
        /// </summary>
        [ReadOnly]
        public NativeArray<float> linePositionsY;

        /// <summary>
        /// Heights for each line.
        /// </summary>
        [ReadOnly]
        public NativeArray<float> lineHeights;

        /// <summary>
        /// Current scroll offset.
        /// </summary>
        public float2 scrollOffset;

        /// <summary>
        /// Display height for culling.
        /// </summary>
        public float displayHeight;

        /// <summary>
        /// Output visibility flags (1 = visible, 0 = culled).
        /// </summary>
        [WriteOnly]
        public NativeArray<int> visibilityFlags;

        /// <summary>
        /// Executes the position calculation and visibility check for a single line.
        /// </summary>
        /// <param name="index">The line index.</param>
        public void Execute(int index)
        {
            float lineY = linePositionsY[index];
            float lineHeight = lineHeights[index];
            
            // Check if line is visible in viewport
            bool isVisible = lineY <= scrollOffset.y + displayHeight &&
                           lineY + lineHeight >= scrollOffset.y;
            
            visibilityFlags[index] = isVisible ? 1 : 0;
        }
    }

    /// <summary>
    /// Burst-compiled job for rectangle intersection tests in batch.
    /// Useful for collision detection and UI element overlap checking.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public struct RectangleIntersectionJob : IJobParallelFor
    {
        /// <summary>
        /// Reference rectangle to test against.
        /// </summary>
        public float4 referenceRect; // x, y, width, height

        /// <summary>
        /// Test rectangles.
        /// </summary>
        [ReadOnly]
        public NativeArray<float4> testRects;

        /// <summary>
        /// Output intersection results (1 = intersects, 0 = no intersection).
        /// </summary>
        [WriteOnly]
        public NativeArray<int> intersectionResults;

        /// <summary>
        /// Executes the intersection test for a single rectangle.
        /// </summary>
        /// <param name="index">The rectangle index.</param>
        public void Execute(int index)
        {
            float4 testRect = testRects[index];
            
            float refLeft = referenceRect.x;
            float refRight = referenceRect.x + referenceRect.z;
            float refTop = referenceRect.y;
            float refBottom = referenceRect.y + referenceRect.w;
            
            float testLeft = testRect.x;
            float testRight = testRect.x + testRect.z;
            float testTop = testRect.y;
            float testBottom = testRect.y + testRect.w;
            
            bool intersects = !(testBottom <= refTop ||
                              testTop >= refBottom ||
                              testRight <= refLeft ||
                              testLeft >= refRight);
            
            intersectionResults[index] = intersects ? 1 : 0;
        }
    }
}
