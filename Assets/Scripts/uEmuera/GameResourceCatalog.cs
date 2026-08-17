using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MinorShift.Emuera.GameProc;

namespace uEmuera
{
    /// <summary>
    /// Image-resource entry in the catalog: path + header-probed dimensions.
    /// </summary>
    public sealed class ResourceEntry
    {
        /// <summary>Absolute path on disk.</summary>
        public readonly string FullPath;
        /// <summary>Base filename (case-preserved).</summary>
        public readonly string FileName;
        /// <summary>Root-relative path with forward slashes.</summary>
        public readonly string RelativePath;
        /// <summary>File size in bytes (0 if unknown).</summary>
        public readonly long SizeBytes;
        /// <summary>Last-write UTC (default if unknown).</summary>
        public readonly DateTime LastWriteUtc;
        /// <summary>Width from ImageHeaderProbe (-1 = not yet probed).</summary>
        public int Width;
        /// <summary>Height from ImageHeaderProbe (-1 = not yet probed).</summary>
        public int Height;
        /// <summary>Format detected by the header probe.</summary>
        public ImageHeaderFormat Format;

        public ResourceEntry(string fullPath, string relativePath, long sizeBytes, DateTime lastWriteUtc)
        {
            FullPath    = fullPath;
            FileName    = Path.GetFileName(fullPath);
            RelativePath = relativePath;
            SizeBytes   = sizeBytes;
            LastWriteUtc = lastWriteUtc;
            Width       = -1;
            Height      = -1;
            Format      = ImageHeaderFormat.Unknown;
        }

        /// <summary>True when header dimensions have been read.</summary>
        public bool HasDimensions => Width > 0 && Height > 0;
    }

    /// <summary>
    /// One-per-game authoritative resource index (uEmuera Phase 6 #25–#30).
    ///
    /// <para>Replaces the three independent directory scans that previously ran on
    /// every startup:</para>
    /// <list type="number">
    ///   <item><c>SpriteManager.InitializeFileIndex</c></item>
    ///   <item><c>Utils.ResourcePrepare</c>'s inner file walk</item>
    ///   <item><c>AppContents.AutoDiscoverImagesFromSubdirectories</c></item>
    /// </list>
    ///
    /// <para>Usage: call <see cref="Scan"/> once per game start (before
    /// <c>AppContents.LoadContents</c>). Then use <see cref="TryResolve"/> for fast
    /// O(1) path lookup and <see cref="TryGetDimensions"/> for dimension queries that
    /// never decode a texture.</para>
    ///
    /// <para>Thread-safety: <see cref="Scan"/> must be called on the interpreter
    /// thread. All read methods are lock-free after <see cref="IsReady"/> becomes true.</para>
    /// </summary>
    public sealed class GameResourceCatalog
    {
        // ---- singleton -------------------------------------------------------
        static GameResourceCatalog instance_;

        /// <summary>Current game's catalog, or null if no scan has been done.</summary>
        public static GameResourceCatalog Instance => instance_;

        // ---- state -----------------------------------------------------------
        readonly string rootDir_;
        readonly List<ResourceEntry> entries_;

        // basename (case-insensitive) → first entry that matched
        readonly Dictionary<string, ResourceEntry> byBaseName_;
        // relative path (forward-slash, case-insensitive) → entry
        readonly Dictionary<string, ResourceEntry> byRelPath_;
        // full path (case-insensitive) → entry
        readonly Dictionary<string, ResourceEntry> byFullPath_;

        bool isReady_;

