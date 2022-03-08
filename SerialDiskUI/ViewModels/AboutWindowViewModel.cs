using Avalonia.Controls;
using ReactiveUI;
using SerialDiskUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace SerialDiskUI.ViewModels
{
    public class AboutWindowViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, SimpleDialogModel> CloseAboutCommand { get; }

        public AboutWindowViewModel()
        {
            CloseAboutCommand = ReactiveCommand.CreateFromTask(CloseAbout);
        }

        private async Task<SimpleDialogModel> CloseAbout()
        {
            return new SimpleDialogModel(SimpleDialogModel.ReturnType.OK);
        }
    }
}
