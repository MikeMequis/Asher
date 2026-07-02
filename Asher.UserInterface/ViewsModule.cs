using Asher.UserInterface.Views;

namespace Asher.UserInterface
{
    public class ViewsModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public ViewsModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Views normais (após instalação)
            containerRegistry.RegisterForNavigation<HomeView>(NavigationNames.Home);
            containerRegistry.RegisterForNavigation<ContentPatcherView>(NavigationNames.ContentPatcher);
            containerRegistry.RegisterForNavigation<PatchManagerView>(NavigationNames.PatchManager);
            containerRegistry.RegisterForNavigation<SettingsView>(NavigationNames.Settings);

            // Views de instalação
            containerRegistry.RegisterForNavigation<WelcomeView>(InstallationNavigationNames.Welcome);
            containerRegistry.RegisterForNavigation<GameDetectionView>(InstallationNavigationNames.GameDetection);
            containerRegistry.RegisterForNavigation<InstallationProgressView>(InstallationNavigationNames.InstallationProgress);
            containerRegistry.RegisterForNavigation<InstallationResultView>(InstallationNavigationNames.InstallationResult);
        }
    }
}