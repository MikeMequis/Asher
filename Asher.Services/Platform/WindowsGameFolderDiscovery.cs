using Asher.Core;
using Asher.Services.Interfaces;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Windows discovery: fixed Steam/GOG/Humble roots, libraryfolders.vdf parsing,
    /// an Asher-installed scan, then a bounded brute-force search.
    /// Moved verbatim from the previous GameFolderService implementation.
    /// </summary>
    public sealed class WindowsGameFolderDiscovery : IGameFolderDiscovery
    {
        private static readonly string[] DustFolderNames =
        {
            "DustAET", "Dust An Elysian Tail", "Dust: An Elysian Tail"
        };

        public IEnumerable<GameFolderCandidate> EnumerateGameFolderCandidates()
        {
            yield return FromPath(GetSteamPath(), "Steam");
            yield return FromPath(GetGogPath(), "GOG");
            yield return FromPath(GetHumblePath(), "Humble");
            yield return FromPath(FindAsherInstalledFolder(), "Installed");
            yield return FromPath(SearchForDustFolder(), "Search");
        }

        private static GameFolderCandidate FromPath(string? path, string source) =>
            new(path ?? string.Empty, source);

        private static string? GetSteamPath()
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
                string libraryFoldersVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(libraryFoldersVdf))
                {
                    var customPath = ParseSteamLibraryFolders(libraryFoldersVdf);
                    if (customPath != null)
                        return customPath;
                }

                string defaultPath = Path.Combine(steamPath, "steamapps", "common", "Dust An Elysian Tail");
                if (Directory.Exists(defaultPath))
                    return defaultPath;
            }

            return null;
        }

        private static string? ParseSteamLibraryFolders(string vdfPath)
        {
            foreach (var part in SteamLibraryVdf.ReadLibraryPaths(vdfPath))
            {
                if (!part.Contains(":\\", StringComparison.Ordinal))
                    continue;

                string candidate = Path.Combine(part, "steamapps", "common", "Dust An Elysian Tail");
                if (Directory.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string? GetGogPath()
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

        private static string? GetHumblePath()
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

        private static string? FindGameInLocations(string[] locations)
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

        private static string? SearchForDustFolder()
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

        private static string? FindAsherInstalledFolder()
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

        private static IEnumerable<string> GetSteamLibraryRoots()
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

                foreach (var part in SteamLibraryVdf.ReadLibraryPaths(libraryFoldersVdf))
                {
                    if (part.Contains(":\\", StringComparison.Ordinal))
                        yield return part;
                }
            }
        }
    }
}
