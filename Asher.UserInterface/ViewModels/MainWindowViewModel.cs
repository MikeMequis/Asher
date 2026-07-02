using Asher.Core;
using Asher.Core.Models;
using Asher.Services.Interfaces;
using Asher.UserInterface.Events;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

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

            // Carrega configurações
            _settings = AsherSettings.Load();

            // Inscreve-se no evento de instalação completa
            _eventAggregator.GetEvent<InstallationCompleteEvent>().Subscribe(OnInstallationComplete);

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

            // Atualiza seleção nos items ativos
            var activeCollection = IsInstallationMode ? InstallationNavigationItems : NavigationItems;

            foreach (var navItem in activeCollection)
                navItem.IsSelected = navItem == item;

            SelectedNavigationItem = item;

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
            // Verifica se já está instalado usando settings E validação real
            bool isInstalled = _settings.IsInstalled &&
                               !string.IsNullOrEmpty(_settings.GameFolderPath) &&
                               _installationService.IsInstalled(_settings.GameFolderPath);

            if (!isInstalled)
            {
                // Modo de instalação
                IsInstallationMode = true;
                WindowTitle = "Asher - Instalação";
                SetupInstallationNavigation();
            }
            else
            {
                // Modo normal
                IsInstallationMode = false;
                WindowTitle = "Asher - Mod Manager";
                SetupNormalNavigation();
            }
        }

        private void SetupInstallationNavigation()
        {
            InstallationNavigationItems.Clear();

            _navigationItemsManager.CreateOptions(InstallationNavigationItems,
                ("Bem-vindo", "Bem-vindo", PackIconKind.Home, InstallationNavigationNames.Welcome, true),
                ("Detectar Jogo", "Detectar Jogo", PackIconKind.Magnify, InstallationNavigationNames.GameDetection, false),
                ("Instalando", "Instalando", PackIconKind.Download, InstallationNavigationNames.InstallationProgress, false),
                ("Concluído", "Concluído", PackIconKind.CheckCircle, InstallationNavigationNames.InstallationResult, false)
            );

            // IMPORTANTE: Navega para a tela de boas-vindas
            _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.Welcome);
        }

        private void SetupNormalNavigation()
        {
            NavigationItems.Clear();

            _navigationItemsManager.CreateOptions(NavigationItems,
                ("Home", "Home", PackIconKind.Home, NavigationNames.Home, true),
                ("ContentPatcher", "Content Patcher", PackIconKind.ContentPaste, NavigationNames.ContentPatcher, true),
                ("PatchManager", "Patch Manager", PackIconKind.Puzzle, NavigationNames.PatchManager, true),
                ("Settings", "Settings", PackIconKind.Settings, NavigationNames.Settings, true)
            );

            // Navega para a home
            _regionManager.RequestNavigate(RegionNames.Main, NavigationNames.Home);
        }

        private void OnInstallationComplete()
        {
            // Quando a instalação é completada, muda para o modo normal
            IsInstallationMode = false;
            WindowTitle = "Asher - Mod Manager";
            SetupNormalNavigation();

            // As configurações já devem ter sido salvas pelo InstallationResultViewModel
            // Mas vamos recarregar para garantir
            _settings = AsherSettings.Load();
        }
    }
}