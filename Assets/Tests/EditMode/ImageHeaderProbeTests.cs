using NUnit.Framework;
using uEmuera;

namespace uEmuera.Tests.EditMode
{
    /// <summary>
    /// Tests for <see cref="ImageHeaderProbe"/> — header-only dimension reads that must
    /// never require a full image decode (Phase 6 #5).
    /// </summary>
    [TestFixture]
    public class ImageHeaderProbeTests
    {
        static byte[] MakePng(int w, int h)
        {
            var d = new byte[24];
            d[0] = 0x89; d[1] = 0x50; d[2] = 0x4E; d[3] = 0x47;
            d[4] = 0x0D; d[5] = 0x0A; d[6] = 0x1A; d[7] = 0x0A;
            // length(4) + "IHDR"
            d[12] = (byte)'I'; d[13] = (byte)'H'; d[14] = (byte)'D'; d[15] = (byte)'R';
            d[16] = (byte)(w >> 24); d[17] = (byte)(w >> 16); d[18] = (byte)(w >> 8); d[19] = (byte)w;
            d[20] = (byte)(h >> 24); d[21] = (byte)(h >> 16); d[22] = (byte)(h >> 8); d[23] = (byte)h;
            return d;
        }

        static byte[] MakeGif(int w, int h)
        {
            var d = new byte[13];
            d[0] = (byte)'G'; d[1] = (byte)'I'; d[2] = (byte)'F';
            d[3] = (byte)'8'; d[4] = (byte)'9'; d[5] = (byte)'a';
            d[6] = (byte)w; d[7] = (byte)(w >> 8);
            d[8] = (byte)h; d[9] = (byte)(h >> 8);
            return d;
        }

        static byte[] MakeBmp(int w, int h)
        {
            var d = new byte[26];
            d[0] = (byte)'B'; d[1] = (byte)'M';
            d[18] = (byte)w; d[19] = (byte)(w >> 8); d[20] = (byte)(w >> 16); d[21] = (byte)(w >> 24);
            d[22] = (byte)h; d[23] = (byte)(h >> 8); d[24] = (byte)(h >> 16); d[25] = (byte)(h >> 24);
            return d;
        }

        static byte[] MakeJpeg(int w, int h)
        {
            // SOI, an APP0 segment, then SOF0 with the dimensions.
            var d = new byte[]
            {
                0xFF, 0xD8,                         // SOI
                0xFF, 0xE0, 0x00, 0x04, 0x01, 0x02, // APP0 len=4 (2 payload bytes)
                0xFF, 0xC0, 0x00, 0x11, 0x08,       // SOF0 len=17, precision=8
                (byte)(h >> 8), (byte)h,
                (byte)(w >> 8), (byte)w,
                0x03, 0x01, 0x22, 0x00              // components (partial, unused)
            };
            return d;
        }

        static byte[] MakeWebpVp8(int w, int h)
        {
            var d = new byte[30];
            d[0] = (byte)'R'; d[1] = (byte)'I'; d[2] = (byte)'F'; d[3] = (byte)'F';
            d[8] = (byte)'W'; d[9] = (byte)'E'; d[10] = (byte)'B'; d[11] = (byte)'P';
            d[12] = (byte)'V'; d[13] = (byte)'P'; d[14] = (byte)'8'; d[15] = (byte)' ';
            d[20] = 0x9D; d[21] = 0x01; d[22] = 0x2A;
            d[23] = (byte)(w & 0xFF); d[24] = (byte)((w >> 8) & 0x3F);
            d[25] = (byte)(h & 0xFF); d[26] = (byte)((h >> 8) & 0x3F);
            return d;
        }

