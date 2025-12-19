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
            _regionManager.RegisterViewWithRegion(RegionNames.Main, typeof(HomeView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation(typeof(HomeView), NavigationNames.Home);
            containerRegistry.RegisterForNavigation(typeof(ContentPatcherView), NavigationNames.ContentPatcher);
            containerRegistry.RegisterForNavigation(typeof(PatchManagerView), NavigationNames.PatchManager);
            containerRegistry.RegisterForNavigation(typeof(SettingsView), NavigationNames.Settings);
        }
    }
}
