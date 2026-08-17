using System;
using System.IO;

namespace uEmuera
{
    public enum ImageHeaderFormat
    {
        Unknown = 0,
        Png,
        Jpeg,
        Bmp,
        Gif,
        WebP,
    }

    /// <summary>
    /// Pixel dimensions of an image discovered from its header, without decoding the
    /// full bitmap. Used by the resource pipeline so that startup never instantiates a
    /// Texture2D merely to learn width/height (uEmuera Phase 6 #5).
    /// </summary>
    public struct ImageHeaderInfo
    {
        public bool HasValue;
        public int Width;
        public int Height;
        public ImageHeaderFormat Format;

        public override string ToString()
        {
            return HasValue
                ? string.Format("{0} {1}x{2}", Format, Width, Height)
                : "Unknown";
        }
    }

    /// <summary>
    /// Reads image dimensions (width/height) from just the header bytes of common
    /// formats — PNG, JPEG, BMP, GIF, WebP — without a full decode. Every parser is
    /// bounds-checked and returns false rather than throwing on malformed input.
    /// </summary>
    public static class ImageHeaderProbe
    {
        // Enough bytes for all fixed-position headers. JPEG SOF markers can appear
        // later (after EXIF/thumbnail); the file reader below streams further for JPEG.
        const int PrefixBytes = 64 * 1024;

        /// <summary>
        /// Reads only enough of <paramref name="path"/> to determine the image
        /// dimensions. Returns false if the file is missing/unreadable or the format
        /// is unrecognized.
        /// </summary>
        public static bool TryReadFile(string path, out ImageHeaderInfo info)
        {
            info = default(ImageHeaderInfo);
            if (string.IsNullOrEmpty(path))
                return false;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long len = fs.Length;
                    int toRead = (int)Math.Min(len, PrefixBytes);
                    byte[] buffer = new byte[toRead];
                    int read = ReadFully(fs, buffer, 0, toRead);
                    if (read <= 0)
                        return false;
                    if (read != buffer.Length)
                        Array.Resize(ref buffer, read);

                    // JPEG may need more than the prefix to reach the SOF marker; give
                    // it the whole file (still just bytes, no decode) up to a sane cap.
                    if (read >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8 && len > read && len <= 8 * 1024 * 1024)
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                        buffer = new byte[(int)len];
                        read = ReadFully(fs, buffer, 0, buffer.Length);
                        if (read != buffer.Length)
                            Array.Resize(ref buffer, read);
                    }
                    return TryReadBytes(buffer, out info);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses dimensions from an in-memory image header.
        /// </summary>
        public static bool TryReadBytes(byte[] data, out ImageHeaderInfo info)
        {
            info = default(ImageHeaderInfo);
            if (data == null || data.Length < 8)
                return false;

            if (TryPng(data, ref info)) return true;
            if (TryGif(data, ref info)) return true;
            if (TryBmp(data, ref info)) return true;
            if (TryWebP(data, ref info)) return true;
            if (TryJpeg(data, ref info)) return true;
            return false;
        }

        // ---- PNG -----------------------------------------------------------
        // 8-byte signature, then IHDR: width @16 (BE), height @20 (BE).
        static bool TryPng(byte[] d, ref ImageHeaderInfo info)
        {
            if (d.Length < 24)
                return false;
            if (d[0] != 0x89 || d[1] != 0x50 || d[2] != 0x4E || d[3] != 0x47 ||
                d[4] != 0x0D || d[5] != 0x0A || d[6] != 0x1A || d[7] != 0x0A)
                return false;
            // IHDR must be the first chunk.
            if (d[12] != 'I' || d[13] != 'H' || d[14] != 'D' || d[15] != 'R')
                return false;
            int w = ReadBE32(d, 16);
            int h = ReadBE32(d, 20);
            if (w <= 0 || h <= 0)
                return false;
            info = new ImageHeaderInfo { HasValue = true, Width = w, Height = h, Format = ImageHeaderFormat.Png };
            return true;
        }

        // ---- GIF -----------------------------------------------------------
        // "GIF87a"/"GIF89a", width @6 (LE16), height @8 (LE16).
        static bool TryGif(byte[] d, ref ImageHeaderInfo info)
        {
            if (d.Length < 10)
                return false;
            if (d[0] != 'G' || d[1] != 'I' || d[2] != 'F')
                return false;
            int w = d[6] | (d[7] << 8);
            int h = d[8] | (d[9] << 8);
            if (w <= 0 || h <= 0)
                return false;
            info = new ImageHeaderInfo { HasValue = true, Width = w, Height = h, Format = ImageHeaderFormat.Gif };
            return true;
        }

        // ---- BMP -----------------------------------------------------------
        // "BM", width @18 (LE32), height @22 (LE32, may be negative = top-down).
        static bool TryBmp(byte[] d, ref ImageHeaderInfo info)
        {
            if (d.Length < 26)
                return false;
            if (d[0] != 'B' || d[1] != 'M')
                return false;
            int w = ReadLE32(d, 18);
            int h = ReadLE32(d, 22);
            if (h < 0) h = -h;
            if (w <= 0 || h <= 0)
                return false;
            info = new ImageHeaderInfo { HasValue = true, Width = w, Height = h, Format = ImageHeaderFormat.Bmp };
            return true;
        }

