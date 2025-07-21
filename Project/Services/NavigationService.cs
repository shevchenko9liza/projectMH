using System.Windows;
using Project.Views;
using Microsoft.Extensions.DependencyInjection;
namespace Project.Services
{
    public class NavigationService : INavigationService
    {
        private Window? _currentWindow;
        private ServiceProvider? _serviceProvider;
        public void SetServiceProvider(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public void NavigateToMainPage()
        {
            try
            {
                if (_serviceProvider == null)
                {
                    var mainPage = new MainPage();
                    var mainPageViewModel = new ViewModels.MainPageViewModel(this);
                    mainPage.DataContext = mainPageViewModel;
                    mainPage.Show();
                    CloseCurrentWindow();
                    _currentWindow = mainPage;
                }
                else
                {
                    var mainPage = _serviceProvider.GetRequiredService<MainPage>();
                    var mainPageViewModel = _serviceProvider.GetRequiredService<ViewModels.MainPageViewModel>();
                    mainPage.DataContext = mainPageViewModel;
                    mainPage.Show();
                    CloseCurrentWindow();
                    _currentWindow = mainPage;
                }
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
                if (_serviceProvider == null)
                {
                    var loginView = new LoginView();
                    var loginViewModel = new ViewModels.LoginViewModel(this);
                    loginView.DataContext = loginViewModel;       
                    loginView.Show();
                    CloseCurrentWindow();
                    _currentWindow = loginView;
                }
                else
                {
                    var loginView = _serviceProvider.GetRequiredService<LoginView>();
                    var loginViewModel = _serviceProvider.GetRequiredService<ViewModels.LoginViewModel>();
                    loginView.DataContext = loginViewModel;
                    loginView.Show();
                    CloseCurrentWindow();
                    _currentWindow = loginView;
                }
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