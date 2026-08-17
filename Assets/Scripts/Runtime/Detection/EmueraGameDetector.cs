using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace uEmuera.Runtime.Detection
{
    /// <summary>
    /// Detects Emuera game packages.
    /// A directory is treated as an Emuera game when it contains
    /// an emuera.config file, an ERB/ sub-directory, or both.
    /// </summary>
    public sealed class EmueraGameDetector : IGameDetector
    {
        public RuntimeKind Kind => RuntimeKind.Emuera;

        public DetectionResult TryDetect(
            string directory,
            IReadOnlyList<string> topLevelFiles)
        {
            var evidence = new List<string>();
            var warnings = new List<string>();

            bool hasConfig = false;
            bool hasErbDir = false;

            // Check top-level files for emuera.config (case-insensitive)
            foreach (var f in topLevelFiles)
            {
                if (string.Equals(Path.GetFileName(f), "emuera.config",
                    StringComparison.OrdinalIgnoreCase))
                {
                    hasConfig = true;
                    evidence.Add("emuera.config present");
                    break;
                }
            }

            // Check for ERB/ sub-directory
            try
            {
                foreach (var d in Directory.GetDirectories(
                    directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (string.Equals(Path.GetFileName(d), "ERB",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        hasErbDir = true;
                        evidence.Add("ERB/ directory present");
                        break;
                    }
                }
            }
            catch { /* access denied */ }

            if (!hasConfig && !hasErbDir)
                return null;

            DetectionConfidence confidence;
            if (hasConfig && hasErbDir) confidence = DetectionConfidence.Certain;
            else if (hasConfig)         confidence = DetectionConfidence.High;
            else                        confidence = DetectionConfidence.Medium;

            if (!hasConfig)
                warnings.Add("No emuera.config found; engine defaults will be used.");

            return new DetectionResult
            {
                Confidence = confidence,
                Evidence   = evidence,
                Warnings   = warnings,
            };
        }

        public GameDescriptor BuildDescriptor(string directory, DetectionResult result)
        {
            string folderName = Path.GetFileName(
                directory.TrimEnd(Path.DirectorySeparatorChar,
                                  Path.AltDirectorySeparatorChar));

            string configRel = null;
            try
            {
                foreach (var f in Directory.GetFiles(
                    directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (string.Equals(Path.GetFileName(f), "emuera.config",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        configRel = Path.GetFileName(f);
                        break;
                    }
                }
            }
            catch { }

            string gameId = MakeId("emu-", directory);

            return new GameDescriptor
            {
                GameId            = gameId,
                Title             = folderName,
                Version           = string.Empty,
                RuntimeKind       = RuntimeKind.Emuera,
                GameRoot          = directory,
                EntryPoint        = null,
                ConfigurationPath = configRel ?? "emuera.config",
                Language          = "ja",
                SaveNamespace     = gameId,
                DetectionResult   = result,
            };
        }

        internal static string MakeId(string prefix, string absolutePath)
        {
            string norm = Path.GetFullPath(absolutePath).ToUpperInvariant();
            using (var sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(norm));
                return prefix +
                    BitConverter.ToString(h).Replace("-", "")
                    .Substring(0, 16).ToLowerInvariant();
            }
        }
    }
}
