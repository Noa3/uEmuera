using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MinorShift.Emuera.Sub
{
    /// <summary>
    /// Single encoding policy for ERB/ERH/CSV text. UTF-8 is preferred when the bytes are valid;
    /// legacy ERA files without a BOM fall back to CP932.
    /// </summary>
    public static class EraEncoding
    {
        static readonly Encoding utf8 = new UTF8Encoding(false, true);
        static readonly Encoding utf8Bom = new UTF8Encoding(true, false);
        static readonly Encoding cp932 = CreateCp932();

        public static Encoding Utf8 { get { return utf8Bom; } }
        public static Encoding Cp932 { get { return cp932; } }

        static Encoding CreateCp932()
        {
            try
            {
                Type providerType = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages")
                    ?? Type.GetType("System.Text.CodePagesEncodingProvider");
                if (providerType != null)
                {
                    var instance = providerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null) as EncodingProvider;
                    if (instance != null) Encoding.RegisterProvider(instance);
                }
            }
            catch { }
            try { return Encoding.GetEncoding(932); }
            catch (NotSupportedException) { return new UTF8Encoding(false, false); }
        }

        public static Encoding Detect(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            byte[] bytes = File.ReadAllBytes(path);
            LoadedFileTracker.Track(path);
            return DetectBytes(bytes);
        }

        public static Encoding DetectBytes(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return utf8Bom;
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode;
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode;
            try
            {
                utf8.GetString(bytes);
                return utf8;
            }
            catch (DecoderFallbackException)
            {
                return cp932;
            }
        }

        public static string ReadText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            LoadedFileTracker.Track(path);
            return DetectBytes(bytes).GetString(bytes);
        }
    }

    /// <summary>
    /// Rooted, deterministic file access for game content. Virtual paths cannot escape the game root.
    /// The case-insensitive index is built once per instance and can be refreshed after a game
    /// installation changes. Ambiguous names are rejected instead of selecting an arbitrary file.
    /// </summary>
    public sealed class GameVirtualFileSystem
    {
        sealed class Index
        {
            public readonly Dictionary<string, string> Files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        static readonly object cacheLock = new object();
        static readonly Dictionary<string, Index> cache = new Dictionary<string, Index>(StringComparer.OrdinalIgnoreCase);
        readonly string root;
        Index index;
        Dictionary<string, string> files { get { return index.Files; } }
        HashSet<string> ambiguous { get { return index.Ambiguous; } }

        public GameVirtualFileSystem(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Game root is required", "rootPath");
            root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            lock (cacheLock)
            {
                if (!cache.TryGetValue(root, out index))
                {
                    index = BuildIndex(root);
                    cache[root] = index;
                }
            }
        }

        public string Root { get { return root; } }
        public IReadOnlyCollection<string> AmbiguousPaths { get { return ambiguous; } }

        public void Refresh()
        {
            Index rebuilt = BuildIndex(root);
            lock (cacheLock)
            {
                index = rebuilt;
                cache[root] = rebuilt;
            }
        }

        static Index BuildIndex(string rootPath)
        {
            Index result = new Index();
            if (!Directory.Exists(rootPath))
                return result;
            try
            {
                foreach (string path in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(rootPath, path).Replace(Path.DirectorySeparatorChar, '/');
                    string key = NormalizeKey(relative);
                    if (result.Files.ContainsKey(key))
                    {
                        result.Ambiguous.Add(key);
                        continue;
                    }
                    result.Files[key] = path;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return result;
        }

        public string CanonicalRelativePath(string virtualPath)
        {
            string full;
            if (!TryResolveCandidate(virtualPath, out full))
                return null;
            return CanonicalRelativePathFromFull(full);
        }

        public bool TryResolve(string virtualPath, out string fullPath)
        {
            fullPath = null;
            string candidate;
            if (!TryResolveCandidate(virtualPath, out candidate))
                return false;
            string relative = CanonicalRelativePathFromFull(candidate);
            string key = NormalizeKey(relative);
            if (ambiguous.Contains(key))
                return false;
            string indexed;
            if (files.TryGetValue(key, out indexed))
            {
                fullPath = indexed;
                return true;
            }
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                fullPath = candidate;
                return true;
            }
            fullPath = candidate;
            return true;
        }

        bool TryResolveCandidate(string virtualPath, out string candidate)
        {
            candidate = null;
            if (virtualPath == null)
                return false;
            try
            {
                if (virtualPath.Length == 0)
                    candidate = root;
                else if (Path.IsPathRooted(virtualPath))
                    candidate = Path.GetFullPath(virtualPath);
                else
                    candidate = Path.GetFullPath(Path.Combine(root, virtualPath.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is NotSupportedException)
            {
                return false;
            }
            return IsInsideRoot(candidate);
        }

        bool IsInsideRoot(string candidate)
        {
            return string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase)
                || candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || candidate.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        string CanonicalRelativePathFromFull(string fullPath)
        {
            string relative = Path.GetRelativePath(root, fullPath);
            return relative == "." ? string.Empty : relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        static string NormalizeKey(string relative)
        {
            return (relative ?? string.Empty).Replace('\\', '/').TrimStart('/').ToUpperInvariant();
        }

        public bool Exists(string virtualPath)
        {
            string path;
            return TryResolve(virtualPath, out path) && File.Exists(path) && !ambiguous.Contains(NormalizeKey(CanonicalRelativePathFromFull(path)));
        }

        public byte[] ReadBytes(string virtualPath)
        {
            string path;
            if (!TryResolve(virtualPath, out path) || !File.Exists(path))
                throw new IOException("File does not exist inside game root: " + virtualPath);
            byte[] bytes = File.ReadAllBytes(path);
            LoadedFileTracker.Track(path);
            return bytes;
        }

        public string ReadText(string virtualPath)
        {
            byte[] bytes = ReadBytes(virtualPath);
            return EraEncoding.DetectBytes(bytes).GetString(bytes);
        }

        public void WriteSaveData(string virtualPath, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            string path;
            if (!TryResolve(virtualPath, out path))
                throw new IOException("Path escapes game root: " + virtualPath);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, data);
            Refresh();
        }

        public string[] EnumerateFiles(string virtualDirectory, string searchPattern, bool recursive)
        {
            string directory;
            if (!TryResolve(virtualDirectory ?? string.Empty, out directory))
                return new string[0];
            string relativeDirectory = CanonicalRelativePathFromFull(directory).TrimEnd('/');
            string pattern = string.IsNullOrEmpty(searchPattern) ? "*" : searchPattern;
            Regex matcher = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var result = new List<string>();
            foreach (KeyValuePair<string, string> entry in files)
            {
                if (ambiguous.Contains(entry.Key))
                    continue;
                string relative = CanonicalRelativePathFromFull(entry.Value);
                string parent = Path.GetDirectoryName(relative.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
                parent = parent.Replace(Path.DirectorySeparatorChar, '/');
                bool inDirectory = string.Equals(parent, relativeDirectory, StringComparison.OrdinalIgnoreCase) ||
                    (recursive && parent.StartsWith(relativeDirectory + "/", StringComparison.OrdinalIgnoreCase));
                if (!inDirectory)
                    continue;
                string fileName = Path.GetFileName(relative);
                if (matcher.IsMatch(fileName))
                    result.Add(relative);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.ToArray();
        }
    }
}
