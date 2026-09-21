using Asher.Core.Platform;
using Asher.Services.Interfaces;
using Asher.Services.Platform;
using System.Text;

namespace Asher.Services.Tests;

public class LinuxGameFolderDiscoveryTests
{
    private static LinuxGameFolderDiscovery Create(Dictionary<string, string> environment) =>
        new(new LinuxPlatformInfo(), name =>
            environment.TryGetValue(name, out var value) ? value : null);

    private static GameFolderCandidate? FindBySource(
        IEnumerable<GameFolderCandidate> candidates,
        string source)
    {
        foreach (var candidate in candidates)
        {
            if (candidate.Source == source && !string.IsNullOrEmpty(candidate.Path))
                return candidate;
        }

        return null;
    }

    private static string Vdf(params string[] libraryPaths)
    {
        var builder = new StringBuilder();
        builder.AppendLine("\"libraryfolders\"");
        builder.AppendLine("{");

        for (var i = 0; i < libraryPaths.Length; i++)
        {
            builder.AppendLine($"\t\"{i}\"");
            builder.AppendLine("\t{");
            builder.AppendLine($"\t\t\"path\"\t\t\"{libraryPaths[i]}\"");
            builder.AppendLine("\t}");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string ToForwardSlashes(string path) => path.Replace('\\', '/');

    [Fact]
    public void Finds_game_in_default_steam_root()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateGameFolder(
            "home", ".steam", "steam", "steamapps", "common", "Dust An Elysian Tail");

        var environment = new Dictionary<string, string>
        {
            ["HOME"] = Path.Combine(temp.Root, "home")
        };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();
        var steam = FindBySource(candidates, "Steam");

        Assert.True(steam.HasValue);
        Assert.Equal(game, steam.Value.Path);
    }

    [Fact]
    public void Finds_game_in_xdg_data_home_steam_root()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateGameFolder(
            "xdg", "Steam", "steamapps", "common", "Dust An Elysian Tail");

        var environment = new Dictionary<string, string>
        {
            ["XDG_DATA_HOME"] = Path.Combine(temp.Root, "xdg"),
            ["HOME"] = Path.Combine(temp.Root, "no-home")
        };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();
        var steam = FindBySource(candidates, "Steam");

        Assert.True(steam.HasValue);
        Assert.Equal(game, steam.Value.Path);
    }

    [Fact]
    public void Finds_game_in_secondary_steam_library()
    {
        using var temp = new TempDirectory();
        var home = temp.CreateDirectory("home");
        var steamRoot = Path.Combine(home, ".steam", "steam");
        var library = temp.CreateDirectory("library2");
        var game = temp.CreateGameFolder(
            "library2", "steamapps", "common", "Dust An Elysian Tail");

        temp.WriteFile(
            Path.Combine("home", ".steam", "steam", "steamapps", "libraryfolders.vdf"),
            Vdf(ToForwardSlashes(steamRoot), ToForwardSlashes(library)));

        var environment = new Dictionary<string, string> { ["HOME"] = home };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();
        var steam = FindBySource(candidates, "Steam");

        Assert.True(steam.HasValue);
        Assert.Equal(Path.GetFullPath(game), Path.GetFullPath(steam.Value.Path));
    }

    [Fact]
    public void Installed_candidate_uses_injected_platform_semantics()
    {
        using var temp = new TempDirectory();
        var home = temp.CreateDirectory("home");
        var game = temp.CreateGameFolder(
            "home", ".steam", "steam", "steamapps", "common", "Dust An Elysian Tail");

        temp.WriteFile(
            Path.Combine("home", ".steam", "steam", "steamapps", "common", "Dust An Elysian Tail", "Asher", "libasher_bootstrap.so"),
            string.Empty);
        temp.WriteFile(
            Path.Combine("home", ".steam", "steam", "steamapps", "common", "Dust An Elysian Tail", "Asher", "Asher.Runtime.dll"),
            string.Empty);

        var environment = new Dictionary<string, string> { ["HOME"] = home };
        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();
        var installed = FindBySource(candidates, "Installed");

        Assert.True(installed.HasValue);
        Assert.Equal(game, installed.Value.Path);
    }

