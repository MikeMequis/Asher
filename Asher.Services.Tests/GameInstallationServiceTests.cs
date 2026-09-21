using Asher.Core.Models;
using Asher.Core.Platform;
using Asher.Services.Implementations;
using Asher.Services.Platform;

namespace Asher.Services.Tests;

public class WindowsInstallStateTests
{
    private static GameInstallationService Create() => new(
        new WindowsGameExecutableLayout(),
        new WindowsRuntimeDeployment(),
        new WindowsPlatformInfo());

    [Fact]
    public void Empty_folder_is_not_installed()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateDirectory("game");

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.NotInstalled, state.State);
        Assert.False(state.CanUninstall);
        Assert.False(state.CanRestore);
        Assert.Equal(string.Empty, state.Marker);
    }

    [Fact]
    public void Renamed_executable_without_runtime_is_partial_but_restorable()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateDirectory("game");
        temp.WriteFile(Path.Combine("game", "DustAET.real.exe"), string.Empty);

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.Partial, state.State);
        Assert.Equal("DustAET.real.exe", state.Marker);
        Assert.True(state.CanRestore);
        Assert.True(state.CanUninstall);
    }

    [Fact]
    public void Runtime_without_renamed_executable_is_partial_and_not_uninstallable()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateDirectory("game");
        temp.WriteFile(Path.Combine("game", "Asher", "Asher.Runtime.dll"), string.Empty);

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.Partial, state.State);
        Assert.Equal("Asher.Runtime.dll", state.Marker);
        Assert.False(state.CanRestore);
        Assert.False(state.CanUninstall);
    }

    [Fact]
    public void Complete_layout_is_installed()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateDirectory("game");
        temp.WriteFile(Path.Combine("game", "DustAET.real.exe"), string.Empty);
        temp.WriteFile(Path.Combine("game", "Asher", "Asher.Runtime.dll"), string.Empty);

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.Installed, state.State);
        Assert.Equal("DustAET.real.exe", state.Marker);
        Assert.True(state.CanRestore);
        Assert.True(state.CanUninstall);
    }
}

public class LinuxInstallStateTests
{
    private static GameInstallationService Create()
    {
        var platform = new LinuxPlatformInfo();
        return new GameInstallationService(
            new LinuxGameExecutableLayout(platform),
            new LinuxRuntimeDeployment(platform),
            platform);
    }

    [Fact]
    public void Empty_folder_is_not_installed()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateDirectory("game");

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.NotInstalled, state.State);
        Assert.False(state.CanUninstall);
        Assert.False(state.CanRestore);
        Assert.Equal(string.Empty, state.Marker);
    }

    [Fact]
    public void Bootstrap_without_runtime_is_partial()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateDirectory("game");
        temp.WriteFile(Path.Combine("game", "Asher", "libasher_bootstrap.so"), string.Empty);

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.Partial, state.State);
        Assert.Equal("libasher_bootstrap.so", state.Marker);
        Assert.False(state.CanRestore);
        Assert.True(state.CanUninstall);
    }

    [Fact]
    public void Runtime_without_bootstrap_is_partial()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateDirectory("game");
        temp.WriteFile(Path.Combine("game", "Asher", "Asher.Runtime.dll"), string.Empty);

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.Partial, state.State);
        Assert.Equal("Asher.Runtime.dll", state.Marker);
        Assert.False(state.CanRestore);
        Assert.True(state.CanUninstall);
    }

    [Fact]
    public void Complete_layout_is_installed_and_never_restorable()
    {
        using var temp = new TempDirectory();
        var game = temp.CreateGameFolder("game");
        WriteCompleteRuntime(temp, "game");

        var state = Create().GetInstallState(game);

        Assert.Equal(InstallationState.Installed, state.State);
        Assert.Equal("libasher_bootstrap.so", state.Marker);
        Assert.False(state.CanRestore);
        Assert.True(state.CanUninstall);
    }

    internal static void WriteCompleteRuntime(TempDirectory temp, string gameRelative)
    {
        temp.WriteFile(Path.Combine(gameRelative, "Asher", "libasher_bootstrap.so"), string.Empty);
        temp.WriteFile(Path.Combine(gameRelative, "Asher", "Asher.Runtime.dll"), string.Empty);
        temp.WriteFile(Path.Combine(gameRelative, "Asher", "Asher.SDK.dll"), string.Empty);
        temp.WriteFile(Path.Combine(gameRelative, "Asher", "0Harmony.dll"), string.Empty);
        temp.WriteFile(
            Path.Combine(gameRelative, "Asher", LinuxRuntimeDeployment.ManifestFileName),
            "{\"schemaVersion\":1}");
    }
}
