using Asher.Services.Interfaces;

namespace Asher.UserInterface.ViewModels
{
    public class GameDetectionViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IGameFolderService _gameFolderService;
        private readonly IInstallationStateService _installationStateService;

        public GameDetectionViewModel(IEventAggregator eventAggregator,
                                      IGameFolderService gameFolderService,
                                      IInstallationStateService installationStateService) 
        {
            _eventAggregator = eventAggregator;
            _gameFolderService = gameFolderService;
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {

        }

        private DelegateCommand _detectGameFolderCommand;
        public DelegateCommand DetectGameFolderCommand =>
            _detectGameFolderCommand ??= new DelegateCommand(ExecuteDetectGameFolderCommand);

        private DelegateCommand _browseCommand;
        public DelegateCommand BrowseCommand =>
            _browseCommand ??= new DelegateCommand(ExecuteBrowseCommand);

        private void ExecuteDetectGameFolderCommand()
        {

        }

        private void ExecuteBrowseCommand()
        {

        }
    }
}
