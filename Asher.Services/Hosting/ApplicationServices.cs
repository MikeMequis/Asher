using Asher.Services.Implementations;
using Asher.Services.Interfaces;
using Asher.Services.Platform;

namespace Asher.Services.Hosting
{
    /// <summary>
    /// Single composition root for application services (no WPF/Prism).
    /// Platform-dependent operations are supplied by <see cref="PlatformServices.Create"/>.
    /// </summary>
    public sealed class ApplicationServices
    {
        public ISettingsService Settings { get; }
        public IGameInstallationService Installation { get; }
        public IGameFolderService GameFolders { get; }
        public IGameLaunchService Launch { get; }
        public IPatchManagerService Patches { get; }
        public PlatformServices Platform { get; }

        private ApplicationServices(
            ISettingsService settings,
            IGameInstallationService installation,
            IGameFolderService gameFolders,
            IGameLaunchService launch,
            IPatchManagerService patches,
            PlatformServices platform)
        {
            Settings = settings;
            Installation = installation;
            GameFolders = gameFolders;
            Launch = launch;
            Patches = patches;
            Platform = platform;
        }

        public static ApplicationServices Create()
        {
            var platform = PlatformServices.Create();

            var settings = new SettingsService();
            var installation = new GameInstallationService(
                platform.ExecutableLayout,
                platform.RuntimeDeployment,
                platform.Info);
            var gameFolders = new GameFolderService(platform.GameFolderDiscovery);
            var launch = new GameLaunchService(gameFolders, installation, platform.ProcessLauncher);
            var patches = new PatchManagerService(launch);

            return new ApplicationServices(
                settings,
                installation,
                gameFolders,
                launch,
                patches,
                platform);
        }
    }
}
