using Asher.Services.Interfaces;

namespace Asher.UserInterface.ViewModels
{
    public class InstallationProgressViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInstallationStateService _stateService;

        public InstallationProgressViewModel(IEventAggregator eventAggregator, 
                                             IInstallationStateService stateService)
        {
            _eventAggregator = eventAggregator;
            _stateService = stateService;
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {

        }
    }
}
