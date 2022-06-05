using AtariST.SerialDisk.Common;
using ReactiveUI;
using SerialDiskUI.Models;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SerialDiskUI.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, SimpleDialogModel> CloseAboutCommand { get; }
        public ICommand ShowWebsiteCommand { get; }

        public string WebsiteButtonText => Constants.PROJECT_URL.Replace(@"https://www.", String.Empty);

        public AboutWindowViewModel()
        {
            CloseAboutCommand = ReactiveCommand.CreateFromTask(CloseAbout);

            ShowWebsiteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = Constants.PROJECT_URL };
                var process = Process.Start(startInfo);
            });
        }

        private async Task<SimpleDialogModel> CloseAbout()
        {
            return new SimpleDialogModel(SimpleDialogModel.ReturnType.OK);
        }
    }
}
