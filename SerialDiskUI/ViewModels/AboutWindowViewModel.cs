using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Comms;
using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.SerialDiskUI.Models;
using Z80andrew.SerialDisk.Utilities;

namespace Z80andrew.SerialDisk.SerialDiskUI.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {
        private ILogger _logger;
        private string _releasesInfo;
        public List<CreditsModel> Credits { get; set; }
        public ReactiveCommand<Unit, SimpleDialogModel> CloseAboutCommand { get; }
        public ICommand ShowWebsiteCommand { get; }
        public ICommand ShowLatestVersionWebpageCommand { get; }
        public String VersionNote => ConfigurationHelper.VERSION_LABEL;
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

        public AboutWindowViewModel(ILogger logger)
        {
            _logger = logger;

            CloseAboutCommand = ReactiveCommand.Create(CloseAbout);

            ShowWebsiteCommand = ReactiveCommand.Create(() =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = Constants.PROJECT_URL };
                var process = Process.Start(startInfo);
            });

            ShowLatestVersionWebpageCommand = ReactiveCommand.Create(() =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = LatestVersionUrl };
                var process = Process.Start(startInfo);
            });

            IsNewVersionAvailable = false;
            NewVersionCheckLabelText = "Checking for new version...";
            Task checkLatestVersionTask = CheckForNewVersion(_logger);

            InitCredits();
        }

        private void InitCredits()
        {
            using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Z80andrew.SerialDisk.SerialDiskUI.Assets.Credits.json"))
            {
                if (stream != null)
                {
                    try
                    {
                        var streamReader = new StreamReader(stream);
                        var creditsJson = streamReader.ReadToEnd();
                        Credits = JsonSerializer.Deserialize<List<CreditsModel>>(creditsJson)!;
                    }

                    catch (Exception ex)
                    {
                        Credits = new List<CreditsModel>();
                        _logger.LogException(ex, "Could not load credits");
                    }
                }

                else
                    _logger.LogException(new Exception("Resource stream was null"), "Could not load credits");
            }
        }

        private async Task CheckForNewVersion(ILogger logger)
        {
            try
            {
                _releasesInfo = await Network.GetReleases();
                LatestVersionUrl = ConfigurationHelper.GetLatestVersionUrl(_releasesInfo);
                IsNewVersionAvailable = ConfigurationHelper.IsNewVersionAvailable(_releasesInfo);

                if (!IsNewVersionAvailable) NewVersionCheckLabelText = "No new version available";
                else NewVersionCheckLabelText = "New version available";
            }

            catch (Exception ex)
            {
                logger.LogException(ex, "Could not check for new version");
                NewVersionCheckLabelText = "Could not check for new version";
            }
        }

        private SimpleDialogModel CloseAbout()
        {
            return new SimpleDialogModel(SimpleDialogModel.ReturnType.OK);
        }
    }
}
