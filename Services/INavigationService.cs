using System.Windows;
namespace Project.Services
{
    public interface INavigationService
    {
        void NavigateToMainPage();
        void NavigateToLogin();
        void CloseCurrentWindow();
    }
} 