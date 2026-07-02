using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class GameFolderService : IGameFolderService
    {
        private static readonly string[] DustFolderNames = { "DustAET", "Dust An Elysian Tail", "Dust: An Elysian Tail" };
        private const string PatchesFolderName = AsherPaths.PatchesFolderName;

        public GameFolderInfo DetectGameFolder()
        {
            var fromManagerLocation = AsherPaths.TryGetGameFolderFromManagerLocation();
            if (!string.IsNullOrEmpty(fromManagerLocation))
                return GetInfo(fromManagerLocation, "Installed");

            var settings = AsherSettings.Load();
            if (!string.IsNullOrWhiteSpace(settings.GameFolderPath))
            {
                var fromSettings = TryGetPath(settings.GameFolderPath, "Settings");
                if (fromSettings != null)
                    return fromSettings;
            }

            return TryGetPath(GetSteamPath(), "Steam")
                ?? TryGetPath(GetGogPath(), "GOG")
                ?? TryGetPath(GetHumblePath(), "Humble")
                ?? TryGetPath(FindAsherInstalledFolder(), "Installed")
                ?? TryGetPath(SearchForDustFolder(), "Search")
                ?? CreateEmptyInfo();
        }

        public void CreatePatchesFolder(string folderPath)
        {
            var patchesPath = AsherPaths.GetPatchesFolderPath(folderPath);
            if (!Directory.Exists(patchesPath))
                Directory.CreateDirectory(patchesPath);
        }

        private GameFolderInfo TryGetPath(string? path, string source)
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

            string patchesFolderPath = AsherPaths.GetPatchesFolderPath(folderPath);

            return new GameFolderInfo
            {
                Path = folderPath,
                Version = version,
                IsValid = isValid,
                Source = source,
                HasPatchesFolder = Directory.Exists(patchesFolderPath),
                PatchesFolderPath = patchesFolderPath
            };
        }

        private GameFolderInfo CreateEmptyInfo()
        {
            return new GameFolderInfo
            {
                Path = string.Empty,
                Version = string.Empty,
                IsValid = false,
                Source = string.Empty,
                HasPatchesFolder = false,
                PatchesFolderPath = string.Empty
            };
        }

        private string GetSteamPath()
        {
            var steamLocations = new[]
            {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
            @"C:\Steam",
            @"D:\Steam",
            @"E:\Steam"
        };

            foreach (var steamPath in steamLocations.Where(Directory.Exists))
            {
                // Check custom library locations from libraryfolders.vdf
                string libraryFoldersVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(libraryFoldersVdf))
                {
                    var customPath = ParseSteamLibraryFolders(libraryFoldersVdf);
                    if (customPath != null)
                        return customPath;
                }

                // Check default location
                string defaultPath = Path.Combine(steamPath, "steamapps", "common", "Dust An Elysian Tail");
                if (Directory.Exists(defaultPath))
                    return defaultPath;
            }

            return null;
        }

        private string ParseSteamLibraryFolders(string vdfPath)
        {
            try
            {
                foreach (var line in File.ReadAllLines(vdfPath).Where(l => l.Contains("path")))
                {
                    var parts = line.Split('"');
                    foreach (var part in parts.Where(p => p.Contains(":\\")))
                    {
                        string candidate = Path.Combine(part, "steamapps", "common", "Dust An Elysian Tail");
                        if (Directory.Exists(candidate))
                            return candidate;
                    }
                }
            }
            catch { }
            return null;
        }

        private string GetGogPath()
        {
            var gogLocations = new[]
            {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"GOG Galaxy\Games"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GOG Galaxy\Games"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GOG.com\Galaxy\Games"),
            @"C:\GOG Games",
            @"D:\GOG Games"
        };

            return FindGameInLocations(gogLocations);
        }

        private string GetHumblePath()
        {
            var humbleLocations = new[]
            {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Humble Bundle"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Humble Bundle"),
            @"C:\Humble Bundle",
            @"D:\Humble Bundle"
        };

            return FindGameInLocations(humbleLocations);
        }

        private string FindGameInLocations(string[] locations)
        {
            foreach (var location in locations.Where(Directory.Exists))
            {
                foreach (var folder in DustFolderNames)
                {
                    string candidate = Path.Combine(location, folder);
                    if (Directory.Exists(candidate))
                        return candidate;
                }
            }
            return null;
        }

        private string SearchForDustFolder()
        {
            var searchLocations = new[]
            {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"C:\Games",
            @"D:\Games",
            @"E:\Games",
            @"C:\Program Files (x86)\Games",
            @"C:\Program Files\Games"
        };

            foreach (var location in searchLocations.Where(Directory.Exists))
            {
                try
                {
                    foreach (var folder in Directory.GetDirectories(location, "*", SearchOption.AllDirectories))
                    {
                        var folderName = Path.GetFileName(folder);
                        if (DustFolderNames.Any(name => folderName.Equals(name, StringComparison.OrdinalIgnoreCase))
                            && AsherPaths.IsValidGameFolder(folder))
                        {
                            return folder;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private string? FindAsherInstalledFolder()
        {
            foreach (var steamRoot in GetSteamLibraryRoots())
            {
                var commonPath = Path.Combine(steamRoot, "steamapps", "common");
                if (!Directory.Exists(commonPath))
                    continue;

                foreach (var folderName in DustFolderNames)
                {
                    var candidate = Path.Combine(commonPath, folderName);
                    if (AsherPaths.IsAsherInstalledIn(candidate))
                        return candidate;
                }

                var dustFolder = Path.Combine(commonPath, "Dust An Elysian Tail");
                if (AsherPaths.IsAsherInstalledIn(dustFolder))
                    return dustFolder;
            }

            return null;
        }

        private IEnumerable<string> GetSteamLibraryRoots()
        {
            var steamLocations = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                @"C:\Steam",
                @"D:\Steam",
                @"E:\Steam"
            };

            foreach (var steamPath in steamLocations.Where(Directory.Exists))
            {
                yield return steamPath;

                var libraryFoldersVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(libraryFoldersVdf))
                    continue;

                foreach (var line in File.ReadAllLines(libraryFoldersVdf).Where(l => l.Contains("path")))
                {
                    foreach (var part in line.Split('"').Where(p => p.Contains(":\\")))
                        yield return part;
                }
            }
        }
    }
}