using Project.Services;
using Project.Constants;
using System.Windows.Input;
namespace Project.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private string _welcomeMessage = AuthenticationConstants.Labels.WelcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }
        public ICommand LogoutCommand { get; }
        private readonly INavigationService _navigationService;
        public MainPageViewModel() : this(new NavigationService())
        {
        }
        public MainPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }
        private void ExecuteLogout(object? parameter)
        {
            _navigationService.NavigateToLogin();
        }
        public void Logout()
        {
            ExecuteLogout(null);
        }
    }
} 