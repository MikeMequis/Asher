using Asher.Services.Implementations;
using Asher.Services.Interfaces;

namespace Asher.Services.Hosting
{
    /// <summary>
    /// Single composition root for application services (no WPF/Prism).
    /// </summary>
    public sealed class ApplicationServices
    {
        public ISettingsService Settings { get; }
        public IGameInstallationService Installation { get; }
        public IGameFolderService GameFolders { get; }
        public IGameLaunchService Launch { get; }
        public IPatchManagerService Patches { get; }

        private ApplicationServices(
            ISettingsService settings,
            IGameInstallationService installation,
            IGameFolderService gameFolders,
            IGameLaunchService launch,
            IPatchManagerService patches)
        {
            Settings = settings;
            Installation = installation;
            GameFolders = gameFolders;
            Launch = launch;
            Patches = patches;
        }

        public static ApplicationServices Create()
        {
            var settings = new SettingsService();
            var installation = new GameInstallationService(settings);
            var gameFolders = new GameFolderService();
            var launch = new GameLaunchService(gameFolders, installation);
            var patches = new PatchManagerService(launch);

            return new ApplicationServices(
                settings,
                installation,
                gameFolders,
                launch,
                patches);
        }
    }
}
