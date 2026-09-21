using Asher.Core;
using Asher.Core.Platform;
using Asher.Services.Interfaces;
using Asher.Services.Platform;
using System.Diagnostics;

namespace Asher.Services.Tests;

public class GameProcessLauncherTests
{
    private sealed class FakeProcessStarter : IProcessStarter
    {
        public ProcessStartInfo? LastStartInfo { get; private set; }
        public Exception? Failure { get; set; }

        public void Start(ProcessStartInfo startInfo)
        {
            if (Failure != null)
                throw Failure;

            LastStartInfo = startInfo;
        }
    }

    private sealed class FakeEnvironmentProvider : IEnvironmentProvider
    {
        private readonly Dictionary<string, string> _values;

        public FakeEnvironmentProvider(Dictionary<string, string>? values = null) =>
            _values = values ?? new Dictionary<string, string>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, string> GetEnvironmentVariables() => _values;
    }

    private static string CreateInstalledLinuxGame(TempDirectory temp)
    {
        var game = temp.CreateGameFolder("game");
        temp.WriteFile(Path.Combine("game", "Asher", "libasher_bootstrap.so"), "bootstrap");
        temp.WriteFile(Path.Combine("game", "Asher", "Asher.Runtime.dll"), "runtime");
        temp.WriteFile(Path.Combine("game", "Asher", "Asher.SDK.dll"), "sdk");
        temp.WriteFile(Path.Combine("game", "Asher", "0Harmony.dll"), "harmony");
        temp.WriteFile(
            Path.Combine("game", "Asher", LinuxRuntimeDeployment.ManifestFileName),
            "{\"schemaVersion\":1}");
        return game;
    }

    private static (LinuxGameProcessLauncher Launcher, FakeProcessStarter Starter, string Game)
        CreateLinuxLauncher(TempDirectory temp, Dictionary<string, string>? environment = null)
    {
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var starter = new FakeProcessStarter();
        var launcher = new LinuxGameProcessLauncher(
            platform,
            deployment,
            starter,
            new FakeEnvironmentProvider(environment));

        return (launcher, starter, CreateInstalledLinuxGame(temp));
    }

    private static string ExecutablePath(string game) => Path.Combine(game, "DustAET");

    private static string BootstrapPath(string game) =>
        AsherPaths.GetBootstrapLibraryPath(game, new LinuxPlatformInfo());

    [Fact]
    public void Launches_native_executable_in_game_directory()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(temp);

