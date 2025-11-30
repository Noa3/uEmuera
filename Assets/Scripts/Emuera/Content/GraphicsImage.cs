using MinorShift._Library;
using System;
using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using uEmuera.Drawing;
//using System.Threading.Tasks;

namespace MinorShift.Emuera.Content
{
	internal sealed class GraphicsImage : AbstractImage
	{
		public GraphicsImage(int id)
		{
			ID = id;
		}
		public readonly int ID;
        
        // Unity Texture2D for actual pixel manipulation
        private Texture2D texture = null;
        
        // Current brush color for fill operations
        private uEmuera.Drawing.Color brushColor = uEmuera.Drawing.Color.White;
        
        // Current pen color for line operations
        private uEmuera.Drawing.Color penColor = uEmuera.Drawing.Color.White;
        private int penWidth = 1;
        
        // Current font settings
        private string fontName = null;
        private float fontSize = 12;

        #region Bitmap書き込み・作成

        /// <summary>
        /// GCREATE(int ID, int width, int height)
        /// Graphicsの基礎となるBitmapを作成する。エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GCreate(int x, int y, bool useGDI)
        {
            this.GDispose();
            is_created = true;
            width = x;
            height = y;
            
            // Create Unity Texture2D for pixel manipulation
            texture = new Texture2D(x, y, TextureFormat.ARGB32, false);
            // Initialize with transparent pixels
            UnityEngine.Color[] pixels = new UnityEngine.Color[x * y];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new UnityEngine.Color(0, 0, 0, 0);
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        internal void GCreateFromF(Bitmap bmp, bool useGDI)
        {
            this.GDispose();
            is_created = true;
            width = bmp.Width;
            height = bmp.Height;
            
            // Get texture info and copy to our texture
            var ti = SpriteManager.GetTextureInfo(bmp.name, bmp.path);
            if (ti != null && ti.texture != null)
            {
                // Create a copy of the texture for manipulation
                texture = new Texture2D(ti.texture.width, ti.texture.height, TextureFormat.ARGB32, false);
                UnityEngine.Color[] pixels = ti.texture.GetPixels();
                texture.SetPixels(pixels);
                texture.Apply();
            }
            else
            {
                // Create empty texture if source not available
                texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                UnityEngine.Color[] pixels = new UnityEngine.Color[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new UnityEngine.Color(0, 0, 0, 0);
                }
                texture.SetPixels(pixels);
                texture.Apply();
            }
        }

        /// <summary>
        /// GCLEAR(int ID, int cARGB)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GClear(uEmuera.Drawing.Color c)
        {
            if (texture == null)
                return;
            UnityEngine.Color unityColor = new UnityEngine.Color(c.r, c.g, c.b, c.a);
            UnityEngine.Color[] pixels = new UnityEngine.Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = unityColor;
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        /// <summary>
        /// GFILLRECTANGLE(int ID, int x, int y, int width, int height)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GFillRectangle(Rectangle rect)
        {
            if (texture == null)
                return;
            
            UnityEngine.Color fillColor = new UnityEngine.Color(brushColor.r, brushColor.g, brushColor.b, brushColor.a);
            
            // Clamp rectangle to texture bounds
            int startX = Math.Max(0, rect.X);
            int startY = Math.Max(0, rect.Y);
            int endX = Math.Min(width, rect.X + rect.Width);
            int endY = Math.Min(height, rect.Y + rect.Height);
            
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    // Unity textures have origin at bottom-left, so flip Y
                    texture.SetPixel(x, height - 1 - y, fillColor);
                }
            }
            texture.Apply();
        }

