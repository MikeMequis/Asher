namespace Asher.UserInterface.ViewModels
{
    public class WelcomeViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        public WelcomeViewModel(IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
        }

        public override Task InitAsync() => Task.CompletedTask;

        private DelegateCommand _beginInstallationCommand;
        public DelegateCommand BeginInstallationCommand =>
            _beginInstallationCommand ??= new DelegateCommand(ExecuteBeginInstallationCommand);

        private void ExecuteBeginInstallationCommand()
        {
            // Navega para a próxima tela (GameDetection)
            _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.GameDetection);
        }
    }
}