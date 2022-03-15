using Avalonia.Controls;
using ReactiveUI;
using SerialDiskUI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SerialDiskUI.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, SimpleDialogModel> CloseAboutCommand { get; }
        public ICommand ShowWebsiteCommand { get; }

        public AboutWindowViewModel()
        {
            CloseAboutCommand = ReactiveCommand.CreateFromTask(CloseAbout);

            ShowWebsiteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = "https://www.github.com/z80andrew/serialdisk" };
                var process = Process.Start(startInfo);
            });
        }

        private async Task<SimpleDialogModel> CloseAbout()
        {
            return new SimpleDialogModel(SimpleDialogModel.ReturnType.OK);
        }
    }
}
