using Asher.Services.Application;
using Asher.Services.Interfaces;

namespace Asher.Services.Hosting
{
    /// <summary>
    /// Wires application services without WPF/Prism. Used by the host spike and available for future backends.
    /// </summary>
    public sealed class AsherServiceHost
    {
        public IAsherApplication Application { get; }

        public ISettingsService Settings => _services.Settings;
        public IGameFolderService GameFolders => _services.GameFolders;
        public IGameInstallationService Installation => _services.Installation;
        public IGameLaunchService Launch => _services.Launch;
        public IPatchManagerService Patches => _services.Patches;
        public IInstallationStateService InstallationState => _services.InstallationState;
        public IShortcutService Shortcuts => _services.Shortcuts;
        public IManagerLaunchService ManagerLaunch => _services.ManagerLaunch;
        public IManagerDeployService ManagerDeploy => _services.ManagerDeploy;

        private readonly ApplicationServices _services;

        private AsherServiceHost(ApplicationServices services, IAsherApplication application)
        {
            _services = services;
            Application = application;
        }

        public static AsherServiceHost Create()
        {
            var services = ApplicationServices.Create();
            return new AsherServiceHost(services, new AsherApplication(services));
        }
    }
}
