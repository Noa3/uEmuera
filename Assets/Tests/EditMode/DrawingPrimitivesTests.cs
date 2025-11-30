using NUnit.Framework;
using uEmuera.Drawing;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for the drawing primitives: Point, Size, and Rectangle.
    /// </summary>
    [TestFixture]
    public class DrawingPrimitivesTests
    {
        #region Point Tests

        [Test]
        public void Point_Constructor_SetsCoordinates()
        {
            var point = new Point(10, 20);
            
            Assert.AreEqual(10, point.X);
            Assert.AreEqual(20, point.Y);
        }

        [Test]
        public void Point_FromSize_SetsCoordinatesFromSize()
        {
            var size = new Size(30, 40);
            var point = new Point(size);
            
            Assert.AreEqual(30, point.X);
            Assert.AreEqual(40, point.Y);
        }

        [Test]
        public void Point_Empty_HasZeroCoordinates()
        {
            Assert.AreEqual(0, Point.Empty.X);
            Assert.AreEqual(0, Point.Empty.Y);
            Assert.IsTrue(Point.Empty.IsEmpty);
        }

        [Test]
        public void Point_Offset_ModifiesCoordinates()
        {
            var point = new Point(10, 20);
            point.Offset(new Point(5, 10));
            
            Assert.AreEqual(15, point.X);
            Assert.AreEqual(30, point.Y);
        }

        [Test]
        public void Point_IsEmpty_ReturnsTrueForZeroCoordinates()
        {
            var point = new Point(0, 0);
            Assert.IsTrue(point.IsEmpty);
        }

        [Test]
        public void Point_IsEmpty_ReturnsFalseForNonZeroCoordinates()
        {
            var point = new Point(1, 0);
            Assert.IsFalse(point.IsEmpty);
        }

        #endregion

        #region Size Tests

        [Test]
        public void Size_Constructor_SetsDimensions()
        {
            var size = new Size(100, 50);
            
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(50, size.Height);
        }

        [Test]
        public void Size_FromPoint_SetsDimensionsFromPoint()
        {
            var point = new Point(75, 25);
            var size = new Size(point);
            
            Assert.AreEqual(75, size.Width);
            Assert.AreEqual(25, size.Height);
        }

        [Test]
        public void Size_Zero_HasZeroDimensions()
        {
            Assert.AreEqual(0, Size.zero.Width);
            Assert.AreEqual(0, Size.zero.Height);
        }

        [Test]
        public void Size_IsEmpty_ReturnsTrueForZeroDimensions()
        {
            var size = new Size(0, 0);
            Assert.IsTrue(size.IsEmpty);
        }

        [Test]
        public void Size_IsEmpty_ReturnsFalseForNonZeroDimensions()
        {
            var size = new Size(1, 0);
            Assert.IsFalse(size.IsEmpty);
        }

        #endregion

        #region Rectangle Tests

        [Test]
        public void Rectangle_Constructor_SetsProperties()
        {
            var rect = new Rectangle(10, 20, 100, 50);
            
            Assert.AreEqual(10, rect.X);
            Assert.AreEqual(20, rect.Y);
            Assert.AreEqual(100, rect.Width);
            Assert.AreEqual(50, rect.Height);
        }

        [Test]
        public void Rectangle_FromPointAndSize_SetsProperties()
        {
            var location = new Point(15, 25);
            var size = new Size(80, 60);
            var rect = new Rectangle(location, size);
            
            Assert.AreEqual(15, rect.X);
            Assert.AreEqual(25, rect.Y);
            Assert.AreEqual(80, rect.Width);
            Assert.AreEqual(60, rect.Height);
        }

        [Test]
        public void Rectangle_EdgeProperties_AreCorrect()
        {
            var rect = new Rectangle(10, 20, 100, 50);
            
            Assert.AreEqual(10, rect.Left);
            Assert.AreEqual(20, rect.Top);
            Assert.AreEqual(110, rect.Right);
            Assert.AreEqual(70, rect.Bottom);
        }

        [Test]
        public void Rectangle_Size_ReturnsCorrectSize()
        {
            var rect = new Rectangle(10, 20, 100, 50);
            var size = rect.Size;
            
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(50, size.Height);
        }

        [Test]
        public void Rectangle_IsEmpty_ReturnsTrueForZeroDimensions()
        {
            var rect = new Rectangle(10, 20, 0, 0);
            Assert.IsTrue(rect.IsEmpty);
        }

        [Test]
        public void Rectangle_IsEmpty_ReturnsFalseForNonZeroDimensions()
        {
            var rect = new Rectangle(10, 20, 1, 0);
            Assert.IsFalse(rect.IsEmpty);
        }

        [Test]
        public void Rectangle_Contains_PointInside_ReturnsTrue()
        {
            var rect = new Rectangle(0, 0, 100, 100);
            var point = new Point(50, 50);
            
            Assert.IsTrue(rect.Contains(point));
        }

        [Test]
        public void Rectangle_Contains_PointOnTopLeftEdge_ReturnsTrue()
        {
            var rect = new Rectangle(10, 10, 100, 100);
            var point = new Point(10, 10);
            
            Assert.IsTrue(rect.Contains(point));
        }

        [Test]
        public void Rectangle_Contains_PointOnRightEdge_ReturnsFalse()
        {
            var rect = new Rectangle(0, 0, 100, 100);
            var point = new Point(100, 50);
            
            Assert.IsFalse(rect.Contains(point));
        }

        [Test]
        public void Rectangle_Contains_PointOnBottomEdge_ReturnsFalse()
        {
            var rect = new Rectangle(0, 0, 100, 100);
            var point = new Point(50, 100);
            
            Assert.IsFalse(rect.Contains(point));
        }

        [Test]
        public void Rectangle_Contains_PointOutside_ReturnsFalse()
        {
            var rect = new Rectangle(0, 0, 100, 100);
            var point = new Point(150, 150);
            
            Assert.IsFalse(rect.Contains(point));
        }

        [Test]
        public void Rectangle_IntersectsWith_OverlappingRectangles_ReturnsTrue()
        {
            var rect1 = new Rectangle(0, 0, 100, 100);
            var rect2 = new Rectangle(50, 50, 100, 100);
            
            Assert.IsTrue(rect1.IntersectsWith(rect2));
            Assert.IsTrue(rect2.IntersectsWith(rect1));
        }

        [Test]
        public void Rectangle_IntersectsWith_NonOverlappingRectangles_ReturnsFalse()
        {
            var rect1 = new Rectangle(0, 0, 50, 50);
            var rect2 = new Rectangle(100, 100, 50, 50);
            
            Assert.IsFalse(rect1.IntersectsWith(rect2));
        }

        [Test]
        public void Rectangle_IntersectsWith_AdjacentRectangles_ReturnsFalse()
        {
            var rect1 = new Rectangle(0, 0, 50, 50);
            var rect2 = new Rectangle(50, 0, 50, 50);
            
            Assert.IsFalse(rect1.IntersectsWith(rect2));
        }

        [Test]
        public void Rectangle_Intersect_ReturnsOverlappingArea()
        {
            var rect1 = new Rectangle(0, 0, 100, 100);
            var rect2 = new Rectangle(50, 50, 100, 100);
            
            var intersection = Rectangle.Intersect(rect1, rect2);
            
            Assert.AreEqual(50, intersection.X);
            Assert.AreEqual(50, intersection.Y);
            Assert.AreEqual(50, intersection.Width);
            Assert.AreEqual(50, intersection.Height);
        }

        [Test]
        public void Rectangle_Intersect_NonOverlapping_ReturnsEmptyRectangle()
        {
            var rect1 = new Rectangle(0, 0, 50, 50);
            var rect2 = new Rectangle(100, 100, 50, 50);
            
            var intersection = Rectangle.Intersect(rect1, rect2);
            
            Assert.AreEqual(0, intersection.Width);
            Assert.AreEqual(0, intersection.Height);
        }

        #endregion

        #region RectangleF Tests

        [Test]
        public void RectangleF_Constructor_Int_SetsProperties()
        {
            var rect = new RectangleF(10, 20, 100, 50);
            
            Assert.AreEqual(10f, rect.X);
            Assert.AreEqual(20f, rect.Y);
            Assert.AreEqual(100f, rect.Width);
            Assert.AreEqual(50f, rect.Height);
        }

        [Test]
        public void RectangleF_Constructor_Float_SetsProperties()
        {
            var rect = new RectangleF(10.5f, 20.5f, 100.5f, 50.5f);
            
            Assert.AreEqual(10.5f, rect.X);
            Assert.AreEqual(20.5f, rect.Y);
            Assert.AreEqual(100.5f, rect.Width);
            Assert.AreEqual(50.5f, rect.Height);
        }

        [Test]
        public void RectangleF_EdgeProperties_AreCorrect()
        {
            var rect = new RectangleF(10.5f, 20.5f, 100f, 50f);
            
            Assert.AreEqual(10.5f, rect.Left);
            Assert.AreEqual(20.5f, rect.Top);
            Assert.AreEqual(110.5f, rect.Right);
            Assert.AreEqual(70.5f, rect.Bottom);
        }

        #endregion

        #region CharacterRange Tests

        [Test]
        public void CharacterRange_Constructor_SetsProperties()
        {
            var range = new CharacterRange(5, 10);
            
            Assert.AreEqual(5, range.First);
            Assert.AreEqual(10, range.Length);
        }

        [Test]
        public void CharacterRange_Properties_CanBeModified()
        {
            var range = new CharacterRange(0, 0);
            range.First = 3;
            range.Length = 7;
            
            Assert.AreEqual(3, range.First);
            Assert.AreEqual(7, range.Length);
        }

        #endregion
    }
}
