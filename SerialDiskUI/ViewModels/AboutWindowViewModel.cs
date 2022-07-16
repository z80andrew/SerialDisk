using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using System.Windows.Input;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.SerialDiskUI.Models;

namespace Z80andrew.SerialDisk.SerialDiskUI.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, SimpleDialogModel> CloseAboutCommand { get; }
        public ICommand ShowWebsiteCommand { get; }
        public String VersionNote => $"v{Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion} {Constants.VERSION_TYPE}";

        public string WebsiteButtonText => Constants.PROJECT_URL.Replace(@"https://www.", String.Empty);

        public AboutWindowViewModel()
        {
            CloseAboutCommand = ReactiveCommand.Create(CloseAbout);

            ShowWebsiteCommand = ReactiveCommand.Create(() =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = Constants.PROJECT_URL };
                var process = Process.Start(startInfo);
            });
        }

        private SimpleDialogModel CloseAbout()
        {
            return new SimpleDialogModel(SimpleDialogModel.ReturnType.OK);
        }
    }
}
