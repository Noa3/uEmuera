using System;
using System.Collections.Generic;
using uEmuera.Drawing;

namespace MinorShift.Emuera.Content
{
    /// <summary>
    /// Graphics (G) object backing store for the E* G-commands. Pixels live in a
    /// CPU <see cref="GraphicsSurface"/>; drawing operations that can be applied on
    /// the ERB thread (clears, fills, pixel writes, surface-to-surface blits) are
    /// applied immediately. Blits that need to read a Unity texture (file-backed
    /// images) are queued and executed on the main thread when the host calls
    /// <see cref="ExecutePendingMainThreadOps"/>.
    /// </summary>
    internal sealed class GraphicsImage : AbstractImage
    {
        public GraphicsImage(int id)
        {
            ID = id;
        }
        public readonly int ID;

        #region lifecycle

        public void GCreate(int width, int height, bool useGDI)
        {
            GDispose();
            is_created = true;
            this.width = width;
            this.height = height;
            surface = new GraphicsSurface(width, height);
        }

        /// <summary>
        /// GCREATEFROMFILE - copies the pixel content of <paramref name="bmp"/> into
        /// a new surface. <see cref="Bitmap.GetPixel"/> reads a Unity texture, so the
        /// actual copy is deferred to the main thread when the bitmap is texture-backed.
        /// </summary>
        public void GCreateFromF(Bitmap bmp, bool useGDI)
        {
            GDispose();
            if (bmp == null || bmp.Width <= 0 || bmp.Height <= 0)
                return;
            is_created = true;
            width = bmp.Width;
            height = bmp.Height;
            surface = new GraphicsSurface(width, height);
            enqueueOp(new BlitBitmapOp(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
                new Rectangle(0, 0, bmp.Width, bmp.Height), null));
        }

        public void GDispose()
        {
            lock (pendingOpLock)
            {
                if (pendingOps != null)
                    pendingOps.Clear();
            }
            GraphicsSurface old = surface;
            surface = null;
            old = null;
            is_created = false;
            width = 0;
            height = 0;
        }

        public override void Dispose()
        {
            GDispose();
        }

        ~GraphicsImage()
        {
            GDispose();
        }

        public override bool IsCreated
        {
            get { return is_created && surface != null; }
        }

        int width;
        int height;
        bool is_created;
        GraphicsSurface surface;

        public int Width { get { return width; } }
        public int Height { get { return height; } }

        #endregion

        #region paint state

        Brush brush;
        Pen pen;
        Font font;

        internal Color CurrentBrushColor
        {
            get
            {
                if (brush is SolidBrush sb)
                    return sb.Color;
                return new Color(0, 0, 0, 0);
            }
        }

        #endregion

        #region drawing primitives

        /// <summary>
        /// GCLEAR(int ID, Color) - overwrite every pixel.
        /// </summary>
        public void GClear(Color c)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            lock (pixelLock)
            {
                s.Clear(c);
            }
            WhileDirty();
        }

