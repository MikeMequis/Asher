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
        public IManagerDeployService ManagerDeploy { get; }
        public IGameInstallationService Installation { get; }
        public IGameFolderService GameFolders { get; }
        public IGameLaunchService Launch { get; }
        public IPatchManagerService Patches { get; }
        public IInstallationStateService InstallationState { get; }
        public IShortcutService Shortcuts { get; }
        public IManagerLaunchService ManagerLaunch { get; }

        private ApplicationServices(
            ISettingsService settings,
            IManagerDeployService managerDeploy,
            IGameInstallationService installation,
            IGameFolderService gameFolders,
            IGameLaunchService launch,
            IPatchManagerService patches,
            IInstallationStateService installationState,
            IShortcutService shortcuts,
            IManagerLaunchService managerLaunch)
        {
            Settings = settings;
            ManagerDeploy = managerDeploy;
            Installation = installation;
            GameFolders = gameFolders;
            Launch = launch;
            Patches = patches;
            InstallationState = installationState;
            Shortcuts = shortcuts;
            ManagerLaunch = managerLaunch;
        }

        public static ApplicationServices Create()
        {
            var settings = new SettingsService();
            var managerDeploy = new ManagerDeployService();
            var installation = new GameInstallationService(managerDeploy);
            var gameFolders = new GameFolderService();
            var launch = new GameLaunchService(gameFolders, installation);
            var patches = new PatchManagerService(launch);

            return new ApplicationServices(
                settings,
                managerDeploy,
                installation,
                gameFolders,
                launch,
                patches,
                new InstallationStateService(),
                new ShortcutService(),
                new ManagerLaunchService());
        }
    }
}
