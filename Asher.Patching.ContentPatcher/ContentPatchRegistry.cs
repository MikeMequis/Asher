using System;
using System.Collections.Generic;
using System.IO;

namespace Asher.Patching.ContentPatcher
{
    internal static class ContentPatchRegistry
    {
        private static readonly object Sync = new object();
        private static string _gameFolder = string.Empty;
        private static IReadOnlyDictionary<string, RuntimeContentPatchEntry> _replacements =
            new Dictionary<string, RuntimeContentPatchEntry>(StringComparer.OrdinalIgnoreCase);

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

                var config = RuntimeContentPatchLoader.Load(_gameFolder);
                _replacements = RuntimeContentPatchLoader.BuildReplacementMap(config);
            }
        }

        public static int ReplacementCount
        {
            get
            {
                lock (Sync)
                    return _replacements.Count;
            }
        }

        public static bool TryGetReplacement(string assetName, out RuntimeContentPatchEntry entry, out string assetPath)
        {
            entry = null;
            assetPath = string.Empty;

            lock (Sync)
            {
                if (string.IsNullOrWhiteSpace(_gameFolder))
                    return false;

                var target = RuntimeContentPatchLoader.NormalizeTarget(assetName);
                if (!_replacements.TryGetValue(target, out entry))
                    return false;

                assetPath = RuntimeContentPatchLoader.ResolveAssetPath(_gameFolder, entry.FromFile);
                return File.Exists(assetPath);
            }
        }
    }
}
