using System;
using MinorShift.Emuera.Content;
using NUnit.Framework;
using uEmuera.Drawing;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the CPU-backed Graphics pixel surface and the G* drawing
    /// primitives on <see cref="GraphicsImage"/>. These cover the pure-C# path only;
    /// main-thread texture ops (file sprites) are exercised elsewhere.
    /// </summary>
    [TestFixture]
    public class GraphicsSurfaceTests
    {
        #region surface basics

        [Test]
        public void NewSurface_IsTransparent()
        {
            var s = new GraphicsSurface(4, 3);
            Assert.AreEqual(4, s.Width);
            Assert.AreEqual(3, s.Height);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 4; x++)
                    Assert.AreEqual(Color.Transparent, s.GetPixel(x, y), $"pixel ({x},{y})");
        }

        [Test]
        public void Resize_PreservesDimensions_ButClears()
        {
            var s = new GraphicsSurface(2, 2);
            s.Clear(new Color(255, 10, 20, 30));
            s.Resize(2, 2);
            Assert.AreEqual(Color.Transparent, s.GetPixel(0, 0));
        }

        [Test]
        public void OutOfBoundsGetPixel_ReturnsTransparent()
        {
            var s = new GraphicsSurface(2, 2);
            Assert.AreEqual(Color.Transparent, s.GetPixel(-1, 0));
            Assert.AreEqual(Color.Transparent, s.GetPixel(0, 5));
        }

        #endregion

        #region set / clear / fill

        [Test]
        public void SetPixel_GetPixel_Roundtrip()
        {
            var s = new GraphicsSurface(3, 3);
            var c = new Color(10, 200, 100, 77);
            s.SetPixel(c, 1, 2);
            Assert.AreEqual(77, s.GetPixel(1, 2).A);
            Assert.AreEqual(10, s.GetPixel(1, 2).R);
            Assert.AreEqual(200, s.GetPixel(1, 2).G);
            Assert.AreEqual(100, s.GetPixel(1, 2).B);
            Assert.AreEqual(Color.Transparent, s.GetPixel(0, 0));
        }

        [Test]
        public void Clear_OverwritesEverything()
        {
            var s = new GraphicsSurface(3, 2);
            s.Clear(new Color(1, 2, 3, 4));
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 3; x++)
                    Assert.AreEqual(new Color(1, 2, 3, 4), s.GetPixel(x, y));
        }

        [Test]
        public void FillRectangle_Opaque_Overwrites()
        {
            var s = new GraphicsSurface(5, 5);
            s.Clear(Color.FromArgb(255, 1, 1, 1));
            s.FillRectangle(new Rectangle(1, 1, 3, 2), Color.FromArgb(255, 9, 9, 9));
            Assert.AreEqual(Color.FromArgb(255, 9, 9, 9), s.GetPixel(1, 1));
            Assert.AreEqual(Color.FromArgb(255, 9, 9, 9), s.GetPixel(3, 2));
            Assert.AreEqual(Color.FromArgb(255, 1, 1, 1), s.GetPixel(0, 0));
            Assert.AreEqual(Color.FromArgb(255, 1, 1, 1), s.GetPixel(4, 4));
        }

        [Test]
        public void FillRectangle_ClipsToSurface()
        {
            var s = new GraphicsSurface(4, 4);
            s.FillRectangle(new Rectangle(-2, -1, 8, 8), Color.FromArgb(255, 12, 34, 56));
            // fully inside
            Assert.AreEqual(Color.FromArgb(255, 12, 34, 56), s.GetPixel(0, 0));
            Assert.AreEqual(Color.FromArgb(255, 12, 34, 56), s.GetPixel(3, 3));
        }

        [Test]
        public void FillRectangle_SemiTransparent_BlendsSourceOverBg()
        {
            var s = new GraphicsSurface(2, 1);
            s.Clear(Color.FromArgb(255, 0, 0, 0)); // black bg
            // 50% white on black => (128,128,128), alpha 255
            s.FillRectangle(new Rectangle(0, 0, 2, 1), Color.FromArgb(128, 255, 255, 255));
            var px = s.GetPixel(0, 0);
            Assert.AreEqual(128, px.R);
            Assert.AreEqual(128, px.G);
            Assert.AreEqual(128, px.B);
            Assert.AreEqual(255, px.A);
        }

        #endregion

        #region DrawImage (GDRAWG)

        [Test]
        public void DrawImage_Opaque_Composite()
        {
            var src = new GraphicsSurface(2, 2);
            src.Clear(Color.FromArgb(255, 10, 20, 30));
            var dst = new GraphicsSurface(4, 4);
            dst.DrawImage(src, new Rectangle(1, 1, 2, 2), new Rectangle(0, 0, 2, 2));
            Assert.AreEqual(Color.FromArgb(255, 10, 20, 30), dst.GetPixel(1, 1));
            Assert.AreEqual(Color.FromArgb(255, 10, 20, 30), dst.GetPixel(2, 2));
            Assert.AreEqual(Color.Transparent, dst.GetPixel(0, 0));
        }

        [Test]
        public void DrawImage_SemiOpaqueSource_Blend_over_Dest()
        {
            var src = new GraphicsSurface(1, 1);
            src.SetPixel(Color.FromArgb(128, 255, 255, 255), 0, 0); // 50% white
            var dst = new GraphicsSurface(1, 1);
            dst.SetPixel(Color.FromArgb(255, 0, 0, 0), 0, 0); // opaque black
            dst.DrawImage(src, new Rectangle(0, 0, 1, 1), new Rectangle(0, 0, 1, 1));
            var px = dst.GetPixel(0, 0);
            Assert.AreEqual(128, px.R);
            Assert.AreEqual(255, px.A);
        }

        [Test]
        public void DrawImage_ClipSkipsOutOfBounds()
        {
            var src = new GraphicsSurface(3, 3);
            src.FillRectangle(new Rectangle(0, 0, 3, 3), Color.FromArgb(255, 5, 5, 5));
            var dst = new GraphicsSurface(2, 2);
            dst.DrawImage(src, new Rectangle(1, 1, 3, 3), new Rectangle(0, 0, 3, 3));
            // Only (1,1) on dest lies inside
            Assert.AreEqual(Color.FromArgb(255, 5, 5, 5), dst.GetPixel(1, 1));
            Assert.AreEqual(Color.Transparent, dst.GetPixel(0, 0));
            Assert.AreEqual(Color.Transparent, dst.GetPixel(0, 1));
        }

        [Test]
        public void DrawImage_FlipHorizontal_NegativeSrcWidth()
        {
            var src = new GraphicsSurface(2, 1);
            src.SetPixel(Color.FromArgb(255, 1, 1, 1), 0, 0); // left source pixel
            src.SetPixel(Color.FromArgb(255, 9, 9, 9), 1, 0); // right source pixel
            var dst = new GraphicsSurface(2, 1);
            // Negative source width requests a horizontal mirror: dest left shows the
            // source's right side and vice versa.
            dst.DrawImage(src, new Rectangle(0, 0, 2, 1), new Rectangle(2, 0, -2, 1));
            Assert.AreEqual(Color.FromArgb(255, 9, 9, 9), dst.GetPixel(0, 0), "dest left shows source right");
            Assert.AreEqual(Color.FromArgb(255, 1, 1, 1), dst.GetPixel(1, 0), "dest right shows source left");
        }

        [Test]
        public void DrawImage_NegativeDest_PhysicalAreaMapped()
        {
            var src = new GraphicsSurface(2, 1);
            src.SetPixel(Color.FromArgb(255, 1, 1, 1), 0, 0);
            src.SetPixel(Color.FromArgb(255, 9, 9, 9), 1, 0);
            var dst = new GraphicsSurface(3, 1);
            // dest (2,0,-2,1) occupies physical x in [-2+2 .. 2) = [0..2)
            // Negative dest width requests a horizontal mirror: dest left shows source right.
            dst.DrawImage(src, new Rectangle(2, 0, -2, 1), new Rectangle(0, 0, 2, 1));
            Assert.AreEqual(Color.FromArgb(255, 9, 9, 9), dst.GetPixel(0, 0), "dest left shows source right");
            Assert.AreEqual(Color.FromArgb(255, 1, 1, 1), dst.GetPixel(1, 0), "dest right shows source left");
        }

        #endregion

        #region DrawImageMasked

        [Test]
        public void DrawImageMasked_Alpha255Copies_ZeroLeavesDest()
        {
            var src = new GraphicsSurface(2, 1);
            src.FillRectangle(new Rectangle(0, 0, 2, 1), Color.FromArgb(255, 42, 77, 120));
            var mask = new GraphicsSurface(2, 1);
            mask.SetPixel(Color.FromArgb(255, 0, 0, 0), 0, 0);
            mask.SetPixel(Color.Transparent, 1, 0); // alpha=0 -> untouched
            var dst = new GraphicsSurface(2, 1);
            dst.SetPixel(Color.FromArgb(255, 9, 9, 9), 1, 0);
            dst.DrawImageMasked(src, mask, new Point(0, 0));
            Assert.AreEqual(Color.FromArgb(255, 42, 77, 120), dst.GetPixel(0, 0));
            Assert.AreEqual(Color.FromArgb(255, 9, 9, 9), dst.GetPixel(1, 0));
        }

        [Test]
        public void DrawImageMasked_HalfMask_blends()
        {
            var src = new GraphicsSurface(1, 1);
            src.SetPixel(Color.FromArgb(255, 255, 255, 255), 0, 0);
            var mask = new GraphicsSurface(1, 1);
            mask.SetPixel(Color.FromArgb(128, 0, 0, 0), 0, 0); // 50% opaque mask
            var dst = new GraphicsSurface(1, 1);
            dst.SetPixel(Color.FromArgb(255, 0, 0, 0), 0, 0);
            dst.DrawImageMasked(src, mask, new Point(0, 0));
            // mask 128 -> roughly half white over black
            var px = dst.GetPixel(0, 0);
            Assert.Less(px.R, 200, "should not be fully opaque white");
            Assert.Greater(px.R, 40, "should not leave dest untouched");
            Assert.AreEqual(255, px.A);
        }

        #endregion

        #region color matrix

        [Test]
        public void ApplyColorMatrix_BrightnessOffset_Applied()
        {
            var s = new GraphicsSurface(1, 1);
            s.Clear(Color.FromArgb(255, 100, 100, 100));
            // scale each channel by 0.5 (diagonal), rows map R,G,B,A
            float[][] cm = new float[][]
            {
                new float[] { 0.5f, 0, 0, 0 },
                new float[] { 0, 0.5f, 0, 0 },
                new float[] { 0, 0, 0.5f, 0 },
                new float[] { 0, 0, 0, 1f },
                new float[] { 0, 0, 0, 0 },
            };
            s.ApplyColorMatrixInPlace(cm);
            var px = s.GetPixel(0, 0);
            Assert.AreEqual(50, px.R);
            Assert.AreEqual(50, px.G);
            Assert.AreEqual(50, px.B);
            Assert.AreEqual(255, px.A);
        }

        #endregion

        #region GraphicsImage (G object) integration

        [Test]
        public void GraphicsImage_GCreate_IsCreated()
        {
            var gi = new GraphicsImage(0);
            Assert.IsFalse(gi.IsCreated);
            gi.GCreate(8, 8, false);
            Assert.IsTrue(gi.IsCreated);
            Assert.AreEqual(8, gi.Width);
            Assert.AreEqual(8, gi.Height);
            gi.GDispose();
            Assert.IsFalse(gi.IsCreated);
        }

        [Test]
        public void GraphicsImage_GFill_UsesBrushColor()
        {
            var gi = new GraphicsImage(1);
            gi.GCreate(3, 3, false);
            var c = Color.FromArgb(255, 12, 34, 56);
            gi.GSetBrush(new SolidBrush(c));
            gi.GFillRectangle(new Rectangle(0, 0, 3, 3));
            Assert.AreEqual(c, gi.GGetColor(1, 1));
            gi.GDispose();
        }

        [Test]
        public void GraphicsImage_GClear_ThenGSetColor()
        {
            var gi = new GraphicsImage(2);
            gi.GDispose();
            gi.GCreate(4, 4, false);
            gi.GClear(Color.FromArgb(255, 7, 8, 9));
            Assert.AreEqual(Color.FromArgb(255, 7, 8, 9), gi.GGetColor(3, 3));
            gi.GSetColor(Color.FromArgb(255, 1, 2, 3), 0, 0);
            Assert.AreEqual(Color.FromArgb(255, 1, 2, 3), gi.GGetColor(0, 0));
            Assert.AreEqual(Color.FromArgb(255, 7, 8, 9), gi.GGetColor(1, 1));
            gi.GDispose();
        }

        [Test]
        public void GraphicsImage_ArrayRoundtrip()
        {
            var gi = new GraphicsImage(3);
            gi.GCreate(2, 2, false);
            gi.GClear(Color.FromArgb(255, 10, 20, 30));
            gi.GSetColor(Color.FromArgb(255, 99, 88, 77), 1, 1);

            var arr = new Int64[3, 3];
            Assert.IsTrue(gi.GBitmapToInt64Array(arr, 1, 1));
            var gi2 = new GraphicsImage(4);
            gi2.GCreate(2, 2, false);
            Assert.IsTrue(gi2.GByteArrayToBitmap(arr, 1, 1));
            Assert.AreEqual(Color.FromArgb(255, 10, 20, 30), gi2.GGetColor(0, 0));
            Assert.AreEqual(Color.FromArgb(255, 99, 88, 77), gi2.GGetColor(1, 1));
        }

        [Test]
        public void GraphicsImage_GDrawGComposite()
        {
            var gi = new GraphicsImage(5);
            gi.GCreate(2, 2, false);
            gi.GSetBrush(new SolidBrush(Color.FromArgb(255, 200, 100, 50)));
            gi.GFillRectangle(new Rectangle(0, 0, 2, 2));
            var dest = new GraphicsImage(6);
            dest.GCreate(4, 4, false);
            dest.GDrawG(gi, new Rectangle(1, 1, 2, 2), new Rectangle(0, 0, 2, 2));
            Assert.AreEqual(Color.FromArgb(255, 200, 100, 50), dest.GGetColor(1, 1));
            Assert.AreEqual(Color.FromArgb(255, 200, 100, 50), dest.GGetColor(2, 2));
            Assert.AreEqual(Color.Transparent, dest.GGetColor(0, 0));
        }

        #endregion

        #region helpers/BlendOver math

        [Test]
        public void BlendOver_Math()
        {
            // opaque src overwrites
            Assert.AreEqual(0xFF112233u, GraphicsSurface.BlendOver(0xFF112233u, 0xFF445566u));
            // transparent src leaves dst
            Assert.AreEqual(0xFF445566u, GraphicsSurface.BlendOver(0x00112233u, 0xFF445566u));
        }

        #endregion
    }
}