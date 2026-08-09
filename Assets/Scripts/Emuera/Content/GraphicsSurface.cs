using System;
using uEmuera.Drawing;

namespace MinorShift.Emuera.Content
{
    /// <summary>
    /// CPU-backed ARGB pixel surface used by <see cref="GraphicsImage"/> so that the
    /// Emuera G* (Graphics) commands actually produce pixels instead of silently
    /// doing nothing. All operations are pure C# / main-thread-independent, which
    /// makes them unit-testable and safe to run from the ERB execution thread.
    ///
    /// Semantics follow reference Emuera (GDI+ Graphics.CompositingMode = SourceOver,
    /// straight (non-premultiplied) alpha):
    ///   * GCLEAR     overwrites every pixel.
    ///   * GSETCOLOR  overwrites the single pixel.
    ///   * GFILLRECTANGLE, GDRAWG, GDRAWGWITHMASK and GDrawCImg blits composite with
    ///     "source-over" alpha blending.
    /// Pixel layout: 0xAARRGGBB.
    /// </summary>
    internal sealed class GraphicsSurface
    {
        public GraphicsSurface(int width, int height)
        {
            Resize(width, height);
        }

        /// <summary>Creates the backing store. Existing content is cleared.</summary>
        public void Resize(int width, int height)
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            this.width = width;
            this.height = height;
            if (pixels == null || pixels.Length != width * height)
                pixels = new uint[width * height];
            else
                Array.Clear(pixels, 0, pixels.Length);
        }

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public bool IsEmpty { get { return pixels == null || pixels.Length == 0; } }

        int width;
        int height;
        uint[] pixels;

        /// <summary>Direct back-buffer access (main-thread texture upload).</summary>
        public uint[] Raw { get { return pixels; } }

        static uint PackColor(Color c)
        {
            return ((uint)(c.A & 0xFF) << 24) |
                   ((uint)(c.R & 0xFF) << 16) |
                   ((uint)(c.G & 0xFF) << 8) |
                   ((uint)(c.B & 0xFF));
        }

        static uint PackBytes(int a, int r, int g, int b)
        {
            return ((uint)(a & 0xFF) << 24) |
                   ((uint)(r & 0xFF) << 16) |
                   ((uint)(g & 0xFF) << 8) |
                   ((uint)(b & 0xFF));
        }

        static Color UnpackColor(uint p)
        {
            return Color.FromArgb(
                (int)(p >> 24) & 0xFF,
                (int)(p >> 16) & 0xFF,
                (int)(p >> 8) & 0xFF,
                (int)(p & 0xFF));
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return Color.Transparent;
            return UnpackColor(pixels[y * width + x]);
        }

        /// <summary>Overwrites a single pixel (GSETCOLOR semantics, ignores alpha compositing).</summary>
        public void SetPixel(Color c, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;
            pixels[y * width + x] = PackColor(c);
        }

        /// <summary>GCLEAR - overwrite every pixel with c.</summary>
        public void Clear(Color c)
        {
            uint v = PackColor(c);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = v;
        }

