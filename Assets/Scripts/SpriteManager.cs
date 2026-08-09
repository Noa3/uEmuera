using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MinorShift.Emuera.Content;
using uEmuera.Drawing;
using WebP;

/// <summary>
/// Manages sprite and texture loading, caching, and lifecycle for the Emuera engine.
/// Provides case-insensitive file resolution, placeholder generation for missing assets,
/// and memory-efficient sprite management with automatic cleanup.
/// Thread-safe concurrent loading with callback validation.
/// </summary>
internal static class SpriteManager
{
    static float kPastTime = 300.0f;
    // Thread synchronization for concurrent access
    static readonly object loading_set_lock = new object();
    static readonly object texture_dict_lock = new object();
    
    // File index for fast case-insensitive lookups (maps lowercase filename to actual path)
    static readonly object file_index_lock = new object();
    static Dictionary<string, string> file_index_ = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    static bool file_index_initialized_ = false;
    
    // Negative cache for files confirmed not to exist (avoids repeated expensive searches)
    static readonly HashSet<string> missing_files_cache_ = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static readonly object missing_files_lock = new object();
    
    /// <summary>
    /// Creates a placeholder transparent texture for missing images.
    /// </summary>
    static Texture2D CreatePlaceholderTexture(int width = 64, int height = 64)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var transparentColor = new UnityEngine.Color(0f, 0f, 0f, 0f);
        var pixels = new UnityEngine.Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = transparentColor;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Creates and stores a placeholder TextureInfo.
    /// </summary>
    static TextureInfo CreateAndStorePlaceholder(string name, Bitmap baseimage = null)
    {
        var placeholderTex = CreatePlaceholderTexture();
        var ti = new TextureInfo(name, placeholderTex);
        lock(texture_dict_lock)
        {
            if (!texture_dict.ContainsKey(name))
                texture_dict.Add(name, ti);
        }
        if (baseimage != null)
        {
            baseimage.size.Width = placeholderTex.width;
            baseimage.size.Height = placeholderTex.height;
        }
        return ti;
    }

    /// <summary>
    /// Creates a placeholder SpriteInfo.
    /// </summary>
    static SpriteInfo CreatePlaceholderSpriteInfo(string name)
    {
        var placeholderTex = CreatePlaceholderTexture();
        var ti = new TextureInfo($"_placeholder_{name}", placeholderTex);
        return CreateSpriteInfoFromTexture(ti, placeholderTex);
    }
    
    /// <summary>
    /// Creates a placeholder SpriteInfo for an existing TextureInfo.
    /// </summary>
    static SpriteInfo CreatePlaceholderSpriteInfoForTexture(TextureInfo parentTexture)
    {
        var placeholderTex = CreatePlaceholderTexture();
        return CreateSpriteInfoFromTexture(parentTexture, placeholderTex);
    }
    
    /// <summary>
    /// Creates a SpriteInfo from a texture.
    /// </summary>
    static SpriteInfo CreateSpriteInfoFromTexture(TextureInfo textureInfo, Texture2D texture)
    {
        var rect = new Rect(0, 0, texture.width, texture.height);
        var sprite = Sprite.Create(texture, rect, Vector2.zero);
        return new SpriteInfo(textureInfo, sprite);
    }
    