    [Fact]
    public void Installed_candidate_ignores_windows_only_layout()
    {
        using var temp = new TempDirectory();
        var home = temp.CreateDirectory("home");
        var common = Path.Combine("home", ".steam", "steam", "steamapps", "common", "Dust An Elysian Tail");

        temp.WriteFile(Path.Combine(common, "DustAET.exe"), string.Empty);
        temp.WriteFile(Path.Combine(common, "Asher", "Asher.Runtime.dll"), string.Empty);

        var environment = new Dictionary<string, string> { ["HOME"] = home };
        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();

        Assert.Null(FindBySource(candidates, "Installed"));
    }

    [Fact]
    public void Finds_game_in_home_folder()
    {
        using var temp = new TempDirectory();
        var home = temp.CreateDirectory("home");
        var game = temp.CreateGameFolder("home", "Dust An Elysian Tail");

        var environment = new Dictionary<string, string> { ["HOME"] = home };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();
        var homeCandidate = FindBySource(candidates, "Home");

        Assert.True(homeCandidate.HasValue);
        Assert.Equal(game, homeCandidate.Value.Path);
    }

    [Fact]
    public void Finds_game_from_heroic_config_when_path_exists()
    {
        using var temp = new TempDirectory();
        var configRoot = temp.CreateDirectory("config");
        var installBase = temp.CreateDirectory("heroic-games");
        var game = temp.CreateGameFolder("heroic-games", "Dust An Elysian Tail");

        temp.WriteFile(
            Path.Combine("config", "heroic", "config.json"),
            "{\"defaultInstallPath\": \"" + installBase.Replace("\\", "\\\\") + "\"}");

        var environment = new Dictionary<string, string>
        {
            ["XDG_CONFIG_HOME"] = configRoot,
            ["HOME"] = Path.Combine(temp.Root, "no-home")
        };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();
        var heroic = FindBySource(candidates, "Heroic");

        Assert.True(heroic.HasValue);
        Assert.Equal(game, heroic.Value.Path);
    }

    [Fact]
    public void Does_not_guess_heroic_path_when_missing()
    {
        using var temp = new TempDirectory();
        var configRoot = temp.CreateDirectory("config");
        var missingBase = Path.Combine(temp.Root, "does-not-exist");

        temp.WriteFile(
            Path.Combine("config", "heroic", "config.json"),
            "{\"defaultInstallPath\": \"" + missingBase.Replace("\\", "\\\\") + "\"}");

        var environment = new Dictionary<string, string>
        {
            ["XDG_CONFIG_HOME"] = configRoot,
            ["HOME"] = Path.Combine(temp.Root, "no-home")
        };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();

        Assert.Null(FindBySource(candidates, "Heroic"));
    }

    [Fact]
    public void Finds_game_from_lutris_config_when_path_exists()
    {
        using var temp = new TempDirectory();
        var configRoot = temp.CreateDirectory("config");
        var installBase = temp.CreateDirectory("lutris-games");
        var game = temp.CreateGameFolder("lutris-games", "Dust An Elysian Tail");

        temp.WriteFile(
            Path.Combine("config", "lutris", "games", "dust.yml"),
            "game:\n  directory: " + installBase + "\n");

        var environment = new Dictionary<string, string>
        {
            ["XDG_CONFIG_HOME"] = configRoot,
            ["HOME"] = Path.Combine(temp.Root, "no-home")
        };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();
        var lutris = FindBySource(candidates, "Lutris");

        Assert.True(lutris.HasValue);
        Assert.Equal(game, lutris.Value.Path);
    }

    [Fact]
    public void Does_not_guess_lutris_path_when_missing()
    {
        using var temp = new TempDirectory();
        var configRoot = temp.CreateDirectory("config");
        var missingBase = Path.Combine(temp.Root, "does-not-exist");

        temp.WriteFile(
            Path.Combine("config", "lutris", "games", "dust.yml"),
            "game:\n  directory: " + missingBase + "\n");

        var environment = new Dictionary<string, string>
        {
            ["XDG_CONFIG_HOME"] = configRoot,
            ["HOME"] = Path.Combine(temp.Root, "no-home")
        };

        var candidates = Create(environment).EnumerateGameFolderCandidates().ToList();

        Assert.Null(FindBySource(candidates, "Lutris"));
    }
}