        /// <summary>Draws a line using Bresenham's line algorithm with source-over blending.</summary>
        public void DrawLine(Color c, int x1, int y1, int x2, int y2)
        {
            uint src = PackColor(c);
            int sa = (int)(src >> 24) & 0xFF;
            if (sa == 0) return;
            int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
            int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                SetPixelRaw(src, sa, x1, y1);
                if (x1 == x2 && y1 == y2) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x1 += sx; }
                if (e2 <= dx) { err += dx; y1 += sy; }
            }
        }

        private void SetPixelRaw(uint src, int sa, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int idx = y * width + x;
            if (sa >= 255) pixels[idx] = src;
            else pixels[idx] = BlendOver(src, pixels[idx]);
        }

        /// <summary>
        /// GFILLRECTANGLE - fills a rectangle, clipping to surface bounds.
        /// Semantics of reference Emuera: an alpha 255 brush overwrites, a
        /// semi-transparent brush blends (GDI+ FillRectangle formulas).
        /// </summary>
        public void FillRectangle(Rectangle rect, Color c)
        {
            uint src = PackColor(c);
            int sa = (int)(src >> 24) & 0xFF;
            int startX = rect.Width > 0 ? rect.X : rect.X + rect.Width;
            int endX = rect.Width > 0 ? rect.X + rect.Width : rect.X;
            int startY = rect.Height > 0 ? rect.Y : rect.Y + rect.Height;
            int endY = rect.Height > 0 ? rect.Y + rect.Height : rect.Y;
            startX = Math.Max(startX, 0);
            startY = Math.Max(startY, 0);
            endX = Math.Min(endX, width);
            endY = Math.Min(endY, height);
            if (endX <= startX || endY <= startY)
                return;
            if (sa >= 255)
            {
                for (int y = startY; y < endY; y++)
                {
                    int row = y * width;
                    for (int x = startX; x < endX; x++)
                        pixels[row + x] = src;
                }
                return;
            }
            if (sa <= 0)
                return;
            for (int y = startY; y < endY; y++)
            {
                int row = y * width;
                for (int x = startX; x < endX; x++)
                    pixels[row + x] = BlendOver(src, pixels[row + x]);
            }
        }

        /// <summary>
        /// Draws a source rectangle of another surface into a destination rectangle
        /// with source-over alpha compositing. Mirrors GDI+ DrawImage semantics
        /// including flip by negative destination width/height: when the dest
        /// dimension is negative, the source is reversed along that axis.
        /// </summary>
        public void DrawImage(GraphicsSurface src, Rectangle destRect, Rectangle srcRect)
        {
            if (src == null || src.IsEmpty)
                return;
            int srcW = src.width;
            int srcH = src.height;
            uint[] sp = src.pixels;
            DrawCore(srcW, srcH, sp, destRect, srcRect, null);
        }

        /// <summary>
        /// GDI-style draw from a <see cref="Bitmap"/>. Reads pixels via
        /// <see cref="Bitmap.GetPixel"/> which in this build is backed by a Unity
        /// texture, so this must only run on the main thread.
        /// </summary>
        public void DrawBitmap(Bitmap bmp, Rectangle destRect, Rectangle srcRect, float[][] cm)
        {
            if (bmp == null)
                return;
            int srcW = bmp.Width;
            int srcH = bmp.Height;
            if (srcW <= 0 || srcH <= 0)
                return;
            uint[] sp = new uint[srcW * srcH];
            for (int y = 0; y < srcH; y++)
                for (int x = 0; x < srcW; x++)
                    sp[y * srcW + x] = PackColor(bmp.GetPixel(x, y));
            DrawCore(srcW, srcH, sp, destRect, srcRect, cm);
        }

        void DrawCore(int srcW, int srcH, uint[] sp,
            Rectangle destRect, Rectangle srcRect, float[][] cm)
        {
            if (srcW <= 0 || srcH <= 0)
                return;
            // Resolve the physical area covered by the destination rect, accepting
            // negative width/height (GDI-style flip) Rectangles.
            int dLeft = destRect.Width >= 0 ? destRect.X : destRect.X + destRect.Width;
            int dTop = destRect.Height >= 0 ? destRect.Y : destRect.Y + destRect.Height;
            int dRight = destRect.Width >= 0 ? destRect.X + destRect.Width : destRect.X;
            int dBottom = destRect.Height >= 0 ? destRect.Y + destRect.Height : destRect.Y;

            if (dRight <= dLeft || dBottom <= dTop)
                return;
            int cx1 = Math.Max(dLeft, 0);
            int cy1 = Math.Max(dTop, 0);
            int cx2 = Math.Min(dRight, width);
            int cy2 = Math.Min(dBottom, height);
            if (cx2 <= cx1 || cy2 <= cy1)
                return;

            // Source rect effective origin + length, axes adjusted by their sign.
            int sLeft = srcRect.Width >= 0 ? srcRect.X : srcRect.X + srcRect.Width;
            int sTop = srcRect.Height >= 0 ? srcRect.Y : srcRect.Y + srcRect.Height;
            int sW = Math.Abs(srcRect.Width);
            int sH = Math.Abs(srcRect.Height);
            if (sW <= 0 || sH <= 0)
                return;

            float[][] cmv = cm;
            bool hasCm = cmv != null && cmv.Length > 0 && cmv[0] != null;
            float m00 = hasCm && cmv[0].Length > 0 ? cmv[0][0] : 1;
            float m01 = hasCm && cmv[0].Length > 1 ? cmv[0][1] : 0;
            float m02 = hasCm && cmv[0].Length > 2 ? cmv[0][2] : 0;
            float m03 = hasCm && cmv[0].Length > 3 ? cmv[0][3] : 0;
            float m10 = hasCm && cmv.Length > 1 && cmv[1].Length > 0 ? cmv[1][0] : 0;
            float m11 = hasCm && cmv.Length > 1 && cmv[1].Length > 1 ? cmv[1][1] : 1;
            float m12 = hasCm && cmv.Length > 1 && cmv[1].Length > 2 ? cmv[1][2] : 0;
            float m13 = hasCm && cmv.Length > 1 && cmv[1].Length > 3 ? cmv[1][3] : 0;
            float m20 = hasCm && cmv.Length > 2 && cmv[2].Length > 0 ? cmv[2][0] : 0;
            float m21 = hasCm && cmv.Length > 2 && cmv[2].Length > 1 ? cmv[2][1] : 0;
            float m22 = hasCm && cmv.Length > 2 && cmv[2].Length > 2 ? cmv[2][2] : 1;
            float m23 = hasCm && cmv.Length > 2 && cmv[2].Length > 3 ? cmv[2][3] : 0;
            float m30 = hasCm && cmv.Length > 3 && cmv[3].Length > 0 ? cmv[3][0] : 0;
            float m31 = hasCm && cmv.Length > 3 && cmv[3].Length > 1 ? cmv[3][1] : 0;
            float m32 = hasCm && cmv.Length > 3 && cmv[3].Length > 2 ? cmv[3][2] : 0;
            float m33 = hasCm && cmv.Length > 3 && cmv[3].Length > 3 ? cmv[3][3] : 1;
            float bt = hasCm && cmv.Length > 4 && cmv[4].Length > 0 ? cmv[4][0] : 0;
            float bt1 = hasCm && cmv.Length > 4 && cmv[4].Length > 1 ? cmv[4][1] : 0;
            float bt2 = hasCm && cmv.Length > 4 && cmv[4].Length > 2 ? cmv[4][2] : 0;
            float bt3 = hasCm && cmv.Length > 4 && cmv[4].Length > 3 ? cmv[4][3] : 0;

            int dW = dRight - dLeft;
            int dH = dBottom - dTop;
            bool destFlipX = destRect.Width < 0;
            bool destFlipY = destRect.Height < 0;
            for (int y = cy1; y < cy2; y++)
            {
                float ny = (float)(y - dTop) / dH;
                if (ny < 0f) ny = 0f;
                else if (ny > 1f) ny = 1f;
                int sy;
                if (destFlipY != (srcRect.Height < 0))
                    sy = sTop + (int)((1f - ny) * (sH - 1));
                else
                    sy = sTop + (int)(ny * sH);
                if (sy < 0 || sy >= srcH)
                    continue;
                int row = y * width;
                for (int x = cx1; x < cx2; x++)
                {
                    float nx = (float)(x - dLeft) / dW;
                    if (nx < 0f) nx = 0f;
                    else if (nx > 1f) nx = 1f;
                    int sx;
                    if (destFlipX != (srcRect.Width < 0))
                        sx = sLeft + (int)((1f - nx) * (sW - 1));
                    else
                        sx = sLeft + (int)(nx * sW);
                    if (sx < 0 || sx >= srcW)
                        continue;
                    uint s = sp[sy * srcW + sx];
                    uint pix;
                    if (hasCm)
                    {
                        float R = (s >> 16) & 0xFF, G = (s >> 8) & 0xFF, B = s & 0xFF, A = (s >> 24) & 0xFF;
                        pix = PackA(
                            (int)(R * m30 + G * m31 + B * m32 + A * m33 + bt3),
                            (int)(R * m00 + G * m01 + B * m02 + A * m03 + bt),
                            (int)(R * m10 + G * m11 + B * m12 + A * m13 + bt1),
                            (int)(R * m20 + G * m21 + B * m22 + A * m23 + bt2));
                    }
                    else
                    {
                        pix = s;
                    }
                    int sa = (int)((pix >> 24) & 0xFF);
                    if (sa >= 255)
                        pixels[row + x] = pix;
                    else if (sa > 0)
                        pixels[row + x] = BlendOver(pix, pixels[row + x]);
                }
            }
        }

        /// <summary>
        /// GDRAWGWITHMASK - draws the source surface onto this surface using a mask
        /// surface. Mask alpha channel acts as the opacity factor for the source
        /// (255 = opaque copy, 128 = ~50%, 0 = untouched). Identical pixel layout.
        /// </summary>
        public void DrawImageMasked(GraphicsSurface src, GraphicsSurface mask, Point destPoint)
        {
            if (src == null || src.IsEmpty || mask == null || mask.IsEmpty)
                return;
            uint[] sp = src.pixels;
            uint[] mp = mask.pixels;
            int sw = src.width;
            int mw = mask.width;
            for (int y = 0; y < src.height; y++)
            {
                int dy = destPoint.Y + y;
                if (dy < 0 || dy >= height)
                    continue;
                int row = dy * width;
                int srow = y * sw;
                int mrow = y * mw;
                for (int x = 0; x < src.width; x++)
                {
                    int dx = destPoint.X + x;
                    if (dx < 0 || dx >= width)
                        continue;
                    int maskA = (int)((mp[mrow + x] >> 24) & 0xFF);
                    if (maskA == 0)
                        continue;
                    uint spix = sp[srow + x];
                    if (maskA >= 255)
                    {
                        int ssa = (int)((spix >> 24) & 0xFF);
                        if (ssa >= 255)
                            pixels[row + dx] = spix;
                        else if (ssa > 0)
                            pixels[row + dx] = BlendOver(spix, pixels[row + dx]);
                    }
                    else
                    {
                        // Composite opacity = mask alpha/255 of source-over result.
                        uint over = BlendOver(spix, pixels[row + dx]);
                        pixels[row + dx] = BlendOver(WithAlpha(over, maskA), pixels[row + dx]);
                    }
                }
            }
        }

        static uint PackA(int a, int r, int g, int b)
        {
            return PackBytes(Clamp255(a), Clamp255(r), Clamp255(g), Clamp255(b));
        }

        /// <summary>
        /// Applies a 5x4 (row-major 5 rows) color matrix to every pixel in place.
        /// Used for the GDrawG/ cm override path where the source is first remapped
        /// into a temp surface then re-colored.
        /// </summary>
        public void ApplyColorMatrixInPlace(float[][] cm)
        {
            if (cm == null || cm.Length == 0 || cm[0] == null)
                return;
            float m00 = cm[0].Length > 0 ? cm[0][0] : 0;
            float m01 = cm[0].Length > 1 ? cm[0][1] : 0;
            float m02 = cm[0].Length > 2 ? cm[0][2] : 0;
            float m03 = cm[0].Length > 3 ? cm[0][3] : 0;
            float m10 = cm.Length > 1 && cm[1].Length > 0 ? cm[1][0] : 0;
            float m11 = cm.Length > 1 && cm[1].Length > 1 ? cm[1][1] : 0;
            float m12 = cm.Length > 1 && cm[1].Length > 2 ? cm[1][2] : 0;
            float m13 = cm.Length > 1 && cm[1].Length > 3 ? cm[1][3] : 0;
            float m20 = cm.Length > 2 && cm[2].Length > 0 ? cm[2][0] : 0;
            float m21 = cm.Length > 2 && cm[2].Length > 1 ? cm[2][1] : 0;
            float m22 = cm.Length > 2 && cm[2].Length > 2 ? cm[2][2] : 0;
            float m23 = cm.Length > 2 && cm[2].Length > 3 ? cm[2][3] : 0;
            float m30 = cm.Length > 3 && cm[3].Length > 0 ? cm[3][0] : 0;
            float m31 = cm.Length > 3 && cm[3].Length > 1 ? cm[3][1] : 0;
            float m32 = cm.Length > 3 && cm[3].Length > 2 ? cm[3][2] : 0;
            float m33 = cm.Length > 3 && cm[3].Length > 3 ? cm[3][3] : 0;
            float bt = cm.Length > 4 && cm[4].Length > 0 ? cm[4][0] : 0;
            float bt1 = cm.Length > 4 && cm[4].Length > 1 ? cm[4][1] : 0;
            float bt2 = cm.Length > 4 && cm[4].Length > 2 ? cm[4][2] : 0;
            float bt3 = cm.Length > 4 && cm[4].Length > 3 ? cm[4][3] : 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                uint v = pixels[i];
                float R = (v >> 16) & 0xFF, G = (v >> 8) & 0xFF, B = v & 0xFF, A = (v >> 24) & 0xFF;
                pixels[i] = BlendOver(PackA(
                    (int)(R * m30 + G * m31 + B * m32 + A * m33 + bt3),
                    (int)(R * m00 + G * m01 + B * m02 + A * m03 + bt),
                    (int)(R * m10 + G * m11 + B * m12 + A * m13 + bt1),
                    (int)(R * m20 + G * m21 + B * m22 + A * m23 + bt2)), pixels[i]);
            }
        }

        static int Clamp255(int v)
        {
            if (v <= 0) return 0;
            if (v >= 255) return 255;
            return v;
        }

        static uint WithAlpha(uint px, int a)
        {
            return (px & 0x00FFFFFFu) | ((uint)(a & 0xFF) << 24);
        }

        /// <summary>Source-over copy: result = src over dst (straight alpha).</summary>
        internal static uint BlendOver(uint src, uint dst)
        {
            int sa = (int)((src >> 24) & 0xFF);
            if (sa == 0)
                return dst;
            if (sa == 255)
                return src;
            int dr = (int)(dst >> 16) & 0xFF;
            int dg = (int)(dst >> 8) & 0xFF;
            int db = (int)(dst) & 0xFF;
            int da = (int)(dst >> 24) & 0xFF;
            int sr = (int)(src >> 16) & 0xFF;
            int sg = (int)(src >> 8) & 0xFF;
            int sb = (int)(src) & 0xFF;
            int inv = 255 - sa;
            int r = (sr * sa + dr * inv) / 255;
            int g = (sg * sa + dg * inv) / 255;
            int b = (sb * sa + db * inv) / 255;
            int a = sa + (da * (255 - sa)) / 255;
            return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
        }
    }
}