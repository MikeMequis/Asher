using Asher.Core;
using Asher.Core.Models;
using Asher.Core.Platform;
using Asher.Services.Implementations;
using Asher.Services.Platform;
using System.Text.Json;

namespace Asher.Services.Tests;

public class LinuxRuntimeDeploymentTests
{
    private static GameInstallationService CreateService(
        LinuxPlatformInfo platform,
        LinuxRuntimeDeployment deployment) =>
        new(new SettingsService(), new LinuxGameExecutableLayout(platform), deployment, platform);

    private static string CreatePayload(
        TempDirectory temp,
        bool includeBootstrap = true,
        bool modsInDefaultFolder = true)
    {
        var modsFolder = modsInDefaultFolder
            ? AsherPaths.DefaultModsFolderName
            : AsherPaths.ModsFolderName;

        temp.WriteFile(Path.Combine("payload", "Asher.Runtime.dll"), "runtime");
        temp.WriteFile(Path.Combine("payload", "Asher.SDK.dll"), "sdk");
        temp.WriteFile(Path.Combine("payload", "0Harmony.dll"), "harmony");

        if (includeBootstrap)
            temp.WriteFile(Path.Combine("payload", "libasher_bootstrap.so"), "bootstrap");

        temp.WriteFile(
            Path.Combine("payload", modsFolder, "Asher.Patching.DebugEnabler.dll"),
            "mod");

        return Path.Combine(temp.Root, "payload");
    }

    private static string DefaultModsSource(string payload) =>
        Path.Combine(payload, AsherPaths.DefaultModsFolderName);

    [Fact]
    public void Fresh_installation_deploys_complete_runtime_and_manifest()
    {
        using var temp = new TempDirectory();
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var service = CreateService(platform, deployment);
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp);

        deployment.DeployRuntimeFiles(game, payload);
        var modsCopied = deployment.DeployDefaultMods(game, new[] { DefaultModsSource(payload) });

        Assert.True(deployment.IsRuntimeInstalled(game));
        Assert.Equal(1, modsCopied);
        Assert.True(File.Exists(LinuxRuntimeDeployment.GetManifestPath(game)));

