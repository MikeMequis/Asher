namespace Asher.UserInterface.ViewModels
{
    public class ContentPatcherViewModel : BaseViewModel
    {
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

        public ContentPatcherViewModel()
        {

        }

        public override Task InitAsync() => Task.CompletedTask;

        private DelegateCommand _addReplacementCommand;
        public DelegateCommand AddReplacementCommand => 
            _addReplacementCommand ??= new DelegateCommand(ExecuteAddReplacementCommand, CanExecuteAddReplacementCommand);

        private void ExecuteAddReplacementCommand()
        {
            // TODO: Implement content replacement logic
            // This will be implemented when we add the content patcher service
            
            // Clear the form after adding
            OriginalAssetPath = string.Empty;
            ReplacementAssetPath = string.Empty;
        }

        private bool CanExecuteAddReplacementCommand()
        {
            return !string.IsNullOrWhiteSpace(OriginalAssetPath) && 
                   !string.IsNullOrWhiteSpace(ReplacementAssetPath);
        }
    }
}
