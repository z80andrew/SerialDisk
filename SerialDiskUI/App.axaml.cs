using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
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
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Startup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            _appSettings = GetApplicationSettings(e.Args);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var model = new SerialDiskUIModel(_appSettings);
                var statusService = new StatusService();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(model, statusService),
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

                configBuilder.AddJsonFile(Constants.configFileName, true, false)
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
