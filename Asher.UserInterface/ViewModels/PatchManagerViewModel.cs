using System.Collections.ObjectModel;

namespace Asher.UserInterface.ViewModels
{
    public class PatchManagerViewModel : BaseViewModel
    {
        public ObservableCollection<PatchInfo> AvailablePatches { get; } = new();

        private int _activePatchCount;
        public int ActivePatchCount
        {
            get => _activePatchCount;
            set => SetProperty(ref _activePatchCount, value);
        }

        private int _totalPatchCount;
        public int TotalPatchCount
        {
            get => _totalPatchCount;
            set => SetProperty(ref _totalPatchCount, value);
        }

        public PatchManagerViewModel()
        {
            LoadSamplePatches();
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            ExecuteRefreshPatchesCommand();
        }

        private DelegateCommand _refreshPatchesCommand;
        public DelegateCommand RefreshPatchesCommand => 
            _refreshPatchesCommand ??= new DelegateCommand(ExecuteRefreshPatchesCommand);

        private void ExecuteRefreshPatchesCommand()
        {
            LoadSamplePatches();
        }

        private void LoadSamplePatches()
        {
            AvailablePatches.Clear();

            // TODO: Implement actual patch discovery
            // Sample patches for demonstration
            AvailablePatches.Add(new PatchInfo
            {
                Name = "Debug Enabler",
                Description = "Enables the debug console on startup",
                IsEnabled = true
            });
            
            AvailablePatches.Add(new PatchInfo
            {
                Name = "Fullscreen Switch",
                Description = "Adds fullscreen toggle functionality",
                IsEnabled = false
            });
            
            AvailablePatches.Add(new PatchInfo
            {
                Name = "Skip Intro",
                Description = "Skips the intro sequence",
                IsEnabled = true
            });

            UpdatePatchCounts();
        }

        private void UpdatePatchCounts()
        {
            TotalPatchCount = AvailablePatches.Count;
            ActivePatchCount = AvailablePatches.Count(p => p.IsEnabled);
        }
    }

    public class PatchInfo : BindableBase
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _isEnabled;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }
}
