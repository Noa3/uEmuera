using System;
using Unity.Mathematics;

namespace uEmuera.Drawing
{
    public class Bitmap : IDisposable
    {
        public Bitmap(string path)
        {
            this.path = path;
            // Trim and normalize filename to prevent whitespace issues
            this.filename = GenericUtils.GetFilename(path).Trim();
        }

        public readonly string path;
        public readonly string filename;
        public string name;
        public Size size;

        public void Dispose()
        { }
        public int Width
        {
            get { return size.Width; }
        }
        public int Height
        {
            get { return size.Height; }
        }
        public Size Size { get { return size; } }
        public Color GetPixel(int x, int y)
        {
            var ti = SpriteManager.GetTextureInfo(name, path);
            var uc = ti.texture.GetPixel(x, y);
            return new Color(uc.r, uc.g, uc.b, uc.a);
        }
        public void SetPixel(Color c, int x, int y)
        {
            var ti = SpriteManager.GetTextureInfo(name, path);
            ti.texture.SetPixel(x, y, new UnityEngine.Color(c.r, c.g, c.b, c.a));
        }
        public void Save(string path)
        {
            var ti = SpriteManager.GetTextureInfo(name, path);
            var data = UnityEngine.ImageConversion.EncodeToPNG(ti.texture);
            System.IO.File.WriteAllBytes(path, data);
        }
    }

    public class BitmapTexture : Bitmap
    {
        public BitmapTexture(string path)
            :base(path)
        {
            var name = string.Concat(":FILE:", filename);
            var tiot = SpriteManager.GetTextureInfoOtherThread(name, path,
                ret =>
                {
                    textureinfo = ret;
                    if(textureinfo == null)
                        return;
                    size.Width = ret.texture.width;
                    size.Height = ret.texture.height;
                });
            
            // Use SpinWait for more efficient waiting on other thread
            var spinWait = new System.Threading.SpinWait();
            while(tiot.mutex == null)
            {
                spinWait.SpinOnce();
                if (spinWait.NextSpinWillYield)
                    System.Threading.Thread.Sleep(1);
            }
            tiot.mutex.WaitOne();

            if(textureinfo == null)
                return;
            tiot.mutex.ReleaseMutex();
            tiot.mutex.Close();
        }
        public UnityEngine.Texture2D texture
        {
            get { return textureinfo.texture; }
        }
        SpriteManager.TextureInfo textureinfo = null;
    }

    public class BitmapRenderTexture : Bitmap
    {
        public BitmapRenderTexture(int x, int y)
            :base(null)
        {
            //var rtot = SpriteManager.GetRenderTextureOtherThread(x, y,
            //    ret =>
            //    {
            //        rt = ret;
            //        size.Width = ret.width;
            //        size.Height = ret.height;
            //    });

            size.Width = x;
            size.Height = y;
        }
        //UnityEngine.RenderTexture rt = null;
    }

    public enum GraphicsUnit
    {
        World = 0,
        Display = 1,
        Pixel = 2,
        Point = 3,
        Inch = 4,
        Document = 5,
        Millimeter = 6
    }

    public sealed class Graphics
    {
        public static Graphics instance
        {
            get
            {
                if(instance_ == null)
                    instance_ = new Graphics();
                return instance_;
            }
        }
        static Graphics instance_ = null;

        private Graphics() { }

        public void Clear() { }
        public void DrawImage(Bitmap texture, Rectangle destrect,
                            Rectangle srcrect, GraphicsUnit unit)
        {
            uEmuera.Logger.Info("Graphics.DrawImage " + texture.name);
        }
        public void DrawImage(Bitmap texture, Rectangle destrect,
                            int x, int y, int w, int h, GraphicsUnit unit, ImageAttributes ia)
        {
            uEmuera.Logger.Info("Graphics.DrawImage " + texture.name);
        }
        public void DrawString(string s, Font font, Brush brush, Point point)
        {
            uEmuera.Logger.Info("Graphics.DrawString " + s);
        }
        public void FillRectangel(SolidBrush brush, Rectangle rect)
        { }
        public void Clear(Color color)
        {
            uEmuera.Logger.Info("Graphics.Clear " + color.ToArgb());
        }
    }

    public class Brush
    { }

    public sealed class SolidBrush : Brush
    {
        public SolidBrush(Color color)
        {
            Color = color;
        }

        public Color Color { get; set; }
    }

    public sealed class Pen
    {
        public Pen()
        { }
        public Pen(Color c, Int64 width)
        { }
    }

