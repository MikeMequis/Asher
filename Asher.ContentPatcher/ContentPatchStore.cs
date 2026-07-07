using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Asher.ContentPatcher
{
    public static class ContentPatchStore
    {
        public const string ConfigFileName = "content.json";
        public const string AssetsFolderName = "assets";
        public const string CurrentFormat = "1.0.0";

        public static string GetPatchesFolder(string gameFolder) =>
            Path.Combine(gameFolder, "Asher", "patches");

        public static string GetConfigPath(string gameFolder) =>
            Path.Combine(GetPatchesFolder(gameFolder), ConfigFileName);

        public static string GetAssetsFolder(string gameFolder) =>
            Path.Combine(GetPatchesFolder(gameFolder), AssetsFolderName);

        public static ContentPatchConfig Load(string gameFolder)
        {
            var configPath = GetConfigPath(gameFolder);
            if (!File.Exists(configPath))
                return new ContentPatchConfig();

            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<ContentPatchConfig>(json);
                return config ?? new ContentPatchConfig();
            }
            catch
            {
                return new ContentPatchConfig();
            }
        }

        public static void Save(string gameFolder, ContentPatchConfig config)
        {
            var patchesFolder = GetPatchesFolder(gameFolder);
            Directory.CreateDirectory(patchesFolder);
            Directory.CreateDirectory(GetAssetsFolder(gameFolder));

            config.Format = CurrentFormat;
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(GetConfigPath(gameFolder), json);
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

        public static IReadOnlyDictionary<string, ContentPatchEntry> BuildReplacementMap(ContentPatchConfig config)
        {
            var map = new Dictionary<string, ContentPatchEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in config.Changes)
            {
                if (!entry.Enabled)
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

        public static string BuildAssetFileName(string target, string sourceExtension)
        {
            var safeTarget = NormalizeTarget(target)
                .Replace('/', '_')
                .Replace('\\', '_');

            var extension = string.IsNullOrWhiteSpace(sourceExtension)
                ? ".png"
                : sourceExtension;

            if (!extension.StartsWith("."))
                extension = "." + extension;

            return safeTarget + extension;
        }

        public static void RemoveOrphanedAsset(string gameFolder, string fromFile)
        {
            if (string.IsNullOrWhiteSpace(fromFile))
                return;

            try
            {
                var assetPath = ResolveAssetPath(gameFolder, fromFile);
                if (File.Exists(assetPath))
                    File.Delete(assetPath);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }
}
