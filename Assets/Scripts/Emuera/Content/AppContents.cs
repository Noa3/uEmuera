using MinorShift.Emuera.Sub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using uEmuera.Drawing;

namespace MinorShift.Emuera.Content
{
    /// <summary>
    /// Manages application content including images, graphics, and resource files.
    /// Provides centralized access to game assets loaded from the resources directory.
    /// </summary>
    static class AppContents
    {
		static AppContents()
		{
			gList = new Dictionary<int, GraphicsImage>();
		}
		static readonly Dictionary<string, AContentFile> resourceDic = new Dictionary<string, AContentFile>();
		static readonly Dictionary<string, ASprite> imageDictionary = new Dictionary<string, ASprite>();
		static readonly Dictionary<int, GraphicsImage> gList;

		//static public T GetContent<T>(string name)where T :AContentItem
		//{
		//	if (name == null)
		//		return null;
		//	name = name.ToUpper();
		//	if (!itemDic.ContainsKey(name))
		//		return null;
		//	return itemDic[name] as T;
		//}
		static public GraphicsImage GetGraphics(int i)
		{
            GraphicsImage gi;
            gList.TryGetValue(i, out gi);
            if(gi != null)
				return gi;
			GraphicsImage g =  new GraphicsImage(i);
			gList[i] = g;
			return g;
		}

		static public ASprite GetSprite(string name)
		{
			if (name == null)
				return null;
            
            // Trim whitespace to handle cases where image names have leading/trailing spaces
            name = name.Trim();
            
            // Guard against empty strings after trimming
            if (string.IsNullOrEmpty(name))
            {
                UnityEngine.Debug.LogWarning("AppContents.GetSprite: Empty or whitespace-only image name provided");
                return null;
            }
            
            name = name.ToUpper();
			ASprite result = null;
			imageDictionary.TryGetValue(name, out result);
			
			// Debug logging to help diagnose image loading issues
			if (result == null)
			{
                // Enhanced diagnostics: show length and first few chars for Unicode issues
                string displayName = name.Length > 20 ? name.Substring(0, 20) + "..." : name;
                UnityEngine.Debug.LogWarning($"AppContents.GetSprite: Image '{displayName}' (len={name.Length}) not found in imageDictionary. " +
					$"Available images count: {imageDictionary.Count}. " +
					$"Make sure the image is registered in a CSV file in the Resources folder.");
			}
			
			return result;
		}

		static public void SpriteDispose(string name)
		{
			if (name == null)
				return;
            name = name.Trim().ToUpper();
            
            // Guard against empty strings after trimming
            if (string.IsNullOrEmpty(name))
                return;

            ASprite sprite = null;
            if(imageDictionary.TryGetValue(name, out sprite))
            {
                sprite.Dispose();
                imageDictionary.Remove(name);
            }
		}

		static public void CreateSpriteG(string imgName, GraphicsImage parent,Rectangle rect)
		{
			if (string.IsNullOrEmpty(imgName))
				throw new ArgumentOutOfRangeException();
			imgName = imgName.Trim().ToUpper();
            if (string.IsNullOrEmpty(imgName))
                throw new ArgumentOutOfRangeException("Image name is empty after trimming");
			SpriteG newCImg = new SpriteG(imgName, parent, rect);
			imageDictionary[imgName] = newCImg;
		}

