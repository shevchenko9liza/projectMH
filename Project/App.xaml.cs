using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Project.Core.Models;
using Project.Core.Services;
using Project.Infrastructure.Services;
using Project.Services;
using Project.Views;
using System.IO;
using System.Windows;
namespace Project;
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _configuration = BuildConfiguration();
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            StartMainApplication();
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup failed: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _serviceProvider?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Application exit error: {ex.Message}");
        }   
        base.OnExit(e);
    }
    private IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        return builder.Build();
    }
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_configuration!);
        services.Configure<ApplicationSettings>(_configuration!.GetSection("ApplicationSettings"));
                    services.Configure<ComPortConfiguration>(_configuration.GetSection("ComPortConfiguration"));
        services.Configure<DataManagementSettings>(_configuration.GetSection("DataManagement"));
        services.Configure<MeasurementSettings>(_configuration.GetSection("MeasurementSettings"));
        services.Configure<ChartSettings>(_configuration.GetSection("ChartSettings"));
        services.Configure<LoggingSettings>(_configuration.GetSection("LoggingSettings"));
        services.Configure<SecuritySettings>(_configuration.GetSection("SecuritySettings"));
        services.Configure<PerformanceSettings>(_configuration.GetSection("Performance"));
        services.Configure<AdvancedSettings>(_configuration.GetSection("Advanced"));
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IComPortService, ComPortService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<ViewModels.MainPageViewModel>();
        services.AddTransient<LoginView>();
        services.AddTransient<MainPage>();
    }
    private void StartMainApplication()
    {
        var navigationService = _serviceProvider!.GetRequiredService<INavigationService>();
        var loginView = _serviceProvider!.GetRequiredService<LoginView>();
        var loginViewModel = _serviceProvider!.GetRequiredService<ViewModels.LoginViewModel>();
        loginView.DataContext = loginViewModel;
        if (navigationService is NavigationService navService)
        {
            navService.SetServiceProvider(_serviceProvider!);
        }
        loginView.Show();
    }
}
