using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace uEmuera
{
    /// <summary>
    /// Logging utility class for debugging errors, warnings, and info messages.
    /// Messages are output to the Unity console with timestamps and source information.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="content">The content to log.</param>
        public static void Info(object content)
        {
            if(info == null)
                return;
            info(content);
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="content">The content to log.</param>
        public static void Warn(object content)
        {
            if(warn == null)
                return;
            warn(content);
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="content">The content to log.</param>
        public static void Error(object content)
        {
            if(error == null)
                return;
            error(content);
        }
        
        /// <summary>
        /// Logs an exception with full details including message, type, and stack trace.
        /// Use this for catching exceptions to get better debugging information.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="context">Optional context message describing what was happening when the exception occurred.</param>
        /// <param name="memberName">Automatically captured - the calling member name.</param>
        /// <param name="filePath">Automatically captured - the source file path.</param>
        /// <param name="lineNumber">Automatically captured - the line number.</param>
        public static void Exception(
            Exception ex, 
            string context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if(error == null)
                return;
            
            // Handle null exception gracefully
            if (ex == null)
            {
                string fileName = Path.GetFileName(filePath);
                error($"[{fileName}:{lineNumber} {memberName}()] Null exception logged" +
                      (string.IsNullOrEmpty(context) ? "" : $" [{context}]"));
                return;
            }
                
            string fileNameEx = Path.GetFileName(filePath);
            string location = $"{fileNameEx}:{lineNumber} in {memberName}()";
            string contextInfo = string.IsNullOrEmpty(context) ? "" : $" [{context}]";
            string message = $"Exception{contextInfo} at {location}\n" +
                           $"Type: {ex.GetType().FullName}\n" +
                           $"Message: {ex.Message}\n" +
                           $"Stack Trace:\n{ex.StackTrace}";
            
            // Include full chain of inner exceptions
            Exception inner = ex.InnerException;
            int depth = 1;
            while (inner != null)
            {
                message += $"\n--- Inner Exception (Level {depth}) ---\n" +
                          $"Type: {inner.GetType().FullName}\n" +
                          $"Message: {inner.Message}\n" +
                          $"Stack Trace:\n{inner.StackTrace}";
                inner = inner.InnerException;
                depth++;
                
                // Safety limit to prevent infinite loops in case of circular references
                if (depth > 10) break;
            }
            
            error(message);
        }
        
        /// <summary>
        /// Logs a debug message with caller information.
        /// Useful for tracing code execution flow.
        /// </summary>
        /// <param name="content">The content to log.</param>
        /// <param name="memberName">Automatically captured - the calling member name.</param>
        /// <param name="filePath">Automatically captured - the source file path.</param>
        /// <param name="lineNumber">Automatically captured - the line number.</param>
        public static void Debug(
            object content,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if(info == null)
                return;
                
            string fileName = Path.GetFileName(filePath);
            info($"[{fileName}:{lineNumber} {memberName}()] {content}");
        }
        
        /// <summary>
        /// Action delegate for info logging.
        /// </summary>
        public static System.Action<object> info;
        
        /// <summary>
        /// Action delegate for warning logging.
        /// </summary>
        public static System.Action<object> warn;
        
        /// <summary>
        /// Action delegate for error logging.
        /// </summary>
        public static System.Action<object> error;
    }

    /// <summary>
    /// Utility class for Emuera operations.
    /// </summary>
    public static class Utils
    {
        // Helper for platform detection compatible with .NET Standard 2.1
        static bool IsWindows()
        {
            var p = Environment.OSVersion.Platform;
            return p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows || p == PlatformID.WinCE;
        }

        /// <summary>
        /// Sets the SHIFT-JIS to UTF-8 conversion dictionary.
        /// </summary>
        /// <param name="dict">The conversion dictionary.</param>
        public static void SetSHIFTJIS_to_UTF8Dict(Dictionary<string, string> dict)
        {
            shiftjis_to_utf8 = dict;
        }
        
        /// <summary>
        /// Sets the UTF-8 Chinese to UTF-8 conversion dictionary.
        /// </summary>
        /// <param name="dict">The conversion dictionary.</param>
        public static void SetUTF8ZHCN_to_UTF8Dict(Dictionary<string, string> dict)
        {
            utf8zhcn_to_utf8 = dict;
        }
        
        /// <summary>
        /// Converts SHIFT-JIS text to UTF-8 using the conversion dictionary.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="md5">The MD5 hash of the text.</param>
        /// <returns>The converted text or null if not found.</returns>
        public static string SHIFTJIS_to_UTF8(string text, string md5)
        {
            if(shiftjis_to_utf8 == null)
                return null;
            string result = null;
            shiftjis_to_utf8.TryGetValue(md5, out result);
            if(string.IsNullOrEmpty(result))
                utf8zhcn_to_utf8.TryGetValue(text, out result);
            return result;
        }
        static Dictionary<string, string> shiftjis_to_utf8;
        static Dictionary<string, string> utf8zhcn_to_utf8;

        /// <summary>
        /// Normalizes a file path by splitting on directory separators,
        /// removing empty segments, and joining with forward slashes.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path with forward slashes.</returns>
        public static string NormalizePath(string path)
        {
            var ps = path.Split('/', '\\');
            var n = "";
            for(int i = 0; i < ps.Length - 1; ++i)
            {
                var p = ps[i];
                if(string.IsNullOrEmpty(p))
                    continue;
                n = string.Concat(n, p, '/');
            }
            if(ps.Length == 1)
                return ps[0];
            else if(ps.Length > 0)
                return n + ps[ps.Length - 1];
            return "";
        }

        /// <summary>
        /// Attempts to resolve the given path to the exact-case path on disk.
        /// Returns null if resolution fails.
        /// On Windows, returns the input path because the filesystem is case-insensitive.
        /// </summary>
        public static string ResolvePathInsensitive(string path, bool expectDirectory)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            try
            {
                // Windows is already case-insensitive
                if (IsWindows())
                    return path;

                // Normalize separators
                var normalized = path.Replace('/', Path.DirectorySeparatorChar)
                                     .Replace('\\', Path.DirectorySeparatorChar);

                // Handle absolute vs relative roots
                string root = Path.GetPathRoot(normalized);
                string current;
                if (string.IsNullOrEmpty(root))
                {
                    current = Directory.GetCurrentDirectory();
                }
                else
                {
                    // On Unix, root is "/" - don't trim it to empty string
                    current = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (string.IsNullOrEmpty(current))
                        current = root; // Preserve "/" as the root directory
                }

                // Build parts excluding root
                string relative = string.IsNullOrEmpty(root) ? normalized : normalized.Substring(root.Length);
                var parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    bool last = i == parts.Length - 1;

                    if (!Directory.Exists(current))
                        return null;

                    if (!last || expectDirectory)
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
                        {
                            // If last part and expectDirectory, a missing directory means failure
                            if (last && expectDirectory)
                                return null;
                            // Otherwise, path cannot be resolved
                            return null;
                        }
                        current = matchedDir;
                    }
                    else
                    {
                        // Last segment expected to be a file
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

                // If we reach here, we resolved a directory path
                return current;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns true if a directory exists, performing case-insensitive lookup on case-sensitive systems.
        /// </summary>
        public static bool DirectoryExistsInsensitive(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (Directory.Exists(path))
                return true;
            var resolved = ResolvePathInsensitive(path, expectDirectory: true);
            return !string.IsNullOrEmpty(resolved) && Directory.Exists(resolved);
        }

        /// <summary>
        /// Returns true if a file exists, performing case-insensitive lookup on case-sensitive systems.
        /// </summary>
        public static bool FileExistsInsensitive(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (File.Exists(path))
                return true;

            try
            {
                var normalized = path.Replace('/', Path.DirectorySeparatorChar)
                                      .Replace('\\', Path.DirectorySeparatorChar);
                var dir = Path.GetDirectoryName(normalized);
                var target = Path.GetFileName(normalized);
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(target))
                    return false;
                if (!Directory.Exists(dir))
                    return false;
                var files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < files.Length; i++)
                {
                    var name = Path.GetFileName(files[i]);
                    if (string.Equals(name, target, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Normalize input and resolve existing directory path to actual casing if found.
        /// Falls back to normalized input if resolution fails.
        /// </summary>
        public static string NormalizeExistingDirectoryPath(string path)
        {
            var normalized = NormalizePath(path);
            var resolved = ResolvePathInsensitive(normalized, expectDirectory: true);
            return string.IsNullOrEmpty(resolved) ? normalized : NormalizePath(resolved);
        }

        /// <summary>
        /// Resolves a file path using case-insensitive matching if the exact path doesn't exist.
        /// Returns the resolved path if found, or null if the file doesn't exist.
        /// On Windows (case-insensitive FS), returns the original path if the file exists.
        /// </summary>
        /// <param name="path">The file path to resolve.</param>
        /// <returns>The resolved path if file exists, null otherwise.</returns>
        public static string ResolveExistingFilePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (File.Exists(path))
                return path;
            var resolved = ResolvePathInsensitive(path, expectDirectory: false);
            return string.IsNullOrEmpty(resolved) ? null : resolved;
        }

        /// <summary>
        /// Gets the file extension from a filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>The file extension.</returns>
        public static string GetSuffix(string filename)
        {
            int last_slash = filename.LastIndexOf('.');
            if(last_slash != -1)
                return filename.Substring(last_slash + 1);
            return filename;
        }
        
        /// <summary>
        /// Gets the display length of a string.
        /// </summary>
        /// <param name="s">The string to measure.</param>
        /// <param name="font">The font to use for measurement.</param>
        /// <returns>The display length in pixels.</returns>
        public static int GetDisplayLength(string s, uEmuera.Drawing.Font font)
        {
            return GetDisplayLength(s, font.Size);
        }

        /// <summary>
        /// Set of characters that are full-width.
        /// </summary>
        public static readonly HashSet<char> fullsize = new HashSet<char>
        {
            '´',
        };
        
        /// <summary>
        /// Checks if a character is full-width.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is full-width.</returns>
        public static bool CheckFullSize(char c)
        {
            return fullsize.Contains(c);
        }
        
        /// <summary>
        /// Set of characters that are half-width.
        /// </summary>
        public static readonly HashSet<char> halfsize = new HashSet<char>
        {
            '▀','▁','▂','▃','▄','▅',
            '▆','▇','█','▉','▊','▋',
            '▌','▍','▎','▏','▐','░',
            '▒','▓','▔','▕', '▮',
            '┮', '╮', '◮', '♮', '❮',
            '⟮', '⠮','⡮','⢮', '⣮', '║',
            '▤','▥','▦', '▧', '▨', '▩',
            '▪', '▫','~', '´', 'ﾄ', '｡', '･',
        };
        
        /// <summary>
        /// Checks if a character is half-width.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is half-width.</returns>
        public static bool CheckHalfSize(char c)
        {
            return c < 0x127 || halfsize.Contains(c);
        }
        
        /// <summary>
        /// Gets the display length of a string.
        /// </summary>
        /// <param name="s">The string to measure.</param>
        /// <param name="fontsize">The font size in pixels.</param>
        /// <returns>The display length in pixels.</returns>
        public static int GetDisplayLength(string s, float fontsize)
        {
            float xsize = 0;
            char c = '\x0';
            for(int i = 0; i < s.Length; ++i)
            {
                c = s[i];
                if(CheckHalfSize(c))
                    xsize += fontsize / 2;
                else
                    xsize += fontsize;
            }

            return (int)xsize;
        }

        public static string GetStBar(char c, uEmuera.Drawing.Font font)
        {
            return GetStBar(c, font.Size);
        }

        public static string GetStBar(char c, float fontsize)
        {
            float s = fontsize;
            if(CheckHalfSize(c))
                s /= 2;
            var w = MinorShift.Emuera.Config.DrawableWidth;
            var count = (int)System.Math.Floor(w / s);
            var build = new System.Text.StringBuilder(count);
            for(int i = 0; i < count; ++i)
                build.Append(c);
            return build.ToString();
        }

        public static int GetByteCount(string str)
        {
            if(string.IsNullOrEmpty(str))
                return 0;
            var count = 0;
            var length = str.Length;
            for(int i = 0; i < length; ++i)
            {
                if(CheckHalfSize(str[i]))
                    count += 1;
                else
                    count += 2;
            }
            return count;
        }
        public static List<string> GetFiles(string search, string extension, SearchOption option)
        {
            var files = Directory.GetFiles(search, "*.???", option);
            var filecount = files.Length;
            var result = new List<string>();
            for(int i=0; i<filecount; ++i)
            {
                var file = files[i];
                string ext = Path.GetExtension(file);
                if(string.Compare(ext, extension, true) == 0)
                    result.Add(file);
            }
            return result;
        }
        public static List<string> GetFiles(string search, string[] extensions, SearchOption option)
        {
            var extension_checker = new HashSet<string>();
            for(int i = 0; i < extensions.Length; ++i)
                extension_checker.Add(extensions[i].ToUpper());

            var files = Directory.GetFiles(search, "*.???", option);
            var filecount = files.Length;
            var result = new List<string>();
            for(int i = 0; i < filecount; ++i)
            {
                var file = files[i];
                string ext = Path.GetExtension(file).ToUpper();
                if(extension_checker.Contains(ext))
                    result.Add(file);
            }
            return result;
        }
        public static Dictionary<string, string> GetContentFiles()
        {
            if(content_files != null)
                return content_files;
            content_files = new Dictionary<string, string>();

            var contentdir = MinorShift._Library.Sys.ExeDir + "resources/";
            if(!Directory.Exists(contentdir))
                return content_files;

            List<string> bmpfilelist = new List<string>();
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.png", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.bmp", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.jpg", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.gif", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.webp", SearchOption.TopDirectoryOnly));
#if(UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.PNG", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.BMP", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.JPG", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.GIF", SearchOption.TopDirectoryOnly));
            bmpfilelist.AddRange(Directory.GetFiles(contentdir, "*.WEBP", SearchOption.TopDirectoryOnly));

#endif
            var filecount = bmpfilelist.Count;
            for(int i=0; i<filecount; ++i)
            {
                var filename = bmpfilelist[i];
                string name = Path.GetFileName(filename).ToUpper();
                content_files.Add(name, filename);
            }
            return content_files;
        }
        public static string[] GetResourceCSVLines(
            string csvpath, System.Text.Encoding encoding)
        {
            string[] lines = null;
            if(resource_csv_lines_ != null &&
                resource_csv_lines_.TryGetValue(csvpath, out lines))
                return lines;
            lines = File.ReadAllLines(csvpath, encoding);
            return lines;
        }
        public static void ResourcePrepare()
        {
            var content_files = GetContentFiles();
            if(content_files.Count == 0)
                return;

            var contentdir = MinorShift._Library.Sys.ExeDir + "resources/";
            List<string> csvFiles = new List<string>(Directory.GetFiles(
                contentdir, "*.csv", SearchOption.TopDirectoryOnly));
#if(UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            csvFiles.AddRange(Directory.GetFiles(
                contentdir, "*.CSV", SearchOption.TopDirectoryOnly));
#endif
            resource_csv_lines_ = new Dictionary<string, string[]>();

            var encoder = MinorShift.Emuera.Config.Encode;
            var filecount = csvFiles.Count;
            for(int index=0; index < filecount; ++index)
            {
                var filename = csvFiles[index];
                //SpriteManager.ClearResourceCSVLines(filename);
                string[] lines = SpriteManager.GetResourceCSVLines(filename);
                if(lines != null)
                {
                    resource_csv_lines_.Add(filename, lines);
                    continue;
                }

                List<string> newlines = new List<string>();
                lines = File.ReadAllLines(filename, encoder);
                int fixcount = 0;
                for(int i = 0; i < lines.Length; ++i)
                {
                    var line = lines[i];
                    if(line.Length == 0)
                        continue;
                    string str = line.Trim();
                    if(str.Length == 0 || str.StartsWith(";"))
                        continue;

                    string[] tokens = str.Split(',');
                    if(tokens.Length >= 6)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(tokens[2]) &&
                                !string.IsNullOrEmpty(tokens[3]) &&
                                !string.IsNullOrEmpty(tokens[4]) &&
                                !string.IsNullOrEmpty(tokens[5]))
                            {
                                var w = int.Parse(tokens[4]);
                                var h = int.Parse(tokens[5]);
                                if (w != 0 && h != 0)
                                {
                                    newlines.Add(line);
                                    continue;
                                }
                            }
                        }
                        catch (Exception)
                        {}
                    }
                    if (tokens.Length <= 1)
                        continue;
                    string name = tokens[1].ToUpper();
                    string imagepath = null;
                    content_files.TryGetValue(name, out imagepath);
                    if(imagepath == null)
                        continue;

                    var ti = SpriteManager.GetTextureInfo(name, imagepath);
                    if(ti == null)
                        continue;
                    line = string.Format("{0},{1},0,0,{2},{3}",
                        tokens[0], tokens[1], ti.width, ti.height);
                    newlines.Add(line);
                    fixcount += 1;
                }
                lines = newlines.ToArray();
                resource_csv_lines_.Add(filename, lines);
                if(fixcount > 0)
                    SpriteManager.SetResourceCSVLine(filename, lines);
            }
        }
        public static void ResourcePrepareSimple()
        {
            var content_files = GetContentFiles();
            if(content_files.Count == 0)
                return;

            var contentdir = MinorShift._Library.Sys.ExeDir + "resources/";
            List<string> csvFiles = new List<string>(Directory.GetFiles(
                contentdir, "*.csv", SearchOption.TopDirectoryOnly));
#if(UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            csvFiles.AddRange(Directory.GetFiles(
                contentdir, "*.CSV", SearchOption.TopDirectoryOnly));
#endif
            resource_csv_lines_ = new Dictionary<string, string[]>();

            var encoder = MinorShift.Emuera.Config.Encode;
            var filecount = csvFiles.Count;
            for(int index = 0; index < filecount; ++index)
            {
                var filename = csvFiles[index];
                //SpriteManager.ClearResourceCSVLines(filename);
                string[] lines = SpriteManager.GetResourceCSVLines(filename);
                if(lines != null)
                    resource_csv_lines_.Add(filename, lines);
            }
        }
        public static void ResourceClear()
        {
            if(content_files != null)
            {
                content_files.Clear();
                content_files = null;
            }
            if(resource_csv_lines_ != null)
            {
                resource_csv_lines_.Clear();
                resource_csv_lines_ = null;
            }
        }
        static Dictionary<string, string> content_files = null;
        static Dictionary<string, string[]> resource_csv_lines_ = null;
    }
}