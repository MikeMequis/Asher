using Asher.Core.Models;
using Asher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace Asher.UserInterface.ViewModels
{
    public class ContentPatcherViewModel : BaseViewModel
    {
        private readonly IContentPatcherService _contentPatcherService;

        public ObservableCollection<ContentReplacementInfo> Replacements { get; } = new();

        private string _originalAssetPath = string.Empty;
        public string OriginalAssetPath
        {
            get => _originalAssetPath;
            set
            {
                SetProperty(ref _originalAssetPath, value);
                AddReplacementCommand.RaiseCanExecuteChanged();
            }
        }

        private string _replacementAssetPath = string.Empty;
        public string ReplacementAssetPath
        {
            get => _replacementAssetPath;
            set
            {
                SetProperty(ref _replacementAssetPath, value);
                AddReplacementCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _hasReplacements;
        public bool HasReplacements
        {
            get => _hasReplacements;
            set => SetProperty(ref _hasReplacements, value);
        }

        public string Title => this["Nav_ContentPatcher"];
        public string Subtitle => this["Home_ContentPatcherDesc"];
        public string OriginalAssetLabel => this["ContentPatcher_OriginalAsset"];
        public string ReplacementAssetLabel => this["ContentPatcher_ReplacementAsset"];
        public string BrowseReplacementLabel => this["ContentPatcher_BrowseReplacement"];
        public string AddReplacementLabel => this["ContentPatcher_AddReplacement"];
        public string ActiveReplacementsLabel => this["ContentPatcher_ActiveReplacements"];
        public string EmptyStateLabel => this["ContentPatcher_EmptyState"];
        public string AppliedOnLaunchLabel => this["ContentPatcher_AppliedOnLaunch"];
        public string RemoveLabel => this["ContentPatcher_Remove"];

        public ContentPatcherViewModel(IContentPatcherService contentPatcherService)
        {
            _contentPatcherService = contentPatcherService;
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            _ = LoadReplacementsAsync();
        }

        protected override void OnLanguageChanged(object? sender, CultureInfo newCulture)
        {
            base.OnLanguageChanged(sender, newCulture);
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(Subtitle));
            RaisePropertyChanged(nameof(OriginalAssetLabel));
            RaisePropertyChanged(nameof(ReplacementAssetLabel));
            RaisePropertyChanged(nameof(BrowseReplacementLabel));
            RaisePropertyChanged(nameof(AddReplacementLabel));
            RaisePropertyChanged(nameof(ActiveReplacementsLabel));
            RaisePropertyChanged(nameof(EmptyStateLabel));
            RaisePropertyChanged(nameof(AppliedOnLaunchLabel));
            RaisePropertyChanged(nameof(RemoveLabel));
        }

        private DelegateCommand? _browseReplacementCommand;
        public DelegateCommand BrowseReplacementCommand =>
            _browseReplacementCommand ??= new DelegateCommand(ExecuteBrowseReplacementCommand);

        private void ExecuteBrowseReplacementCommand()
        {
            using var dialog = new OpenFileDialog
            {
                Title = ReplacementAssetLabel,
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ReplacementAssetPath = dialog.FileName;
        }

        private DelegateCommand? _addReplacementCommand;
        public DelegateCommand AddReplacementCommand =>
            _addReplacementCommand ??= new DelegateCommand(
                () => _ = ExecuteAddReplacementCommandAsync(),
                CanExecuteAddReplacementCommand);

        private async Task ExecuteAddReplacementCommandAsync()
        {
            var success = await _contentPatcherService.AddReplacementAsync(
                OriginalAssetPath,
                ReplacementAssetPath);

            if (success)
            {
                OriginalAssetPath = string.Empty;
                ReplacementAssetPath = string.Empty;
                await LoadReplacementsAsync();
            }
        }

        private bool CanExecuteAddReplacementCommand()
        {
            return !string.IsNullOrWhiteSpace(OriginalAssetPath)
                && !string.IsNullOrWhiteSpace(ReplacementAssetPath);
        }

        private DelegateCommand<ContentReplacementInfo>? _removeReplacementCommand;
        public DelegateCommand<ContentReplacementInfo> RemoveReplacementCommand =>
            _removeReplacementCommand ??= new DelegateCommand<ContentReplacementInfo>(
                replacement => _ = RemoveReplacementAsync(replacement));

        private async Task RemoveReplacementAsync(ContentReplacementInfo? replacement)
        {
            if (replacement == null)
                return;

            if (await _contentPatcherService.RemoveReplacementAsync(replacement.Target))
                await LoadReplacementsAsync();
        }

        private async Task ToggleReplacementAsync(ContentReplacementInfo? replacement)
        {
            if (replacement == null)
                return;

            if (await _contentPatcherService.SetReplacementEnabledAsync(
                    replacement.Target,
                    replacement.IsEnabled))
            {
                return;
            }

            replacement.IsEnabled = !replacement.IsEnabled;
        }

        private bool _suppressToggle;

        private async Task LoadReplacementsAsync()
        {
            _suppressToggle = true;

            foreach (var replacement in Replacements)
                replacement.PropertyChanged -= OnReplacementPropertyChanged;

            Replacements.Clear();

            var items = await _contentPatcherService.GetReplacementsAsync();
            foreach (var item in items)
            {
                item.PropertyChanged += OnReplacementPropertyChanged;
                Replacements.Add(item);
            }

            HasReplacements = Replacements.Count > 0;
            _suppressToggle = false;
        }

        private void OnReplacementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suppressToggle)
                return;

            if (sender is ContentReplacementInfo replacement
                && e.PropertyName == nameof(ContentReplacementInfo.IsEnabled))
            {
                _ = ToggleReplacementAsync(replacement);
            }
        }
    }
}