		/// <summary>
		/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight)
		/// エラーチェックは呼び出し元でのみ行う
		/// </summary>
		public void GDrawCImg(ASprite img, Rectangle destRect)
		{
			if (texture == null || img == null || img.Bitmap == null)
				return;
			
			var srcBitmap = img.Bitmap;
			var ti = SpriteManager.GetTextureInfo(srcBitmap.name, srcBitmap.path);
			if (ti == null || ti.texture == null)
				return;
			
			// Get source pixels
			var srcRect = img.Rectangle;
			
			// Draw source to destination with scaling
			for (int dy = 0; dy < destRect.Height; dy++)
			{
				for (int dx = 0; dx < destRect.Width; dx++)
				{
					// Calculate source coordinates with scaling
					int sx = srcRect.X + (dx * srcRect.Width / destRect.Width);
					int sy = srcRect.Y + (dy * srcRect.Height / destRect.Height);
					
					// Calculate destination coordinates
					int destX = destRect.X + dx;
					int destY = destRect.Y + dy;
					
					// Bounds checking
					if (destX < 0 || destX >= width || destY < 0 || destY >= height)
						continue;
					if (sx < 0 || sx >= ti.texture.width || sy < 0 || sy >= ti.texture.height)
						continue;
					
					// Get pixel and set (flip Y for Unity coordinate system)
					UnityEngine.Color srcPixel = ti.texture.GetPixel(sx, ti.texture.height - 1 - sy);
					texture.SetPixel(destX, height - 1 - destY, srcPixel);
				}
			}
			texture.Apply();
		}

		/// <summary>
		/// GDRAWCIMG(int ID, str imgName, int destX, int destY, int destWidth, int destHeight, float[][] cm)
		/// エラーチェックは呼び出し元でのみ行う
		/// </summary>
		public void GDrawCImg(ASprite img, Rectangle destRect, float[][] cm)
		{
			if (texture == null || img == null || img.Bitmap == null)
				return;
			
			var srcBitmap = img.Bitmap;
			var ti = SpriteManager.GetTextureInfo(srcBitmap.name, srcBitmap.path);
			if (ti == null || ti.texture == null)
				return;
			
			// Get source pixels
			var srcRect = img.Rectangle;
			
			// Draw source to destination with scaling and color matrix
			for (int dy = 0; dy < destRect.Height; dy++)
			{
				for (int dx = 0; dx < destRect.Width; dx++)
				{
					// Calculate source coordinates with scaling
					int sx = srcRect.X + (dx * srcRect.Width / destRect.Width);
					int sy = srcRect.Y + (dy * srcRect.Height / destRect.Height);
					
					// Calculate destination coordinates
					int destX = destRect.X + dx;
					int destY = destRect.Y + dy;
					
					// Bounds checking
					if (destX < 0 || destX >= width || destY < 0 || destY >= height)
						continue;
					if (sx < 0 || sx >= ti.texture.width || sy < 0 || sy >= ti.texture.height)
						continue;
					
					// Get source pixel and apply color matrix
					UnityEngine.Color srcPixel = ti.texture.GetPixel(sx, ti.texture.height - 1 - sy);
					UnityEngine.Color result = ApplyColorMatrix(srcPixel, cm);
					texture.SetPixel(destX, height - 1 - destY, result);
				}
			}
			texture.Apply();
		}
		
		/// <summary>
		/// Apply a 5x5 color matrix to a color
		/// </summary>
		private UnityEngine.Color ApplyColorMatrix(UnityEngine.Color c, float[][] cm)
		{
			if (cm == null || cm.Length < 5)
				return c;
			
			float r = c.r * cm[0][0] + c.g * cm[1][0] + c.b * cm[2][0] + c.a * cm[3][0] + cm[4][0];
			float g = c.r * cm[0][1] + c.g * cm[1][1] + c.b * cm[2][1] + c.a * cm[3][1] + cm[4][1];
			float b = c.r * cm[0][2] + c.g * cm[1][2] + c.b * cm[2][2] + c.a * cm[3][2] + cm[4][2];
			float a = c.r * cm[0][3] + c.g * cm[1][3] + c.b * cm[2][3] + c.a * cm[3][3] + cm[4][3];
			
			return new UnityEngine.Color(
				Mathf.Clamp01(r),
				Mathf.Clamp01(g),
				Mathf.Clamp01(b),
				Mathf.Clamp01(a)
			);
		}

