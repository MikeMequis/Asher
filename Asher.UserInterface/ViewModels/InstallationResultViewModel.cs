using Asher.Core;
using Asher.Localization;
using Asher.Services.Interfaces;
using Asher.UserInterface.Events;
using System.Globalization;

namespace Asher.UserInterface.ViewModels
{
    public class InstallationResultViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInstallationStateService _stateService;
        private readonly IRegionManager _regionManager;
        private readonly IShortcutService _shortcutService;
        private readonly IManagerLaunchService _managerLaunchService;
        private readonly IManagerDeployService _managerDeployService;

        public InstallationResultViewModel(
            IEventAggregator eventAggregator,
            IInstallationStateService stateService,
            IRegionManager regionManager,
            IShortcutService shortcutService,
            IManagerLaunchService managerLaunchService,
            IManagerDeployService managerDeployService)
        {
            _eventAggregator = eventAggregator;
            _stateService = stateService;
            _regionManager = regionManager;
            _shortcutService = shortcutService;
            _managerLaunchService = managerLaunchService;
            _managerDeployService = managerDeployService;
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        private string _headerTitle = string.Empty;
        public string HeaderTitle
        {
            get => _headerTitle;
            set => SetProperty(ref _headerTitle, value);
        }

        private string _headerSubtitle = string.Empty;
        public string HeaderSubtitle
        {
            get => _headerSubtitle;
            set => SetProperty(ref _headerSubtitle, value);
        }

        private string _resultMessage = string.Empty;
        public string ResultMessage
        {
            get => _resultMessage;
            set => SetProperty(ref _resultMessage, value);
        }

        private string _errorDetails = string.Empty;
        public string ErrorDetails
        {
            get => _errorDetails;
            set => SetProperty(ref _errorDetails, value);
        }

        private bool _hasErrorDetails;
        public bool HasErrorDetails
        {
            get => _hasErrorDetails;
            set => SetProperty(ref _hasErrorDetails, value);
        }

        private bool _createDesktopShortcut = true;
        public bool CreateDesktopShortcut
        {
            get => _createDesktopShortcut;
            set => SetProperty(ref _createDesktopShortcut, value);
        }

        public string ReadyTitle => this["InstallResult_ReadyTitle"];
        public string ErrorHeading => this["InstallResult_ErrorHeading"];
        public string ErrorDetailsLabel => this["InstallResult_ErrorDetails"];
        public string FinishLabel => this["InstallResult_Finish"];
        public string RetryLabel => this["InstallResult_Retry"];
        public string CancelLabel => this["InstallResult_Cancel"];
        public string CreateShortcutLabel => this["InstallResult_CreateShortcut"];
        public string NextStepsLabel => this["InstallResult_NextSteps"];
        public string StepModsLabel => this["InstallResult_StepMods"];
        public string StepContentPatcherLabel => this["InstallResult_StepContentPatcher"];
        public string StepEnjoyLabel => this["InstallResult_StepEnjoy"];

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            var result = _stateService.GetInstallationResult();

            if (result == null)
            {
                _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.Welcome);
                return;
            }

            IsSuccess = result.Success;
            ApplyLocalizedContent(result);
        }

        protected override void OnLanguageChanged(object? sender, CultureInfo newCulture)
        {
            base.OnLanguageChanged(sender, newCulture);

            var result = _stateService.GetInstallationResult();
            if (result != null)
                ApplyLocalizedContent(result);

            RaisePropertyChanged(nameof(ReadyTitle));
            RaisePropertyChanged(nameof(ErrorHeading));
            RaisePropertyChanged(nameof(ErrorDetailsLabel));
            RaisePropertyChanged(nameof(FinishLabel));
            RaisePropertyChanged(nameof(RetryLabel));
            RaisePropertyChanged(nameof(CancelLabel));
            RaisePropertyChanged(nameof(CreateShortcutLabel));
            RaisePropertyChanged(nameof(NextStepsLabel));
            RaisePropertyChanged(nameof(StepModsLabel));
            RaisePropertyChanged(nameof(StepContentPatcherLabel));
            RaisePropertyChanged(nameof(StepEnjoyLabel));
        }

        private void ApplyLocalizedContent(Core.Models.InstallationResult result)
        {
            if (result.Success)
            {
                HeaderTitle = this["InstallResult_SuccessTitle"];
                HeaderSubtitle = this["InstallResult_SuccessSubtitle"];
                ResultMessage = this["InstallResult_SuccessMessage"];
                HasErrorDetails = false;
                ErrorDetails = string.Empty;
            }
            else
            {
                HeaderTitle = this["InstallResult_ErrorTitle"];
                HeaderSubtitle = this["InstallResult_ErrorSubtitle"];
                ResultMessage = result.Message ?? this["InstallResult_UnknownError"];

                if (result.Error != null)
                {
                    ErrorDetails = result.Error.ToString();
                    HasErrorDetails = true;
                }
                else
                {
                    HasErrorDetails = false;
                    ErrorDetails = string.Empty;
                }
            }
        }

        private DelegateCommand? _finishInstallationCommand;
        public DelegateCommand FinishInstallationCommand =>
            _finishInstallationCommand ??= new DelegateCommand(ExecuteFinishInstallationCommand);

        private DelegateCommand? _retryInstallationCommand;
        public DelegateCommand RetryInstallationCommand =>
            _retryInstallationCommand ??= new DelegateCommand(ExecuteRetryInstallationCommand);

        private DelegateCommand? _cancelInstallationCommand;
        public DelegateCommand CancelInstallationCommand =>
            _cancelInstallationCommand ??= new DelegateCommand(ExecuteCancelInstallationCommand);

        private void ExecuteFinishInstallationCommand()
        {
            var result = _stateService.GetInstallationResult();
            string? gameFolderPath = null;

            if (result?.Success == true)
            {
                var gameInfo = _stateService.GetGameFolder();
                var settings = AsherSettings.Load();

                if (gameInfo != null)
                {
                    gameFolderPath = gameInfo.Path;
                    settings.MarkAsInstalled(gameInfo.Path, gameInfo.Version);
                }
                else if (!string.IsNullOrWhiteSpace(result.GameFolderPath))
                {
                    gameFolderPath = result.GameFolderPath;
                    settings.MarkAsInstalled(result.GameFolderPath, settings.GameVersion);
                }

                if (CreateDesktopShortcut && !string.IsNullOrWhiteSpace(gameFolderPath))
                {
                    var managerExe = _managerLaunchService.GetInstalledManagerPath(gameFolderPath);
                    _shortcutService.TryCreateDesktopShortcut(managerExe, "Asher", out _);
                }

                if (!string.IsNullOrWhiteSpace(gameFolderPath)
                    && _managerDeployService.HasPendingPayload(gameFolderPath)
                    && _managerDeployService.IsRunningFromManagerOf(gameFolderPath))
                {
                    if (_managerLaunchService.TryRestartCurrentManager(out _))
                    {
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(gameFolderPath)
                    && _managerLaunchService.ShouldRelaunchAfterInstall(gameFolderPath)
                    && _managerLaunchService.TryRelaunchManager(gameFolderPath, out _))
                {
                    System.Windows.Application.Current.Shutdown();
                    return;
                }
            }

            _eventAggregator.GetEvent<InstallationCompleteEvent>().Publish();
        }

        private void ExecuteRetryInstallationCommand()
        {
            _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.GameDetection);
        }

        private void ExecuteCancelInstallationCommand()
        {
            _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.Welcome);
        }
    }
}