        foreach (var fileName in deployment.RequiredRuntimeFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(AsherPaths.GetRuntimeFolderPath(game), fileName)),
                $"missing {fileName}");
        }

        var state = service.GetInstallState(game);
        Assert.Equal(InstallationState.Installed, state.State);
        Assert.True(state.CanUninstall);
        Assert.False(state.CanRestore);
        Assert.Equal("libasher_bootstrap.so", state.Marker);
    }

    [Fact]
    public void Deployment_is_idempotent()
    {
        using var temp = new TempDirectory();
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp);

        deployment.DeployRuntimeFiles(game, payload);
        deployment.DeployRuntimeFiles(game, payload);

        Assert.Equal(1, deployment.DeployDefaultMods(game, new[] { DefaultModsSource(payload) }));
        Assert.Equal(1, deployment.DeployDefaultMods(game, new[] { DefaultModsSource(payload) }));

        Assert.True(deployment.IsRuntimeInstalled(game));
        Assert.Single(Directory.GetFiles(AsherPaths.GetModsFolderPath(game), "*.dll"));
    }

    [Fact]
    public void Partial_installation_becomes_installed_after_deploy()
    {
        using var temp = new TempDirectory();
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var service = CreateService(platform, deployment);
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp);

        temp.WriteFile(Path.Combine("game", "Asher", "libasher_bootstrap.so"), "bootstrap");
        Assert.Equal(InstallationState.Partial, service.GetInstallState(game).State);

        deployment.DeployRuntimeFiles(game, payload);

        Assert.Equal(InstallationState.Installed, service.GetInstallState(game).State);
    }

    [Fact]
    public void Missing_bootstrap_in_payload_throws_and_writes_no_manifest()
    {
        using var temp = new TempDirectory();
        var deployment = new LinuxRuntimeDeployment(new LinuxPlatformInfo());
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp, includeBootstrap: false);

        Assert.Throws<FileNotFoundException>(() => deployment.DeployRuntimeFiles(game, payload));
        Assert.False(File.Exists(LinuxRuntimeDeployment.GetManifestPath(game)));
    }

    [Fact]
    public void Missing_managed_file_after_deploy_is_partial()
    {
        using var temp = new TempDirectory();
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var service = CreateService(platform, deployment);
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp);

        deployment.DeployRuntimeFiles(game, payload);
        File.Delete(Path.Combine(AsherPaths.GetRuntimeFolderPath(game), "Asher.SDK.dll"));

        Assert.False(deployment.IsRuntimeInstalled(game));
        Assert.Equal(InstallationState.Partial, service.GetInstallState(game).State);
    }

    [Fact]
    public void Removing_manifest_is_partial()
    {
        using var temp = new TempDirectory();
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var service = CreateService(platform, deployment);
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp);

        deployment.DeployRuntimeFiles(game, payload);
        File.Delete(LinuxRuntimeDeployment.GetManifestPath(game));

        Assert.False(deployment.IsRuntimeInstalled(game));
        Assert.Equal(InstallationState.Partial, service.GetInstallState(game).State);
    }

    [Fact]
    public void Manifest_records_schema_version()
    {
        using var temp = new TempDirectory();
        var deployment = new LinuxRuntimeDeployment(new LinuxPlatformInfo());
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp);

        deployment.DeployRuntimeFiles(game, payload);

        using var document = JsonDocument.Parse(File.ReadAllText(LinuxRuntimeDeployment.GetManifestPath(game)));
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public void Game_executable_is_untouched()
    {
        using var temp = new TempDirectory();
        var deployment = new LinuxRuntimeDeployment(new LinuxPlatformInfo());
        var game = temp.CreateGameFolder("game");
        var executablePath = Path.Combine(game, "DustAET");
        File.WriteAllText(executablePath, "native-binary");
        var payload = CreatePayload(temp);

        deployment.DeployRuntimeFiles(game, payload);

        Assert.Equal("native-binary", File.ReadAllText(executablePath));
        Assert.False(File.Exists(Path.Combine(game, "DustAET.real.exe")));
    }

    [Fact]
    public async Task Uninstall_removes_asher_files_and_keeps_game_executable()
    {
        using var temp = new TempDirectory();
        var platform = new LinuxPlatformInfo();
        var deployment = new LinuxRuntimeDeployment(platform);
        var service = CreateService(platform, deployment);
        var game = temp.CreateGameFolder("game");
        var executablePath = Path.Combine(game, "DustAET");
        File.WriteAllText(executablePath, "native-binary");
        var payload = CreatePayload(temp);

        deployment.DeployRuntimeFiles(game, payload);
        deployment.DeployDefaultMods(game, new[] { DefaultModsSource(payload) });
        Assert.True(service.IsInstalled(game));

        var result = await service.UninstallAsync(
            game,
            new Progress<InstallationProgress>(_ => { }));

        Assert.True(result.Success, result.Message);
        Assert.False(service.IsInstalled(game));
        Assert.Equal("native-binary", File.ReadAllText(executablePath));
        Assert.False(File.Exists(LinuxRuntimeDeployment.GetManifestPath(game)));
        Assert.False(File.Exists(Path.Combine(AsherPaths.GetRuntimeFolderPath(game), "libasher_bootstrap.so")));
        Assert.False(File.Exists(Path.Combine(AsherPaths.GetRuntimeFolderPath(game), "Asher.Runtime.dll")));
    }

    [Fact]
    public void Default_mods_resolve_from_raw_build_mods_folder()
    {
        using var temp = new TempDirectory();
        var deployment = new LinuxRuntimeDeployment(new LinuxPlatformInfo());
        var game = temp.CreateGameFolder("game");
        var payload = CreatePayload(temp, modsInDefaultFolder: false);

        var candidates = new[]
        {
            Path.Combine(payload, AsherPaths.DefaultModsFolderName),
            Path.Combine(payload, AsherPaths.ModsFolderName)
        };

        var copied = deployment.DeployDefaultMods(game, candidates);

        Assert.Equal(1, copied);
        Assert.True(File.Exists(
            Path.Combine(AsherPaths.GetModsFolderPath(game), "Asher.Patching.DebugEnabler.dll")));
        Assert.Contains(AsherPaths.DefaultModsFolderName, deployment.DefaultModsSourceFolderNames);
        Assert.Contains(AsherPaths.ModsFolderName, deployment.DefaultModsSourceFolderNames);
    }
}
