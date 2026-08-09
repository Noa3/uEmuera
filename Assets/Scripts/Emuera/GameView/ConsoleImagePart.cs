using MinorShift._Library;
using MinorShift.Emuera.Content;
using System;
using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Text;
using uEmuera.Drawing;
using uEmuera.Forms;

namespace MinorShift.Emuera.GameView
{
	/// <summary>
	/// Represents an image part in the console display.
	/// Images are loaded from the resources folder and displayed inline with text.
	/// Supports sizing (absolute px or relative %), button variants, and vertical positioning.
	/// </summary>
	class ConsoleImagePart : AConsoleDisplayPart
	{

		/// <summary>
		/// Creates a new console image part.
		/// Images are loaded via AppContents.GetSprite() from the resources folder.
		/// If image loading fails, displays alt text with the img tag parameters.
		/// </summary>
		/// <param name="resName">Image resource name (src attribute) - required</param>
		/// <param name="resNameb">Button state image resource name (srcb attribute) - optional</param>
		/// <param name="raw_height">Image height - 0=font size, positive number=percentage of font size, number with px=absolute pixels</param>
		/// <param name="raw_width">Image width - 0=maintain aspect ratio, positive number=percentage of font size, number with px=absolute pixels</param>
		/// <param name="raw_ypos">Vertical position offset - number=percentage of font size, number with px=absolute pixels</param>
		public ConsoleImagePart(string resName, string resNameb, MixedNum raw_height, MixedNum raw_width, MixedNum raw_ypos)
		{
			ResourceName = resName ?? "";
			ButtonResourceName = resNameb;
            // Keep the raw attribute values so the exact img tag (src, srcb, height,
            // width, ypos with px suffixes) can be reconstructed for HTML_GETPRINTEDSTR
            // in release builds as well as the editor.
            rawHeight = raw_height;
            rawWidth = raw_width;
            rawYpos = raw_ypos;

            cImage = AppContents.GetSprite(ResourceName);
		
			// If image loading failed, create alt text and return early
			if(cImage == null)
			{
				decline = true;
				// Initialize fields for failed image load
				top = 0;
				bottom = Config.FontSize;
				Width = 0;
				XsubPixel = 0;
				destRect = new Rectangle(0, 0, 0, 0);
				
				// Create alt text showing the img tag
				AltText = BuildAltText();
				Str = AltText;
				return;
			}
		
			// Image loaded successfully - set Str to empty and calculate dimensions
			Str = "";
			
			int height = 0;
			if (raw_height.num == 0) // If height not specified in HTML or 0 is specified, use font size directly as height (in px units)
				height = Config.FontSize;
			else // If height is specified in HTML, interpret it as a percentage of font size
				height = raw_height.isPx ? raw_height.num : (Config.FontSize * raw_height.num / 100);
			// If width not specified or 0 is specified, set width (in px units) to maintain original image aspect ratio. Fractional parts less than 1 are recorded in XsubPixel.
			// Negative values are possible, but final Width is adjusted to be positive later.
			// A negative width/height must ALSO flip the image (Emuera semantics) and the
			// flip flag is preserved here for the Unity renderer instead of being lost.
			FlipX = raw_width.num < 0;
			FlipY = raw_height.num < 0;
			if (raw_width.num == 0)
			{
				Width = cImage.DestBaseSize.Width * height / cImage.DestBaseSize.Height;
				XsubPixel = ((float)cImage.DestBaseSize.Width * height) / cImage.DestBaseSize.Height - Width;
			}
			else
			{
				Width = raw_width.isPx ? raw_width.num : (Config.FontSize * raw_width.num / 100);
				if (raw_width.isPx)
					XsubPixel = 0;
				else
					XsubPixel = ((float)Config.FontSize * raw_width.num / 100f) - Width;
			}

			top = raw_ypos.isPx ? raw_ypos.num : (raw_ypos.num * Config.FontSize / 100);
			destRect = new Rectangle(0, top, Width, height);
			if (destRect.Width < 0)
			{
				destRect.X = -destRect.Width;
				Width = -destRect.Width;
			}
			if (destRect.Height < 0)
			{
				destRect.Y = destRect.Y - destRect.Height;
				height = -destRect.Height;
			}
			bottom = top + height;
			//if(top > 0)
			//	top = 0;
			//if(bottom < Config.FontSize)
			//	bottom = Config.FontSize;
			if (ButtonResourceName != null)
			{
				if(ButtonResourceName == ResourceName)
					cImageB = cImage;
				else
				{
					cImageB = AppContents.GetSprite(ButtonResourceName);
					if(cImageB == null)
						cImageB = null;
				}
			}
            // Successful images also need a serializable representation so that
            // DisplayLine2Html/HTML_GETPRINTEDSTR can round-trip the img tag.
            AltText = BuildAltText();
		}

