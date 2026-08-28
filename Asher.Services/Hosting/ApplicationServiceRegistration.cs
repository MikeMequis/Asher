using Asher.Services.Interfaces;

namespace Asher.Services.Hosting
{
    /// <summary>
    /// Registers <see cref="ApplicationServices"/> instances with a host-specific container.
    /// </summary>
    public static class ApplicationServiceRegistration
    {
        public static void RegisterInstances(ApplicationServices services, Action<Type, object> registerInstance)
        {
            registerInstance(typeof(ISettingsService), services.Settings);
            registerInstance(typeof(IManagerDeployService), services.ManagerDeploy);
            registerInstance(typeof(IGameInstallationService), services.Installation);
            registerInstance(typeof(IGameFolderService), services.GameFolders);
            registerInstance(typeof(IGameLaunchService), services.Launch);
            registerInstance(typeof(IPatchManagerService), services.Patches);
            registerInstance(typeof(IInstallationStateService), services.InstallationState);
            registerInstance(typeof(IShortcutService), services.Shortcuts);
            registerInstance(typeof(IManagerLaunchService), services.ManagerLaunch);
        }
    }
}
