using Asher.Core;
using Asher.Core.Platform;
using Asher.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Linux game discovery. Steam roots follow XDG conventions and libraryfolders.vdf is
    /// parsed for additional libraries. Heroic and Lutris are best-effort: only paths found
    /// in their machine-readable configuration are used, and every candidate is verified to
    /// exist. Manual folder selection is handled by the caller and needs no discovery here.
    /// </summary>
    public sealed class LinuxGameFolderDiscovery : IGameFolderDiscovery
    {
        private static readonly Regex HeroicInstallPathRegex = new(
            "\"[^\"]*[Ii]nstall[^\"]*[Pp]ath[^\"]*\"\\s*:\\s*\"([^\"]+)\"",
            RegexOptions.Compiled);

        private readonly IPlatformInfo _platform;
        private readonly Func<string, string?> _getEnvironmentVariable;

        public LinuxGameFolderDiscovery(IPlatformInfo platform)
            : this(platform, Environment.GetEnvironmentVariable)
        {
        }

        public LinuxGameFolderDiscovery(
            IPlatformInfo platform,
            Func<string, string?> getEnvironmentVariable)
        {
            _platform = platform;
            _getEnvironmentVariable = getEnvironmentVariable;
        }

        public IEnumerable<GameFolderCandidate> EnumerateGameFolderCandidates()
        {
            yield return new GameFolderCandidate(FindInSteam() ?? string.Empty, "Steam");
            yield return new GameFolderCandidate(FindInHeroic() ?? string.Empty, "Heroic");
            yield return new GameFolderCandidate(FindInLutris() ?? string.Empty, "Lutris");
            yield return new GameFolderCandidate(FindInstalled() ?? string.Empty, "Installed");
            yield return new GameFolderCandidate(FindInHome() ?? string.Empty, "Home");
        }

        private string? FindInSteam()
        {
            foreach (var common in EnumerateSteamCommonFolders())
            {
                var found = FindGameFolder(common);
                if (found != null)
                    return found;
            }

            return null;
        }

        private string? FindInstalled()
        {
            foreach (var common in EnumerateSteamCommonFolders())
            {
                if (!Directory.Exists(common))
                    continue;

                foreach (var name in GameFolderNames())
                {
                    var candidate = Path.Combine(common, name);
                    if (AsherPaths.IsAsherInstalledIn(candidate, _platform))
                        return candidate;
                }
            }

            return null;
        }

        private string? FindInHome()
        {
            var home = GetEnvironment("HOME");
            if (string.IsNullOrWhiteSpace(home) || !Directory.Exists(home))
                return null;

            foreach (var root in new[] { home, Path.Combine(home, "Games"), Path.Combine(home, "GOG Games") })
            {
                var found = FindGameFolder(root);
                if (found != null)
                    return found;
            }

            return null;
        }

        private string? FindInHeroic()
        {
            foreach (var configRoot in EnumerateConfigRoots())
            {
                var configPath = Path.Combine(configRoot, "heroic", "config.json");
                if (!File.Exists(configPath))
                    continue;

                foreach (var installPath in ReadHeroicInstallPaths(configPath))
                {
                    foreach (var candidate in CandidateGameFolders(installPath))
                    {
                        if (AsherPaths.IsValidGameFolder(candidate, _platform))
                            return candidate;
                    }
                }
            }

            return null;
        }

        private string? FindInLutris()
        {
            foreach (var configRoot in EnumerateConfigRoots())
            {
                var gamesDir = Path.Combine(configRoot, "lutris", "games");
                if (!Directory.Exists(gamesDir))
                    continue;

                string[] files;
                try
                {
                    files = Directory.GetFiles(gamesDir, "*.yml", SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    foreach (var basePath in ReadLutrisBasePaths(file))
                    {
                        foreach (var candidate in CandidateGameFolders(basePath))
                        {
                            if (AsherPaths.IsValidGameFolder(candidate, _platform))
                                return candidate;
                        }
                    }
                }
            }

            return null;
        }

        private IEnumerable<string> EnumerateSteamCommonFolders()
        {
            foreach (var root in EnumerateSteamRoots())
            {
                if (!Directory.Exists(root))
                    continue;

                yield return Path.Combine(root, "steamapps", "common");

                var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf))
                    continue;

                foreach (var library in SteamLibraryVdf.ReadLibraryPaths(vdf))
                {
                    if (!Path.IsPathRooted(library))
                        continue;

                    yield return Path.Combine(library, "steamapps", "common");
                }
            }
        }

        private IEnumerable<string> EnumerateSteamRoots()
        {
            var xdgDataHome = GetEnvironment("XDG_DATA_HOME");
            var home = GetEnvironment("HOME");
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var root in BuildSteamRoots(xdgDataHome, home))
            {
                if (!string.IsNullOrWhiteSpace(root) && seen.Add(root))
                    yield return root;
            }
        }

        private static IEnumerable<string?> BuildSteamRoots(string? xdgDataHome, string? home)
        {
            if (!string.IsNullOrWhiteSpace(xdgDataHome))
                yield return Path.Combine(xdgDataHome, "Steam");

            if (string.IsNullOrWhiteSpace(home))
                yield break;

            yield return Path.Combine(home, ".local", "share", "Steam");
            yield return Path.Combine(home, ".steam", "steam");
            yield return Path.Combine(home, ".steam", "debian-installation");
            yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam");
            yield return Path.Combine(home, "snap", "steam", "common", ".local", "share", "Steam");
        }

        private IEnumerable<string> EnumerateConfigRoots()
        {
            var xdgConfigHome = GetEnvironment("XDG_CONFIG_HOME");
            if (!string.IsNullOrWhiteSpace(xdgConfigHome))
                yield return xdgConfigHome;

            var home = GetEnvironment("HOME");
            if (!string.IsNullOrWhiteSpace(home))
                yield return Path.Combine(home, ".config");
        }

        private string? FindGameFolder(string directory)
        {
            if (!Directory.Exists(directory))
                return null;

            foreach (var name in GameFolderNames())
            {
                var candidate = Path.Combine(directory, name);
                if (Directory.Exists(candidate))
                    return candidate;
            }

            try
            {
                foreach (var candidate in Directory.GetDirectories(directory))
                {
                    var folderName = Path.GetFileName(candidate);
                    if (GameFolderNames().Any(name =>
                            name.Equals(folderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
                // Unreadable common folder: skip.
            }

            return null;
        }

        private IEnumerable<string> CandidateGameFolders(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                yield break;

            yield return basePath;
            yield return Path.Combine(basePath, "Heroic");
            foreach (var name in GameFolderNames())
            {
                yield return Path.Combine(basePath, name);
                yield return Path.Combine(basePath, "Heroic", name);
            }
        }

        private IEnumerable<string> GameFolderNames()
        {
            yield return _platform.DefaultGameFolderName;
            yield return "DustAET";
            yield return "Dust: An Elysian Tail";
        }

        private static IEnumerable<string> ReadHeroicInstallPaths(string configPath)
        {
            string content;
            try
            {
                content = File.ReadAllText(configPath);
            }
            catch
            {
                yield break;
            }

            foreach (Match match in HeroicInstallPathRegex.Matches(content))
            {
                var value = match.Groups[1].Value.Replace("\\\\", "\\");
                if (!string.IsNullOrWhiteSpace(value))
                    yield return value;
            }
        }

        private static IEnumerable<string> ReadLutrisBasePaths(string ymlPath)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(ymlPath);
            }
            catch
            {
                yield break;
            }

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                string? value = null;

                if (trimmed.StartsWith("directory:", StringComparison.Ordinal))
                    value = trimmed["directory:".Length..];
                else if (trimmed.StartsWith("game_path:", StringComparison.Ordinal))
                    value = trimmed["game_path:".Length..];

                if (value == null)
                    continue;

                var path = value.Trim().Trim('"', '\'');
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (trimmed.StartsWith("game_path:", StringComparison.Ordinal))
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(directory))
                        yield return directory;
                }
                else
                {
                    yield return path;
                }
            }
        }

        private string? GetEnvironment(string name)
        {
            var value = _getEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
