using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BackupUtility.Services.Interfaces;
using BackupUtility.Services.Implementations;
using BackupUtility.ViewModels;
using BackupUtility.Views;
using BackupUtility.Properties;

namespace BackupUtility
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddSingleton<ILogger, Logger>(); // Singleton for logger
                    services.AddSingleton<IDriveService, DriveService>();
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IBackupObjectStorageService, BackupObjectStorageService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<IBackupService, BackupService>();

                    // Register ViewModels
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<BackupObjectFormViewModel>();

                    // Register MainWindow itself
                    services.AddSingleton<MainWindow>();

                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            // Resolve the MainWindow from the DI container
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();

            // Resolve the MainWindowViewModel from the DI container
            // and assign it as the DataContext
            mainWindow.DataContext = _host.Services.GetRequiredService<MainWindowViewModel>();

            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            base.OnExit(e);
        }
    }
}