        static byte[] MakeWebpVp8x(int w, int h)
        {
            var d = new byte[30];
            d[0] = (byte)'R'; d[1] = (byte)'I'; d[2] = (byte)'F'; d[3] = (byte)'F';
            d[8] = (byte)'W'; d[9] = (byte)'E'; d[10] = (byte)'B'; d[11] = (byte)'P';
            d[12] = (byte)'V'; d[13] = (byte)'P'; d[14] = (byte)'8'; d[15] = (byte)'X';
            int wm1 = w - 1, hm1 = h - 1;
            d[24] = (byte)(wm1 & 0xFF); d[25] = (byte)((wm1 >> 8) & 0xFF); d[26] = (byte)((wm1 >> 16) & 0xFF);
            d[27] = (byte)(hm1 & 0xFF); d[28] = (byte)((hm1 >> 8) & 0xFF); d[29] = (byte)((hm1 >> 16) & 0xFF);
            return d;
        }

        [Test]
        public void Png_ReadsDimensions()
        {
            Assert.IsTrue(ImageHeaderProbe.TryReadBytes(MakePng(640, 480), out var info));
            Assert.AreEqual(ImageHeaderFormat.Png, info.Format);
            Assert.AreEqual(640, info.Width);
            Assert.AreEqual(480, info.Height);
        }

        [Test]
        public void Gif_ReadsDimensions()
        {
            Assert.IsTrue(ImageHeaderProbe.TryReadBytes(MakeGif(300, 200), out var info));
            Assert.AreEqual(ImageHeaderFormat.Gif, info.Format);
            Assert.AreEqual(300, info.Width);
            Assert.AreEqual(200, info.Height);
        }

        [Test]
        public void Bmp_ReadsDimensions()
        {
            Assert.IsTrue(ImageHeaderProbe.TryReadBytes(MakeBmp(128, 256), out var info));
            Assert.AreEqual(ImageHeaderFormat.Bmp, info.Format);
            Assert.AreEqual(128, info.Width);
            Assert.AreEqual(256, info.Height);
        }

        [Test]
        public void Bmp_TopDown_NegativeHeightNormalized()
        {
            Assert.IsTrue(ImageHeaderProbe.TryReadBytes(MakeBmp(64, -48), out var info));
            Assert.AreEqual(64, info.Width);
            Assert.AreEqual(48, info.Height);
        }

        [Test]
        public void Jpeg_ReadsDimensionsAfterAppSegment()
        {
            Assert.IsTrue(ImageHeaderProbe.TryReadBytes(MakeJpeg(1920, 1080), out var info));
            Assert.AreEqual(ImageHeaderFormat.Jpeg, info.Format);
            Assert.AreEqual(1920, info.Width);
            Assert.AreEqual(1080, info.Height);
        }

        [Test]
        public void WebpVp8_ReadsDimensions()
        {
            Assert.IsTrue(ImageHeaderProbe.TryReadBytes(MakeWebpVp8(800, 600), out var info));
            Assert.AreEqual(ImageHeaderFormat.WebP, info.Format);
            Assert.AreEqual(800, info.Width);
            Assert.AreEqual(600, info.Height);
        }

        [Test]
        public void WebpVp8x_ReadsDimensions()
        {
            Assert.IsTrue(ImageHeaderProbe.TryReadBytes(MakeWebpVp8x(4096, 2048), out var info));
            Assert.AreEqual(ImageHeaderFormat.WebP, info.Format);
            Assert.AreEqual(4096, info.Width);
            Assert.AreEqual(2048, info.Height);
        }

        [Test]
        public void Garbage_ReturnsFalse()
        {
            Assert.IsFalse(ImageHeaderProbe.TryReadBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, out var info));
            Assert.IsFalse(info.HasValue);
        }

        [Test]
        public void Null_And_Empty_ReturnFalse()
        {
            Assert.IsFalse(ImageHeaderProbe.TryReadBytes(null, out _));
            Assert.IsFalse(ImageHeaderProbe.TryReadBytes(new byte[0], out _));
            Assert.IsFalse(ImageHeaderProbe.TryReadFile(null, out _));
            Assert.IsFalse(ImageHeaderProbe.TryReadFile("Z:/does/not/exist_k\u00e4se.png", out _));
        }

        [Test]
        public void Truncated_Png_ReturnsFalse()
        {
            var d = MakePng(10, 10);
            System.Array.Resize(ref d, 16); // cut before dimensions
            Assert.IsFalse(ImageHeaderProbe.TryReadBytes(d, out _));
        }
    }
}