        Assert.True(launcher.TryStart(ExecutablePath(game), game, out var error));
        Assert.Null(error);
        Assert.NotNull(starter.LastStartInfo);
        Assert.Equal(ExecutablePath(game), starter.LastStartInfo!.FileName);
        Assert.Equal(game, starter.LastStartInfo.WorkingDirectory);
        Assert.False(starter.LastStartInfo.UseShellExecute);
    }

    [Fact]
    public void Linux_does_not_inherit_host_stdout_or_stderr()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(temp);

        launcher.TryStart(ExecutablePath(game), game, out _);

        Assert.True(starter.LastStartInfo!.RedirectStandardOutput);
        Assert.True(starter.LastStartInfo.RedirectStandardError);
    }

    [Fact]
    public void Sets_ld_preload_when_absent()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(temp);

        launcher.TryStart(ExecutablePath(game), game, out _);

        Assert.Equal(BootstrapPath(game), starter.LastStartInfo!.Environment["LD_PRELOAD"]);
    }

    [Fact]
    public void Prepends_existing_ld_preload()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(
            temp,
            new Dictionary<string, string> { ["LD_PRELOAD"] = "/opt/other.so" });

        launcher.TryStart(ExecutablePath(game), game, out _);

        Assert.Equal(
            BootstrapPath(game) + ":/opt/other.so",
            starter.LastStartInfo!.Environment["LD_PRELOAD"]);
    }

    [Fact]
    public void Does_not_duplicate_bootstrap_in_ld_preload()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateGameFolder("game");
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var starter = new FakeProcessStarter();
        var bootstrap = AsherPaths.GetBootstrapLibraryPath(game, platform);

        var launcher = new LinuxGameProcessLauncher(
            platform,
            deployment,
            starter,
            new FakeEnvironmentProvider(new Dictionary<string, string> { ["LD_PRELOAD"] = bootstrap }));
        CreateInstalledLinuxGameIn(game);

        launcher.TryStart(ExecutablePath(game), game, out _);

        Assert.Equal(bootstrap, starter.LastStartInfo!.Environment["LD_PRELOAD"]);
    }

    private static void CreateInstalledLinuxGameIn(string game)
    {
        File.WriteAllText(Path.Combine(game, "DustAET"), string.Empty);
        var asher = Path.Combine(game, "Asher");
        Directory.CreateDirectory(asher);
        File.WriteAllText(Path.Combine(asher, "libasher_bootstrap.so"), "bootstrap");
        File.WriteAllText(Path.Combine(asher, "Asher.Runtime.dll"), "runtime");
        File.WriteAllText(Path.Combine(asher, "Asher.SDK.dll"), "sdk");
        File.WriteAllText(Path.Combine(asher, "0Harmony.dll"), "harmony");
        File.WriteAllText(Path.Combine(asher, LinuxRuntimeDeployment.ManifestFileName), "{\"schemaVersion\":1}");
    }

    [Fact]
    public void Sets_asher_environment_variables()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(temp);

        launcher.TryStart(ExecutablePath(game), game, out _);
        var environment = starter.LastStartInfo!.Environment;

        Assert.Equal(AsherPaths.GetRuntimeFolderPath(game), environment["ASHER_HOME"]);
        Assert.Equal(AsherPaths.GetModsFolderPath(game), environment["ASHER_MODS_PATH"]);
        Assert.Equal(AsherPaths.GetLogsFolderPath(game), environment["ASHER_LOG_PATH"]);
        Assert.Equal("default", environment["ASHER_PROFILE"]);
        Assert.Equal(AsherPaths.GetRuntimeFolderPath(game), environment["MONO_PATH"]);
    }

    [Fact]
    public void Preserves_existing_asher_profile()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(
            temp,
            new Dictionary<string, string> { ["ASHER_PROFILE"] = "modpack" });

        launcher.TryStart(ExecutablePath(game), game, out _);

        Assert.Equal("modpack", starter.LastStartInfo!.Environment["ASHER_PROFILE"]);
    }

    [Fact]
    public void Prepends_existing_mono_path()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(
            temp,
            new Dictionary<string, string> { ["MONO_PATH"] = "/opt/mono" });

        launcher.TryStart(ExecutablePath(game), game, out _);

        Assert.Equal(
            AsherPaths.GetRuntimeFolderPath(game) + ":/opt/mono",
            starter.LastStartInfo!.Environment["MONO_PATH"]);
    }

    [Fact]
    public void Preserves_unrelated_inherited_variables()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(
            temp,
            new Dictionary<string, string>
            {
                ["PATH"] = "/usr/bin",
                ["SOME_PARENT_VAR"] = "keep-me"
            });

        launcher.TryStart(ExecutablePath(game), game, out _);
        var environment = starter.LastStartInfo!.Environment;

        Assert.Equal("/usr/bin", environment["PATH"]);
        Assert.Equal("keep-me", environment["SOME_PARENT_VAR"]);
    }

    [Fact]
    public void Rejects_incomplete_installation()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateGameFolder("game");
        temp.WriteFile(Path.Combine("game", "Asher", "libasher_bootstrap.so"), "bootstrap");

        var platform = new LinuxPlatformInfo();
        var starter = new FakeProcessStarter();
        var launcher = new LinuxGameProcessLauncher(
            platform,
            new LinuxRuntimeDeployment(platform),
            starter,
            new FakeEnvironmentProvider());

        Assert.False(launcher.TryStart(ExecutablePath(game), game, out var error));
        Assert.NotNull(error);
        Assert.Contains("incompleta", error);
        Assert.Null(starter.LastStartInfo);
    }

    [Fact]
    public void Rejects_missing_bootstrap()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(temp);
        File.Delete(BootstrapPath(game));

        Assert.False(launcher.TryStart(ExecutablePath(game), game, out var error));
        Assert.NotNull(error);
        Assert.Contains("Bootstrap", error);
        Assert.Null(starter.LastStartInfo);
    }

    [Fact]
    public void Returns_false_when_start_throws()
    {
        using var temp = new TempDirectory();
        var (launcher, starter, game) = CreateLinuxLauncher(temp);
        starter.Failure = new InvalidOperationException("boom");

        Assert.False(launcher.TryStart(ExecutablePath(game), game, out var error));
        Assert.NotNull(error);
        Assert.Contains("boom", error);
    }

    [Fact]
    public void Windows_launches_via_shell_unchanged()
    {
        using var temp = new TempDirectory();
        var starter = new FakeProcessStarter();
        var launcher = new WindowsGameProcessLauncher(starter);
        var game = temp.CreateDirectory("game");
        var executable = Path.Combine(game, "DustAET.exe");

        Assert.True(launcher.TryStart(executable, game, out var error));
        Assert.Null(error);
        Assert.NotNull(starter.LastStartInfo);
        Assert.Equal(executable, starter.LastStartInfo!.FileName);
        Assert.Equal(game, starter.LastStartInfo.WorkingDirectory);
        Assert.True(starter.LastStartInfo.UseShellExecute);
        Assert.False(starter.LastStartInfo.RedirectStandardOutput);
        Assert.False(starter.LastStartInfo.RedirectStandardError);
    }

    [Fact]
    public void Windows_returns_false_when_start_throws()
    {
        using var temp = new TempDirectory();
        var starter = new FakeProcessStarter { Failure = new InvalidOperationException("boom") };
        var launcher = new WindowsGameProcessLauncher(starter);
        var game = temp.CreateDirectory("game");

        Assert.False(launcher.TryStart(Path.Combine(game, "DustAET.exe"), game, out var error));
        Assert.NotNull(error);
        Assert.Contains("boom", error);
    }
}
