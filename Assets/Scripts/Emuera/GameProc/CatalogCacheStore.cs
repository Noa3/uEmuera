using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using uEmuera;

namespace MinorShift.Emuera.GameProc
{
    /// <summary>
    /// Persistent binary cache for the two boot-time scans (uEmuera Phase 6 — Fast-boot,
    /// catalog cache). Turns the FunctionCatalog's full ERB read-and-parse pass and the
    /// GameResourceCatalog's header probing into a near-zero-cost load on the second and
    /// subsequent boots.
    ///
    /// <para>Layout (both cache kinds share a header):</para>
    /// <code>
    ///   magic  "UEMCB" (5 bytes)
    ///   byte   version (= 1)
    ///   byte   kind     (0 = FunctionCatalog, 1 = ResourceCatalog)
    ///   ... kind-specific fingerprint + payload ...
    /// </code>
    ///
    /// <para>Validation is fingerprint-based (not content-hashed): for every source file
    /// we store (full path, last-write UTC ticks, length). A cache is reused only when
    /// the file set matches exactly AND every file's length + last-write match. This
    /// catches edits, additions, removals and renames without paying for content reads.
    /// The FunctionCatalog cache additionally embeds the encoding codepage and the
    /// ICFunction flag (both change how names are stored), so toggling either
    /// invalidates it.</para>
    ///
    /// <para>All methods are best-effort: any failure (missing dir, lock, corrupt data,
    /// version bump) returns "no cache" and the caller re-scans. Caching must never
    /// break boot.</para>
    ///
    /// <para>Thread-safety: called only from the interpreter/main thread during
    /// startup (FunctionCatalog.Build / GameResourceCatalog.Scan).</para>
    /// </summary>
    internal static class CatalogCacheStore
    {
        const string Magic      = "UEMCB";
        const byte   Version    = 1;
        const byte   KindFunc   = 0;
        const byte   KindRes    = 1;

        static string CacheDir
        {
            get
            {
                try
                {
                    return Path.Combine(UnityEngine.Application.persistentDataPath, "uEmueraCache");
                }
                catch
                {
                    return null;
                }
            }
        }

        // ================================================================== //
        //  FunctionCatalog                                                    //
        // ================================================================== //

