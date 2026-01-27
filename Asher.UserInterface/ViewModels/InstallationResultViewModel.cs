namespace Asher.UserInterface.ViewModels
{
    public class InstallationResultViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;

        public InstallationResultViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public override Task InitAsync() => Task.CompletedTask;

        private DelegateCommand _finishInstallationCommand;
        public DelegateCommand FinishInstallationCommand =>
            _finishInstallationCommand ??= new DelegateCommand(ExecuteFinishInstallationCommand);

        private void ExecuteFinishInstallationCommand()
        {

        }
    }
}