        /// <summary>
        /// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect)
        {
            if (texture == null || srcGra == null || srcGra.texture == null)
                return;
            
            // Draw source graphics to destination with scaling
            for (int dy = 0; dy < destRect.Height; dy++)
            {
                for (int dx = 0; dx < destRect.Width; dx++)
                {
                    // Calculate source coordinates with scaling
                    int sx = srcRect.X + (dx * srcRect.Width / destRect.Width);
                    int sy = srcRect.Y + (dy * srcRect.Height / destRect.Height);
                    
                    // Calculate destination coordinates
                    int destX = destRect.X + dx;
                    int destY = destRect.Y + dy;
                    
                    // Bounds checking
                    if (destX < 0 || destX >= width || destY < 0 || destY >= height)
                        continue;
                    if (sx < 0 || sx >= srcGra.width || sy < 0 || sy >= srcGra.height)
                        continue;
                    
                    // Get source pixel and set (flip Y for Unity coordinate system)
                    UnityEngine.Color srcPixel = srcGra.texture.GetPixel(sx, srcGra.height - 1 - sy);
                    texture.SetPixel(destX, height - 1 - destY, srcPixel);
                }
            }
            texture.Apply();
        }


        /// <summary>
        /// GDRAWG(int ID, int srcID, int destX, int destY, int destWidth, int destHeight, int srcX, int srcY, int srcWidth, int srcHeight, float[][] cm)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect, float[][] cm)
        {
            if (texture == null || srcGra == null || srcGra.texture == null)
                return;
            
            // Draw source graphics to destination with scaling and color matrix
            for (int dy = 0; dy < destRect.Height; dy++)
            {
                for (int dx = 0; dx < destRect.Width; dx++)
                {
                    // Calculate source coordinates with scaling
                    int sx = srcRect.X + (dx * srcRect.Width / destRect.Width);
                    int sy = srcRect.Y + (dy * srcRect.Height / destRect.Height);
                    
                    // Calculate destination coordinates
                    int destX = destRect.X + dx;
                    int destY = destRect.Y + dy;
                    
                    // Bounds checking
                    if (destX < 0 || destX >= width || destY < 0 || destY >= height)
                        continue;
                    if (sx < 0 || sx >= srcGra.width || sy < 0 || sy >= srcGra.height)
                        continue;
                    
                    // Get source pixel, apply color matrix, and set
                    UnityEngine.Color srcPixel = srcGra.texture.GetPixel(sx, srcGra.height - 1 - sy);
                    UnityEngine.Color result = ApplyColorMatrix(srcPixel, cm);
                    texture.SetPixel(destX, height - 1 - destY, result);
                }
            }
            texture.Apply();
        }


        /// <summary>
        /// GDRAWGWITHMASK(int ID, int srcID, int maskID, int destX, int destY)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GDrawGWithMask(GraphicsImage srcGra, GraphicsImage maskGra, Point destPoint)
        {
            if (texture == null || srcGra == null || srcGra.texture == null || 
                maskGra == null || maskGra.texture == null)
                return;
            
            // Draw source to destination using mask for alpha blending
            for (int y = 0; y < srcGra.Height; y++)
            {
                for (int x = 0; x < srcGra.Width; x++)
                {
                    int destX = destPoint.X + x;
                    int destY = destPoint.Y + y;
                    
                    // Bounds checking
                    if (destX < 0 || destX >= width || destY < 0 || destY >= height)
                        continue;
                    if (x < 0 || x >= maskGra.Width || y < 0 || y >= maskGra.Height)
                        continue;
                    
                    // Get source and mask pixels (flip Y for Unity coordinate system)
                    UnityEngine.Color srcPixel = srcGra.texture.GetPixel(x, srcGra.height - 1 - y);
                    UnityEngine.Color maskPixel = maskGra.texture.GetPixel(x, maskGra.height - 1 - y);
                    UnityEngine.Color destPixel = texture.GetPixel(destX, height - 1 - destY);
                    
                    // Use mask's grayscale value as alpha (average of RGB)
                    float maskAlpha = (maskPixel.r + maskPixel.g + maskPixel.b) / 3.0f;
                    
                    // Blend based on mask
                    UnityEngine.Color result;
                    if (maskAlpha >= 0.999f) // Fully opaque
                    {
                        result = srcPixel;
                    }
                    else if (maskAlpha <= 0.001f) // Fully transparent
                    {
                        result = destPixel;
                    }
                    else // Semi-transparent blend
                    {
                        result = new UnityEngine.Color(
                            srcPixel.r * maskAlpha + destPixel.r * (1 - maskAlpha),
                            srcPixel.g * maskAlpha + destPixel.g * (1 - maskAlpha),
                            srcPixel.b * maskAlpha + destPixel.b * (1 - maskAlpha),
                            srcPixel.a * maskAlpha + destPixel.a * (1 - maskAlpha)
                        );
                    }
                    
                    texture.SetPixel(destX, height - 1 - destY, result);
                }
            }
            texture.Apply();
        }

