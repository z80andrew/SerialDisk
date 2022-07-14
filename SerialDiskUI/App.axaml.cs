using Z80andrew.SerialDisk.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Z80andrew.SerialDisk.SerialDiskUI.Models;
using Z80andrew.SerialDisk.SerialDiskUI.Services;
using Z80andrew.SerialDisk.SerialDiskUI.ViewModels;
using Z80andrew.SerialDisk.SerialDiskUI.Views;
using System;
using System.IO;

namespace Z80andrew.SerialDisk.SerialDiskUI
{
    public class App : Application
    {
        private UIApplicationSettings _appSettings;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Startup += Desktop_Startup;
                desktop.ShutdownRequested += Desktop_ShutdownRequested;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow as MainWindow;
                _appSettings.MainWindowHeight = Convert.ToInt32(desktop.MainWindow.Height);
                _appSettings.MainWindowWidth = Convert.ToInt32(desktop.MainWindow.Width);
                _appSettings.MainWindowX = mainWindow.SavedWindowPosition.X;
                _appSettings.MainWindowY = mainWindow.SavedWindowPosition.Y;
                _appSettings.WriteSettingsToDisk();
            }
        }

        private void Desktop_Startup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            _appSettings = GetApplicationSettings(e.Args);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var model = new SerialDiskUIModel(_appSettings);
                var statusService = new StatusService();
                var logger = new Logger(_appSettings.LoggingLevel, _appSettings.LogFileName);

                desktop.MainWindow = new MainWindow(_appSettings.MainWindowWidth, _appSettings.MainWindowHeight, _appSettings.MainWindowX, _appSettings.MainWindowY)
                {
                    DataContext = new MainWindowViewModel(model, statusService, logger),                    
                };
            }
        }

        private UIApplicationSettings GetApplicationSettings(string[] args)
        {
            var defaultApplicationSettings = ConfigurationHelper.GetDefaultApplicationSettings();
            
            UIApplicationSettings appSettings = new UIApplicationSettings(defaultApplicationSettings);

            try
            {
                var configBuilder = new ConfigurationBuilder();

                if (File.Exists(Common.Constants.ConfigFilePath))
                {
                    configBuilder.AddJsonFile(Common.Constants.ConfigFilePath, true, false)
                        .Build()
                        .Bind(appSettings);
                }

                configBuilder.AddCommandLine(args, Z80andrew.SerialDisk.Common.Constants.ConsoleParameterMappings)
                    .Build()
                    .Bind(appSettings);

            }

            catch (Exception parameterException)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing parameters: {parameterException.Message}");
            }

            // Directory can be specified as relative, so get full path
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettings.LocalDirectoryPath)))
            {
                appSettings.LocalDirectoryPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, appSettings.LocalDirectoryPath));
            }

            return appSettings;
        }
    }
}
