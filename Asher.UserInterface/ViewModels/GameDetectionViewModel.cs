using Asher.Core.Models;
using Asher.Services.Interfaces;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Asher.UserInterface.ViewModels
{
    public class GameDetectionViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IGameFolderService _gameFolderService;
        private readonly IInstallationStateService _installationStateService;
        private readonly IRegionManager _regionManager;

        private GameFolderInfo _currentGameInfo;

        public GameDetectionViewModel(IEventAggregator eventAggregator,
                                      IGameFolderService gameFolderService,
                                      IInstallationStateService installationStateService,
                                      IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            _gameFolderService = gameFolderService;
            _installationStateService = installationStateService;
            _regionManager = regionManager;
        }

        private string _gameFolderPath;
        public string GameFolderPath
        {
            get => _gameFolderPath;
            set => SetProperty(ref _gameFolderPath, value);
        }

        private bool _isDetecting;
        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetProperty(ref _isDetecting, value);
        }

        private bool _hasGameInfo;
        public bool HasGameInfo
        {
            get => _hasGameInfo;
            set => SetProperty(ref _hasGameInfo, value);
        }

        private bool _isGameValid;
        public bool IsGameValid
        {
            get => _isGameValid;
            set => SetProperty(ref _isGameValid, value);
        }

        private string _gameVersion;
        public string GameVersion
        {
            get => _gameVersion;
            set => SetProperty(ref _gameVersion, value);
        }

        private string _gameSource;
        public string GameSource
        {
            get => _gameSource;
            set => SetProperty(ref _gameSource, value);
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            // Tenta detectar automaticamente ao navegar para a tela
            ExecuteDetectGameFolderCommand();
        }

        private DelegateCommand _detectGameFolderCommand;
        public DelegateCommand DetectGameFolderCommand =>
            _detectGameFolderCommand ??= new DelegateCommand(ExecuteDetectGameFolderCommand);

        private DelegateCommand _browseCommand;
        public DelegateCommand BrowseCommand =>
            _browseCommand ??= new DelegateCommand(ExecuteBrowseCommand);

        private DelegateCommand _continueCommand;
        public DelegateCommand ContinueCommand =>
            _continueCommand ??= new DelegateCommand(ExecuteContinueCommand, CanExecuteContinueCommand)
                .ObservesProperty(() => IsGameValid);

        private void ExecuteDetectGameFolderCommand()
        {
            IsDetecting = true;

            Task.Run(() =>
            {
                var gameInfo = _gameFolderService.DetectGameFolder();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateGameInfo(gameInfo);
                    IsDetecting = false;
                });
            });
        }

        private void ExecuteBrowseCommand()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Selecione a pasta de instalação do Dust: An Elysian Tail",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var gameInfo = _gameFolderService.GetInfo(dialog.SelectedPath);
                UpdateGameInfo(gameInfo);
            }
        }

        private void UpdateGameInfo(GameFolderInfo gameInfo)
        {
            _currentGameInfo = gameInfo;
            GameFolderPath = gameInfo.Path;
            IsGameValid = gameInfo.IsValid;
            GameVersion = gameInfo.Version;
            GameSource = gameInfo.Source;
            HasGameInfo = !string.IsNullOrEmpty(gameInfo.Path);

            if (gameInfo.IsValid)
            {
                // Salva no estado global
                _installationStateService.SetGameFolder(gameInfo);
            }
        }

        private bool CanExecuteContinueCommand()
        {
            return IsGameValid;
        }

        private void ExecuteContinueCommand()
        {
            if (_currentGameInfo == null || !_currentGameInfo.IsValid)
                return;

            // Criar pasta de patches se não existir
            _gameFolderService.CreatePatchesFolder(_currentGameInfo.Path);

            // Navega para a próxima tela (InstallationProgress)
            _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.InstallationProgress);
        }
    }
}