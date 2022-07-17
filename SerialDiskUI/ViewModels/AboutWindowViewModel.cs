using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Windows.Input;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Comms;
using Z80andrew.SerialDisk.SerialDiskUI.Models;
using Z80andrew.SerialDisk.Utilities;

namespace Z80andrew.SerialDisk.SerialDiskUI.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {
        private string _latestVersionInfo;
        public ReactiveCommand<Unit, SimpleDialogModel> CloseAboutCommand { get; }
        public ICommand ShowWebsiteCommand { get; }
        public ICommand ShowLatestVersionWebpageCommand { get; }
        public String VersionNote => $"v{ConfigurationHelper.ApplicationVersion} {ConfigurationHelper.VERSION_TYPE}";
        public string WebsiteButtonText => Constants.PROJECT_URL.Replace(@"https://www.", String.Empty);

        public bool IsNewVersionAvailable
        {
            get
            {
                return ConfigurationHelper.IsNewVersionAvailable(_latestVersionInfo);
            }
        }

        public string LatestVersionUrl
        {
            get
            {
                return ConfigurationHelper.GetLatestVersionUrl(_latestVersionInfo);
            }
        }

        public AboutWindowViewModel()
        {
            CloseAboutCommand = ReactiveCommand.Create(CloseAbout);

            ShowWebsiteCommand = ReactiveCommand.Create(() =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = Constants.PROJECT_URL };
                var process = Process.Start(startInfo);
            });

            ShowLatestVersionWebpageCommand = ReactiveCommand.Create(() =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = LatestVersionUrl};
                var process = Process.Start(startInfo);
            });

            _latestVersionInfo = Network.GetLatestVersionInfo();
        }

        private SimpleDialogModel CloseAbout()
        {
            return new SimpleDialogModel(SimpleDialogModel.ReturnType.OK);
        }
    }
}
