using System;
using System.Collections.Generic;
using System.Text;
//using System.Drawing;
using MinorShift.Emuera.Sub;
using MinorShift._Library;
//using System.Windows.Forms;
using uEmuera.Drawing;

namespace MinorShift.Emuera.GameView
{

	/// <summary>
	/// Text length measurement device
	/// 1819 Stop calling CreateGraphics each time it is needed; prepare a Graphics in advance instead
	/// </summary>
	internal sealed class StringMeasure : IDisposable
	{
		public StringMeasure()
		{
			textDrawingMode = Config.TextDrawingMode;
			//layoutSize = new Size(Config.WindowX * 2, Config.LineHeight);
			//layoutRect = new RectangleF(0, 0, Config.WindowX * 2, Config.LineHeight);
			//fontDisplaySize = Config.Font.Size / 2 * 1.04f;//in practice it takes a bit more width than the specified font?
			////bmp = new Bitmap(Config.WindowX, Config.LineHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			//bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			//graph = Graphics.FromImage(bmp);
			//if (textDrawingMode == TextDrawingMode.WINAPI)
			//	GDI.GdiMesureTextStart(graph);
		}

		readonly TextDrawingMode textDrawingMode;
		//readonly StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
		//readonly CharacterRange[] ranges = new CharacterRange[] { new CharacterRange(0, 1) };
		//readonly Size layoutSize;
		//readonly RectangleF layoutRect;
		//readonly float fontDisplaySize;

		//readonly Graphics graph = null;
		//readonly Bitmap bmp = null;

		public int GetDisplayLength(string s, Font font)
		{
            if (string.IsNullOrEmpty(s))
            	return 0;
            if (textDrawingMode == TextDrawingMode.GRAPHICS)
            {
            	if (s.Contains("\t"))
            		s = s.Replace("\t", "        ");
            //	ranges[0].Length = s.Length;
            //	//CharacterRange[] ranges = new CharacterRange[] { new CharacterRange(0, s.Length) };
            //	sf.SetMeasurableCharacterRanges(ranges);
            //	Region[] regions = graph.MeasureCharacterRanges(s, font, layoutRect, sf);
            //	RectangleF rectF = regions[0].GetBounds(graph);
            //	//return (int)rectF.Width;//off by a few pixels even when not proportional
            //	return (int)((int)((rectF.Width - 1) / fontDisplaySize + 0.95f) * fontDisplaySize);
            }
            //else if (textDrawingMode == TextDrawingMode.TEXTRENDERER)
            //{
            //	Size size = TextRenderer.MeasureText(graph, s, font, layoutSize, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            //	//Size size = TextRenderer.MeasureText(g, s, StaticConfig.Font);
            //	return size.Width;
            //}
            //else// if (StaticConfig.TextDrawingMode == TextDrawingMode.WINAPI)
            //{
            //	Size size = GDI.MeasureText(s, font);
            //	return size.Width;
            //}
            ////Will never be reached
            ////else
            ////    throw new ExeEE("Unknown drawing mode");

            return uEmuera.Utils.GetDisplayLength(s, font);
		}


		//bool disposed = false;
		public void Dispose()
		{
			//if (disposed)
			//	return;
			//disposed = true;
			//if (textDrawingMode == TextDrawingMode.WINAPI)
			//	GDI.GdiMesureTextEnd(graph);
			//graph.Dispose();
			//bmp.Dispose();
            //sf.Dispose();
		}
	}
}
