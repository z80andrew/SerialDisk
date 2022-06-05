using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using SerialDiskUI.Common;
using SerialDiskUI.Models;
using SerialDiskUI.Services;
using SerialDiskUI.ViewModels;
using SerialDiskUI.Views;
using System;
using System.IO;

namespace SerialDiskUI
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
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
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

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(model, statusService, logger),
                };

                if (_appSettings.MainWindowHeight > -1) desktop.MainWindow.Height = Convert.ToDouble(_appSettings.MainWindowHeight);
                if (_appSettings.MainWindowWidth > -1) desktop.MainWindow.Width = Convert.ToDouble(_appSettings.MainWindowWidth);
                if (_appSettings.MainWindowX > -1 && _appSettings.MainWindowY > -1)
                {
                    desktop.MainWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;
                    desktop.MainWindow.Position = new PixelPoint(_appSettings.MainWindowX, _appSettings.MainWindowY);
                }
            }
        }

        private UIApplicationSettings GetApplicationSettings(string[] args)
        {
            var defaultApplicationSettings = ConfigurationHelper.GetDefaultApplicationSettings();
            
            UIApplicationSettings appSettings = new UIApplicationSettings(defaultApplicationSettings);

            try
            {
                var configBuilder = new ConfigurationBuilder();

                configBuilder.AddJsonFile(Settings.ConfigFilePath, true, false)
                    .Build()
                    .Bind(appSettings);

                configBuilder.AddCommandLine(args, Constants.ConsoleParameterMappings)
                    .Build()
                    .Bind(appSettings);

                // Directory can be specified as relative, so get full path
                if (Directory.Exists(appSettings.LocalDirectoryPath))
                {
                    appSettings.LocalDirectoryPath = Path.GetFullPath(appSettings.LocalDirectoryPath);
                }
            }

            catch (Exception parameterException)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing parameters: {parameterException.Message}");
            }

            return appSettings;
        }
    }
}
