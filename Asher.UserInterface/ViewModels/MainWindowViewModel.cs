using Asher.Core.Models;
using Asher.Services.Interfaces;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace Asher.UserInterface.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
        public ObservableCollection<NavigationItem> InstallationNavigationItems { get; } = new();

        private bool _isSidebarExpanded = true;
        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set => SetProperty(ref _isSidebarExpanded, value);
        }

        private NavigationItem? _selectedNavigationItem;
        public NavigationItem? SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set => SetProperty(ref _selectedNavigationItem, value);
        }

        private readonly IRegionManager _regionManager;
        private readonly INavigationItemsManager _navigationItemsManager;

        public MainWindowViewModel(IRegionManager regionManager,
                                   INavigationItemsManager navigationItemsManager)
        {
            _regionManager = regionManager;
            _navigationItemsManager = navigationItemsManager;

            InitializeNavigationItems();
            NavigateToHome();
        }

        public override Task InitAsync() => Task.CompletedTask;

        private DelegateCommand _toggleSidebarCommand;
        public DelegateCommand ToggleSidebarCommand =>
            _toggleSidebarCommand ??= new DelegateCommand(ExecuteToggleSidebarCommand);

        private DelegateCommand<NavigationItem> _navigateCommand;
        public DelegateCommand<NavigationItem> NavigateCommand =>
            _navigateCommand ??= new DelegateCommand<NavigationItem>(ExecuteNavigateCommand);

        private void ExecuteToggleSidebarCommand() => IsSidebarExpanded = !IsSidebarExpanded;

        private void ExecuteNavigateCommand(NavigationItem? item)
        {
            if (item == null)
                return;

            foreach (var navItem in NavigationItems)
                navItem.IsSelected = navItem == item;

            SelectedNavigationItem = item;

            _regionManager.RequestNavigate(RegionNames.Main, item.NavigationPath);
        }

        private void InitializeNavigationItems()
        {
            if (false)
            {
                InstallationNavigationItems.Clear();

                _navigationItemsManager.CreateOptions(InstallationNavigationItems,
                    ("Home", "Home", PackIconKind.Home, InstallationNavigationNames.Welcome, true),
                    ("ContentPatcher", "Content Patcher", PackIconKind.ContentPaste, InstallationNavigationNames.GameDetection, false),
                    ("PatchManager", "Patch Manager", PackIconKind.Puzzle, InstallationNavigationNames.InstallationProgress, false),
                    ("Settings", "Settings", PackIconKind.Settings, InstallationNavigationNames.InstallationResult, false)
                    );
            }
            else
            {
                NavigationItems.Clear();

                _navigationItemsManager.CreateOptions(NavigationItems,
                        ("Home", "Home", PackIconKind.Home, NavigationNames.Home, true),
                        ("ContentPatcher", "Content Patcher", PackIconKind.ContentPaste, NavigationNames.ContentPatcher, true),
                        ("PatchManager", "Patch Manager", PackIconKind.Puzzle, NavigationNames.PatchManager, true),
                        ("Settings", "Settings", PackIconKind.Settings, NavigationNames.Settings, true)
                        );
            }
        }

        private void NavigateToHome()
        {
            var homeItem = NavigationItems.FirstOrDefault
                (x => x.NavigationPath == NavigationNames.Home);

            if (homeItem != null)
                ExecuteNavigateCommand(homeItem);
        }
    }
}