        /// <summary>
        /// GFILLRECTANGLE(int ID, Rectangle).
        /// </summary>
        public void GFillRectangle(Rectangle rect)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            Color c = CurrentBrushColor;
            lock (pixelLock)
            {
                s.FillRectangle(rect, c);
            }
            WhileDirty();
        }

        /// <summary>
        /// GSETCOLOR(int ID, Color, int x, int y) - overwrite a single pixel.
        /// </summary>
        public void GSetColor(Color c, int x, int y)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            lock (pixelLock)
            {
                s.SetPixel(c, x, y);
            }
            WhileDirty();
        }

        /// <summary>
        /// GGETCOLOR(int ID, int x, int y). Assumes coordinates are in bounds.
        /// </summary>
        public Color GGetColor(int x, int y)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            lock (pixelLock)
                return s.GetPixel(x, y);
        }

        public void GSetBrush(Brush r)
        {
            brush = r;
        }

        public void GSetPen(Pen r)
        {
            pen = r;
        }

        public void GSetFont(Font r)
        {
            font = r;
        }

        /// <summary>
        /// GDRAWCIMG(int ID, str imgName, ...). Graphics-backed sprites (SpriteG) can
        /// be composited straight from the source CPU surface; texture-backed sprites
        /// are queued to the main thread.
        /// </summary>
        public void GDrawCImg(ASprite img, Rectangle destRect)
        {
            DrawSpriteInternal(img, destRect, null);
        }

        public void GDrawCImg(ASprite img, Rectangle destRect, float[][] cm)
        {
            DrawSpriteInternal(img, destRect, cm);
        }

        void DrawSpriteInternal(ASprite img, Rectangle destRect, float[][] cm)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            if (img == null)
                return;
            if (img is ASpriteSingle single && single.BaseImage is GraphicsImage srcG &&
                srcG.InternalSurface != null)
            {
                Rectangle srcRect = single.SrcRectangle;
                lock (pixelLock)
                {
                    lock (srcG.pixelLock)
                    {
                        s.DrawImage(srcG.InternalSurface, destRect, srcRect);
                    }
                }
                WhileDirty();
                return;
            }
            enqueueOp(new BlitSpriteOp(img, destRect, cm));
        }

        /// <summary>
        /// GDRAWG(int ID, int srcID, ...)... surface-to-surface compositing.
        /// </summary>
        public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            if (srcGra == null || srcGra.InternalSurface == null)
                return;
            lock (pixelLock)
            {
                GraphicsSurface srcSurf = srcGra.InternalSurface;
                lock (srcGra.pixelLock)
                {
                    s.DrawImage(srcSurf, destRect, srcRect);
                }
            }
            WhileDirty();
        }

        public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect, float[][] cm)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            if (srcGra == null || srcGra.InternalSurface == null)
                return;
            lock (pixelLock)
            {
                GraphicsSurface srcSurf = srcGra.InternalSurface;
                lock (srcGra.pixelLock)
                {
// Apply matrix through a temporary surface so the source is 1:1
                    // composited (matrix + source-over) into the dest rect.
                    GraphicsSurface tmp = new GraphicsSurface(
                        destRect.Width > 0 ? destRect.Width : -destRect.Width,
                        destRect.Height > 0 ? destRect.Height : -destRect.Height);
                    tmp.DrawImage(srcSurf, new Rectangle(0, 0, tmp.Width, tmp.Height), srcRect);
                    tmp.ApplyColorMatrixInPlace(cm);
                    s.DrawImage(tmp, destRect, new Rectangle(0, 0, tmp.Width, tmp.Height));
                }
            }
            WhileDirty();
        }

        /// <summary>
        /// GDRAWGWITHMASK. Mask alpha scales the source opacity per pixel.
        /// </summary>
        public void GDrawGWithMask(GraphicsImage srcGra, GraphicsImage maskGra, Point destPoint)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            if (srcGra == null || srcGra.InternalSurface == null || maskGra == null || maskGra.InternalSurface == null)
                return;
            lock (pixelLock)
            {
                GraphicsSurface src = srcGra.InternalSurface;
                GraphicsSurface mask = maskGra.InternalSurface;
                lock (srcGra.pixelLock)
                {
                    lock (maskGra.pixelLock)
                    {
                        s.DrawImageMasked(src, mask, destPoint);
                    }
                }
            }
            WhileDirty();
        }

        #endregion

        #region array conversion

        /// <summary>
        /// GTOARRAY equivalent - pack the surface into a 2D Int64 array at (xstart, ystart).
        /// Packing matches reference Emuera: ABGR byte order within each pixel.
        /// </summary>
        public bool GBitmapToInt64Array(Int64[,] array, int xstart, int ystart)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            if (xstart + s.Width > array.GetLength(0) || ystart + s.Height > array.GetLength(1))
                return false;
            lock (pixelLock)
            {
                uint[] p = s.Raw;
                for (int y = 0; y < s.Height; y++)
                {
                    int row = y * s.Width;
                    for (int x = 0; x < s.Width; x++)
                    {
                        uint v = p[row + x];
                        array[x + xstart, y + ystart] = (Int64)(v & 0xFF) |
                            ((Int64)((v >> 8) & 0xFF) << 8) |
                            ((Int64)((v >> 16) & 0xFF) << 16) |
                            ((Int64)((v >> 24) & 0xFF) << 24);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// GFROMARRAY equivalent - write a 2D Int64 array (ABGR packed) into the surface
        /// starting at (xstart, ystart).
        /// </summary>
        public bool GByteArrayToBitmap(Int64[,] array, int xstart, int ystart)
        {
            GraphicsSurface s = surface;
            if (s == null)
                throw new NullReferenceException();
            int w = s.Width, h = s.Height;
            if (xstart + w > array.GetLength(0) || ystart + h > array.GetLength(1))
                return false;
            lock (pixelLock)
            {
                uint[] p = s.Raw;
                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        Int64 c = array[x + xstart, y + ystart];
                        p[row + x] = ((uint)((c >> 24) & 0xFF) << 24) |
                                     ((uint)((c >> 16) & 0xFF) << 16) |
                                     ((uint)((c >> 8) & 0xFF) << 8) |
                                     ((uint)(c & 0xFF));
                    }
                }
            }
            WhileDirty();
            return true;
        }

        #endregion

        #region main-thread deferred ops

        internal abstract class MainThreadOp
        {
            public abstract void Apply(GraphicsImage owner, GraphicsSurface target);
        }

        /// <summary>File/texture-backed bitmap → surface blit.</summary>
        sealed class BlitBitmapOp : MainThreadOp
        {
            readonly Bitmap bmp;
            readonly Rectangle srcRect;
            readonly Rectangle destRect;
            readonly float[][] cm;
            public BlitBitmapOp(Bitmap bmp, Rectangle srcRect, Rectangle destRect, float[][] cm)
            {
                this.bmp = bmp;
                this.srcRect = srcRect;
                this.destRect = destRect;
                this.cm = cm;
            }
            public override void Apply(GraphicsImage owner, GraphicsSurface target)
            {
                target.DrawBitmap(bmp, destRect, srcRect, cm);
            }
        }

        /// <summary>Texture-backed sprite blit (should only reach here for file sprites).</summary>
        sealed class BlitSpriteOp : MainThreadOp
        {
            readonly ASprite img;
            readonly Rectangle destRect;
            readonly float[][] cm;
            public BlitSpriteOp(ASprite img, Rectangle destRect, float[][] cm)
            {
                this.img = img;
                this.destRect = destRect;
                this.cm = cm;
            }
            public override void Apply(GraphicsImage owner, GraphicsSurface target)
            {
                if (img is ASpriteSingle single && single.BaseImage != null)
                {
                    Bitmap bmp = single.BaseImage.Bitmap;
                    if (bmp == null || bmp.Width <= 0 || bmp.Height <= 0)
                        return;
                    Rectangle srcRect = single.SrcRectangle;
                    if (srcRect.Width == 0 || srcRect.Height == 0)
                        srcRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    target.DrawBitmap(bmp, destRect, srcRect, cm);
                }
            }
        }

        void enqueueOp(MainThreadOp op)
        {
            lock (pendingOpLock)
            {
                if (pendingOps == null)
                    pendingOps = new List<MainThreadOp>(4);
                pendingOps.Add(op);
            }
        }

        /// <summary>
        /// Runs any queued main-thread-only ops. Must be called from the Unity main
        /// thread (typically from the game's update loop / renderer synchronization).
        /// </summary>
        internal void ExecutePendingMainThreadOps()
        {
            List<MainThreadOp> ops;
            lock (pendingOpLock)
            {
                if (pendingOps == null || pendingOps.Count == 0)
                    return;
                ops = pendingOps;
                pendingOps = new List<MainThreadOp>(4);
            }
            GraphicsSurface s = surface;
            if (s == null)
                return;
            lock (pixelLock)
            {
                for (int i = 0; i < ops.Count; i++)
                {
                    try
                    {
                        ops[i].Apply(this, s);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning("GraphicsImage op failed: " + e.Message);
                    }
                }
            }
            WhileDirty();
        }

        List<MainThreadOp> pendingOps;
        readonly object pendingOpLock = new object();
        readonly object pixelLock = new object();

        internal void WhileDirty()
        {
            System.Threading.Interlocked.Exchange(ref dirty, 1);
        }
        internal int dirty;

        #endregion

        internal GraphicsSurface InternalSurface
        {
            get { return is_created ? surface : null; }
        }

        internal uint[] SampleSurface()
        {
            GraphicsSurface s = surface;
            if (s == null)
                return null;
            lock (pixelLock)
            {
                uint[] copy = new uint[s.Raw.Length];
                Array.Copy(s.Raw, copy, copy.Length);
                return copy;
            }
        }

        internal bool IsDirty
        {
            get { return dirty != 0; }
        }
    }
}