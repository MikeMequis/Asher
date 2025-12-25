using Asher.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace Asher.UserInterface.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

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

        public MainWindowViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            
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
            NavigationItems.Clear();

            NavigationItems.Add(new NavigationItem
            {
                Name = "Home",
                Label = "Home",
                Icon = PackIconKind.Home,
                NavigationPath = NavigationNames.Home
            });

            NavigationItems.Add(new NavigationItem
            {
                Name = "ContentPatcher",
                Label = "Content Patcher",
                Icon = PackIconKind.ContentPaste,
                NavigationPath = NavigationNames.ContentPatcher
            });

            NavigationItems.Add(new NavigationItem
            {
                Name = "PatchManager",
                Label = "Patch Manager",
                Icon = PackIconKind.Puzzle,
                NavigationPath = NavigationNames.PatchManager
            });

            NavigationItems.Add(new NavigationItem
            {
                Name = "Settings",
                Label = "Settings",
                Icon = PackIconKind.Settings,
                NavigationPath = NavigationNames.Settings
            });
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
