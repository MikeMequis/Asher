using Asher.Core;
using Asher.Core.Models;
using Asher.Localization;
using Asher.Services.Interfaces;
using Asher.UserInterface.Events;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Asher.UserInterface.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public ObservableCollection<NavigationItem> NavigationItems { get; }
        public ObservableCollection<NavigationItem> InstallationNavigationItems { get; }

        private NavigationItem _selectedNavigationItem;
        public NavigationItem SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set => SetProperty(ref _selectedNavigationItem, value);
        }

        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly INavigationItemsManager _navigationItemsManager;
        private readonly IGameInstallationService _installationService;
        private AsherSettings _settings;
        private string? _currentNavigationPath;
        private bool _startupNavigationPending;

        public MainWindowViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            INavigationItemsManager navigationItemsManager,
            IGameInstallationService installationService)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _navigationItemsManager = navigationItemsManager;
            _installationService = installationService;

            NavigationItems = new();
            InstallationNavigationItems = new();

            _settings = AsherSettings.Load();

            _eventAggregator.GetEvent<InstallationCompleteEvent>().Subscribe(OnInstallationComplete);
            _eventAggregator.GetEvent<UninstallCompleteEvent>().Subscribe(OnUninstallComplete);
            LocalizationManager.LanguageChanged += OnMainWindowLanguageChanged;

            InitializeNavigationItems();
        }

        public override Task InitAsync() => Task.CompletedTask;

        private DelegateCommand<NavigationItem> _navigateCommand;
        public DelegateCommand<NavigationItem> NavigateCommand =>
            _navigateCommand ??= new DelegateCommand<NavigationItem>(ExecuteNavigateCommand);

        private DelegateCommand _toggleSidebarCommand;
        public DelegateCommand ToggleSidebarCommand =>
            _toggleSidebarCommand ??= new DelegateCommand(ExecuteToggleSidebarCommand);

        private void ExecuteNavigateCommand(NavigationItem item)
        {
            if (item == null)
                return;

            var activeCollection = IsInstallationMode ? InstallationNavigationItems : NavigationItems;

            foreach (var navItem in activeCollection)
                navItem.IsSelected = navItem == item;

            SelectedNavigationItem = item;
            _currentNavigationPath = item.NavigationPath;

            _regionManager.RequestNavigate(RegionNames.Main, item.NavigationPath);
        }

        private void ExecuteToggleSidebarCommand()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }

        private bool _isInstallationMode;
        public bool IsInstallationMode
        {
            get => _isInstallationMode;
            set => SetProperty(ref _isInstallationMode, value);
        }

        private bool _isSidebarExpanded = true;
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set => SetProperty(ref _isSidebarExpanded, value);
        }

        private string _windowTitle = "Asher - Mod Manager";
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        private void InitializeNavigationItems()
        {
            var resolvedGamePath = ResolveInstalledGamePath();
            bool isInstalled = !string.IsNullOrEmpty(resolvedGamePath);

            if (isInstalled && (!_settings.IsInstalled || _settings.GameFolderPath != resolvedGamePath))
            {
                var gameInfo = new GameFolderInfo { Path = resolvedGamePath, Version = _settings.GameVersion };
                _settings.MarkAsInstalled(resolvedGamePath, gameInfo.Version ?? string.Empty);
            }

            if (!isInstalled)
            {
                IsInstallationMode = true;
                WindowTitle = LocalizationManager.Instance["Window_InstallTitle"];
                SetupInstallationNavigation(navigateToStart: false);
                _startupNavigationPending = true;
            }
            else
            {
                AsherPaths.MigrateLegacyLayout(resolvedGamePath!);
                IsInstallationMode = false;
                WindowTitle = LocalizationManager.Instance["Window_ManagerTitle"];
                SetupNormalNavigation(navigateToHome: false);
                _startupNavigationPending = true;
            }

            if (_startupNavigationPending)
            {
                if (IsInstallationMode)
                {
                    _currentNavigationPath = InstallationNavigationNames.Welcome;
                    RestoreNavigationSelection(InstallationNavigationItems, InstallationNavigationNames.Welcome);
                }
                else
                {
                    _currentNavigationPath = NavigationNames.Home;
                    RestoreNavigationSelection(NavigationItems, NavigationNames.Home);
                }
            }
        }

        public void PerformStartupNavigation()
        {
            if (!_startupNavigationPending)
                return;

            _startupNavigationPending = false;

            if (IsInstallationMode)
            {
                _currentNavigationPath = InstallationNavigationNames.Welcome;
                RestoreNavigationSelection(InstallationNavigationItems, InstallationNavigationNames.Welcome);
                _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.Welcome);
            }
            else
            {
                _currentNavigationPath = NavigationNames.Home;
                RestoreNavigationSelection(NavigationItems, NavigationNames.Home);
                _regionManager.RequestNavigate(RegionNames.Main, NavigationNames.Home);
            }
        }

        private void OnMainWindowLanguageChanged(object? sender, CultureInfo e)
        {
            WindowTitle = IsInstallationMode
                ? LocalizationManager.Instance["Window_InstallTitle"]
                : LocalizationManager.Instance["Window_ManagerTitle"];

            if (IsInstallationMode)
                RefreshInstallationNavigationLabels();
            else
                RefreshNormalNavigationLabels();
        }

        private string? ResolveInstalledGamePath()
        {
            var fromManagerLocation = AsherPaths.TryGetGameFolderFromManagerLocation();
            if (!string.IsNullOrEmpty(fromManagerLocation) && _installationService.IsInstalled(fromManagerLocation))
            {
                AsherPaths.MigrateLegacyLayout(fromManagerLocation);
                return fromManagerLocation;
            }

            if (!string.IsNullOrWhiteSpace(_settings.GameFolderPath)
                && _installationService.IsInstalled(_settings.GameFolderPath))
            {
                AsherPaths.MigrateLegacyLayout(_settings.GameFolderPath);
                return _settings.GameFolderPath;
            }

            return null;
        }

        private void SetupInstallationNavigation(bool navigateToStart)
        {
            if (InstallationNavigationItems.Count == 0)
            {
                _navigationItemsManager.CreateOptions(InstallationNavigationItems,
                    ("Welcome", LocalizationManager.Instance["Install_Welcome"], PackIconKind.Home, InstallationNavigationNames.Welcome, true),
                    ("Detect", LocalizationManager.Instance["Install_Detect"], PackIconKind.Magnify, InstallationNavigationNames.GameDetection, false),
                    ("Installing", LocalizationManager.Instance["Install_Installing"], PackIconKind.Download, InstallationNavigationNames.InstallationProgress, false),
                    ("Complete", LocalizationManager.Instance["Install_Complete"], PackIconKind.CheckCircle, InstallationNavigationNames.InstallationResult, false)
                );
            }
            else
            {
                RefreshInstallationNavigationLabels();
            }

            if (navigateToStart)
            {
                _currentNavigationPath = InstallationNavigationNames.Welcome;
                RestoreNavigationSelection(InstallationNavigationItems, InstallationNavigationNames.Welcome);
                _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.Welcome);
            }
            else
            {
                RestoreNavigationSelection(InstallationNavigationItems, _currentNavigationPath);
            }
        }

        private void SetupNormalNavigation(bool navigateToHome)
        {
            if (NavigationItems.Count == 0)
            {
                _navigationItemsManager.CreateOptions(NavigationItems,
                    ("Home", LocalizationManager.Instance["Nav_Home"], PackIconKind.Home, NavigationNames.Home, true),
                    ("ContentPatcher", LocalizationManager.Instance["Nav_ContentPatcher"], PackIconKind.ContentPaste, NavigationNames.ContentPatcher, true),
                    ("PatchManager", LocalizationManager.Instance["Nav_PatchManager"], PackIconKind.Puzzle, NavigationNames.PatchManager, true),
                    ("Settings", LocalizationManager.Instance["Nav_Settings"], PackIconKind.Settings, NavigationNames.Settings, true)
                );
            }
            else
            {
                RefreshNormalNavigationLabels();
            }

            if (navigateToHome)
            {
                _currentNavigationPath = NavigationNames.Home;
                RestoreNavigationSelection(NavigationItems, NavigationNames.Home);
                _regionManager.RequestNavigate(RegionNames.Main, NavigationNames.Home);
            }
            else
            {
                RestoreNavigationSelection(NavigationItems, _currentNavigationPath);
            }
        }

        private void RefreshInstallationNavigationLabels()
        {
            UpdateNavLabel(InstallationNavigationItems, InstallationNavigationNames.Welcome, "Install_Welcome");
            UpdateNavLabel(InstallationNavigationItems, InstallationNavigationNames.GameDetection, "Install_Detect");
            UpdateNavLabel(InstallationNavigationItems, InstallationNavigationNames.InstallationProgress, "Install_Installing");
            UpdateNavLabel(InstallationNavigationItems, InstallationNavigationNames.InstallationResult, "Install_Complete");
            RestoreNavigationSelection(InstallationNavigationItems, _currentNavigationPath);
        }

        private void RefreshNormalNavigationLabels()
        {
            UpdateNavLabel(NavigationItems, NavigationNames.Home, "Nav_Home");
            UpdateNavLabel(NavigationItems, NavigationNames.ContentPatcher, "Nav_ContentPatcher");
            UpdateNavLabel(NavigationItems, NavigationNames.PatchManager, "Nav_PatchManager");
            UpdateNavLabel(NavigationItems, NavigationNames.Settings, "Nav_Settings");
            RestoreNavigationSelection(NavigationItems, _currentNavigationPath);
        }

        private static void UpdateNavLabel(ObservableCollection<NavigationItem> items, string path, string resourceKey)
        {
            var item = items.FirstOrDefault(i => i.NavigationPath == path);
            if (item != null)
                item.Label = LocalizationManager.Instance[resourceKey];
        }

        private void RestoreNavigationSelection(ObservableCollection<NavigationItem> items, string? navigationPath)
        {
            if (string.IsNullOrWhiteSpace(navigationPath))
                return;

            foreach (var item in items)
                item.IsSelected = item.NavigationPath == navigationPath;

            SelectedNavigationItem = items.FirstOrDefault(i => i.NavigationPath == navigationPath) ?? SelectedNavigationItem;
        }

        private void OnInstallationComplete()
        {
            IsInstallationMode = false;
            WindowTitle = LocalizationManager.Instance["Window_ManagerTitle"];
            SetupNormalNavigation(navigateToHome: true);

            _settings = AsherSettings.Load();
        }

        private void OnUninstallComplete()
        {
            IsInstallationMode = true;
            WindowTitle = LocalizationManager.Instance["Window_InstallTitle"];
            _settings = AsherSettings.Load();
            SetupInstallationNavigation(navigateToStart: true);
        }
    }
}
