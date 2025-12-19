namespace Asher.UserInterface.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IRegionManager _regionManager;

        public HomeViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public override Task InitAsync() => Task.CompletedTask;

        private DelegateCommand _navigateToContentPatcherCommand;
        public DelegateCommand NavigateToContentPatcherCommand =>
            _navigateToContentPatcherCommand ??= new DelegateCommand
            (() => ExecuteNavigateCommand(NavigationNames.ContentPatcher));

        private DelegateCommand _navigateToPatchManagerCommand;
        public DelegateCommand NavigateToPatchManagerCommand =>
            _navigateToPatchManagerCommand ??= new DelegateCommand
                (() => ExecuteNavigateCommand(NavigationNames.PatchManager));

        private DelegateCommand _navigateToSettingsCommand;
        public DelegateCommand NavigateToSettingsCommand =>
            _navigateToSettingsCommand ??= new DelegateCommand
            (() => ExecuteNavigateCommand(NavigationNames.Settings));

        public DelegateCommand _launchGameCommand;
        public DelegateCommand LaunchGameCommand =>
            _launchGameCommand ??= new DelegateCommand(ExecuteLaunchGameCommand);

        private void ExecuteNavigateCommand(string navigationName)
        {
            _regionManager.RequestNavigate(RegionNames.Main, navigationName);
        }

        private void ExecuteLaunchGameCommand()
        {
            // TODO: Implement game launching logic
            // This will be implemented when we add the LoaderService
        }
    }
}