    public enum FontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikeout = 8
    }

    public class FontFamily
    {
        public FontFamily(string name)
        {
            this.name = name;
        }
        public string Name { get { return name; } }
        string name;
    }

    public sealed class Font : IDisposable
    {
        static bool GetMonospaced(string name)
        {
            return !monospaced_disable_set.Contains(name);
        }
        static readonly System.Collections.Generic.HashSet<string> monospaced_disable_set = 
            new System.Collections.Generic.HashSet<string>
        {
            "ＭＳ Ｐゴシック",
            "MS PGothic",
        };

        public Font(string familyName, float emSize, FontStyle style, 
            GraphicsUnit unit)
        {
            fontFamily = new FontFamily(familyName);
            monospaced = GetMonospaced(familyName);
            size = emSize;
            fontStyle = style;
            graphicsUnit = unit;
        }
        public Font(string familyName, float emSize, FontStyle style, 
            GraphicsUnit unit, byte gdiCharSet)
        {
            fontFamily = new FontFamily(familyName);
            monospaced = GetMonospaced(familyName);
            size = emSize;
            fontStyle = style;
            graphicsUnit = unit;
        }
        public Font(string familyName, float emSize, FontStyle style,
            GraphicsUnit unit, byte gdiCharSet, bool gdiVericalFont)
        {
            fontFamily = new FontFamily(familyName);
            monospaced = GetMonospaced(familyName);
            size = emSize;
            fontStyle = style;
            graphicsUnit = unit;
        }

        public void Dispose()
        { }

        public FontFamily FontFamily { get { return fontFamily; } }
        FontFamily fontFamily;

        public bool Monospaced { get { return monospaced; } }
        bool monospaced = true;

        public float Size { get { return size; } }
        float size;

        public FontStyle Style { get { return fontStyle; } }
        FontStyle fontStyle;

        public bool Bold { get { return (fontStyle & FontStyle.Bold) > 0; } }
        public bool Italic { get { return (fontStyle & FontStyle.Italic) > 0; } }
        public bool Underline { get { return (fontStyle & FontStyle.Underline) > 0; } }
        public bool Strikeout { get { return (fontStyle & FontStyle.Strikeout) > 0; } }

        public GraphicsUnit Unit { get { return graphicsUnit; } }
        GraphicsUnit graphicsUnit;
    }

    /// <summary>
    /// Represents an RGBA color with float components.
    /// Optimized for Unity 6 / .NET Standard 2.1 with Unity.Mathematics.
    /// </summary>
    public readonly struct Color : IEquatable<Color>
    {
        private readonly Unity.Mathematics.float4 rgba;

        private Color(Unity.Mathematics.float4 rgba)
        {
            this.rgba = rgba;
        }

        public static Color FromArgb(int argb)
        {
            return FromArgb(
                    ((argb >> 24) & 0xFF),
                    ((argb >> 16) & 0xFF),
                    ((argb >> 8) & 0xFF),
                    (argb & 0xFF));
        }
        
        public static Color FromArgb(int red, int green, int blue)
        {
            return FromArgb(255, red, green, blue);
        }
        
        public static Color FromArgb(int alpha, int red, int green, int blue)
        {
            // Use Unity.Mathematics for efficient SIMD operations
            return new Color(new Unity.Mathematics.float4(
                red / 255.0f, 
                green / 255.0f, 
                blue / 255.0f, 
                alpha / 255.0f));
        }

        public Color(int R, int G, int B)
        {
            rgba = new Unity.Mathematics.float4(
                R / 255.0f,
                G / 255.0f,
                B / 255.0f,
                1.0f);
        }
        
        public Color(int R, int G, int B, int A)
        {
            rgba = new Unity.Mathematics.float4(
                R / 255.0f,
                G / 255.0f,
                B / 255.0f,
                A / 255.0f);
        }
        
        public Color(float R, float G, float B, float A)
        {
            rgba = new Unity.Mathematics.float4(R, G, B, A);
        }

        // Expose individual components for compatibility
        public float r => rgba.x;
        public float g => rgba.y;
        public float b => rgba.z;
        public float a => rgba.w;

        public int R => (int)(rgba.x * 255);
        public int G => (int)(rgba.y * 255);
        public int B => (int)(rgba.z * 255);
        public int A => (int)(rgba.w * 255);
        
        public int ToArgb() => (A << 24) + (R << 16) + (G << 8) + B;
        public int ToRGBA() => (R << 24) + (G << 16) + (B << 8) + A;

        public static readonly Color Black = new Color(0, 0, 0);
        public static readonly Color White = new Color(255, 255, 255);
        public static readonly Color Blue = new Color(0, 0, 255);
        public static readonly Color Red = new Color(255, 0, 0);
        public static readonly Color Green = new Color(0, 128, 0);
        public static readonly Color Grey = new Color(128, 128, 128);
        public static readonly Color Gray = Grey;
        public static readonly Color Transparent = new Color(0, 0, 0, 0);

        /// <summary>
        /// .NET known-color table, case-insensitive, as used by reference Emuera's
        /// HTML color-name parsing (System.Drawing.Color.FromName / KnownColor).
        /// Games commonly use names such as Yellow, which the previous table lacked.
        /// </summary>
        static readonly System.Collections.Generic.Dictionary<string, Color> colorNameTable =
            BuildColorNameTable();

        static System.Collections.Generic.Dictionary<string, Color> BuildColorNameTable()
        {
            var t = new System.Collections.Generic.Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            void Add(string n, int r, int g, int b) { t[n] = new Color(r, g, b); }
            t["Transparent"] = new Color(255, 255, 255, 0); // A=0 like .NET; rejected by HTML stringToColorInt32
            Add("AliceBlue", 240, 248, 255);
            Add("AntiqueWhite", 250, 235, 215);
            Add("Aqua", 0, 255, 255);
            Add("Aquamarine", 127, 255, 212);
            Add("Azure", 240, 255, 255);
            Add("Beige", 245, 245, 220);
            Add("Bisque", 255, 228, 196);
            Add("Black", 0, 0, 0);
            Add("BlanchedAlmond", 255, 235, 205);
            Add("Blue", 0, 0, 255);
            Add("BlueViolet", 138, 43, 226);
            Add("Brown", 165, 42, 42);
            Add("BurlyWood", 222, 184, 135);
            Add("CadetBlue", 95, 158, 160);
            Add("Chartreuse", 127, 255, 0);
            Add("Chocolate", 210, 105, 30);
            Add("Coral", 255, 127, 80);
            Add("CornflowerBlue", 100, 149, 237);
            Add("Cornsilk", 255, 248, 220);
            Add("Crimson", 220, 20, 60);
            Add("Cyan", 0, 255, 255);
            Add("DarkBlue", 0, 0, 139);
            Add("DarkCyan", 0, 139, 139);
            Add("DarkGoldenrod", 184, 134, 11);
            Add("DarkGray", 169, 169, 169);
            Add("DarkGreen", 0, 100, 0);
            Add("DarkGrey", 169, 169, 169);
            Add("DarkKhaki", 189, 183, 107);
            Add("DarkMagenta", 139, 0, 139);
            Add("DarkOliveGreen", 85, 107, 47);
            Add("DarkOrange", 255, 140, 0);
            Add("DarkOrchid", 153, 50, 204);
            Add("DarkRed", 139, 0, 0);
            Add("DarkSalmon", 233, 150, 122);
            Add("DarkSeaGreen", 143, 188, 143);
            Add("DarkSlateBlue", 72, 61, 139);
            Add("DarkSlateGray", 47, 79, 79);
            Add("DarkSlateGrey", 47, 79, 79);
            Add("DarkTurquoise", 0, 206, 209);
            Add("DarkViolet", 148, 0, 211);
            Add("DeepPink", 255, 20, 147);
            Add("DeepSkyBlue", 0, 191, 255);
            Add("DimGray", 105, 105, 105);
            Add("DimGrey", 105, 105, 105);
            Add("DodgerBlue", 30, 144, 255);
            Add("Firebrick", 178, 34, 34);
            Add("FloralWhite", 255, 250, 240);
            Add("ForestGreen", 34, 139, 34);
            Add("Fuchsia", 255, 0, 255);
            Add("Gainsboro", 220, 220, 220);
            Add("GhostWhite", 248, 248, 255);
            Add("Gold", 255, 215, 0);
            Add("Goldenrod", 218, 165, 32);
            Add("Gray", 128, 128, 128);
            Add("Green", 0, 128, 0);
            Add("GreenYellow", 173, 255, 47);
            Add("Grey", 128, 128, 128);
            Add("Honeydew", 240, 255, 240);
            Add("HotPink", 255, 105, 180);
            Add("IndianRed", 205, 92, 92);
            Add("Indigo", 75, 0, 130);
            Add("Ivory", 255, 255, 240);
            Add("Khaki", 240, 230, 140);
            Add("Lavender", 230, 230, 250);
            Add("LavenderBlush", 255, 240, 245);
            Add("LawnGreen", 124, 252, 0);
            Add("LemonChiffon", 255, 250, 205);
            Add("LightBlue", 173, 216, 230);
            Add("LightCoral", 240, 128, 128);
            Add("LightCyan", 224, 255, 255);
            Add("LightGoldenrodYellow", 250, 250, 210);
            Add("LightGray", 211, 211, 211);
            Add("LightGreen", 144, 238, 144);
            Add("LightGrey", 211, 211, 211);
            Add("LightPink", 255, 182, 193);
            Add("LightSalmon", 255, 160, 122);
            Add("LightSeaGreen", 32, 178, 170);
            Add("LightSkyBlue", 135, 206, 250);
            Add("LightSlateGray", 119, 136, 153);
            Add("LightSlateGrey", 119, 136, 153);
            Add("LightSteelBlue", 176, 196, 222);
            Add("LightYellow", 255, 255, 224);
            Add("Lime", 0, 255, 0);
            Add("LimeGreen", 50, 205, 50);
            Add("Linen", 250, 240, 230);
            Add("Magenta", 255, 0, 255);
            Add("Maroon", 128, 0, 0);
            Add("MediumAquamarine", 102, 205, 170);
            Add("MediumBlue", 0, 0, 205);
            Add("MediumOrchid", 186, 85, 211);
            Add("MediumPurple", 147, 112, 219);
            Add("MediumSeaGreen", 60, 179, 113);
            Add("MediumSlateBlue", 123, 104, 238);
            Add("MediumSpringGreen", 0, 250, 154);
            Add("MediumTurquoise", 72, 209, 204);
            Add("MediumVioletRed", 199, 21, 133);
            Add("MidnightBlue", 25, 25, 112);
            Add("MintCream", 245, 255, 250);
            Add("MistyRose", 255, 228, 225);
            Add("Moccasin", 255, 228, 181);
            Add("NavajoWhite", 255, 222, 173);
            Add("Navy", 0, 0, 128);
            Add("OldLace", 253, 245, 230);
            Add("Olive", 128, 128, 0);
            Add("OliveDrab", 107, 142, 35);
            Add("Orange", 255, 165, 0);
            Add("OrangeRed", 255, 69, 0);
            Add("Orchid", 218, 112, 214);
            Add("PaleGoldenrod", 238, 232, 170);
            Add("PaleGreen", 152, 251, 152);
            Add("PaleTurquoise", 175, 238, 238);
            Add("PaleVioletRed", 219, 112, 147);
            Add("PapayaWhip", 255, 239, 213);
            Add("PeachPuff", 255, 218, 185);
            Add("Peru", 205, 133, 63);
            Add("Pink", 255, 192, 203);
            Add("Plum", 221, 160, 221);
            Add("PowderBlue", 176, 224, 230);
            Add("Purple", 128, 0, 128);
            Add("RebeccaPurple", 102, 51, 153);
            Add("Red", 255, 0, 0);
            Add("RosyBrown", 188, 143, 143);
            Add("RoyalBlue", 65, 105, 225);
            Add("SaddleBrown", 139, 69, 19);
            Add("Salmon", 250, 128, 114);
            Add("SandyBrown", 244, 164, 96);
            Add("SeaGreen", 46, 139, 87);
            Add("SeaShell", 255, 245, 238);
            Add("Sienna", 160, 82, 45);
            Add("Silver", 192, 192, 192);
            Add("SkyBlue", 135, 206, 235);
            Add("SlateBlue", 106, 90, 205);
            Add("SlateGray", 112, 128, 144);
            Add("SlateGrey", 112, 128, 144);
            Add("Snow", 255, 250, 250);
            Add("SpringGreen", 0, 255, 127);
            Add("SteelBlue", 70, 130, 180);
            Add("Tan", 210, 180, 140);
            Add("Teal", 0, 128, 128);
            Add("Thistle", 216, 191, 216);
            Add("Tomato", 255, 99, 71);
            Add("Turquoise", 64, 224, 208);
            Add("Violet", 238, 130, 238);
            Add("Wheat", 245, 222, 179);
            Add("White", 255, 255, 255);
            Add("WhiteSmoke", 245, 245, 245);
            Add("Yellow", 255, 255, 0);
            Add("YellowGreen", 154, 205, 50);
            // Alias used in some era games (case-insensitive lookups already handled)
            Add("Orange2", 238, 154, 0);
            Add("Yellow2", 238, 238, 0);
            return t;
        }

        static readonly System.Collections.Generic.HashSet<string> unknownColorLogged =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Color FromName(string name)
        {
            if (!string.IsNullOrEmpty(name) && colorNameTable.TryGetValue(name, out var color))
                return color;
            // Report an unknown color name (compatibility diagnostic) without spamming.
            if (!string.IsNullOrEmpty(name) && unknownColorLogged.Add(name))
                uEmuera.Logger.Info("Color.FromName: unknown color name '" + name + "' mapped to Black");
            return Black;
        }

        public static bool operator ==(Color left, Color right) => left.Equals(right);
        public static bool operator !=(Color left, Color right) => !left.Equals(right);
        
        public bool Equals(Color other)
        {
            return A == other.A && R == other.R && G == other.G && B == other.B;
        }
        
        public override bool Equals(object obj)
        {
            return obj is Color color && Equals(color);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }
    }

    public struct Point
    {

        public static readonly Point Empty = new Point(0, 0);

        public Point(Size size)
        {
            X = size.Width;
            Y = size.Height;
        }
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public void Offset(Point pt)
        {
            X += pt.X;
            Y += pt.Y;
        }
        public bool IsEmpty
        {
            get { return X == 0 && Y == 0; }
        }
    }

    public struct Size
    {
        public static readonly Size zero;

        public Size(Point pt)
        {
            Width = pt.X;
            Height = pt.Y;
        }
        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsEmpty
        {
            get { return Width == 0 && Height == 0; }
        }
    }

    public struct Rectangle
    {
        public static Rectangle Intersect(Rectangle left, Rectangle right)
        {
            int l = Math.Max(left.Left, right.Left);
            int r = Math.Min(left.Right, right.Right);
            int t = Math.Max(left.Top, right.Top);
            int b = Math.Min(left.Bottom, right.Bottom);
            if(l < r && t < b)
                return new Rectangle(l, t, r - l, b - t);
            else
                return new Rectangle(0, 0, 0, 0);
        }

        public Rectangle(Point location, Size size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }
        //public Rectangle(Point location, Vector2 size)
        //{
        //    X = location.X;
        //    Y = location.Y;
        //    Width = (int)size.x;
        //    Height = (int)size.y;
        //}
        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int Top { get { return Y; } }
        public int Bottom { get { return Y + Height; } }
        public int Left { get { return X; } }
        public int Right { get { return X + Width; } }

        public Size Size { get { return new Size(Width, Height); } }
        public bool IsEmpty { get { return Width == 0 && Height == 0; } }

        public bool Contains(Point point)
        {
            return Left <= point.X && point.X < Right &&
                Top <= point.Y && point.Y < Bottom;
        }
        public bool IntersectsWith(Rectangle rect)
        {
            return !(rect.Bottom <= Top ||
                    rect.Top >= Bottom ||
                    rect.Right <= Left ||
                    rect.Left >= Right);
        }
    }

    public struct RectangleF
    {
        public RectangleF(Point location, Size size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }
        //public RectangleF(Point location, Vector2 size)
        //{
        //    X = location.X;
        //    Y = location.Y;
        //    Width = (int)size.x;
        //    Height = (int)size.y;
        //}
        public RectangleF(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public float Top { get { return Y; } }
        public float Bottom { get { return Y + Height; } }
        public float Left { get { return X; } }
        public float Right { get { return X + Width; } }
    }

    public class ImageAttributes
    { }

    public enum StringFormatFlags
    {
        DirectionRightToLeft = 1,
        DirectionVertical = 2,
        FitBlackBox = 4,
        DisplayFormatControl = 32,
        NoFontFallback = 1024,
        MeasureTrailingSpaces = 2048,
        NoWrap = 4096,
        LineLimit = 8192,
        NoClip = 16384
    }

    public class StringFormat
    {

    }

    //public class Bitmap
    //{ }

    public struct CharacterRange
    {
        public CharacterRange(int first, int length)
        {
            First = first;
            Length = length;
        }

        public int First { get; set; }
        public int Length { get; set; }

        //public override bool Equals(object obj);
        //public override int GetHashCode();

        //public static bool operator ==(CharacterRange cr1, CharacterRange cr2);
        //public static bool operator !=(CharacterRange cr1, CharacterRange cr2);
    }
}