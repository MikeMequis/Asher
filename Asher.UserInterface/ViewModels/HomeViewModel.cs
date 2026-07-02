using Asher.Services.Interfaces;

namespace Asher.UserInterface.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IRegionManager _regionManager;
        private readonly IGameLaunchService _gameLaunchService;

        public HomeViewModel(IRegionManager regionManager, IGameLaunchService gameLaunchService)
        {
            _regionManager = regionManager;
            _gameLaunchService = gameLaunchService;
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
            if (_gameLaunchService.TryLaunchGame(out var error))
                return;

            System.Windows.MessageBox.Show(
                error ?? "Não foi possível iniciar o jogo.",
                "Asher - Launch Game",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
