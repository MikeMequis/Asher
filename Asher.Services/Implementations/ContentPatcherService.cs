using Asher.ContentPatcher;
using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Interfaces;
using System.Linq;

namespace Asher.Services.Implementations
{
    public class ContentPatcherService : IContentPatcherService
    {
        private readonly IGameLaunchService _gameLaunchService;

        public ContentPatcherService(IGameLaunchService gameLaunchService)
        {
            _gameLaunchService = gameLaunchService;
        }

        public Task<IReadOnlyList<ContentReplacementInfo>> GetReplacementsAsync()
        {
            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult<IReadOnlyList<ContentReplacementInfo>>(Array.Empty<ContentReplacementInfo>());

            var config = ContentPatchStore.Load(gameFolder);
            var replacements = config.Changes
                .Select(entry => new ContentReplacementInfo
                {
                    Target = ContentPatchStore.NormalizeTarget(entry.Target),
                    FromFile = entry.FromFile,
                    IsEnabled = entry.Enabled
                })
                .OrderBy(r => r.Target, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult<IReadOnlyList<ContentReplacementInfo>>(replacements);
        }

        public Task<bool> AddReplacementAsync(string target, string sourceFilePath)
        {
            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult(false);

            var normalizedTarget = ContentPatchStore.NormalizeTarget(target);
            if (string.IsNullOrWhiteSpace(normalizedTarget))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                return Task.FromResult(false);

            try
            {
                var assetsFolder = ContentPatchStore.GetAssetsFolder(gameFolder);
                Directory.CreateDirectory(assetsFolder);

                var assetFileName = ContentPatchStore.BuildAssetFileName(
                    normalizedTarget,
                    Path.GetExtension(sourceFilePath));

                var destinationPath = Path.Combine(assetsFolder, assetFileName);
                File.Copy(sourceFilePath, destinationPath, overwrite: true);

                var relativeFromFile = $"{ContentPatchStore.AssetsFolderName}/{assetFileName}"
                    .Replace('\\', '/');

                var config = ContentPatchStore.Load(gameFolder);
                var existing = config.Changes.FirstOrDefault(change =>
                    string.Equals(
                        ContentPatchStore.NormalizeTarget(change.Target),
                        normalizedTarget,
                        StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    ContentPatchStore.RemoveOrphanedAsset(gameFolder, existing.FromFile);
                    existing.FromFile = relativeFromFile;
                    existing.Enabled = true;
                    existing.Action = "Load";
                }
                else
                {
                    config.Changes.Add(new ContentPatchEntry
                    {
                        Action = "Load",
                        Target = normalizedTarget,
                        FromFile = relativeFromFile,
                        Enabled = true
                    });
                }

                ContentPatchStore.Save(gameFolder, config);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> RemoveReplacementAsync(string target)
        {
            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult(false);

            var normalizedTarget = ContentPatchStore.NormalizeTarget(target);
            var config = ContentPatchStore.Load(gameFolder);
            var entry = config.Changes.FirstOrDefault(change =>
                string.Equals(
                    ContentPatchStore.NormalizeTarget(change.Target),
                    normalizedTarget,
                    StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return Task.FromResult(false);

            try
            {
                ContentPatchStore.RemoveOrphanedAsset(gameFolder, entry.FromFile);
                config.Changes.Remove(entry);
                ContentPatchStore.Save(gameFolder, config);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> SetReplacementEnabledAsync(string target, bool enabled)
        {
            var gameFolder = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolder))
                return Task.FromResult(false);

            var normalizedTarget = ContentPatchStore.NormalizeTarget(target);
            var config = ContentPatchStore.Load(gameFolder);
            var entry = config.Changes.FirstOrDefault(change =>
                string.Equals(
                    ContentPatchStore.NormalizeTarget(change.Target),
                    normalizedTarget,
                    StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return Task.FromResult(false);

            entry.Enabled = enabled;
            ContentPatchStore.Save(gameFolder, config);
            return Task.FromResult(true);
        }
    }
}
