using Asher.Core;
using Asher.Localization;
using Asher.Services.Interfaces;
using Asher.UserInterface.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Asher.UserInterface.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IGameFolderService _gameFolderService;
        private readonly IGameLaunchService _gameLaunchService;
        private readonly IGameInstallationService _installationService;
        private readonly IRegionManager _regionManager;
        private readonly IThemeService _themeService;
        private bool _isApplyingSettings;

        private string _gamePath = string.Empty;
        public string GamePath
        {
            get => _gamePath;
            set => SetProperty(ref _gamePath, value);
        }

        private bool _autoLaunchEnabled = true;
        public bool AutoLaunchEnabled
        {
            get => _autoLaunchEnabled;
            set => SetProperty(ref _autoLaunchEnabled, value);
        }

        private bool _backupEnabled = true;
        public bool BackupEnabled
        {
            get => _backupEnabled;
            set => SetProperty(ref _backupEnabled, value);
        }

        private string _selectedLanguage = "English";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (!SetProperty(ref _selectedLanguage, value) || _isApplyingSettings)
                    return;

                ApplyLanguage(value);
                ExecuteSaveSettingsCommand();
            }
        }

        private string _selectedTheme = "Light";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (!SetProperty(ref _selectedTheme, value) || _isApplyingSettings)
                    return;

                ApplyTheme(value);
                ExecuteSaveSettingsCommand();
            }
        }

        private bool _checkForUpdatesEnabled = true;
        public bool CheckForUpdatesEnabled
        {
            get => _checkForUpdatesEnabled;
            set => SetProperty(ref _checkForUpdatesEnabled, value);
        }

        public ObservableCollection<string> AvailableLanguages { get; } = new()
        {
            "English",
            "Portuguese (Brazil)",
            "Spanish"
        };

        public ObservableCollection<string> AvailableThemes { get; } = new()
        {
            "Light",
            "Dark"
        };

        public string Title => this["Settings_Title"];
        public string Subtitle => this["Settings_Subtitle"];
        public string GameSettingsLabel => this["Settings_GameSettings"];
        public string GamePathLabel => this["Settings_GamePath"];
        public string BrowseLabel => this["Settings_Browse"];
        public string AutoLaunchLabel => this["Settings_AutoLaunch"];
        public string BackupLabel => this["Settings_Backup"];
        public string AppSettingsLabel => this["Settings_AppSettings"];
        public string LanguageLabel => this["Settings_Language"];
        public string ThemeLabel => this["Settings_Theme"];
        public string CheckUpdatesLabel => this["Settings_CheckUpdates"];
        public string ActionsLabel => this["Settings_Actions"];
        public string ResetLabel => this["Settings_Reset"];
        public string SaveLabel => this["Settings_Save"];
        public string UninstallSectionLabel => this["Settings_UninstallSection"];
        public string UninstallDescription => this["Settings_UninstallDescription"];
        public string UninstallLabel => this["Settings_Uninstall"];
        public string UninstallConfirmTitle => this["Settings_UninstallConfirmTitle"];
        public string UninstallConfirmMessage => this["Settings_UninstallConfirmMessage"];

        private bool _canUninstall;
        public bool CanUninstall
        {
            get => _canUninstall;
            set => SetProperty(ref _canUninstall, value);
        }

        public SettingsViewModel(
            IGameFolderService gameFolderService,
            IGameLaunchService gameLaunchService,
            IGameInstallationService installationService,
            IRegionManager regionManager,
            IThemeService themeService)
        {
            _gameFolderService = gameFolderService;
            _gameLaunchService = gameLaunchService;
            _installationService = installationService;
            _regionManager = regionManager;
            _themeService = themeService;
            InitializeSettings();
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedFrom(NavigationContext navigationContext) => ExecuteSaveSettingsCommand();

        public override void OnNavigatedTo(NavigationContext navigationContext) => InitializeSettings();

        protected override void OnLanguageChanged(object? sender, CultureInfo newCulture)
        {
            base.OnLanguageChanged(sender, newCulture);
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Subtitle));
            RaisePropertyChanged(nameof(GameSettingsLabel));
            RaisePropertyChanged(nameof(GamePathLabel));
            RaisePropertyChanged(nameof(BrowseLabel));
            RaisePropertyChanged(nameof(AutoLaunchLabel));
            RaisePropertyChanged(nameof(BackupLabel));
            RaisePropertyChanged(nameof(AppSettingsLabel));
            RaisePropertyChanged(nameof(LanguageLabel));
            RaisePropertyChanged(nameof(ThemeLabel));
            RaisePropertyChanged(nameof(CheckUpdatesLabel));
            RaisePropertyChanged(nameof(ActionsLabel));
            RaisePropertyChanged(nameof(ResetLabel));
            RaisePropertyChanged(nameof(SaveLabel));
            RaisePropertyChanged(nameof(UninstallSectionLabel));
            RaisePropertyChanged(nameof(UninstallDescription));
            RaisePropertyChanged(nameof(UninstallLabel));
            RaisePropertyChanged(nameof(UninstallConfirmTitle));
            RaisePropertyChanged(nameof(UninstallConfirmMessage));
        }

        private DelegateCommand? _browseGamePathCommand;
        public DelegateCommand BrowseGamePathCommand =>
            _browseGamePathCommand ??= new DelegateCommand(ExecuteBrowseGamePathCommand);

        private DelegateCommand? _resetToDefaultsCommand;
        public DelegateCommand ResetToDefaultsCommand =>
            _resetToDefaultsCommand ??= new DelegateCommand(ExecuteResetToDefaultsCommand);

        private DelegateCommand? _saveSettingsCommand;
        public DelegateCommand SaveSettingsCommand =>
            _saveSettingsCommand ??= new DelegateCommand(ExecuteSaveSettingsCommand);

        private DelegateCommand? _uninstallCommand;
        public DelegateCommand UninstallCommand =>
            _uninstallCommand ??= new DelegateCommand(ExecuteUninstallCommand, () => CanUninstall)
                .ObservesProperty(() => CanUninstall);

        private void ExecuteBrowseGamePathCommand()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Dust: An Elysian Tail game folder"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var gameInfo = _gameFolderService.GetInfo(dialog.SelectedPath);
                if (gameInfo.IsValid)
                {
                    GamePath = gameInfo.Path;
                    if (!gameInfo.HasPatchesFolder)
                        _gameFolderService.CreatePatchesFolder(gameInfo.Path);
                }
            }
        }

        private void ExecuteResetToDefaultsCommand()
        {
            _isApplyingSettings = true;
            GamePath = _gameLaunchService.ResolveGameFolderPath() ?? string.Empty;
            AutoLaunchEnabled = true;
            BackupEnabled = true;
            SelectedLanguage = "English";
            SelectedTheme = "Light";
            CheckForUpdatesEnabled = true;
            _isApplyingSettings = false;

            ApplyLanguage(SelectedLanguage);
            ApplyTheme(SelectedTheme);
            ExecuteSaveSettingsCommand();
        }

        private void ExecuteUninstallCommand()
        {
            var confirm = System.Windows.MessageBox.Show(
                UninstallConfirmMessage,
                UninstallConfirmTitle,
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes)
                return;

            _regionManager.RequestNavigate(RegionNames.Main, NavigationNames.UninstallProgress);
        }

        private void ExecuteSaveSettingsCommand()
        {
            var settings = AsherSettings.Load();
            settings.GameFolderPath = GamePath;
            settings.AutoLaunchEnabled = AutoLaunchEnabled;
            settings.BackupEnabled = BackupEnabled;
            settings.Theme = SelectedTheme;
            settings.CheckForUpdatesEnabled = CheckForUpdatesEnabled;
            settings.Language = LocalizationManager.GetCultureNameFromDisplay(SelectedLanguage);
            settings.Save();
        }

        private void InitializeSettings()
        {
            _isApplyingSettings = true;

            var settings = AsherSettings.Load();
            GamePath = _gameLaunchService.ResolveGameFolderPath()
                ?? settings.GameFolderPath
                ?? string.Empty;
            AutoLaunchEnabled = settings.AutoLaunchEnabled;
            BackupEnabled = settings.BackupEnabled;
            SelectedTheme = string.IsNullOrWhiteSpace(settings.Theme) ? "Light" : settings.Theme;
            CheckForUpdatesEnabled = settings.CheckForUpdatesEnabled;
            SelectedLanguage = LocalizationManager.GetCultureDisplayName(
                LocalizationManager.Instance.UILanguage);

            if (!string.IsNullOrWhiteSpace(settings.Language))
                ApplyLanguage(LocalizationManager.GetCultureDisplayName(
                    CultureInfo.GetCultureInfo(settings.Language)));

            ApplyTheme(SelectedTheme);
            UpdateUninstallAvailability();

            _isApplyingSettings = false;
        }

        private void UpdateUninstallAvailability()
        {
            var gameFolderPath = _gameLaunchService.ResolveGameFolderPath() ?? GamePath;
            CanUninstall = !string.IsNullOrWhiteSpace(gameFolderPath)
                && _installationService.IsInstalled(gameFolderPath)
                && _installationService.HasRestorableBackup(gameFolderPath);
        }

        private void ApplyTheme(string themeName)
        {
            _themeService.Apply(themeName);
        }

        private static void ApplyLanguage(string displayLanguage)
        {
            var cultureName = LocalizationManager.GetCultureNameFromDisplay(displayLanguage);
            LocalizationManager.Instance.ApplyCulture(cultureName);
        }
    }
}
