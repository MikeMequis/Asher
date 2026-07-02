using Asher.Core;
using Asher.Core.Models;
using Asher.Localization;
using Asher.Services.Interfaces;
using Asher.UserInterface.Events;
using System.Globalization;

namespace Asher.UserInterface.ViewModels
{
    public class UninstallProgressViewModel : BaseViewModel
    {
        private readonly IGameInstallationService _installationService;
        private readonly IGameLaunchService _gameLaunchService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;

        public UninstallProgressViewModel(
            IGameInstallationService installationService,
            IGameLaunchService gameLaunchService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator)
        {
            _installationService = installationService;
            _gameLaunchService = gameLaunchService;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
        }

        private double _progressPercentage;
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _currentStepDetails = string.Empty;
        public string CurrentStepDetails
        {
            get => _currentStepDetails;
            set => SetProperty(ref _currentStepDetails, value);
        }

        private bool _isIndeterminate = true;
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

        public string Title => this["Uninstall_Title"];
        public string Subtitle => this["Uninstall_Subtitle"];
        public string InfoMessage => this["Uninstall_Info"];

        public override Task InitAsync() => Task.CompletedTask;

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await RunUninstallAsync();
        }

        protected override void OnLanguageChanged(object? sender, CultureInfo newCulture)
        {
            base.OnLanguageChanged(sender, newCulture);
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Subtitle));
            RaisePropertyChanged(nameof(InfoMessage));
        }

        private async Task RunUninstallAsync()
        {
            IsIndeterminate = true;
            StatusMessage = this["Uninstall_Starting"];
            ShowStepDetails = false;

            var gameFolderPath = _gameLaunchService.ResolveGameFolderPath();
            if (string.IsNullOrWhiteSpace(gameFolderPath)
                || !_installationService.IsInstalled(gameFolderPath))
            {
                System.Windows.MessageBox.Show(
                    this["Uninstall_NotInstalled"],
                    this["Uninstall_Title"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                _regionManager.RequestNavigate(RegionNames.Main, NavigationNames.Settings);
                return;
            }

            if (!_installationService.HasRestorableBackup(gameFolderPath))
            {
                System.Windows.MessageBox.Show(
                    this["Uninstall_NoBackup"],
                    this["Uninstall_Title"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                _regionManager.RequestNavigate(RegionNames.Main, NavigationNames.Settings);
                return;
            }

            IsIndeterminate = false;
            var progress = new Progress<InstallationProgress>(OnProgressChanged);

            try
            {
                var result = await _installationService.UninstallAsync(gameFolderPath, progress);

                if (result.Success)
                {
                    var settings = AsherSettings.Load();
                    settings.MarkAsUninstalled();
                    _eventAggregator.GetEvent<UninstallCompleteEvent>().Publish();
                    return;
                }

                System.Windows.MessageBox.Show(
                    result.Message ?? this["Uninstall_Failed"],
                    this["Uninstall_Title"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.Message,
                    this["Uninstall_Title"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }

            _regionManager.RequestNavigate(RegionNames.Main, NavigationNames.Settings);
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