		internal static void CreateSpriteAnime(string imgName, int w, int h)
		{
			if (string.IsNullOrEmpty(imgName))
				throw new ArgumentOutOfRangeException();
			imgName = imgName.Trim().ToUpper();
            if (string.IsNullOrEmpty(imgName))
                throw new ArgumentOutOfRangeException("Image name is empty after trimming");
			SpriteAnime newCImg = new SpriteAnime(imgName, new Size(w, h));
			imageDictionary[imgName] = newCImg;
		}
		static public bool LoadContents()
		{
			if (!Directory.Exists(Program.ContentDir))
				return true;
			try
			{
				//Search all csv files in the resources folder
				List<string> csvFiles = new List<string>(Directory.GetFiles(Program.ContentDir, "*.csv", SearchOption.TopDirectoryOnly));
#if(UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                csvFiles.AddRange(Directory.GetFiles(Program.ContentDir, "*.CSV", SearchOption.TopDirectoryOnly));
#endif
                UnityEngine.Debug.Log($"AppContents.LoadContents: Found {csvFiles.Count} CSV files in Resources folder");
                
                var count = csvFiles.Count;
                int totalImagesLoaded = 0;
                for(var i=0; i<count; ++i)
				{
                    var filepath = csvFiles[i];
					SpriteAnime currentAnime = null;
					string directory = Path.GetDirectoryName(filepath) + "/";
					string filename = Path.GetFileName(filepath);
                    //string[] lines = File.ReadAllLines(filepath, Config.Encode);
                    string[] lines = uEmuera.Utils.GetResourceCSVLines(filepath, Config.Encode);
					int lineNo = 0;
                    var linecount = lines.Length;
                    for (var l=0; l<linecount; ++l)
					{
                        var line = lines[l];
						lineNo++;
						if (line.Length == 0)
							continue;
						string str = line.Trim();
						if (str.Length == 0 || str.StartsWith(";"))
							continue;
						string[] tokens = str.Split(',');
						//AContentItem item = CreateFromCsv(tokens);
						ScriptPosition sp = new ScriptPosition(filename, lineNo);
						ASprite item = CreateFromCsv(tokens, directory, currentAnime, sp) as ASprite;
						if (item != null)
						{
							currentAnime = item as SpriteAnime;
							if (!imageDictionary.ContainsKey(item.Name))
							{
								imageDictionary.Add(item.Name, item);
								totalImagesLoaded++;
							}
							else
							{
								ParserMediator.Warn("A resource with the same name has already been created:" + item.Name, sp, 0);
								item.Dispose();
							}
						}
					}
				}
				
				UnityEngine.Debug.Log($"AppContents.LoadContents: Successfully loaded {totalImagesLoaded} images from CSV files");
				
				// Log first few image names for debugging
				if (imageDictionary.Count > 0)
				{
					UnityEngine.Debug.Log("Sample of loaded images (first 10):");
					int sampleCount = 0;
					foreach (var key in imageDictionary.Keys)
					{
						UnityEngine.Debug.Log($"  - {key}");
						if (++sampleCount >= 10) break;
					}
				}
			}
			catch(Exception ex)
			{
				UnityEngine.Debug.LogError($"AppContents.LoadContents: Exception during loading - {ex.GetType().Name}: {ex.Message}");
				return false;
				//throw new CodeEE("An error occurred while loading the resource file");
			}
			return true;
		}

		static public void UnloadContents()
		{
            var iter = resourceDic.Values.GetEnumerator();
            while(iter.MoveNext())
				iter.Current.Dispose();
			resourceDic.Clear();
			imageDictionary.Clear();
			foreach (var graph in gList.Values)
				graph.GDispose();
			gList.Clear();
		}

		//For returning to title (no code changes, only delete dynamically created items)
		static public void UnloadGraphicList()
		{
			foreach (var graph in gList.Values)
				graph.GDispose();
			gList.Clear();
		}