    /// <summary>
    /// Supported image file extensions for indexing.
    /// </summary>
    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };
    
    /// <summary>
    /// Initializes the file index by scanning the resources directory.
    /// Single-pass scan for optimal performance.
    /// Call this when loading a game to enable fast case-insensitive file lookups.
    /// </summary>
    /// <param name="resourcesDirectory">The resources directory to scan</param>
    public static void InitializeFileIndex(string resourcesDirectory)
    {
        if (string.IsNullOrEmpty(resourcesDirectory))
            return;
            
        lock (file_index_lock)
        {
            file_index_.Clear();
            file_index_initialized_ = false;
        }
        lock (missing_files_lock)
        {
            missing_files_cache_.Clear();
        }
        
        if (!Directory.Exists(resourcesDirectory))
        {
            Debug.LogWarning($"SpriteManager: Resources directory not found: {resourcesDirectory}");
            return;
        }
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int fileCount = 0;
        
        try
        {
            // Single-pass: scan all image files at once
            var allFiles = new List<string>();
            foreach (var ext in ImageExtensions)
            {
                try
                {
                    allFiles.AddRange(Directory.GetFiles(resourcesDirectory, "*" + ext, SearchOption.AllDirectories));
                }
                catch { }
            }
            
            // Index all files in one pass
            foreach (var file in allFiles)
            {
                var fileName = Path.GetFileName(file);
                
                // Index by filename
                if (!file_index_.ContainsKey(fileName))
                {
                    file_index_[fileName] = file;
                    fileCount++;
                }
                
                // Also index by relative path (normalized with forward slashes)
                if (file.StartsWith(resourcesDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = file.Substring(resourcesDirectory.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace('\\', '/');
                    if (!file_index_.ContainsKey(relativePath))
                    {
                        file_index_[relativePath] = file;
                    }
                }
            }
            
            file_index_initialized_ = true;
                    stopwatch.Stop();
                    Debug.Log($"SpriteManager: Indexed {fileCount} files in {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SpriteManager: Failed to initialize file index: {ex.Message}");
                }
            }

            /// <summary>
            /// Tries to resolve a file path using the pre-built index.
            /// </summary>
            static string TryResolveFromIndex(string originalPath)
            {
                if (string.IsNullOrEmpty(originalPath) || !file_index_initialized_)
                    return null;
            
                // Try exact path first
                if (file_index_.TryGetValue(originalPath, out var resolved))
                    return resolved;
            
                // Try with normalized path separators
                var normalized = originalPath.Replace('\\', '/');
                if (file_index_.TryGetValue(normalized, out resolved))
                    return resolved;
            
                // Try filename only
                var fileName = Path.GetFileName(originalPath);
                if (file_index_.TryGetValue(fileName, out resolved))
                    return resolved;
        
                return null;
            }
    
            /// <summary>
            /// Performs Unicode normalization search for Japanese filenames.
    /// Only called as last resort when standard lookups fail.
    /// </summary>
    static string TryUnicodeNormalizedSearch(string originalPath)
    {
        if (string.IsNullOrEmpty(originalPath) || !file_index_initialized_)
            return null;
            
        var fileName = Path.GetFileName(originalPath);
        
        // Try Unicode normalized versions
        var normalizedFormC = fileName.Normalize(NormalizationForm.FormC);
        var normalizedFormD = fileName.Normalize(NormalizationForm.FormD);
        
        if (file_index_.TryGetValue(normalizedFormC, out var resolved))
            return resolved;
        if (file_index_.TryGetValue(normalizedFormD, out resolved))
                return resolved;
        
            return null;
        }
    
        /// <summary>
        /// Checks if a file is in the missing files cache.
        /// </summary>
        static bool IsKnownMissing(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            
            lock (missing_files_lock)
            {
                return missing_files_cache_.Contains(path) || 
                       missing_files_cache_.Contains(Path.GetFileName(path));
            }
        }
    
        /// <summary>
        /// Adds a file path to the missing files cache.
        /// </summary>
        static void MarkAsMissing(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            
            lock (missing_files_lock)
            {
                missing_files_cache_.Add(path);
                missing_files_cache_.Add(Path.GetFileName(path));
            }
    }

    /// <summary>
    /// Normalizes path separators for the current platform.
    /// Replaces both forward and backward slashes with the platform-specific separator.
    /// </summary>
    static string NormalizePathSeparators(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        
        // Replace both slash types with platform separator
        if (Path.DirectorySeparatorChar == '/')
            return path.Replace('\\', '/');
        else
            return path.Replace('/', '\\');
    }

    // File resolution is handled by uEmuera.Utils.ResolvePathInsensitive
    // This method is kept for backward compatibility but delegates to Utils
    static string ResolvePathCaseInsensitive(string originalPath)
    {
        return uEmuera.Utils.ResolvePathInsensitive(originalPath, expectDirectory: false);
    }

    internal class SpriteInfo : IDisposable
    {
        internal SpriteInfo(TextureInfo p, Sprite s)
        {
            parent = p;
            sprite = s;
        }
        public void Dispose()
        {
            UnityEngine.Object.Destroy(sprite);
            sprite = null;
        }
        internal Sprite sprite;
        internal TextureInfo parent;
    }
    internal class TextureInfo : IDisposable
    {
        internal TextureInfo(string b, Texture2D tex)
        {
            imagename = b;
            texture = tex;
            pasttime = Time.unscaledTime + kPastTime;
        }
        internal SpriteInfo GetSprite(ASprite src)
        {
            SpriteInfo sprite = null;
            if(!sprites.TryGetValue(src.Name, out sprite))
            {
                // For ASpriteSingle, use SrcRectangle (texture coordinates) instead of Rectangle (destination coords)
                Rectangle sourceRect;
                if (src is ASpriteSingle spriteSingle)
                {
                    sourceRect = spriteSingle.SrcRectangle;
                }
                else
                {
                    // For other sprite types (like SpriteAnime), fall back to Rectangle
                    // This may need adjustment if SpriteAnime has similar requirements
                    sourceRect = src.Rectangle;
                }
                
                // Handle 0x0 rectangle as "use full texture" (for auto-discovered images)
                if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
                {
                    sourceRect = new Rectangle(0, 0, texture.width, texture.height);
                }
                
                // Convert source rectangle to Unity coordinates
                var rect = GenericUtils.ToUnityRect(sourceRect, texture.width, texture.height);
                
                // Check if the conversion resulted in an invalid/empty rectangle
                if (rect.width <= 0 || rect.height <= 0)
                {
                    Debug.LogWarning($"SpriteManager: Invalid sprite rectangle for '{src?.Name}' on '{imagename}'. " +
                        $"Source=({sourceRect.X},{sourceRect.Y},{sourceRect.Width},{sourceRect.Height}), " +
                        $"Converted=({rect.x},{rect.y},{rect.width},{rect.height}), " +
                        $"Texture=({texture.width},{texture.height}). Creating placeholder sprite.");
                    
                    // Use the centralized placeholder creation method
                    sprite = CreatePlaceholderSpriteInfoForTexture(this);
                    sprites[src.Name] = sprite;
                    refcount += 1;
                    return sprite;
                }
                
                // Validate final rectangle bounds (should be guaranteed by clamping, but double-check)
                if (rect.x < 0 || rect.y < 0 || 
                    rect.x + rect.width > texture.width || rect.y + rect.height > texture.height)
                {
                    Debug.LogError($"SpriteManager: Rectangle validation failed for '{src?.Name}' on '{imagename}'. " +
                        $"Rect=({rect.x},{rect.y},{rect.width},{rect.height}), Texture=({texture.width},{texture.height})");
                    return null;
                }
                
                try
                {
                    sprite = new SpriteInfo(this, 
                        Sprite.Create(texture,
                            rect,
                            Vector2.zero)
                        );
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SpriteManager: Failed to create Sprite for '{src?.Name}' from '{imagename}'. Error={ex.GetType().Name}: {ex.Message}");
                    return null;
                }
                sprites[src.Name] = sprite;
            }
            if(sprite != null)
                refcount += 1;
            return sprite;
        }
        internal void Release()
        {
            refcount -= 1;
            pasttime = Time.unscaledTime + kPastTime;
        }
        public void Dispose()
        {
            var iter = sprites.Values.GetEnumerator();
            while(iter.MoveNext())
            {
                iter.Current.Dispose();
            }
            sprites.Clear();
            sprites = null;

            UnityEngine.Object.Destroy(texture);
            texture = null;
        }
        internal string imagename = null;
        internal int refcount = 0;
        internal float pasttime = 0;
        internal float width { get { return texture.width; } }
        internal float height { get { return texture.height; } }
        internal Texture2D texture = null;
        Dictionary<string, SpriteInfo> sprites = new Dictionary<string, SpriteInfo>(StringComparer.OrdinalIgnoreCase);
    }
    class CallbackInfo
    {
        public CallbackInfo(ASprite src, object obj, 
                            Action<object, SpriteInfo> callback)
        {
            this.src = src;
            this.obj = obj;
            this.callback = callback;
            this.target_alive = obj != null ? true : false;
        }
        public void DoCallback(SpriteInfo info)
        {
            // Validate that the target object is still alive (not destroyed)
            // This prevents callbacks from executing on deleted GameObjects
            if (obj != null && target_alive)
            {
                try
                {
                    callback(obj, info);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SpriteManager: Exception in sprite callback: {ex.GetType().Name}: {ex.Message}");
                }
            }
            else if (info != null)
            {
                // Target is gone, clean up the sprite
                GivebackSpriteInfo(info);
            }
        }
        // Made internal for deduplication checks in GetSprite
        internal ASprite src;
        internal object obj;
        Action<object, SpriteInfo> callback;
        bool target_alive;
    }

    public static void Init()
    {
#if UNITY_EDITOR
        kPastTime = 300.0f;
        GenericUtils.StartCoroutine(Update());
        GenericUtils.StartCoroutine(UpdateRenderOP());
        GenericUtils.StartCoroutine(UpdateGraphicsSurface());
#else
        var memorysize = SystemInfo.systemMemorySize;
        if(memorysize <= 4096)
        {
            kPastTime = 300.0f;
            GenericUtils.StartCoroutine(Update());

        }
        else if(memorysize <= 8192)
        {
            kPastTime = 600.0f;
            GenericUtils.StartCoroutine(Update());
            GenericUtils.StartCoroutine(UpdateRenderOP());
        }
        //else
        //{
            //
        //}
#endif
    }
    public static void GetSprite(ASprite src, 
                                object obj, Action<object, SpriteInfo> callback)
    {
        if(src == null)
        {
            Debug.LogError("SpriteManager: GetSprite called with null ASprite");
            if(callback != null)
                callback(null, null);
            return;
        }
        if(src.Bitmap == null)
        {
            Debug.LogWarning($"SpriteManager: ASprite '{src?.Name}' has null Bitmap. Creating transparent placeholder sprite.");
            // Create a transparent placeholder sprite instead of passing null
            if(callback != null)
            {
                var placeholderSprite = CreatePlaceholderSpriteInfo(src.Name);
                callback(obj, placeholderSprite);
            }
            return;
        }

        var basename = src.Bitmap.filename;
        TextureInfo ti = null;
        lock(texture_dict_lock)
        {
            texture_dict.TryGetValue(basename, out ti);
        }
        if(ti == null)
        {
            // IMMEDIATE NON-BLOCKING: Return placeholder immediately so screen can switch
            // The actual texture will load in background and update later.
            // NOTE: The callback will be called TWICE:
            //   1. Immediately with a placeholder (transparent sprite)
            //   2. Later when the actual texture loads
            // This is intentional - SetSprite() handles sprite replacement properly
            // by releasing the old sprite before storing the new one.
            if(callback != null)
            {
                var placeholderSprite = CreatePlaceholderSpriteInfo(src.Name);
                callback(obj, placeholderSprite);
            }
            
            // Queue background loading
            var item = new CallbackInfo(src, obj, callback);
            lock(loading_set_lock)
            {
                List<CallbackInfo> list = null;
                if(loading_set.TryGetValue(basename, out list))
                {
                    // Check if this exact callback is already pending (deduplication)
                    bool already_queued = false;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].obj == obj && list[i].src == src)
                        {
                            already_queued = true;
                            break;
                        }
                    }
                    if (!already_queued)
                    {
                        list.Add(item);
                    }
                }
                else
                {
                    list = new List<CallbackInfo> { item };
                    loading_set.Add(basename, list);
                    GenericUtils.StartCoroutine(Loading(src.Bitmap));
                }
            }
        }
        else
            callback(obj, GetSpriteInfo(ti, src));
    }

    public static TextureInfo GetTextureInfo(string name, string filename)
    {
        // Normalize names by trimming whitespace
        name = name?.Trim() ?? "";
        
        TextureInfo ti = null;
        lock(texture_dict_lock)
        {
            if(texture_dict.TryGetValue(name, out ti))
                return ti;
        }
        if(string.IsNullOrEmpty(filename))
        {
            return CreateAndStorePlaceholder(name);
        }

        // Use file index for fast lookup
        string pathToLoad = TryResolveFromIndex(filename);
        if (string.IsNullOrEmpty(pathToLoad))
        {
            // Try Unicode normalized search
            pathToLoad = TryUnicodeNormalizedSearch(filename);
        }
        if (string.IsNullOrEmpty(pathToLoad))
        {
            // Fall back to original path with normalized separators
            pathToLoad = NormalizePathSeparators(filename);
            }
        
            if(!File.Exists(pathToLoad))
            {
                return CreateAndStorePlaceholder(name);
            }

            try
            {
                var content = File.ReadAllBytes(pathToLoad);
                if (content == null || content.Length == 0)
                {
                    return CreateAndStorePlaceholder(name);
                }

                TextureFormat format = TextureFormat.RGBA32;
                var extname = uEmuera.Utils.GetSuffix(pathToLoad).ToLower();

                if (extname == "webp")
                {
                    var tex = Texture2DExt.CreateTexture2DFromWebP(content, false, false,
                    out Error err);
                if (err != Error.Success)
                {
                    Debug.LogError($"SpriteManager: Failed to decode WEBP '{pathToLoad}'. Error={err}");
                    return null;
                }
                ti = new TextureInfo(name, tex);
                lock(texture_dict_lock)
                {
                    texture_dict.Add(name, ti);
                }
            }
            else
            {
                var tex = new Texture2D(2, 2, format, false);
                if (tex.LoadImage(content))
                {
                    ti = new TextureInfo(name, tex);
                    lock(texture_dict_lock)
                    {
                        texture_dict.Add(name, ti);
                    }
                }
                else
                {
                    Debug.LogError($"SpriteManager: Failed to load image '{pathToLoad}' (ext={extname})");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SpriteManager: Exception while reading texture '{name}' from '{pathToLoad}'. Error={ex.GetType().Name}: {ex.Message}");
            return null;
        }
        return ti;
    }

    public static TextureInfoOtherThread GetTextureInfoOtherThread(
        string name, string path, Action<TextureInfo> callback)
    {
        var ti = new TextureInfoOtherThread
        {
            name = name,
            path = path,
            callback = callback,
            mutex = null,
        };
        texture_other_threads.Add(ti);
        return ti;
    }
    public class TextureInfoOtherThread
    {
        public string name;
        public string path;
        public Action<TextureInfo> callback;
        public System.Threading.Mutex mutex;
    }
    static List<TextureInfoOtherThread> texture_other_threads = new List<TextureInfoOtherThread>();

    public static RenderTextureOtherThread GetRenderTextureOtherThread(int x, int y, Action<RenderTexture> callback)
    {
        var ti = new RenderTextureOtherThread
        {
            x = x,
            y = y,
            callback = callback,
            mutex = null,
        };
        render_texture_other_threads.Add(ti);
        return ti;
    }
    public class RenderTextureOtherThread
    {
        public int x;
        public int y;
        public Action<RenderTexture> callback;
        public System.Threading.Mutex mutex;
    }
    static List<RenderTextureOtherThread> render_texture_other_threads = new List<RenderTextureOtherThread>();

    ///public static RenderTextureDoSomething RenderTexture
    ///


    public class RenderTextureDoSomething
    {
        public enum Code
        {
            kClear,
            kDrawRectangle,
            kFillRectangle,
            kDrawCImg,
            kDrawG,
            kDrawGWithMask,
            kSetColor,
            kGetColor,
        }
        //Todo: ??????
    }

    // Preloading support for critical images
    static readonly List<string> preload_queue_ = new List<string>();
    static readonly object preload_queue_lock_ = new object();
    static bool preload_in_progress_ = false;

    /// <summary>
    /// Adds an image to the preload queue. Images in the preload queue will be loaded
    /// in the background with higher priority than on-demand loads.
    /// </summary>
    /// <param name="imageName">The image name to preload</param>
    public static void PreloadImage(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
            return;

        lock (preload_queue_lock_)
        {
            if (!preload_queue_.Contains(imageName))
            {
                preload_queue_.Add(imageName);
                
                // Start preloading coroutine if not already running
                if (!preload_in_progress_)
                {
                    preload_in_progress_ = true;
                    GenericUtils.StartCoroutine(PreloadCoroutine());
                }
            }
        }
    }

    /// <summary>
    /// Adds multiple images to the preload queue.
    /// </summary>
    /// <param name="imageNames">Array of image names to preload</param>
    public static void PreloadImages(params string[] imageNames)
    {
        if (imageNames == null || imageNames.Length == 0)
            return;

        lock (preload_queue_lock_)
        {
            int addedCount = 0;
            foreach (var name in imageNames)
            {
                if (!string.IsNullOrEmpty(name) && !preload_queue_.Contains(name))
                {
                    preload_queue_.Add(name);
                    addedCount++;
                }
            }

#if UNITY_EDITOR || DEBUG
            if (addedCount > 0)
            {
                Debug.Log($"SpriteManager: Added {addedCount} images to preload queue (total: {preload_queue_.Count})");
            }
#endif

            // Start preloading coroutine if not already running
            if (!preload_in_progress_ && preload_queue_.Count > 0)
            {
                preload_in_progress_ = true;
                GenericUtils.StartCoroutine(PreloadCoroutine());
            }
        }
    }

    /// <summary>
    /// Checks if preloading is currently in progress.
    /// </summary>
    public static bool IsPreloadingInProgress()
    {
        lock (preload_queue_lock_)
        {
            return preload_in_progress_;
        }
    }

    /// <summary>
    /// Coroutine that processes the preload queue.
    /// </summary>
    static IEnumerator PreloadCoroutine()
    {
        while (true)
        {
            string imageToLoad = null;
            
            lock (preload_queue_lock_)
            {
                if (preload_queue_.Count > 0)
                {
                    imageToLoad = preload_queue_[0];
                    preload_queue_.RemoveAt(0);
                }
                else
                {
                    preload_in_progress_ = false;
                    yield break;
                }
            }

            if (!string.IsNullOrEmpty(imageToLoad))
            {
                // Try to get the sprite from AppContents
                var sprite = MinorShift.Emuera.Content.AppContents.GetSprite(imageToLoad);
                if (sprite != null && sprite.Bitmap != null)
                {
                    // Check if already loaded
                    bool alreadyLoaded = false;
                    lock (texture_dict_lock)
                    {
                        alreadyLoaded = texture_dict.ContainsKey(sprite.Bitmap.filename);
                    }

                    if (!alreadyLoaded)
                    {
                        // Start loading in background via coroutine
                        // The Loading coroutine already handles yielding appropriately
                        yield return Loading(sprite.Bitmap);
                    }
                }
            }

            // Yield between items to maintain UI responsiveness
            // Small delay prevents tight loop when processing many items
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Gets cache statistics for debugging and monitoring.
    /// </summary>
    public static CacheStats GetCacheStats()
    {
        var stats = new CacheStats();
        
        lock (texture_dict_lock)
        {
            stats.LoadedTexturesCount = texture_dict.Count;
        }
        
        lock (file_index_lock)
        {
            stats.IndexedFilesCount = file_index_.Count;
            stats.FileIndexInitialized = file_index_initialized_;
        }
        
        lock (missing_files_lock)
        {
            stats.MissingFilesCachedCount = missing_files_cache_.Count;
        }
        
        lock (loading_set_lock)
        {
            stats.LoadingInProgressCount = loading_set.Count;
        }
        
        lock (preload_queue_lock_)
        {
            stats.PreloadQueueCount = preload_queue_.Count;
        }
        
        return stats;
    }

    /// <summary>
    /// Cache statistics structure.
    /// </summary>
    public struct CacheStats
    {
        public int LoadedTexturesCount;
        public int IndexedFilesCount;
        public bool FileIndexInitialized;
        public int MissingFilesCachedCount;
        public int LoadingInProgressCount;
        public int PreloadQueueCount;

        public override string ToString()
        {
            return $"SpriteManager Stats: Loaded={LoadedTexturesCount}, " +
                   $"Indexed={IndexedFilesCount}, Missing={MissingFilesCachedCount}, " +
                   $"Loading={LoadingInProgressCount}, Preload={PreloadQueueCount}";
        }
    }

    static IEnumerator Loading(Bitmap baseimage)
    {
        TextureInfo ti = null;
        string pathToLoad = NormalizePathSeparators(baseimage.path);
        
        // Fast path: Check if already known to be missing
        if (IsKnownMissing(pathToLoad))
        {
            ti = CreateAndStorePlaceholder(baseimage.filename, baseimage);
            ProcessLoadingCallbacks(baseimage.filename, ti);
            yield break;
        }
        
        // Try index-based lookup first (fast O(1) lookup)
        var indexResolved = TryResolveFromIndex(pathToLoad);
        if (!string.IsNullOrEmpty(indexResolved))
        {
            pathToLoad = indexResolved;
        }
        else
        {
            // Try Unicode normalized search for Japanese filenames
            var unicodeResolved = TryUnicodeNormalizedSearch(pathToLoad);
            if (!string.IsNullOrEmpty(unicodeResolved))
                    {
                        pathToLoad = unicodeResolved;
                    }
                }
        
                if(File.Exists(pathToLoad))
                {
                    byte[] content = null;
                    try
                    {
                        content = File.ReadAllBytes(pathToLoad);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"SpriteManager: Failed to read '{pathToLoad}': {ex.Message}");
                    }

                    if (content != null && content.Length > 0)
                    {
                        TextureFormat format = TextureFormat.RGBA32;
                        var extname = uEmuera.Utils.GetSuffix(pathToLoad).ToLower();

                        if (extname == "webp")
                        {
                            var tex = Texture2DExt.CreateTexture2DFromWebP(content, false, false, out Error err);
                            if (err == Error.Success)
                            {
                                ti = new TextureInfo(baseimage.filename, tex);
                                lock(texture_dict_lock)
                                {
                                    if (!texture_dict.ContainsKey(baseimage.filename))
                                        texture_dict.Add(baseimage.filename, ti);
                                    baseimage.size.Width = tex.width;
                                    baseimage.size.Height = tex.height;
                                }
                            }
                        }
                        else
                        {
                            var tex = new Texture2D(2, 2, format, false);
                            if (tex.LoadImage(content))
                            {
                                ti = new TextureInfo(baseimage.filename, tex);
                                lock(texture_dict_lock)
                                {
                                    if (!texture_dict.ContainsKey(baseimage.filename))
                                        texture_dict.Add(baseimage.filename, ti);
                                    baseimage.size.Width = tex.width;
                                    baseimage.size.Height = tex.height;
                                }
                            }
                        }
                    }
                }
        
                // If loading failed, create placeholder and mark as missing
                if (ti == null)
                {
                    MarkAsMissing(baseimage.path);
                    ti = CreateAndStorePlaceholder(baseimage.filename, baseimage);
                }
        
                ProcessLoadingCallbacks(baseimage.filename, ti);
                yield break;
            }
    
    /// <summary>
    /// Processes callbacks for loaded textures.
    /// Extracted to avoid code duplication.
    /// </summary>
    static void ProcessLoadingCallbacks(string filename, TextureInfo ti)
    {
        lock(loading_set_lock)
        {
            List<CallbackInfo> list = null;
            if(loading_set.TryGetValue(filename, out list))
            {
                var count = list.Count;
                CallbackInfo item = null;
                for(int i=0; i<count; ++i)
                {
                    item = list[i];
                    item.DoCallback(GetSpriteInfo(ti, item.src));
                }
                list.Clear();
                loading_set.Remove(filename);
            }
        }
    }
    static SpriteInfo GetSpriteInfo(TextureInfo textinfo, ASprite src)
    {
        if (textinfo == null)
        {
            Debug.LogError($"SpriteManager: TextureInfo is null for sprite '{src?.Name}'. Bitmap='{src?.Bitmap?.filename}', Path='{src?.Bitmap?.path}'");
            return null;
        }
        if (src == null)
        {
            Debug.LogError("SpriteManager: GetSpriteInfo called with null ASprite");
            return null;
        }
        return textinfo.GetSprite(src);
    }
    internal static void GivebackSpriteInfo(SpriteInfo info)
    {
        if(info == null)
            return;
        info.parent.Release();
    }
    static IEnumerator Update()
    {
        while(true)
        {
            do
            {
                yield return new WaitForSeconds(15.0f);
            } while(texture_dict.Count == 0);

            var now = Time.unscaledTime;
            TextureInfo tinfo = null;
            TextureInfo ti = null;
            lock(texture_dict_lock)
            {
                var iter = texture_dict.Values.GetEnumerator();
                while(iter.MoveNext())
                {
                    ti = iter.Current;
                    if(ti.refcount == 0 && now > ti.pasttime)
                    {
                        tinfo = ti;
                        break;
                    }
                }
            }
            if(tinfo != null)
            {
                Debug.Log("Unload Texture " + tinfo.imagename);

                tinfo.Dispose();
                lock(texture_dict_lock)
                {
                    texture_dict.Remove(tinfo.imagename);
                }
                tinfo = null;

                GC.Collect();
            }
        }
    }
    /// <summary>
    /// Runs pending Graphics (G) main-thread blit operations on the Unity main thread
    /// once per frame. CPU-only operations never enter this queue.
    /// </summary>
    static IEnumerator UpdateGraphicsSurface()
    {
        while(true)
        {
            AppContents.ExecutePendingGraphicsOps();
            yield return null;
        }
    }

    static IEnumerator UpdateRenderOP()
    {
        while(true)
        {
            do
            {
                yield return new WaitForSeconds(15);
            } while(texture_other_threads.Count == 0
                && render_texture_other_threads.Count == 0);

            TextureInfo ti = null;
            if(texture_other_threads.Count > 0)
            {
                TextureInfoOtherThread tiot = null;
                var tiotiter = texture_other_threads.GetEnumerator();
                while(tiotiter.MoveNext())
                {
                    tiot = tiotiter.Current;
                    tiot.mutex = new System.Threading.Mutex(true);
                    //tiot.mutex.WaitOne();
                    ti = GetTextureInfo(tiot.name, tiot.path);
                    tiot.callback(ti);
                    tiot.mutex.ReleaseMutex();
                }
                texture_other_threads.Clear();
            }
            if(render_texture_other_threads.Count > 0)
            {
                RenderTextureOtherThread rtot = null;
                var rtotiter = render_texture_other_threads.GetEnumerator();
                while(rtotiter.MoveNext())
                {
                    rtot = rtotiter.Current;
                    rtot.mutex = new System.Threading.Mutex(true);
                    //tiot.mutex.WaitOne();
                    var rt = new RenderTexture(rtot.x, rtot.y, 24, RenderTextureFormat.ARGB32);
                    rtot.callback(rt);
                    rtot.mutex.ReleaseMutex();
                }
                render_texture_other_threads.Clear();
            }
        }
    }
    internal static void ForceClear()
    {
        lock(texture_dict_lock)
        {
            var iter = texture_dict.Values.GetEnumerator();
            while(iter.MoveNext())
            {
                iter.Current.Dispose();
            }
            texture_dict.Clear();
        }
        lock(loading_set_lock)
        {
            loading_set.Clear();
        }
        lock(file_index_lock)
        {
            file_index_.Clear();
            file_index_initialized_ = false;
        }
        lock(missing_files_lock)
        {
            missing_files_cache_.Clear();
        }
        lock(preload_queue_lock_)
        {
            preload_queue_.Clear();
            preload_in_progress_ = false;
        }
        GC.Collect();
    }
    internal static void SetResourceCSVLine(string filename, string[] lines)
    {
        var cache = string.Join("\n", lines);
        UnityEngine.PlayerPrefs.SetInt(filename + "_fixed", 1);
        UnityEngine.PlayerPrefs.SetString(filename + "_time",
                        File.GetLastWriteTime(filename).ToString());
        UnityEngine.PlayerPrefs.SetString(filename, cache);
    }
    internal static string[] GetResourceCSVLines(string filename)
    {
        if(PlayerPrefs.GetInt(filename + "_fixed", 0) == 0)
            return null;
        var oldwritetime = PlayerPrefs.GetString(filename + "_time", null);
        if(string.IsNullOrEmpty(oldwritetime))
            return null;
        var writetime = File.GetLastWriteTime(filename).ToString();
        if(oldwritetime != writetime)
            return null;
        var cache = UnityEngine.PlayerPrefs.GetString(filename, null);
        if(string.IsNullOrEmpty(cache))
            return null;
        return cache.Split('\n');
    }
    internal static void ClearResourceCSVLines(string filename)
    {
        UnityEngine.PlayerPrefs.SetInt(filename + "_fixed", 0);
        UnityEngine.PlayerPrefs.SetString(filename + "_time", null);
        UnityEngine.PlayerPrefs.SetString(filename, null);
    }
    static Dictionary<string, List<CallbackInfo>> loading_set =
        new Dictionary<string, List<CallbackInfo>>(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, TextureInfo> texture_dict =
        new Dictionary<string, TextureInfo>(StringComparer.OrdinalIgnoreCase);
    }