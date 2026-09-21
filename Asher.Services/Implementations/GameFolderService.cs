using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class GameFolderService : IGameFolderService
    {
        private readonly IGameFolderDiscovery _discovery;

        public GameFolderService(IGameFolderDiscovery discovery)
        {
            _discovery = discovery;
        }

        public GameFolderInfo DetectGameFolder()
        {
            var settings = AsherSettings.Load();
            if (!string.IsNullOrWhiteSpace(settings.GameFolderPath))
            {
                var fromSettings = TryGetPath(settings.GameFolderPath, "Settings");
                if (fromSettings != null)
                    return fromSettings;
            }

            foreach (var candidate in _discovery.EnumerateGameFolderCandidates())
            {
                var info = TryGetPath(candidate.Path, candidate.Source);
                if (info != null)
                    return info;
            }

            return CreateEmptyInfo();
        }

        private GameFolderInfo? TryGetPath(string? path, string source)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path) ? GetInfo(path, source) : null;
        }

        public GameFolderInfo GetInfo(string folderPath)
        {
            return GetInfo(folderPath, "Manual");
        }

        private GameFolderInfo GetInfo(string folderPath, string source)
        {
            string exePath = Path.Combine(folderPath, AsherPaths.GameExecutableName);
            bool isValid = AsherPaths.IsValidGameFolder(folderPath);
            string version = string.Empty;

            if (File.Exists(exePath))
            {
                try
                {
                    version = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath).FileVersion ?? string.Empty;
                }
                catch { }
            }

            return new GameFolderInfo
            {
                Path = folderPath,
                Version = version,
                IsValid = isValid,
                Source = source
            };
        }

        private GameFolderInfo CreateEmptyInfo()
        {
            return new GameFolderInfo
            {
                Path = string.Empty,
                Version = string.Empty,
                IsValid = false,
                Source = string.Empty
            };
        }
    }
}
