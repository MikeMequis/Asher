using Asher.Models;
using Asher.Services.Interfaces;

namespace Asher.Services.Implementations
{
    public class GameFolderService : IGameFolderService
    {
        private static readonly string[] DustFolderNames = { "DustAET", "Dust An Elysian Tail", "Dust: An Elysian Tail" };
        private const string ExeName = "DustAET.exe";
        private const string PatchesFolderName = "patches";

        public GameFolderInfo DetectGameFolder()
        {
            // Try detection methods in order of likelihood
            return TryGetPath(GetSteamDustPath())
                ?? TryGetPath(GetGogDustPath())
                ?? TryGetPath(GetHumbleDustPath())
                ?? TryGetPath(SearchForDustFolder())
                ?? CreateEmptyInfo();
        }



        public void CreatePatchesFolder(string folderPath)
        {
            var patchesPath = Path.Combine(folderPath, PatchesFolderName);
            if (!Directory.Exists(patchesPath))
                Directory.CreateDirectory(patchesPath);
        }

        private GameFolderInfo TryGetPath(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path) ? GetInfo(path) : null;
        }

        public GameFolderInfo GetInfo(string folderPath)
        {
            string exePath = Path.Combine(folderPath, ExeName);
            bool isValid = File.Exists(exePath);
            string version = string.Empty;

            if (isValid)
            {
                try
                {
                    version = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath).FileVersion;
                }
                catch { }
            }

            string patchesFolderPath = Path.Combine(folderPath, PatchesFolderName);

            return new GameFolderInfo
            {
                Path = folderPath,
                Version = version,
                IsValid = isValid,
                Source = "Manual",
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

        private string GetSteamDustPath()
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
                string defaultPath = Path.Combine(steamPath, "steamapps", "common", "DustAET");
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
                        string candidate = Path.Combine(part, "steamapps", "common", "DustAET");
                        if (Directory.Exists(candidate))
                            return candidate;
                    }
                }
            }
            catch { }
            return null;
        }

        private string GetGogDustPath()
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

        private string GetHumbleDustPath()
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
                            && File.Exists(Path.Combine(folder, ExeName)))
                        {
                            return folder;
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
}