        static readonly string[] ImageExtensions =
            { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

        // ---- construction ----------------------------------------------------
        GameResourceCatalog(string root, int capacity)
        {
            rootDir_    = root;
            entries_    = new List<ResourceEntry>(capacity);
            byBaseName_ = new Dictionary<string, ResourceEntry>(capacity, StringComparer.OrdinalIgnoreCase);
            byRelPath_  = new Dictionary<string, ResourceEntry>(capacity, StringComparer.OrdinalIgnoreCase);
            byFullPath_ = new Dictionary<string, ResourceEntry>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        // ---- public API ------------------------------------------------------

        public bool IsReady   => isReady_;
        public string RootDir => rootDir_;
        public int Count      => byFullPath_.Count;

        /// <summary>All indexed entries (internal — cache serialization).</summary>
        internal IReadOnlyList<ResourceEntry> AllEntries => entries_;

        /// <summary>
        /// Fast O(1) path lookup.  Accepts basename, relative path (any separator),
        /// or full absolute path.  Returns the canonical full path, or null.
        /// </summary>
        public string TryResolve(string query)
        {
            if (string.IsNullOrEmpty(query))
                return null;
            // Try basename first (most common query form from SpriteManager)
            ResourceEntry entry;
            string normalized = query.Replace('\\', '/');
            if (byBaseName_.TryGetValue(normalized, out entry)) return entry.FullPath;
            if (byRelPath_.TryGetValue(normalized, out entry))  return entry.FullPath;
            if (byFullPath_.TryGetValue(normalized, out entry)) return entry.FullPath;
            return null;
        }

        /// <summary>
        /// Returns header-probed dimensions for a resource (by any query form).
        /// Reads the header on first access and caches it — never decodes the full image.
        /// </summary>
        public bool TryGetDimensions(string query, out int width, out int height)
        {
            width = height = 0;
            string full = TryResolve(query);
            if (full == null) return false;
            ResourceEntry entry;
            if (!byFullPath_.TryGetValue(full.Replace('\\', '/'), out entry)) return false;
            if (!entry.HasDimensions)
                ProbeEntry(entry);
            if (!entry.HasDimensions) return false;
            width  = entry.Width;
            height = entry.Height;
            return true;
        }

        /// <summary>
        /// Populates <paramref name="dest"/> with all keys (basename + relative path +
        /// full path) for every indexed file.  Used by SpriteManager to build its
        /// internal <c>file_index_</c> in one pass.
        /// </summary>
        public void ExportFileIndex(Dictionary<string, string> dest)
        {
            if (!isReady_ || dest == null) return;
            foreach (var kv in byBaseName_)
                dest[kv.Key] = kv.Value.FullPath;
            foreach (var kv in byRelPath_)
                if (!dest.ContainsKey(kv.Key))
                    dest[kv.Key] = kv.Value.FullPath;
            foreach (var kv in byFullPath_)
                if (!dest.ContainsKey(kv.Key))
                    dest[kv.Key] = kv.Value.FullPath;
        }

        // ---- builder ---------------------------------------------------------

        /// <summary>
        /// Scans <paramref name="resourceDirectory"/> once and replaces the process-wide
        /// singleton.  Call from the interpreter/main thread before
        /// <c>AppContents.LoadContents</c>.
        /// </summary>
        public static GameResourceCatalog Scan(string resourceDirectory)
        {
            if (string.IsNullOrEmpty(resourceDirectory) || !Directory.Exists(resourceDirectory))
            {
                var empty = new GameResourceCatalog(resourceDirectory ?? "", 0);
                empty.isReady_ = true;
                instance_ = empty;
                return empty;
            }

            // Persistent cache: reuse a validated directory listing + probed header
            // dimensions when possible. Best-effort — misses/failures fall through
            // to a full scan.
            GameResourceCatalog cached;
            if (CatalogCacheStore.TryLoadResourceCatalog(resourceDirectory, out cached))
            {
                instance_ = cached;
                return cached;
            }

            var catalog = new GameResourceCatalog(resourceDirectory, 256);

            try
            {
                foreach (var ext in ImageExtensions)
                {
                    IEnumerable<string> files;
                    try
                    {
                        files = Directory.GetFiles(resourceDirectory, "*" + ext,
                            SearchOption.AllDirectories);
                    }
                    catch
                    {
                        continue;
                    }
                    foreach (var full in files)
                        catalog.AddFile(full, resourceDirectory);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[GameResourceCatalog] Scan error: " + ex.Message);
            }

            catalog.isReady_ = true;
            instance_ = catalog;
            CatalogCacheStore.SaveResourceCatalog(catalog);
            UnityEngine.Debug.Log(string.Format(
                "[GameResourceCatalog] Indexed {0} files under '{1}'",
                catalog.Count, resourceDirectory));
            return catalog;
        }

        /// <summary>Clears the singleton (call on game teardown).</summary>
        public static void Clear()
        {
            instance_ = null;
        }

        /// <summary>
        /// Rebuilds a ready catalog from previously serialized entries (cache load).
        /// Entry order is preserved; header dimensions carried in the entries are kept.
        /// </summary>
        internal static GameResourceCatalog FromEntries(string rootDir, IList<ResourceEntry> entries)
        {
            var catalog = new GameResourceCatalog(rootDir, entries.Count);
            for (int i = 0; i < entries.Count; i++)
                catalog.IndexEntry(entries[i]);
            catalog.isReady_ = true;
            return catalog;
        }

        // ---- internals -------------------------------------------------------

        void AddFile(string fullPath, string root)
        {
            string relPath = BuildRelPath(fullPath, root);
            string normFull = fullPath.Replace('\\', '/');
            if (byFullPath_.ContainsKey(normFull))
                return; // deduplicate

            FileInfo fi;
            try { fi = new FileInfo(fullPath); }
            catch { return; }

            IndexEntry(new ResourceEntry(fullPath, relPath, fi.Length, fi.LastWriteTimeUtc));
        }

        /// <summary>
        /// Registers an entry into the three lookup dictionaries. Order of insertion
        /// is preserved in <see cref="entries_"/> (first registration wins in the
        /// basename index, matching reference behaviour).
        /// </summary>
        void IndexEntry(ResourceEntry entry)
        {
            if (entry == null) return;
            string normFull = entry.FullPath.Replace('\\', '/');
            if (byFullPath_.ContainsKey(normFull))
                return; // deduplicate

            byFullPath_[normFull] = entry;

            string relNorm = entry.RelativePath.Replace('\\', '/');
            if (!byRelPath_.ContainsKey(relNorm))
                byRelPath_[relNorm] = entry;

            string baseName = Path.GetFileName(entry.FullPath);
            if (!byBaseName_.ContainsKey(baseName))
                byBaseName_[baseName] = entry;

            entries_.Add(entry);
        }

        static string BuildRelPath(string full, string root)
        {
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return full.Substring(root.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
            }
            return Path.GetFileName(full);
        }

        static void ProbeEntry(ResourceEntry entry)
        {
            try
            {
                ImageHeaderInfo hdr;
                if (ImageHeaderProbe.TryReadFile(entry.FullPath, out hdr) && hdr.HasValue)
                {
                    entry.Width  = hdr.Width;
                    entry.Height = hdr.Height;
                    entry.Format = hdr.Format;
                }
            }
            catch { /* leave dimensions -1 */ }
        }
    }
}