		/// <summary>
		/// Reads a line from csv in resources folder and creates a new resource (or adds a frame to existing animation sprite)
		/// </summary>
		/// <param name="tokens"></param>
		/// <param name="dir"></param>
		/// <param name="currentAnime"></param>
		/// <param name="sp"></param>
		/// <returns></returns>
		static private AContentItem CreateFromCsv(string[] tokens, string dir, SpriteAnime currentAnime, ScriptPosition sp)
		{
			if(tokens.Length < 2)
				return null;
			string name = tokens[0].Trim().ToUpper();
			string arg2Original = tokens[1].Trim(); // Image file name - preserve original case for file access
			string arg2 = arg2Original.ToUpper(); // Uppercase version for dictionary keys and comparison
			if (name.Length == 0 || arg2.Length == 0)
				return null;
			//Animation sprite declaration
			if (arg2 == "ANIME")
			{
				if (tokens.Length < 4)
				{
					ParserMediator.Warn("Animation sprite size has not been declared", sp, 1);
					return null;
				}
				//w,h
				int[] sizeValue = new int[2];
				bool sccs = true;
				for (int i = 0; i < 2; i++)
					sccs &= int.TryParse(tokens[i + 2], out sizeValue[i]);
				if (!sccs || sizeValue[0] <= 0 || sizeValue[1] <= 0 || sizeValue[0] > AbstractImage.MAX_IMAGESIZE || sizeValue[1] > AbstractImage.MAX_IMAGESIZE)
				{
					ParserMediator.Warn("Animation sprite size specification is not appropriate", sp, 1);
					return null;
				}
				SpriteAnime anime = new SpriteAnime(name, new Size(sizeValue[0],sizeValue[1]));

				return anime;
			}
			//Other than animation declaration (including animation frames)

			if(arg2.IndexOf('.') < 0)
			{
				ParserMediator.Warn("Second argument has no file extension:" + arg2Original, sp, 1);
				return null;
			}
			// Use original casing for file path, but uppercase for dictionary key
			string parentNameKey = dir + arg2;
			string parentNamePath = dir + arg2Original;

			//Load parent image ConstImage
			if (!resourceDic.ContainsKey(parentNameKey))
			{
				// Try exact path first, then case-insensitive resolution
				string filepath = parentNamePath;
				string resolvedPath = null;
				if (!File.Exists(filepath))
				{
					// Try case-insensitive resolution
					resolvedPath = uEmuera.Utils.ResolvePathInsensitive(filepath, expectDirectory: false);
					if (string.IsNullOrEmpty(resolvedPath))
					{
						ParserMediator.Warn("Specified image file was not found:" + arg2Original + ". A grey placeholder will be displayed.", sp, 1);
						// Create a placeholder bitmap to allow the sprite to be created
						// The actual texture will be created by SpriteManager when needed
						filepath = parentNamePath; // Keep the original path, SpriteManager will create placeholder
					}
					else
					{
						filepath = resolvedPath;
					}
				}
				Bitmap bmp = new Bitmap(filepath);
				if (bmp == null)
				{
					ParserMediator.Warn("Failed to load the specified file:" + arg2Original, sp, 1);
					return null;
				}
                bmp.name = name;
				if (bmp.Width > AbstractImage.MAX_IMAGESIZE || bmp.Height > AbstractImage.MAX_IMAGESIZE)
				{
					//1824-2 Variants using images with width over 8192 already existed, so changed to warn but allow
					//	bmp.Dispose();
					ParserMediator.Warn("Specified image file is too large (strongly recommended to keep width and height to " + AbstractImage.MAX_IMAGESIZE.ToString() + " or less):" + arg2Original, sp, 1);
					//return null;
				}
				ConstImage img = new ConstImage(parentNameKey);
				img.CreateFrom(bmp, Config.TextDrawingMode == TextDrawingMode.WINAPI);
				if (!img.IsCreated)
				{
					ParserMediator.Warn("Failed to create image resource:" + arg2Original, sp, 1);
					return null;
				}
				resourceDic.Add(parentNameKey, img);
			}
			ConstImage parentImage = resourceDic[parentNameKey] as ConstImage;
			if (parentImage == null || !parentImage.IsCreated)
			{
				ParserMediator.Warn("Attempted to create sprite from a resource that failed to create:" + arg2Original, sp, 1);
				return null;
			}
			Rectangle rect = new Rectangle(new Point(0, 0), parentImage.Bitmap.Size);
			Point pos = new Point();
			int delay = 1000;
			//name,parentname, x,y,w,h ,offset_x,offset_y, delayTime
			if(tokens.Length >= 6)//x,y,w,h
			{
				int[] rectValue = new int[4];
				bool sccs = true;
				for (int i = 0; i < 4; i++)
					sccs &= int.TryParse(tokens[i + 2], out rectValue[i]);
				if (sccs)
				{
					rect = new Rectangle(rectValue[0], rectValue[1], rectValue[2], rectValue[3]);
                    pos = new Point(rectValue[0], rectValue[1]);

                    if (rect.Width <= 0 || rect.Height <= 0)
					{
						ParserMediator.Warn("Sprite height or width can only be specified as positive values:" + name, sp, 1);
						return null;
					}
                    //uEmuera has not yet obtained image size at this point
					//if (!rect.IntersectsWith(new Rectangle(0,0,parentImage.Bitmap.Width, parentImage.Bitmap.Height)))
					//{
					//	ParserMediator.Warn("Referencing outside the parent image bounds:" + name, sp, 1);
					//	return null;
					//}
				}
				if(tokens.Length >= 8)
				{
					sccs = true;
					for (int i = 0; i < 2; i++)
						sccs &= int.TryParse(tokens[i + 6], out rectValue[i]);
					if (sccs)
						pos = new Point(rectValue[0], rectValue[1]);
					if (tokens.Length >= 9)
					{
						sccs = int.TryParse(tokens[8], out delay);
						if (sccs && delay <= 0)
						{
							ParserMediator.Warn("Frame display time can only be specified as positive values:" + name, sp, 1);
							return null;
						}
					}
				}
			}
			//Add frame to existing sprite
			if (currentAnime != null && currentAnime.Name == name)
			{
				if(!currentAnime.AddFrame(parentImage, rect, pos, delay))
				{
					ParserMediator.Warn("Failed to add frame to animation sprite:" + arg2Original, sp, 1);
					return null;
				}
				return null;
			}

			//New sprite definition
			ASprite image = new SpriteF(name, parentImage, rect, pos);
			return image;
		}



	}
}
