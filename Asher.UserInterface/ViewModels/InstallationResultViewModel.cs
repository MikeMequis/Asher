using Asher.Services.Interfaces;
using Asher.UserInterface.Events;

namespace Asher.UserInterface.ViewModels
{
    public class InstallationResultViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInstallationStateService _stateService;
        private readonly IRegionManager _regionManager;

        public InstallationResultViewModel(IEventAggregator eventAggregator,
                                           IInstallationStateService stateService,
                                           IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            _stateService = stateService;
            _regionManager = regionManager;
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        private string _headerTitle;
        public string HeaderTitle
        {
            get => _headerTitle;
            set => SetProperty(ref _headerTitle, value);
        }

        private string _headerSubtitle;
        public string HeaderSubtitle
        {
            get => _headerSubtitle;
            set => SetProperty(ref _headerSubtitle, value);
        }

        private string _resultMessage;
        public string ResultMessage
        {
            get => _resultMessage;
            set => SetProperty(ref _resultMessage, value);
        }

        private string _errorDetails;
        public string ErrorDetails
        {
            get => _errorDetails;
            set => SetProperty(ref _errorDetails, value);
        }

        private bool _hasErrorDetails;
        public bool HasErrorDetails
        {
            get => _hasErrorDetails;
            set => SetProperty(ref _hasErrorDetails, value);
        }

        public override Task InitAsync() => Task.CompletedTask;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            var result = _stateService.GetInstallationResult();

            if (result == null)
            {
                // Se não há resultado, volta para o início
                _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.Welcome);
                return;
            }

            IsSuccess = result.Success;

            if (result.Success)
            {
                HeaderTitle = "Instalação Completa";
                HeaderSubtitle = "O Asher foi configurado com sucesso";
                ResultMessage = "Você já pode começar a usar mods no Dust: An Elysian Tail!";
            }
            else
            {
                HeaderTitle = "Erro na Instalação";
                HeaderSubtitle = "Não foi possível completar a instalação";
                ResultMessage = result.Message ?? "Ocorreu um erro desconhecido durante a instalação.";

                if (result.Error != null)
                {
                    ErrorDetails = result.Error.ToString();
                    HasErrorDetails = true;
                }
            }
        }

        private DelegateCommand _finishInstallationCommand;
        public DelegateCommand FinishInstallationCommand =>
            _finishInstallationCommand ??= new DelegateCommand(ExecuteFinishInstallationCommand);

        private DelegateCommand _retryInstallationCommand;
        public DelegateCommand RetryInstallationCommand =>
            _retryInstallationCommand ??= new DelegateCommand(ExecuteRetryInstallationCommand);

        private DelegateCommand _cancelInstallationCommand;
        public DelegateCommand CancelInstallationCommand =>
            _cancelInstallationCommand ??= new DelegateCommand(ExecuteCancelInstallationCommand);

        private void ExecuteFinishInstallationCommand()
        {
            // Publica evento de instalação completa
            _eventAggregator.GetEvent<InstallationCompleteEvent>().Publish();

            // O MainWindow vai escutar este evento e alternar os NavigationItems
        }

        private void ExecuteRetryInstallationCommand()
        {
            // Volta para a tela de detecção para tentar novamente
            _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.GameDetection);
        }

        private void ExecuteCancelInstallationCommand()
        {
            // Volta para a tela inicial
            _regionManager.RequestNavigate(RegionNames.Main, InstallationNavigationNames.Welcome);
        }
    }
}