        /// <summary>
        /// Reconstructs the original img tag with all attributes. Used both as the
        /// failure alt-text (drawn as text) and for markup round-tripping via
        /// HTML_GETPRINTEDSTR (which must work in release builds, not only UNITY_EDITOR).
        /// </summary>
        string BuildAltText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<img src='");
            sb.Append(ResourceName);
            if(ButtonResourceName != null)
            {
                sb.Append("' srcb='");
                sb.Append(ButtonResourceName);
            }
            if(rawHeight.num != 0)
            {
                sb.Append("' height='");
                sb.Append(rawHeight.num.ToString());
                if (rawHeight.isPx)
                    sb.Append("px");
            }
            if(rawWidth.num != 0)
            {
                sb.Append("' width='");
                sb.Append(rawWidth.num.ToString());
                if (rawWidth.isPx)
                    sb.Append("px");
            }
            if(rawYpos.num != 0)
            {
                sb.Append("' ypos='");
                sb.Append(rawYpos.num.ToString());
                if (rawYpos.isPx)
                    sb.Append("px");
            }
            sb.Append("'>");
            return sb.ToString();
        }

        /// <summary>True when the sprite failed to load (a valid <see cref="AltText"/> is then rendered as text).</summary>
        public bool IsLoadFailed { get { return decline; } }
        readonly bool decline = false;
        readonly MixedNum rawHeight;
        readonly MixedNum rawWidth;
        readonly MixedNum rawYpos;
        /// <summary>Raw negative width flips the image horizontally.</summary>
        public bool FlipX { get; private set; }
        /// <summary>Raw negative height flips the image vertically.</summary>
        public bool FlipY { get; private set; }

        public ASprite Image { get { return cImage; } }
        public ASprite ImageBackground { get { return cImageB; } }
        public Rectangle rect { get { return cImage != null ? cImage.Rectangle : new Rectangle(0, 0, 0, 0); } }
        public Rectangle dest_rect { get { return destRect; } }

		private readonly ASprite cImage;
		private readonly ASprite cImageB;
		private readonly int top;
		private readonly int bottom;
		private readonly Rectangle destRect;
//#pragma warning disable CS0649 // Field 'ConsoleImagePart.ia' is never assigned. It always uses the default value null.
//		private readonly ImageAttributes ia;
//#pragma warning restore CS0649 // Field 'ConsoleImagePart.ia' is never assigned. It always uses the default value null.
		public readonly string ResourceName;
		public readonly string ButtonResourceName;
		public override int Top { get { return top; } }
		public override int Bottom { get { return bottom; } }
		
		public override bool CanDivide { get { return false; } }
		public override void SetWidth(StringMeasure sm, float subPixel)
		{
			if (this.Error)
			{
				Width = 0;
				return;
			}
			if (cImage != null)
				return;
			Width = sm.GetDisplayLength(Str, Config.Font);
			XsubPixel = subPixel;
		}

		public override string ToString()
		{
			if (AltText == null)
				return "";
			return AltText;
		}

		public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode)
		{
			//if (this.Error)
			//	return;
			//ASprite img = cImage;
			//if (isSelecting && cImageB != null)
			//	img = cImageB;
            //
			//if (img != null && img.IsCreated)
			//{
			//	Rectangle rect = destRect;
			//	//PointX fine adjustment
			//	rect.X = destRect.X + PointX + Config.DrawingParam_ShapePositionShift;
			//	rect.Y = destRect.Y + pointY;
			//	img.GraphicsDraw(graph, rect);
			//}
			//else
			//{
			//	if (mode == TextDrawingMode.GRAPHICS)
			//		graph.DrawString(AltText, Config.Font, new SolidBrush(Config.ForeColor), new Point(PointX, pointY));
			//	else
			//		System.Windows.Forms.TextRenderer.DrawText(graph, AltText, Config.Font, new Point(PointX, pointY), Config.ForeColor, System.Windows.Forms.TextFormatFlags.NoPrefix);
			//}
		}

		public override void GDIDrawTo(int pointY, bool isSelecting, bool isBackLog)
		{
			//if (this.Error)
			//	return;
			//SpriteF img = cImage as SpriteF;//images created from a Graphics are not GDI targets
			//if (isSelecting && cImageB != null)
			//	img = cImageB as SpriteF;
			//if (img != null && img.IsCreated)
			//{
			//	int x = PointX + destRect.X;
			//	int y = pointY + destRect.Y;
			//	if (!img.DestBasePosition.IsEmpty)
			//	{
			//		x = x + img.DestBasePosition.X * destRect.Width / img.SrcRectangle.Width;
			//		y = y + img.DestBasePosition.Y * destRect.Height / img.SrcRectangle.Height;
			//	}
			//	GDI.DrawImage(x, y, Width, destRect.Height, img.BaseImage.GDIhDC, img.SrcRectangle);
			//}
			//else
			//	GDI.TabbedTextOutFull(Config.Font, Config.ForeColor, AltText, PointX, pointY);
		}
	}
}
