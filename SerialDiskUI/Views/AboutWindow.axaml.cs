using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Z80andrew.SerialDisk.SerialDiskUI.ViewModels;
using System;
using System.Reactive;

namespace Z80andrew.SerialDisk.SerialDiskUI.Views
{
    public partial class AboutWindow : ReactiveWindow<AboutWindowViewModel>
    {
        public AboutWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(d => d(ViewModel.CloseAboutCommand.Subscribe(Close)));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
