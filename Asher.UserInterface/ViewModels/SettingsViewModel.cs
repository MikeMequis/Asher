using Asher.Services.Interfaces;
using System.Collections.ObjectModel;

namespace Asher.UserInterface.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly IGameFolderService _gameFolderService;

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
            set => SetProperty(ref _selectedLanguage, value);
        }

        private string _selectedTheme = "Light";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
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
        "Spanish",
    };

        public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "Light",
        "Dark"
    };

        public SettingsViewModel(IGameFolderService gameFolderService)
        {
            _gameFolderService = gameFolderService;
            InitializeSettings();
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            ExecuteSaveSettingsCommand();
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            InitializeSettings();
        }

        private DelegateCommand _browseGamePathCommand;
        public DelegateCommand BrowseGamePathCommand =>
            _browseGamePathCommand ??= new DelegateCommand(ExecuteBrowseGamePathCommand);

        private DelegateCommand _resetToDefaultsCommand;
        public DelegateCommand ResetToDefaultsCommand =>
            _resetToDefaultsCommand ??= new DelegateCommand(ExecuteResetToDefaultsCommand);

        private DelegateCommand _saveSettingsCommand;
        public DelegateCommand SaveSettingsCommand =>
            _saveSettingsCommand ??= new DelegateCommand(ExecuteSaveSettingsCommand);

        private void ExecuteBrowseGamePathCommand()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Dust: An Elysian Tail game folder";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var gameInfo = _gameFolderService.GetInfo(dialog.SelectedPath);

                    if (gameInfo.IsValid)
                    {
                        GamePath = gameInfo.Path;
                        // Optionally create patches folder if it doesn't exist
                        if (!gameInfo.HasPatchesFolder)
                            _gameFolderService.CreatePatchesFolder(gameInfo.Path);
                    }
                    else
                    {
                        // TODO: Show error message to user
                        // Invalid game folder selected
                    }
                }
            }
        }

        private void ExecuteResetToDefaultsCommand()
        {
            GamePath = string.Empty;
            AutoLaunchEnabled = true;
            BackupEnabled = true;
            SelectedLanguage = "English";
            SelectedTheme = "Light";
            CheckForUpdatesEnabled = true;
        }

        private void ExecuteSaveSettingsCommand()
        {
            // TODO: Implement settings persistence
            // This will be implemented when we add configuration management
        }

        private void InitializeSettings()
        {
            // TODO: Load settings from configuration
            // For now, use default values
        }
    }
}
