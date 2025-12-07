using Unity.Mathematics;

namespace uEmuera.Data
{
    /// <summary>
    /// Immutable display configuration using C# 9.0 records.
    /// Provides efficient value-based equality and immutability.
    /// </summary>
    public readonly record struct DisplayConfig
    {
        /// <summary>
        /// Display width in pixels.
        /// </summary>
        public float Width { get; init; }

        /// <summary>
        /// Display height in pixels.
        /// </summary>
        public float Height { get; init; }

        /// <summary>
        /// Content width in pixels.
        /// </summary>
        public float ContentWidth { get; init; }

        /// <summary>
        /// Content height in pixels.
        /// </summary>
        public float ContentHeight { get; init; }

        /// <summary>
        /// Offset height for scrolling.
        /// </summary>
        public float OffsetHeight { get; init; }

        /// <summary>
        /// Gets the display size as a float2.
        /// </summary>
        public float2 DisplaySize => new float2(Width, Height);

        /// <summary>
        /// Gets the content size as a float2.
        /// </summary>
        public float2 ContentSize => new float2(ContentWidth, ContentHeight);

        /// <summary>
        /// Checks if content is wider than display (requires horizontal scrolling).
        /// </summary>
        public bool RequiresHorizontalScroll => ContentWidth > Width;

        /// <summary>
        /// Checks if content is taller than display (requires vertical scrolling).
        /// </summary>
        public bool RequiresVerticalScroll => ContentHeight > Height;
    }

    /// <summary>
    /// Immutable viewport bounds using C# 9.0 records.
    /// </summary>
    public readonly record struct ViewportBounds
    {
        /// <summary>
        /// Top position.
        /// </summary>
        public float Top { get; init; }

        /// <summary>
        /// Bottom position.
        /// </summary>
        public float Bottom { get; init; }

        /// <summary>
        /// Left position.
        /// </summary>
        public float Left { get; init; }

        /// <summary>
        /// Right position.
        /// </summary>
        public float Right { get; init; }

        /// <summary>
        /// Gets the width of the bounds.
        /// </summary>
        public float Width => Right - Left;

        /// <summary>
        /// Gets the height of the bounds.
        /// </summary>
        public float Height => Bottom - Top;

        /// <summary>
        /// Gets the center position as a float2.
        /// </summary>
        public float2 Center => new float2((Left + Right) / 2, (Top + Bottom) / 2);

        /// <summary>
        /// Checks if a point is within the bounds.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if the point is within bounds.</returns>
        public bool Contains(float2 point)
        {
            return point.x >= Left && point.x <= Right &&
                   point.y >= Top && point.y <= Bottom;
        }

        /// <summary>
        /// Checks if this bounds intersects with another.
        /// </summary>
        /// <param name="other">The other bounds to check.</param>
        /// <returns>True if the bounds intersect.</returns>
        public bool Intersects(ViewportBounds other)
        {
            return !(other.Bottom <= Top ||
                    other.Top >= Bottom ||
                    other.Right <= Left ||
                    other.Left >= Right);
        }
    }

    /// <summary>
    /// Immutable line descriptor using C# 9.0 records.
    /// Represents a single line of text in the console.
    /// </summary>
    public readonly record struct LineDescriptor
    {
        /// <summary>
        /// Line number.
        /// </summary>
        public int LineNo { get; init; }

        /// <summary>
        /// Y position of the line.
        /// </summary>
        public float PositionY { get; init; }

        /// <summary>
        /// Height of the line.
        /// </summary>
        public float Height { get; init; }

        /// <summary>
        /// Whether this is a logical line (vs wrapped line).
        /// </summary>
        public bool IsLogicalLine { get; init; }

        /// <summary>
        /// Gets the bottom position of the line.
        /// </summary>
        public float BottomY => PositionY + Height;

        /// <summary>
        /// Gets the bounds of the line.
        /// </summary>
        public ViewportBounds Bounds => new ViewportBounds
        {
            Top = PositionY,
            Bottom = BottomY,
            Left = 0,
            Right = float.MaxValue
        };

        /// <summary>
        /// Checks if the line is visible within the viewport.
        /// </summary>
        /// <param name="viewportTop">Top of the viewport.</param>
        /// <param name="viewportBottom">Bottom of the viewport.</param>
        /// <returns>True if the line is visible.</returns>
        public bool IsVisible(float viewportTop, float viewportBottom)
        {
            return PositionY <= viewportBottom && BottomY >= viewportTop;
        }
    }

    /// <summary>
    /// Immutable drag state using C# 9.0 records.
    /// Represents the state of a drag operation.
    /// </summary>
    public readonly record struct DragState
    {
        /// <summary>
        /// Beginning position of the drag.
        /// </summary>
        public float2 BeginPosition { get; init; }

        /// <summary>
        /// Current position of the drag.
        /// </summary>
        public float2 CurrentPosition { get; init; }

        /// <summary>
        /// Delta velocity for inertial scrolling.
        /// </summary>
        public float2 Delta { get; init; }

        /// <summary>
        /// Whether a drag is currently active.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Gets the total drag distance.
        /// </summary>
        public float2 DragDistance => CurrentPosition - BeginPosition;

        /// <summary>
        /// Gets the magnitude of the drag distance.
        /// </summary>
        public float DragMagnitude => math.length(DragDistance);

        /// <summary>
        /// Creates a new drag state with updated position.
        /// </summary>
        /// <param name="newPosition">The new current position.</param>
        /// <returns>A new drag state.</returns>
        public DragState WithPosition(float2 newPosition)
        {
            return this with { CurrentPosition = newPosition };
        }

        /// <summary>
        /// Creates a new drag state with updated delta.
        /// </summary>
        /// <param name="newDelta">The new delta value.</param>
        /// <returns>A new drag state.</returns>
        public DragState WithDelta(float2 newDelta)
        {
            return this with { Delta = newDelta };
        }

        /// <summary>
        /// Creates an inactive drag state (no drag in progress).
        /// </summary>
        public static DragState Inactive => new DragState
        {
            BeginPosition = float2.zero,
            CurrentPosition = float2.zero,
            Delta = float2.zero,
            IsActive = false
        };
    }
}
