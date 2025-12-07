using System;

namespace uEmuera.Data
{
    /// <summary>
    /// Immutable display configuration using C# 9.0 compatible struct.
    /// Provides efficient value-based equality and immutability.
    /// </summary>
    public readonly struct DisplayConfig : IEquatable<DisplayConfig>
    {
        /// <summary>
        /// Display width in pixels.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Display height in pixels.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Content width in pixels.
        /// </summary>
        public float ContentWidth { get; }

        /// <summary>
        /// Content height in pixels.
        /// </summary>
        public float ContentHeight { get; }

        /// <summary>
        /// Offset height for scrolling.
        /// </summary>
        public float OffsetHeight { get; }

        /// <summary>
        /// Initializes a new DisplayConfig.
        /// </summary>
        public DisplayConfig(float width, float height, float contentWidth, float contentHeight, float offsetHeight)
        {
            Width = width;
            Height = height;
            ContentWidth = contentWidth;
            ContentHeight = contentHeight;
            OffsetHeight = offsetHeight;
        }

        /// <summary>
        /// Checks if content is wider than display (requires horizontal scrolling).
        /// </summary>
        public bool RequiresHorizontalScroll => ContentWidth > Width;

        /// <summary>
        /// Checks if content is taller than display (requires vertical scrolling).
        /// </summary>
        public bool RequiresVerticalScroll => ContentHeight > Height;

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        public bool Equals(DisplayConfig other)
        {
            return Width == other.Width &&
                   Height == other.Height &&
                   ContentWidth == other.ContentWidth &&
                   ContentHeight == other.ContentHeight &&
                   OffsetHeight == other.OffsetHeight;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is DisplayConfig other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, ContentWidth, ContentHeight, OffsetHeight);
        }