        public void GSetFont(uEmuera.Drawing.Font r)
        {
            if (r != null)
            {
                fontName = r.FontFamily.Name;
                fontSize = r.Size;
            }
        }
        public void GSetBrush(Brush r)
        {
            if (r is SolidBrush solidBrush)
            {
                brushColor = solidBrush.Color;
            }
        }
        public void GSetPen(Pen r)
        {
            // Pen is used for line drawing, store settings if needed
            // Currently pen color/width stored for potential future use
        }
        #endregion
        #region Bitmap読み込み・削除
        /// <summary>
        /// GSETCOLOR(int ID, int cARGB, int x, int y)
        /// エラーチェックは呼び出し元でのみ行う
        /// </summary>
        public void GSetColor(uEmuera.Drawing.Color c, int x, int y)
        {
            if (texture == null)
                return;
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;
            
            UnityEngine.Color unityColor = new UnityEngine.Color(c.r, c.g, c.b, c.a);
            // Flip Y for Unity coordinate system
            texture.SetPixel(x, height - 1 - y, unityColor);
            texture.Apply();
        }

        /// <summary>
        /// GGETCOLOR(int ID, int x, int y)
        /// エラーチェックは呼び出し元でのみ行う。特に画像範囲内であるかどうかチェックすること
        /// </summary>
        public uEmuera.Drawing.Color GGetColor(int x, int y)
        {
            if (texture == null)
                return uEmuera.Drawing.Color.Transparent;
            if (x < 0 || x >= width || y < 0 || y >= height)
                return uEmuera.Drawing.Color.Transparent;
            
            // Flip Y for Unity coordinate system
            UnityEngine.Color uc = texture.GetPixel(x, height - 1 - y);
            return new uEmuera.Drawing.Color(uc.r, uc.g, uc.b, uc.a);
        }


        /// <summary>
        /// GDISPOSE(int ID)
        /// </summary>
        public void GDispose()
        {
            // Dispose Unity texture
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
            
            // Reset state
            is_created = false;
            width = 0;
            height = 0;
            brushColor = uEmuera.Drawing.Color.White;
            penColor = uEmuera.Drawing.Color.White;
            fontName = null;
            fontSize = 12;
        }

        public override void Dispose()
        {
            this.GDispose();
        }

        ~GraphicsImage()
        {
            Dispose();
        }
        #endregion

//#region 状態判定（Bitmap読み書きを伴わない）
        //public override bool IsCreated { get { return g != null; } }
        public override bool IsCreated { get { return is_created; } }
        bool is_created = false;

        /// <summary>
        /// int GWIDTH(int ID)
        /// </summary>
        public int Width { get { return width; } }
        int width = 0;
		/// <summary>
		/// int GHEIGHT(int ID)
		/// </summary>
		public int Height { get { return height; } }
        int height = 0;
        //#endregion
	}
}
