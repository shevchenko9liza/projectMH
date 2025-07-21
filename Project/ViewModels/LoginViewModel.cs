using System.Windows;
using System.Windows.Input;
using Project.Services;
using Project.Constants;
namespace Project.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _loginMessage = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                LoginMessage = string.Empty; 
            }
        }
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                LoginMessage = string.Empty; 
            }
        }
        public string LoginMessage
        {
            get => _loginMessage;
            set
            {
                _loginMessage = value;
                OnPropertyChanged();
            }
        }
        public ICommand LoginCommand { get; }
        private readonly INavigationService _navigationService;
        public LoginViewModel() : this(new NavigationService())
        {
        }
        public LoginViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            LoginCommand = new RelayCommand(ExecuteLogin);
        }
        private void ExecuteLogin(object? parameter)
        {
            LoginMessage = string.Empty;
            
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                LoginMessage = AuthenticationConstants.ErrorMessages.EmptyFields;
                return;
            }
            if (Username == AuthenticationConstants.ValidUsername && Password == AuthenticationConstants.ValidPassword)
            {
                if (parameter is Window currentWindow)
                {
                    if (_navigationService is NavigationService navService)
                    {
                        navService.SetCurrentWindow(currentWindow);
                    }
                }
                _navigationService.NavigateToMainPage();
            }
            else
            {
                LoginMessage = AuthenticationConstants.ErrorMessages.InvalidCredentials;
            }
        }
    }
} 