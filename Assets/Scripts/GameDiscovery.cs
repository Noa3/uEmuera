using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Finds ERA game roots without recursively scanning arbitrary user storage.
/// A game root is either a directory containing emuera.config or an ERB directory.
/// The direct root and one-level children of root/game are supported deliberately:
/// this is enough for packaged desktop builds while avoiding accidental deep scans.
/// </summary>
public static class GameDiscovery
{
    public static IReadOnlyList<string> Discover(string root)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(root))
            return result;

        var normalizedRoot = Normalize(root);
        AddIfGame(normalizedRoot, result, seen);

        foreach (var child in EnumerateDirectories(normalizedRoot))
        {
            if (IsDirectoryNamed(child, "game"))
            {
                AddIfGame(child, result, seen);
                foreach (var gameChild in EnumerateDirectories(child))
                    AddIfGame(gameChild, result, seen);
                continue;
            }

            AddIfGame(child, result, seen);
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public static bool IsGameDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        return HasFile(path, "emuera.config") || HasDirectory(path, "ERB");
    }

    public static string FindSingle(string root)
    {
        var games = Discover(root);
        return games.Count == 1 ? games[0] : null;
    }

    static void AddIfGame(string path, List<string> result, HashSet<string> seen)
    {
        if (!IsGameDirectory(path))
            return;

        var normalized = Normalize(path);
        if (seen.Add(normalized))
            result.Add(normalized);
    }

    static IEnumerable<string> EnumerateDirectories(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        string[] directories;
        try
        {
            directories = Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        for (var i = 0; i < directories.Length; i++)
            yield return Normalize(directories[i]);
    }

    static bool HasFile(string directory, string fileName)
    {
        try
        {
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return false;
    }

    static bool HasDirectory(string directory, string directoryName)
    {
        try
        {
            foreach (var child in Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(Path.GetFileName(child), directoryName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return false;
    }

    static bool IsDirectoryNamed(string path, string name)
    {
        return string.Equals(Path.GetFileName(path), name, StringComparison.OrdinalIgnoreCase);
    }

    static string Normalize(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (ArgumentException)
        {
            return path;
        }
        catch (NotSupportedException)
        {
            return path;
        }
    }
}
