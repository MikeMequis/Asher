using Asher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace Asher.UserInterface.ViewModels
{
    public class PatchManagerViewModel : BaseViewModel
    {
        private readonly IPatchManagerService _patchManagerService;

        public ObservableCollection<ManagedModItemViewModel> AvailablePatches { get; } = new();

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

        public string Title => this["PatchManager_Title"];
        public string Subtitle => this["PatchManager_Subtitle"];
        public string AvailablePatchesLabel => this["PatchManager_AvailablePatches"];
        public string RefreshLabel => this["PatchManager_Refresh"];
        public string EmptyState => this["PatchManager_Empty"];
        public string ActivePatchesLabel => this["PatchManager_ActivePatches"];
        public string TotalPatchesLabel => this["PatchManager_TotalPatches"];
        public string PatchInfoLabel => this["PatchManager_PatchInfo"];

        public PatchManagerViewModel(IPatchManagerService patchManagerService)
        {
            _patchManagerService = patchManagerService;
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            ExecuteRefreshPatchesCommand();
        }

        protected override void OnLanguageChanged(object sender, CultureInfo newCulture)
        {
            base.OnLanguageChanged(sender, newCulture);
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Subtitle));
            RaisePropertyChanged(nameof(AvailablePatchesLabel));
            RaisePropertyChanged(nameof(RefreshLabel));
            RaisePropertyChanged(nameof(EmptyState));
            RaisePropertyChanged(nameof(ActivePatchesLabel));
            RaisePropertyChanged(nameof(TotalPatchesLabel));
            RaisePropertyChanged(nameof(PatchInfoLabel));
        }

        private DelegateCommand? _refreshPatchesCommand;
        public DelegateCommand RefreshPatchesCommand =>
            _refreshPatchesCommand ??= new DelegateCommand(() => _ = LoadPatchesAsync());

        private async Task LoadPatchesAsync()
        {
            foreach (var patch in AvailablePatches)
                patch.PropertyChanged -= OnPatchPropertyChanged;

            AvailablePatches.Clear();

            var mods = await _patchManagerService.GetModsAsync();
            foreach (var mod in mods)
            {
                var item = ManagedModItemViewModel.From(mod);
                item.PropertyChanged += OnPatchPropertyChanged;
                AvailablePatches.Add(item);
            }

            UpdatePatchCounts();
        }

        private async void OnPatchPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ManagedModItemViewModel mod || e.PropertyName != nameof(ManagedModItemViewModel.IsEnabled))
                return;

            await _patchManagerService.SetModEnabledAsync(mod.FileName, mod.IsEnabled);
            UpdatePatchCounts();
        }

        private void ExecuteRefreshPatchesCommand() => _ = LoadPatchesAsync();

        private void UpdatePatchCounts()
        {
            TotalPatchCount = AvailablePatches.Count;
            ActivePatchCount = AvailablePatches.Count(p => p.IsEnabled);
        }
    }
}
