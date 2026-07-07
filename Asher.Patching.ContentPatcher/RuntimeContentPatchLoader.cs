using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace Asher.Patching.ContentPatcher
{
    /// <summary>
    /// net472-compatible content.json loader (no netstandard / Newtonsoft dependency).
    /// </summary>
    internal static class RuntimeContentPatchLoader
    {
        public const string ConfigFileName = "content.json";
        public const string AssetsFolderName = "assets";

        public static string GetPatchesFolder(string gameFolder) =>
            Path.Combine(gameFolder, "Asher", "patches");

        public static RuntimeContentPatchConfig Load(string gameFolder)
        {
            var configPath = Path.Combine(GetPatchesFolder(gameFolder), ConfigFileName);
            if (!File.Exists(configPath))
                return new RuntimeContentPatchConfig();

            try
            {
                var json = File.ReadAllText(configPath);
                var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                return serializer.Deserialize<RuntimeContentPatchConfig>(json)
                    ?? new RuntimeContentPatchConfig();
            }
            catch
            {
                return new RuntimeContentPatchConfig();
            }
        }

        public static string ResolveAssetPath(string gameFolder, string fromFile)
        {
            var patchesFolder = GetPatchesFolder(gameFolder);
            var relative = fromFile.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.GetFullPath(Path.Combine(patchesFolder, relative));
        }

        public static string NormalizeTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return string.Empty;

            var normalized = target.Trim()
                .Replace('\\', '/')
                .TrimStart('/');

            if (normalized.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("Content/".Length);

            if (normalized.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(0, normalized.Length - 4);

            return normalized;
        }

        public static IReadOnlyDictionary<string, RuntimeContentPatchEntry> BuildReplacementMap(
            RuntimeContentPatchConfig config)
        {
            var map = new Dictionary<string, RuntimeContentPatchEntry>(StringComparer.OrdinalIgnoreCase);

            if (config?.Changes == null)
                return map;

            foreach (var entry in config.Changes)
            {
                if (entry == null || !entry.Enabled)
                    continue;

                if (!string.Equals(entry.Action, "Load", StringComparison.OrdinalIgnoreCase))
                    continue;

                var target = NormalizeTarget(entry.Target);
                if (string.IsNullOrEmpty(target) || string.IsNullOrWhiteSpace(entry.FromFile))
                    continue;

                map[target] = entry;
            }

            return map;
        }
    }
}
