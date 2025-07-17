using System.Windows;
using Project.Views;
namespace Project.Services
{
    public class NavigationService : INavigationService
    {
        private Window? _currentWindow;
        public void NavigateToMainPage()
        {
            try
            {
                var mainPage = new MainPage();
                var mainPageViewModel = new ViewModels.MainPageViewModel(this);
                mainPage.DataContext = mainPageViewModel;
                mainPage.Show();
                CloseCurrentWindow();
                _currentWindow = mainPage;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
        public void NavigateToLogin()
        {
            try
            {
                var loginView = new LoginView();
                var loginViewModel = new ViewModels.LoginViewModel(this);
                loginView.DataContext = loginViewModel;       
                loginView.Show();
                CloseCurrentWindow();
                _currentWindow = loginView;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
        public void CloseCurrentWindow()
        {
            try
            {
                if (_currentWindow != null)
                {
                    if (_currentWindow.DataContext is ViewModels.BaseViewModel viewModel)
                    {
                    }          
                    _currentWindow.Close();
                    _currentWindow = null;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка закрытия окна: {ex.Message}");
            }
        }
        public void SetCurrentWindow(Window window)
        {
            _currentWindow = window;
        }
    }
} 