        // ---- WebP ----------------------------------------------------------
        // "RIFF"????"WEBP" then a VP8 / VP8L / VP8X chunk.
        static bool TryWebP(byte[] d, ref ImageHeaderInfo info)
        {
            if (d.Length < 30)
                return false;
            if (d[0] != 'R' || d[1] != 'I' || d[2] != 'F' || d[3] != 'F' ||
                d[8] != 'W' || d[9] != 'E' || d[10] != 'B' || d[11] != 'P')
                return false;

            // Chunk FourCC at offset 12.
            if (d[12] == 'V' && d[13] == 'P' && d[14] == '8' && d[15] == ' ')
            {
                // Simple lossy: frame tag (3 bytes) then 0x9D 0x01 0x2A start code at
                // offset 20, then 14-bit width and 14-bit height (little-endian).
                int p = 20;
                if (d.Length < p + 7)
                    return false;
                if (d[p] != 0x9D || d[p + 1] != 0x01 || d[p + 2] != 0x2A)
                    return false;
                int w = (d[p + 3] | (d[p + 4] << 8)) & 0x3FFF;
                int h = (d[p + 5] | (d[p + 6] << 8)) & 0x3FFF;
                if (w <= 0 || h <= 0)
                    return false;
                info = new ImageHeaderInfo { HasValue = true, Width = w, Height = h, Format = ImageHeaderFormat.WebP };
                return true;
            }
            if (d[12] == 'V' && d[13] == 'P' && d[14] == '8' && d[15] == 'L')
            {
                // Lossless: [20]=0x2F signature, then 14-bit (w-1), 14-bit (h-1).
                int p = 20;
                if (d.Length < p + 5 || d[p] != 0x2F)
                    return false;
                int b0 = d[p + 1], b1 = d[p + 2], b2 = d[p + 3], b3 = d[p + 4];
                int bits = b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
                int w = (bits & 0x3FFF) + 1;
                int h = ((bits >> 14) & 0x3FFF) + 1;
                if (w <= 0 || h <= 0)
                    return false;
                info = new ImageHeaderInfo { HasValue = true, Width = w, Height = h, Format = ImageHeaderFormat.WebP };
                return true;
            }
            if (d[12] == 'V' && d[13] == 'P' && d[14] == '8' && d[15] == 'X')
            {
                // Extended: flags @20 (1 byte) + reserved, canvas width-1 @24 (LE24),
                // height-1 @27 (LE24).
                if (d.Length < 30)
                    return false;
                int w = (d[24] | (d[25] << 8) | (d[26] << 16)) + 1;
                int h = (d[27] | (d[28] << 8) | (d[29] << 16)) + 1;
                if (w <= 0 || h <= 0)
                    return false;
                info = new ImageHeaderInfo { HasValue = true, Width = w, Height = h, Format = ImageHeaderFormat.WebP };
                return true;
            }
            return false;
        }

        // ---- JPEG ----------------------------------------------------------
        // Walk marker segments until an SOF marker, whose payload holds height/width.
        static bool TryJpeg(byte[] d, ref ImageHeaderInfo info)
        {
            if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8)
                return false;
            int pos = 2;
            while (pos + 4 < d.Length)
            {
                // Markers are 0xFF followed by a non-0xFF, non-0x00 id. Skip fill bytes.
                if (d[pos] != 0xFF)
                {
                    pos++;
                    continue;
                }
                byte marker = d[pos + 1];
                if (marker == 0xFF)
                {
                    pos++;
                    continue;
                }
                // Standalone markers without length payload.
                if (marker == 0xD8 || marker == 0xD9 ||
                    (marker >= 0xD0 && marker <= 0xD7) || marker == 0x01)
                {
                    pos += 2;
                    continue;
                }
                if (pos + 4 > d.Length)
                    break;
                int segLen = (d[pos + 2] << 8) | d[pos + 3];
                if (segLen < 2)
                    return false;
                // SOF0..SOF15 carry the frame size, excluding DHT/DAC/etc.
                bool isSof = (marker >= 0xC0 && marker <= 0xCF) &&
                             marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                if (isSof)
                {
                    // Payload: [precision(1)][height(2 BE)][width(2 BE)]...
                    if (pos + 9 > d.Length)
                        return false;
                    int h = (d[pos + 5] << 8) | d[pos + 6];
                    int w = (d[pos + 7] << 8) | d[pos + 8];
                    if (w <= 0 || h <= 0)
                        return false;
                    info = new ImageHeaderInfo { HasValue = true, Width = w, Height = h, Format = ImageHeaderFormat.Jpeg };
                    return true;
                }
                pos += 2 + segLen;
            }
            return false;
        }

        // ---- helpers -------------------------------------------------------
        static int ReadBE32(byte[] d, int o)
        {
            return (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];
        }

        static int ReadLE32(byte[] d, int o)
        {
            return d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);
        }

        static int ReadFully(Stream s, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int n = s.Read(buffer, offset + total, count - total);
                if (n <= 0)
                    break;
                total += n;
            }
            return total;
        }
    }
}