        /// <summary>
        /// Determines whether two specified instances are equal.
        /// </summary>
        public static bool operator ==(DisplayConfig left, DisplayConfig right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified instances are not equal.
        /// </summary>
        public static bool operator !=(DisplayConfig left, DisplayConfig right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Immutable viewport bounds using C# 9.0 compatible struct.
    /// </summary>
    public readonly struct ViewportBounds : IEquatable<ViewportBounds>
    {
        /// <summary>
        /// Top position.
        /// </summary>
        public float Top { get; }

        /// <summary>
        /// Bottom position.
        /// </summary>
        public float Bottom { get; }

        /// <summary>
        /// Left position.
        /// </summary>
        public float Left { get; }

        /// <summary>
        /// Right position.
        /// </summary>
        public float Right { get; }

        /// <summary>
        /// Initializes a new ViewportBounds.
        /// </summary>
        public ViewportBounds(float top, float bottom, float left, float right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Gets the width of the bounds.
        /// </summary>
        public float Width => Right - Left;

        /// <summary>
        /// Gets the height of the bounds.
        /// </summary>
        public float Height => Bottom - Top;

        /// <summary>
        /// Checks if a point is within the bounds.
        /// </summary>
        /// <param name="x">The x coordinate to check.</param>
        /// <param name="y">The y coordinate to check.</param>
        /// <returns>True if the point is within bounds.</returns>
        public bool Contains(float x, float y)
        {
            return x >= Left && x <= Right &&
                   y >= Top && y <= Bottom;
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

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        public bool Equals(ViewportBounds other)
        {
            return Top == other.Top &&
                   Bottom == other.Bottom &&
                   Left == other.Left &&
                   Right == other.Right;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ViewportBounds other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Top, Bottom, Left, Right);
        }

        /// <summary>
        /// Determines whether two specified instances are equal.
        /// </summary>
        public static bool operator ==(ViewportBounds left, ViewportBounds right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified instances are not equal.
        /// </summary>
        public static bool operator !=(ViewportBounds left, ViewportBounds right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Immutable line descriptor using C# 9.0 compatible struct.
    /// Represents a single line of text in the console.
    /// </summary>
    public readonly struct LineDescriptor : IEquatable<LineDescriptor>
    {
        /// <summary>
        /// Line number.
        /// </summary>
        public int LineNo { get; }

        /// <summary>
        /// Y position of the line.
        /// </summary>
        public float PositionY { get; }

        /// <summary>
        /// Height of the line.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Whether this is a logical line (vs wrapped line).
        /// </summary>
        public bool IsLogicalLine { get; }

        /// <summary>
        /// Initializes a new LineDescriptor.
        /// </summary>
        public LineDescriptor(int lineNo, float positionY, float height, bool isLogicalLine)
        {
            LineNo = lineNo;
            PositionY = positionY;
            Height = height;
            IsLogicalLine = isLogicalLine;
        }

        /// <summary>
        /// Gets the bottom position of the line.
        /// </summary>
        public float BottomY => PositionY + Height;

        /// <summary>
        /// Gets the bounds of the line.
        /// </summary>
        public ViewportBounds Bounds => new ViewportBounds(PositionY, BottomY, 0, float.MaxValue);

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

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        public bool Equals(LineDescriptor other)
        {
            return LineNo == other.LineNo &&
                   PositionY == other.PositionY &&
                   Height == other.Height &&
                   IsLogicalLine == other.IsLogicalLine;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is LineDescriptor other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(LineNo, PositionY, Height, IsLogicalLine);
        }

        /// <summary>
        /// Determines whether two specified instances are equal.
        /// </summary>
        public static bool operator ==(LineDescriptor left, LineDescriptor right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified instances are not equal.
        /// </summary>
        public static bool operator !=(LineDescriptor left, LineDescriptor right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Immutable drag state using C# 9.0 compatible struct.
    /// Represents the state of a drag operation.
    /// </summary>
    public readonly struct DragState : IEquatable<DragState>
    {
        /// <summary>
        /// Beginning X position of the drag.
        /// </summary>
        public float BeginX { get; }

        /// <summary>
        /// Beginning Y position of the drag.
        /// </summary>
        public float BeginY { get; }

        /// <summary>
        /// Current X position of the drag.
        /// </summary>
        public float CurrentX { get; }

        /// <summary>
        /// Current Y position of the drag.
        /// </summary>
        public float CurrentY { get; }

        /// <summary>
        /// Delta X velocity for inertial scrolling.
        /// </summary>
        public float DeltaX { get; }

        /// <summary>
        /// Delta Y velocity for inertial scrolling.
        /// </summary>
        public float DeltaY { get; }

        /// <summary>
        /// Whether a drag is currently active.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Initializes a new DragState.
        /// </summary>
        public DragState(float beginX, float beginY, float currentX, float currentY, float deltaX, float deltaY, bool isActive)
        {
            BeginX = beginX;
            BeginY = beginY;
            CurrentX = currentX;
            CurrentY = currentY;
            DeltaX = deltaX;
            DeltaY = deltaY;
            IsActive = isActive;
        }

        /// <summary>
        /// Gets the total X drag distance.
        /// </summary>
        public float DragDistanceX => CurrentX - BeginX;

        /// <summary>
        /// Gets the total Y drag distance.
        /// </summary>
        public float DragDistanceY => CurrentY - BeginY;

        /// <summary>
        /// Gets the magnitude of the drag distance.
        /// </summary>
        public float DragMagnitude => (float)System.Math.Sqrt(DragDistanceX * DragDistanceX + DragDistanceY * DragDistanceY);

        /// <summary>
        /// Creates a new drag state with updated position.
        /// </summary>
        /// <param name="newX">The new current X position.</param>
        /// <param name="newY">The new current Y position.</param>
        /// <returns>A new drag state.</returns>
        public DragState WithPosition(float newX, float newY)
        {
            return new DragState(BeginX, BeginY, newX, newY, DeltaX, DeltaY, IsActive);
        }

        /// <summary>
        /// Creates a new drag state with updated delta.
        /// </summary>
        /// <param name="newDeltaX">The new delta X value.</param>
        /// <param name="newDeltaY">The new delta Y value.</param>
        /// <returns>A new drag state.</returns>
        public DragState WithDelta(float newDeltaX, float newDeltaY)
        {
            return new DragState(BeginX, BeginY, CurrentX, CurrentY, newDeltaX, newDeltaY, IsActive);
        }

        /// <summary>
        /// Creates an inactive drag state (no drag in progress).
        /// </summary>
        public static DragState Inactive => new DragState(0, 0, 0, 0, 0, 0, false);

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        public bool Equals(DragState other)
        {
            return BeginX == other.BeginX &&
                   BeginY == other.BeginY &&
                   CurrentX == other.CurrentX &&
                   CurrentY == other.CurrentY &&
                   DeltaX == other.DeltaX &&
                   DeltaY == other.DeltaY &&
                   IsActive == other.IsActive;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is DragState other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(BeginX, BeginY, CurrentX, CurrentY, DeltaX, DeltaY, IsActive);
        }

        /// <summary>
        /// Determines whether two specified instances are equal.
        /// </summary>
        public static bool operator ==(DragState left, DragState right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified instances are not equal.
        /// </summary>
        public static bool operator !=(DragState left, DragState right)
        {
            return !left.Equals(right);
        }
    }
}
