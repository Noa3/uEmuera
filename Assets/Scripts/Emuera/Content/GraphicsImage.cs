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
            
            int rectWidth = endX - startX;
            int rectHeight = endY - startY;
            
            if (rectWidth <= 0 || rectHeight <= 0)
                return;
            
            // Use bulk operations for better performance
            UnityEngine.Color[] pixels = new UnityEngine.Color[rectWidth * rectHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fillColor;
            }
            
            // SetPixels uses bottom-left origin, so flip Y
            texture.SetPixels(startX, height - endY, rectWidth, rectHeight, pixels);
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
			
			// Get source pixels using bulk operation
			var srcRect = img.Rectangle;
			var srcPixels = ti.texture.GetPixels();
			
			// Calculate clamped destination bounds
			int clampedStartX = Math.Max(0, destRect.X);
			int clampedStartY = Math.Max(0, destRect.Y);
			int clampedEndX = Math.Min(width, destRect.X + destRect.Width);
			int clampedEndY = Math.Min(height, destRect.Y + destRect.Height);
			
			int clampedWidth = clampedEndX - clampedStartX;
			int clampedHeight = clampedEndY - clampedStartY;
			
			if (clampedWidth <= 0 || clampedHeight <= 0)
				return;
			
			// Create destination pixel array
			UnityEngine.Color[] destPixels = new UnityEngine.Color[clampedWidth * clampedHeight];
			
			for (int dy = 0; dy < clampedHeight; dy++)
			{
				for (int dx = 0; dx < clampedWidth; dx++)
				{
					// Calculate actual destination position
					int actualDestX = clampedStartX + dx - destRect.X;
					int actualDestY = clampedStartY + dy - destRect.Y;
					
					// Calculate source coordinates with scaling
					int sx = srcRect.X + (actualDestX * srcRect.Width / destRect.Width);
					int sy = srcRect.Y + (actualDestY * srcRect.Height / destRect.Height);
					
					// Bounds checking for source
					if (sx < 0 || sx >= ti.texture.width || sy < 0 || sy >= ti.texture.height)
					{
						destPixels[dy * clampedWidth + dx] = new UnityEngine.Color(0, 0, 0, 0);
						continue;
					}
					
					// Get pixel (flip Y for source Unity coordinate system)
					int srcIndex = (ti.texture.height - 1 - sy) * ti.texture.width + sx;
					destPixels[dy * clampedWidth + dx] = srcPixels[srcIndex];
				}
			}
			
			// Set pixels using bulk operation (flip Y for destination)
			texture.SetPixels(clampedStartX, height - clampedEndY, clampedWidth, clampedHeight, destPixels);
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
			
			// Get source pixels using bulk operation
			var srcRect = img.Rectangle;
			var srcPixels = ti.texture.GetPixels();
			
			// Calculate clamped destination bounds
			int clampedStartX = Math.Max(0, destRect.X);
			int clampedStartY = Math.Max(0, destRect.Y);
			int clampedEndX = Math.Min(width, destRect.X + destRect.Width);
			int clampedEndY = Math.Min(height, destRect.Y + destRect.Height);
			
			int clampedWidth = clampedEndX - clampedStartX;
			int clampedHeight = clampedEndY - clampedStartY;
			
			if (clampedWidth <= 0 || clampedHeight <= 0)
				return;
			
			// Create destination pixel array
			UnityEngine.Color[] destPixels = new UnityEngine.Color[clampedWidth * clampedHeight];
			
			for (int dy = 0; dy < clampedHeight; dy++)
			{
				for (int dx = 0; dx < clampedWidth; dx++)
				{
					// Calculate actual destination position
					int actualDestX = clampedStartX + dx - destRect.X;
					int actualDestY = clampedStartY + dy - destRect.Y;
					
					// Calculate source coordinates with scaling
					int sx = srcRect.X + (actualDestX * srcRect.Width / destRect.Width);
					int sy = srcRect.Y + (actualDestY * srcRect.Height / destRect.Height);
					
					// Bounds checking for source
					if (sx < 0 || sx >= ti.texture.width || sy < 0 || sy >= ti.texture.height)
					{
						destPixels[dy * clampedWidth + dx] = new UnityEngine.Color(0, 0, 0, 0);
						continue;
					}
					
					// Get pixel and apply color matrix
					int srcIndex = (ti.texture.height - 1 - sy) * ti.texture.width + sx;
					destPixels[dy * clampedWidth + dx] = ApplyColorMatrix(srcPixels[srcIndex], cm);
				}
			}
			
			// Set pixels using bulk operation (flip Y for destination)
			texture.SetPixels(clampedStartX, height - clampedEndY, clampedWidth, clampedHeight, destPixels);
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
            
            // Get source pixels using bulk operation
            var srcPixels = srcGra.texture.GetPixels();
            
            // Calculate clamped destination bounds
            int clampedStartX = Math.Max(0, destRect.X);
            int clampedStartY = Math.Max(0, destRect.Y);
            int clampedEndX = Math.Min(width, destRect.X + destRect.Width);
            int clampedEndY = Math.Min(height, destRect.Y + destRect.Height);
            
            int clampedWidth = clampedEndX - clampedStartX;
            int clampedHeight = clampedEndY - clampedStartY;
            
            if (clampedWidth <= 0 || clampedHeight <= 0)
                return;
            
            // Create destination pixel array
            UnityEngine.Color[] destPixels = new UnityEngine.Color[clampedWidth * clampedHeight];
            
            for (int dy = 0; dy < clampedHeight; dy++)
            {
                for (int dx = 0; dx < clampedWidth; dx++)
                {
                    // Calculate actual destination position relative to destRect
                    int actualDestX = clampedStartX + dx - destRect.X;
                    int actualDestY = clampedStartY + dy - destRect.Y;
                    
                    // Calculate source coordinates with scaling
                    int sx = srcRect.X + (actualDestX * srcRect.Width / destRect.Width);
                    int sy = srcRect.Y + (actualDestY * srcRect.Height / destRect.Height);
                    
                    // Bounds checking for source
                    if (sx < 0 || sx >= srcGra.width || sy < 0 || sy >= srcGra.height)
                    {
                        destPixels[dy * clampedWidth + dx] = new UnityEngine.Color(0, 0, 0, 0);
                        continue;
                    }
                    
                    // Get source pixel (flip Y for Unity coordinate system)
                    int srcIndex = (srcGra.height - 1 - sy) * srcGra.width + sx;
                    destPixels[dy * clampedWidth + dx] = srcPixels[srcIndex];
                }
            }
            
            // Set pixels using bulk operation (flip Y for destination)
            texture.SetPixels(clampedStartX, height - clampedEndY, clampedWidth, clampedHeight, destPixels);
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
            
            // Get source pixels using bulk operation
            var srcPixels = srcGra.texture.GetPixels();
            
            // Calculate clamped destination bounds
            int clampedStartX = Math.Max(0, destRect.X);
            int clampedStartY = Math.Max(0, destRect.Y);
            int clampedEndX = Math.Min(width, destRect.X + destRect.Width);
            int clampedEndY = Math.Min(height, destRect.Y + destRect.Height);
            
            int clampedWidth = clampedEndX - clampedStartX;
            int clampedHeight = clampedEndY - clampedStartY;
            
            if (clampedWidth <= 0 || clampedHeight <= 0)
                return;
            
            // Create destination pixel array
            UnityEngine.Color[] destPixels = new UnityEngine.Color[clampedWidth * clampedHeight];
            
            for (int dy = 0; dy < clampedHeight; dy++)
            {
                for (int dx = 0; dx < clampedWidth; dx++)
                {
                    // Calculate actual destination position relative to destRect
                    int actualDestX = clampedStartX + dx - destRect.X;
                    int actualDestY = clampedStartY + dy - destRect.Y;
                    
                    // Calculate source coordinates with scaling
                    int sx = srcRect.X + (actualDestX * srcRect.Width / destRect.Width);
                    int sy = srcRect.Y + (actualDestY * srcRect.Height / destRect.Height);
                    
                    // Bounds checking for source
                    if (sx < 0 || sx >= srcGra.width || sy < 0 || sy >= srcGra.height)
                    {
                        destPixels[dy * clampedWidth + dx] = new UnityEngine.Color(0, 0, 0, 0);
                        continue;
                    }
                    
                    // Get source pixel and apply color matrix
                    int srcIndex = (srcGra.height - 1 - sy) * srcGra.width + sx;
                    destPixels[dy * clampedWidth + dx] = ApplyColorMatrix(srcPixels[srcIndex], cm);
                }
            }
            
            // Set pixels using bulk operation (flip Y for destination)
            texture.SetPixels(clampedStartX, height - clampedEndY, clampedWidth, clampedHeight, destPixels);
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
            
            // Get source, mask, and destination pixels using bulk operations
            var srcPixels = srcGra.texture.GetPixels();
            var maskPixels = maskGra.texture.GetPixels();
            
            // Calculate clamped destination bounds
            int clampedStartX = Math.Max(0, destPoint.X);
            int clampedStartY = Math.Max(0, destPoint.Y);
            int clampedEndX = Math.Min(width, destPoint.X + srcGra.Width);
            int clampedEndY = Math.Min(height, destPoint.Y + srcGra.Height);
            
            int clampedWidth = clampedEndX - clampedStartX;
            int clampedHeight = clampedEndY - clampedStartY;
            
            if (clampedWidth <= 0 || clampedHeight <= 0)
                return;
            
            // Get current destination pixels for blending
            var currentDestPixels = texture.GetPixels(clampedStartX, height - clampedEndY, clampedWidth, clampedHeight);
            
            // Create result pixel array
            UnityEngine.Color[] resultPixels = new UnityEngine.Color[clampedWidth * clampedHeight];
            
            for (int dy = 0; dy < clampedHeight; dy++)
            {
                for (int dx = 0; dx < clampedWidth; dx++)
                {
                    // Calculate source position
                    int sx = clampedStartX + dx - destPoint.X;
                    int sy = clampedStartY + dy - destPoint.Y;
                    
                    // Bounds checking for source and mask
                    if (sx < 0 || sx >= srcGra.Width || sy < 0 || sy >= srcGra.Height ||
                        sx < 0 || sx >= maskGra.Width || sy < 0 || sy >= maskGra.Height)
                    {
                        resultPixels[dy * clampedWidth + dx] = currentDestPixels[dy * clampedWidth + dx];
                        continue;
                    }
                    
                    // Get source, mask, and dest pixels (flip Y for Unity)
                    int srcIndex = (srcGra.height - 1 - sy) * srcGra.width + sx;
                    int maskIndex = (maskGra.height - 1 - sy) * maskGra.width + sx;
                    
                    UnityEngine.Color srcPixel = srcPixels[srcIndex];
                    UnityEngine.Color maskPixel = maskPixels[maskIndex];
                    UnityEngine.Color destPixel = currentDestPixels[dy * clampedWidth + dx];
                    
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
                    
                    resultPixels[dy * clampedWidth + dx] = result;
                }
            }
            
            // Set pixels using bulk operation
            texture.SetPixels(clampedStartX, height - clampedEndY, clampedWidth, clampedHeight, resultPixels);
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
