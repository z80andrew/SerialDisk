using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SerialDiskUI.ViewModels;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Reactive;

namespace SerialDiskUI.Views
{
    public class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
    {
        public SettingsWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(d => d(ViewModel.ApplySettingsCommand.Subscribe(Close)));
            this.WhenActivated(d => d(ViewModel.ShowFolderDialog.RegisterHandler(WindowShowFolderDialog)));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task WindowShowFolderDialog(InteractionContext<Unit, string?> interaction)
        {
            var dialog = new OpenFolderDialog();
            var folderName = await dialog.ShowAsync(this);
            interaction.SetOutput(folderName);
        }
    }
}