        /// <summary>
        /// Attempts to load a cached FunctionCatalog for <paramref name="erbFiles"/>.
        /// Returns false (and <paramref name="catalog"/> = null) when there is no
        /// cache, it is stale, or it is unreadable — caller should scan+save.
        /// </summary>
        public static bool TryLoadFunctionCatalog(
            IList<KeyValuePair<string, string>> erbFiles,
            Encoding encoding,
            out FunctionCatalog catalog)
        {
            catalog = null;
            string dir = CacheDir;
            if (dir == null || erbFiles == null) return false;

            string file = Path.Combine(dir, "FunctionCatalog-" + CacheKey(erbFiles) + ".bin");
            try
            {
                if (!File.Exists(file)) return false;
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    if (!ReadHeader(br, KindFunc)) return false;
                    if (br.ReadInt32() != encoding.CodePage) return false;
                    if (br.ReadBoolean() != Config.ICFunction) return false;

                    if (!FingerprintsMatch(br, erbFiles)) return false;

                    int recordCount = br.ReadInt32();
                    if (recordCount < 0 || recordCount > 5_000_000) return false;
                    var records = new List<FunctionMetadata>(recordCount);
                    for (int i = 0; i < recordCount; i++)
                    {
                        string name       = br.ReadString();
                        string fileName   = br.ReadString();
                        string filePath   = br.ReadString();
                        int    lineNumber = br.ReadInt32();
                        byte   flags      = br.ReadByte();
                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(filePath))
                            return false;
                        var kind = (FunctionReturnKind)((flags >> 4) & 0x03);
                        records.Add(new FunctionMetadata(
                            name, fileName, filePath, lineNumber, kind,
                            (flags & 0x01) != 0, (flags & 0x02) != 0,
                            (flags & 0x04) != 0, (flags & 0x08) != 0));
                    }
                    catalog = FunctionCatalog.FromMetadata(records);
                    UnityEngine.Debug.Log(string.Format(
                        "[CatalogCacheStore] FunctionCatalog cache hit: {0} functions from {1} files.",
                        records.Count, erbFiles.Count));
                    return true;
                }
            }
            catch
            {
                catalog = null;
                return false;
            }
        }

        /// <summary>
        /// Writes <paramref name="catalog"/> plus the current file fingerprints to
        /// disk. Best-effort; failures are logged once and ignored.
        /// </summary>
        public static void SaveFunctionCatalog(
            FunctionCatalog catalog,
            IList<KeyValuePair<string, string>> erbFiles,
            Encoding encoding)
        {
            string dir = CacheDir;
            if (dir == null || catalog == null || erbFiles == null) return;
            try
            {
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "FunctionCatalog-" + CacheKey(erbFiles) + ".bin");
                string tmp  = file + ".tmp";
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var bw = new BinaryWriter(fs))
                {
                    WriteHeader(bw, KindFunc);
                    bw.Write(encoding.CodePage);
                    bw.Write(Config.ICFunction);
                    WriteFingerprints(bw, erbFiles);
                    bw.Write(catalog.AllOrdered.Count);
                    foreach (var meta in catalog.AllOrdered)
                    {
                        bw.Write(meta.Name);
                        bw.Write(meta.FileName);
                        bw.Write(meta.FilePath);
                        bw.Write(meta.LineNumber);
                        byte flags = (byte)(
                            ((byte)meta.ReturnKind << 4) |
                            (meta.IsPri ? 0x01 : 0) |
                            (meta.IsLater ? 0x02 : 0) |
                            (meta.IsOnly ? 0x04 : 0) |
                            (meta.IsSingle ? 0x08 : 0));
                        bw.Write(flags);
                    }
                }
                ReplaceFile(tmp, file);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[CatalogCacheStore] Could not save FunctionCatalog cache: " + ex.Message);
            }        }

        // ================================================================== //
        //  GameResourceCatalog                                                //
        // ================================================================== //

        /// <summary>
        /// Attempts to load a cached GameResourceCatalog for <paramref name="rootDir"/>.
        /// The directory is re-listed (enumeration only — no content reads) to validate
        /// the fingerprint, so cached header dimensions are preserved across boots.
        /// </summary>
        public static bool TryLoadResourceCatalog(string rootDir, out GameResourceCatalog catalog)
        {
            catalog = null;
            string dir = CacheDir;
            if (dir == null || string.IsNullOrEmpty(rootDir)) return false;

            string file = Path.Combine(dir, "ResourceCatalog-" + CacheKey(rootDir) + ".bin");
            try
            {
                if (!File.Exists(file)) return false;
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var br = new BinaryReader(fs))
                {
                    if (!ReadHeader(br, KindRes)) return false;
                    if (br.ReadString() != rootDir) return false;
                    if (br.ReadInt32() != CacheKindMarker) return false; // reserved

                    int count = br.ReadInt32();
                    if (count < 0 || count > 5_000_000) return false;
                    var entries = new List<ResourceEntry>(count);
                    for (int i = 0; i < count; i++)
                    {
                        string fullPath     = br.ReadString();
                        string relativePath = br.ReadString();
                        long   size         = br.ReadInt64();
                        long   ticks        = br.ReadInt64();
                        int    width        = br.ReadInt32();
                        int    height       = br.ReadInt32();
                        var    format       = (ImageHeaderFormat)br.ReadInt32();
                        var    entry        = new ResourceEntry(fullPath, relativePath, size, new DateTime(ticks));
                        entry.Width  = width;
                        entry.Height = height;
                        entry.Format = format;
                        entries.Add(entry);
                    }

                    // Validate against the current directory listing (enumeration only).
                    if (!ResourceFingerprintsMatch(rootDir, entries))
                        return false;

                    catalog = GameResourceCatalog.FromEntries(rootDir, entries);
                    UnityEngine.Debug.Log(string.Format(
                        "[CatalogCacheStore] ResourceCatalog cache hit: {0} files under '{1}'.",
                        entries.Count, rootDir));
                    return true;
                }
            }
            catch
            {
                catalog = null;
                return false;
            }
        }

        /// <summary>
        /// Writes the resource catalog (including any header dimensions already probed)
        /// to disk. Best-effort; failures are ignored.
        /// </summary>
        public static void SaveResourceCatalog(GameResourceCatalog catalog)
        {
            string dir = CacheDir;
            if (dir == null || catalog == null || !catalog.IsReady) return;
            var entries = catalog.AllEntries;
            if (entries == null) return;
            try
            {
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "ResourceCatalog-" + CacheKey(catalog.RootDir) + ".bin");
                string tmp  = file + ".tmp";
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var bw = new BinaryWriter(fs))
                {
                    WriteHeader(bw, KindRes);
                    bw.Write(catalog.RootDir);
                    bw.Write(CacheKindMarker);
                    bw.Write(entries.Count);
                    foreach (var entry in entries)
                    {
                        bw.Write(entry.FullPath);
                        bw.Write(entry.RelativePath);
                        bw.Write(entry.SizeBytes);
                        bw.Write(entry.LastWriteUtc.Ticks);
                        bw.Write(entry.Width);
                        bw.Write(entry.Height);
                        bw.Write((int)entry.Format);
                    }
                }
                ReplaceFile(tmp, file);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[CatalogCacheStore] Could not save ResourceCatalog cache: " + ex.Message);
            }
        }

        // ================================================================== //
        //  Shared IO                                                          //
        // ================================================================== //

        const int CacheKindMarker = 0x4D4F5247; // "GROM" — kind-1 reserved marker

        /// <summary>Atomic-ish replace: delete the target if present, then move.</summary>
        static void ReplaceFile(string tmp, string file)
        {
            if (File.Exists(file)) File.Delete(file);
            File.Move(tmp, file);
        }

        static bool ReadHeader(BinaryReader br, byte expectedKind)
        {
            if (br.ReadChar() != 'U' || br.ReadChar() != 'E' || br.ReadChar() != 'M' ||
                br.ReadChar() != 'C' || br.ReadChar() != 'B')
                return false;
            byte version = br.ReadByte();
            if (version != Version) return false;
            byte kind = br.ReadByte();
            if (kind != expectedKind) return false;
            return true;
        }

        static void WriteHeader(BinaryWriter bw, byte kind)
        {
            bw.Write(Magic.ToCharArray());
            bw.Write(Version);
            bw.Write(kind);
        }

        static string CacheKey(IList<KeyValuePair<string, string>> erbFiles)
        {
            var text = new StringBuilder();
            text.Append(erbFiles.Count).Append('\n');
            for (int i = 0; i < erbFiles.Count; i++)
                text.Append(erbFiles[i].Key).Append('\0').Append(erbFiles[i].Value).Append('\n');
            return CacheKey(text.ToString());
        }

        static string CacheKey(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 16);
            }
        }

        // ---- fingerprints -------------------------------------------------

        static bool FingerprintsMatch(
            BinaryReader br,
            IList<KeyValuePair<string, string>> erbFiles)
        {
            int n = br.ReadInt32();
            if (n != erbFiles.Count) return false;
            for (int i = 0; i < n; i++)
            {
                string path  = br.ReadString();
                long   ticks = br.ReadInt64();
                long   len   = br.ReadInt64();
                if (!string.Equals(path, erbFiles[i].Value, StringComparison.OrdinalIgnoreCase))
                    return false;
                FileInfo fi;
                try { fi = new FileInfo(path); }
                catch { return false; }
                if (!fi.Exists) return false;
                if (fi.Length != len || fi.LastWriteTimeUtc.Ticks != ticks)
                    return false;
            }
            return true;
        }

        static void WriteFingerprints(
            BinaryWriter bw,
            IList<KeyValuePair<string, string>> erbFiles)
        {
            bw.Write(erbFiles.Count);
            for (int i = 0; i < erbFiles.Count; i++)
            {
                bw.Write(erbFiles[i].Value);
                FileInfo fi;
                try { fi = new FileInfo(erbFiles[i].Value); }
                catch { fi = null; }
                if (fi != null && fi.Exists)
                {
                    bw.Write(fi.LastWriteTimeUtc.Ticks);
                    bw.Write(fi.Length);
                }
                else
                {
                    bw.Write(0L);
                    bw.Write(0L);
                }
            }
        }

        /// <summary>
        /// Re-enumerates <paramref name="rootDir"/> (enumeration only, no content
        /// reads) and requires an exact match against the cached entries. Any mismatch
        /// (add/remove/edit/rename) invalidates the cache.
        /// </summary>
        static bool ResourceFingerprintsMatch(string rootDir, List<ResourceEntry> cached)
        {
            if (!Directory.Exists(rootDir)) return false;
            var current = new List<ResourceEntry>();
            try
            {
                foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" })
                {
                    IEnumerable<string> files;
                    try { files = Directory.GetFiles(rootDir, "*" + ext, SearchOption.AllDirectories); }
                    catch { continue; }
                    foreach (var full in files)
                    {
                        FileInfo fi;
                        try { fi = new FileInfo(full); }
                        catch { continue; }
                        if (!fi.Exists) continue;
                        current.Add(new ResourceEntry(full, "", fi.Length, fi.LastWriteTimeUtc));
                    }
                }
            }
            catch
            {
                return false;
            }

            if (current.Count != cached.Count) return false;
            var byPath = new Dictionary<string, ResourceEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in current)
                byPath[e.FullPath] = e;
            for (int i = 0; i < cached.Count; i++)
            {
                ResourceEntry c;
                if (!byPath.TryGetValue(cached[i].FullPath, out c)) return false;
                if (c.SizeBytes != cached[i].SizeBytes ||
                    c.LastWriteUtc.Ticks != cached[i].LastWriteUtc.Ticks)
                    return false;
            }
            return true;
        }
    }
}
