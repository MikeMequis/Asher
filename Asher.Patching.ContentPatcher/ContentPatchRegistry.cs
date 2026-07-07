using Asher.ContentPatcher;
using System;
using System.Collections.Generic;
using System.IO;

namespace Asher.Patching.ContentPatcher
{
    internal static class ContentPatchRegistry
    {
        private static readonly object Sync = new object();
        private static string _gameFolder = string.Empty;
        private static IReadOnlyDictionary<string, ContentPatchEntry> _replacements =
            new Dictionary<string, ContentPatchEntry>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize(string gameFolder)
        {
            lock (Sync)
            {
                _gameFolder = gameFolder;
                Reload();
            }
        }

        public static void Reload()
        {
            lock (Sync)
            {
                if (string.IsNullOrWhiteSpace(_gameFolder))
                    return;

                var config = ContentPatchStore.Load(_gameFolder);
                _replacements = ContentPatchStore.BuildReplacementMap(config);
            }
        }

        public static bool TryGetReplacement(string assetName, out ContentPatchEntry entry, out string assetPath)
        {
            entry = null;
            assetPath = string.Empty;

            lock (Sync)
            {
                if (string.IsNullOrWhiteSpace(_gameFolder))
                    return false;

                var target = ContentPatchStore.NormalizeTarget(assetName);
                if (!_replacements.TryGetValue(target, out entry))
                    return false;

                assetPath = ContentPatchStore.ResolveAssetPath(_gameFolder, entry.FromFile);
                return File.Exists(assetPath);
            }
        }
    }
}
