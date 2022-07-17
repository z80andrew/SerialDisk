using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
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

        private string _newVersionCheckLabelText;
        public string NewVersionCheckLabelText
        {
            get => _newVersionCheckLabelText;
            set => this.RaiseAndSetIfChanged(ref _newVersionCheckLabelText, value);
        }

        private bool _isNewVersionAvailable;
        public bool IsNewVersionAvailable
        {
            get => _isNewVersionAvailable;
            set => this.RaiseAndSetIfChanged(ref _isNewVersionAvailable, value);
        }

        private string _latestVersionUrl;
        public string LatestVersionUrl
        {
            get => _latestVersionUrl;
            set => this.RaiseAndSetIfChanged(ref _latestVersionUrl, value);
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

            NewVersionCheckLabelText = "Checking for new version...";

            Task checkLatestVersionTask = CheckForNewVersion();
        }

        private async Task CheckForNewVersion()
        {
            _latestVersionInfo = await Network.GetLatestVersionInfo();            
            LatestVersionUrl = ConfigurationHelper.GetLatestVersionUrl(_latestVersionInfo);
            IsNewVersionAvailable = ConfigurationHelper.IsNewVersionAvailable(_latestVersionInfo);

            if (!IsNewVersionAvailable) NewVersionCheckLabelText = "No new version available";
        }

        private SimpleDialogModel CloseAbout()
        {
            return new SimpleDialogModel(SimpleDialogModel.ReturnType.OK);
        }
    }
}
