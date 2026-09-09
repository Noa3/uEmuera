using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace uEmuera.Runtime
{
    /// <summary>
    /// File-backed per-game storage for EraElectron runtime state.
    ///
    /// Keys are sandboxed to one namespace under persistentDataPath/saves and
    /// writes use a temporary sibling file before replacement.
    /// </summary>
    public sealed class FileGameStorage : IGameStorage
    {
        readonly string _root;

        public string RootPath => _root;

        public FileGameStorage(string saveNamespace, string baseDirectory = null)
        {
            string ns = SanitizeSegment(string.IsNullOrWhiteSpace(saveNamespace)
                ? "unknown-game"
                : saveNamespace);

            string root = string.IsNullOrWhiteSpace(baseDirectory)
                ? Path.Combine(Application.persistentDataPath, "saves")
                : Path.GetFullPath(baseDirectory);

            _root = Path.Combine(root, ns);
            Directory.CreateDirectory(_root);
        }

        public byte[] LoadSlot(string slotKey)
        {
            string path = GetPath(slotKey);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public void SaveSlot(string slotKey, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string path = GetPath(slotKey);
            Directory.CreateDirectory(_root);
            string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");

            try
            {
                File.WriteAllBytes(temp, data);
                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temp, path, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temp, path, true);
                        File.Delete(temp);
                    }
                    catch (IOException)
                    {
                        File.Copy(temp, path, true);
                        File.Delete(temp);
                    }
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch { }
            }
        }

        public bool SlotExists(string slotKey) => File.Exists(GetPath(slotKey));

        public void DeleteSlot(string slotKey)
        {
            string path = GetPath(slotKey);
            if (File.Exists(path))
                File.Delete(path);
        }

        public string[] ListSlots(string prefix)
        {
            if (!Directory.Exists(_root))
                return Array.Empty<string>();

            string normalizedPrefix = string.IsNullOrEmpty(prefix)
                ? ""
                : SanitizeSegment(prefix);

            return Directory.GetFiles(_root, "*.bin", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => x.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        string GetPath(string key)
        {
            string safe = SanitizeSegment(string.IsNullOrWhiteSpace(key) ? "slot" : key);
            return Path.Combine(_root, safe + ".bin");
        }

        internal static string SanitizeSegment(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "_";

            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.'))
                    chars[i] = '_';
            }

            string result = new string(chars).Trim('.');
            return string.IsNullOrEmpty(result) ? "_" : result;
        }
    }
}
