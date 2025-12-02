using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera.Content;
using uEmuera.Drawing;
using WebP;

internal static class SpriteManager
{
    static float kPastTime = 300.0f;

    // Attempts to resolve a full path in a case-insensitive manner by walking each segment.
    // Useful on case-sensitive file systems or when asset references use inconsistent casing.
    static string ResolvePathCaseInsensitive(string originalPath)
    {
        if (string.IsNullOrEmpty(originalPath))
            return null;
        try
        {
            var seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var root = Path.GetPathRoot(originalPath);
            var relative = string.IsNullOrEmpty(root) ? originalPath : originalPath.Substring(root.Length);
            var parts = relative.Split(seps, StringSplitOptions.RemoveEmptyEntries);

            string current = string.IsNullOrEmpty(root) ? Directory.GetCurrentDirectory() : root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                bool last = i == parts.Length - 1;
                if (!Directory.Exists(current))
                    return null;

                if (!last)
                {
                    string matchedDir = null;
                    var dirs = Directory.GetDirectories(current);
                    for (int d = 0; d < dirs.Length; d++)
                    {
                        var dirName = Path.GetFileName(dirs[d]);
                        if (string.Equals(dirName, part, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedDir = dirs[d];
                            break;
                        }
                    }
                    if (matchedDir == null)
                        return null;
                    current = matchedDir;
                }
                else
                {
                    // Last segment assumed to be a file
                    var files = Directory.GetFiles(current);
                    for (int f = 0; f < files.Length; f++)
                    {
                        var fileName = Path.GetFileName(files[f]);
                        if (string.Equals(fileName, part, StringComparison.OrdinalIgnoreCase))
                            return files[f];
                    }
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SpriteManager: ResolvePathCaseInsensitive failed for '{originalPath}': {ex.GetType().Name}: {ex.Message}");
        }
        return null;
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
                // Validate rectangle before creating sprite to provide deterministic errors
                var rect = GenericUtils.ToUnityRect(src.Rectangle, texture.width, texture.height);
                if (rect.width <= 0 || rect.height <= 0 || rect.x < 0 || rect.y < 0 ||
                    rect.x + rect.width > texture.width || rect.y + rect.height > texture.height)
                {
                    Debug.LogError($"SpriteManager: Invalid sprite rectangle for '{src?.Name}' on '{imagename}'. Rect=({rect.x},{rect.y},{rect.width},{rect.height}), Texture=({texture.width},{texture.height})");
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
        }
        public void DoCallback(SpriteInfo info)
        {
            callback(obj, info);
        }
        public ASprite src;
        object obj;
        Action<object, SpriteInfo> callback;
    }

    public static void Init()
    {
#if UNITY_EDITOR
        kPastTime = 300.0f;
        GenericUtils.StartCoroutine(Update());
        GenericUtils.StartCoroutine(UpdateRenderOP());
#else
        var memorysize = SystemInfo.systemMemorySize;
        if(memorysize <= 4096)
        {
            kPastTime = 300.0f;
            GenericUtils.StartCoroutine(Update());
            GenericUtils.StartCoroutine(UpdateRenderOP());
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
            Debug.LogError($"SpriteManager: ASprite '{src?.Name}' has null Bitmap");
            if(callback != null)
                callback(null, null);
            return;
        }

        var basename = src.Bitmap.filename;
        TextureInfo ti = null;
        texture_dict.TryGetValue(basename, out ti);
        if(ti == null)
        {
            var item = new CallbackInfo(src, obj, callback);
            List<CallbackInfo> list = null;
            if(loading_set.TryGetValue(basename, out list))
                list.Add(item);
            else
            {
                list = new List<CallbackInfo> { item };
                loading_set.Add(basename, list);
                GenericUtils.StartCoroutine(Loading(src.Bitmap));
            }
        }
        else
            callback(obj, GetSpriteInfo(ti, src));
    }

    public static TextureInfo GetTextureInfo(string name, string filename)
    {
        TextureInfo ti = null;
        if(texture_dict.TryGetValue(name, out ti))
            return ti;
        if(string.IsNullOrEmpty(filename))
        {
            Debug.LogError($"SpriteManager: Empty filename for texture '{name}'");
            return null;
        }

        string pathToLoad = filename.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        FileInfo fi = new FileInfo(pathToLoad);
        if(!fi.Exists)
        {
            // Try original, lower, upper, and mixed-case filename variants within the same directory first
            var dir = Path.GetDirectoryName(pathToLoad);
            var file = Path.GetFileName(pathToLoad);
            if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(file) && Directory.Exists(dir))
            {
                foreach (var variant in GenerateFilenameCaseVariants(dir, file))
                {
                    if (File.Exists(variant))
                    {
                        Debug.LogWarning($"SpriteManager: Using case-variant for '{pathToLoad}' -> '{variant}'");
                        pathToLoad = variant;
                        fi = new FileInfo(pathToLoad);
                        break;
                    }
                }
            }
            // Attempt case-insensitive full path resolution
            if(!fi.Exists)
            {
                var resolved = ResolvePathCaseInsensitive(pathToLoad);
                if (!string.IsNullOrEmpty(resolved))
                {
                    Debug.LogWarning($"SpriteManager: Fixed path casing for '{filename}' -> '{resolved}'");
                    pathToLoad = resolved;
                    fi = new FileInfo(pathToLoad);
                }
                else
                {
                    Debug.LogError($"SpriteManager: File not found for texture '{name}': {filename}");
                    return null;
                }
            }
        }

        try
        {
            var content = File.ReadAllBytes(pathToLoad);
            if (content == null || content.Length == 0)
            {
                Debug.LogError($"SpriteManager: Empty content while reading '{pathToLoad}'");
                return null;
            }

            // Use a safe default format for runtime-loaded images
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
                texture_dict.Add(name, ti);
            }
            else
            {
                var tex = new Texture2D(2, 2, format, false);
                if (tex.LoadImage(content))
                {
                    ti = new TextureInfo(name, tex);
                    texture_dict.Add(name, ti);
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
        //Todo: 实现对于方法
    }

    static IEnumerator Loading(Bitmap baseimage)
    {
        TextureInfo ti = null;
        string pathToLoad = baseimage.path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        FileInfo fi = new FileInfo(pathToLoad);
        if(!fi.Exists)
        {
            var dir = Path.GetDirectoryName(pathToLoad);
            var file = Path.GetFileName(pathToLoad);
            if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(file) && Directory.Exists(dir))
            {
                foreach (var variant in GenerateFilenameCaseVariants(dir, file))
                {
                    if (File.Exists(variant))
                    {
                        Debug.LogWarning($"SpriteManager: Using case-variant for '{pathToLoad}' -> '{variant}'");
                        pathToLoad = variant;
                        fi = new FileInfo(pathToLoad);
                        break;
                    }
                }
            }
            if(!fi.Exists)
            {
                var resolved = ResolvePathCaseInsensitive(pathToLoad);
                if (!string.IsNullOrEmpty(resolved))
                {
                    Debug.LogWarning($"SpriteManager: Fixed path casing for '{pathToLoad}' -> '{resolved}'");
                    pathToLoad = resolved;
                    fi = new FileInfo(pathToLoad);
                }
            }
        }
        if(fi.Exists)
        {
            byte[] content = null;
            try
            {
                content = File.ReadAllBytes(pathToLoad);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SpriteManager: Exception while reading image '{pathToLoad}'. Error={ex.GetType().Name}: {ex.Message}");
            }

            if (content == null || content.Length == 0)
            {
                Debug.LogError($"SpriteManager: Empty content after reading '{pathToLoad}'");
            }
            else
            {
                // Use a safe default format for runtime-loaded images
                TextureFormat format = TextureFormat.RGBA32;

                var extname = uEmuera.Utils.GetSuffix(pathToLoad).ToLower();

                if (extname == "webp")
                {
                    var tex = Texture2DExt.CreateTexture2DFromWebP(content, false, false,
                    out Error err);
                    if (err != Error.Success)
                    {
                        Debug.LogError($"SpriteManager: Failed to decode WEBP '{pathToLoad}'. Error={err}");
                    }
                    else
                    {
                        ti = new TextureInfo(baseimage.filename, tex);
                        texture_dict.Add(baseimage.filename, ti);

                        baseimage.size.Width = tex.width;
                        baseimage.size.Height = tex.height;
                    }
                }
                else
                {
                    var tex = new Texture2D(2, 2, format, false);
                    if (tex.LoadImage(content))
                    {
                        ti = new TextureInfo(baseimage.filename, tex);
                        texture_dict.Add(baseimage.filename, ti);

                        baseimage.size.Width = tex.width;
                        baseimage.size.Height = tex.height;
                    }
                    else
                    {
                        Debug.LogError($"SpriteManager: Failed to load image '{pathToLoad}' (ext={extname})");
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"SpriteManager: File not found '{pathToLoad}' for bitmap '{baseimage.filename}'");
        }
        List<CallbackInfo> list = null;
        if(loading_set.TryGetValue(baseimage.filename, out list))
        {
            var count = list.Count;
            CallbackInfo item = null;
            for(int i=0; i<count; ++i)
            {
                item = list[i];
                item.DoCallback(GetSpriteInfo(ti, item.src));
            }
            list.Clear();
            loading_set.Remove(baseimage.filename);
        }
        yield break;
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
            if(tinfo != null)
            {
                Debug.Log("Unload Texture " + tinfo.imagename);

                tinfo.Dispose();
                texture_dict.Remove(tinfo.imagename);
                tinfo = null;

                GC.Collect();
            }
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
        var iter = texture_dict.Values.GetEnumerator();
        while(iter.MoveNext())
        {
            iter.Current.Dispose();
        }
        texture_dict.Clear();
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
    // Generate mixed-case variants for a filename to improve chances on case-sensitive filesystems.
    static IEnumerable<string> GenerateFilenameCaseVariants(string directory, string file)
    {
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(file))
            yield break;
        var name = Path.GetFileNameWithoutExtension(file);
        var ext = Path.GetExtension(file);
        var extNoDot = ext?.TrimStart('.') ?? string.Empty;

        // Base variants
        yield return Path.Combine(directory, file); // original
        yield return Path.Combine(directory, file.ToLowerInvariant());
        yield return Path.Combine(directory, file.ToUpperInvariant());

        // Name variants
        var capFirst = (name.Length > 0) ? char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant() : name;
        var capWords = name;
        try
        {
            var parts = name.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p.Length > 0)
                    parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant();
            }
            capWords = string.Join("_", parts);
        }
        catch { }

        // Extension variants
        var extLower = string.IsNullOrEmpty(ext) ? ext : ext.ToLowerInvariant();
        var extUpper = string.IsNullOrEmpty(ext) ? ext : ext.ToUpperInvariant();

        // Combine name + extension variants
        if (!string.IsNullOrEmpty(ext))
        {
            yield return Path.Combine(directory, name + extLower);
            yield return Path.Combine(directory, name + extUpper);
            yield return Path.Combine(directory, capFirst + extLower);
            yield return Path.Combine(directory, capFirst + extUpper);
            yield return Path.Combine(directory, capWords + extLower);
            yield return Path.Combine(directory, capWords + extUpper);
        }
        else
        {
            yield return Path.Combine(directory, capFirst);
            yield return Path.Combine(directory, capWords);
        }
    }
}
