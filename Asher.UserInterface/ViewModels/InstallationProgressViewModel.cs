using Asher.Core.Models;
using Asher.Core;
using Asher.Services.Interfaces;

namespace Asher.UserInterface.ViewModels
{
    public class InstallationProgressViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInstallationStateService _stateService;
        private readonly IGameInstallationService _installationService;
        private readonly IRegionManager _regionManager;
        private readonly ISettingsService _settingsService;

        public InstallationProgressViewModel(IEventAggregator eventAggregator,
                                             IInstallationStateService stateService,
                                             IGameInstallationService installationService,
                                             IRegionManager regionManager,
                                             ISettingsService settingsService)
        {
            _eventAggregator = eventAggregator;
            _stateService = stateService;
            _installationService = installationService;
            _regionManager = regionManager;
            _settingsService = settingsService;
        }

        private double _progressPercentage;
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _currentStepDetails;
        public string CurrentStepDetails
        {
            get => _currentStepDetails;
            set => SetProperty(ref _currentStepDetails, value);
        }

        private bool _isIndeterminate;
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetProperty(ref _isIndeterminate, value);
        }

        private bool _showStepDetails;
        public bool ShowStepDetails
        {
            get => _showStepDetails;
            set => SetProperty(ref _showStepDetails, value);
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await PrepareInstallationAsync();
        }

        private async Task PrepareInstallationAsync()
        {
            IsIndeterminate = true;
            StatusMessage = "Iniciando instalação...";
            ShowStepDetails = false;

            var progress = new Progress<InstallationProgress>(OnProgressChanged);

            try
            {
                var gameInfo = _stateService.GetGameFolder();
                if (gameInfo == null || !gameInfo.IsValid)
                {
                    // Se não há informação do jogo, volta para detecção
                    _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.GameDetection);
                    return;
                }

                IsIndeterminate = false;

                // Chama o método correto: InstallAsync (não PrepareAsync)
                var result = await _installationService.InstallAsync(gameInfo, progress);

                if (result.Success)
                {
                    var settings = _settingsService.Load();
                    settings.MarkAsInstalled(gameInfo.Path, gameInfo.Version);
                }

                _stateService.SetInstallationResult(result);

                // Navega para a tela de resultado
                _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.InstallationResult);
            }
            catch (Exception ex)
            {
                // Em caso de erro, cria um resultado de falha
                var errorResult = new InstallationResult
                {
                    Success = false,
                    Message = $"Erro durante a instalação: {ex.Message}",
                    Error = ex
                };

                _stateService.SetInstallationResult(errorResult);
                _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.InstallationResult);
            }
        }

        private void OnProgressChanged(InstallationProgress progress)
        {
            ProgressPercentage = progress.Percentage;
            StatusMessage = progress.Message;
            CurrentStepDetails = progress.Details;
            ShowStepDetails = !string.IsNullOrEmpty(progress.Details);
        }
    }
}