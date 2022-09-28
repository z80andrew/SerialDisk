using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Z80andrew.SerialDisk.SerialDiskUI.ViewModels;

namespace Z80andrew.SerialDisk.SerialDiskUI.Views
{
    public class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
    {
        public SettingsWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(d => d(ViewModel.ApplySettingsCommand.Subscribe(CloseSettingsWindow)));
            this.WhenActivated(d => d(ViewModel.CloseSettingsCommand.Subscribe(CloseSettingsWindow)));
            this.WhenActivated(d => d(ViewModel.ShowFolderDialog.RegisterHandler(WindowShowFolderDialog)));
            this.WhenActivated(d => d(ViewModel.ShowFileDialog.RegisterHandler(WindowShowFileDialog)));
        }

        private void CloseSettingsWindow(Unit commandOutput)
        {
            this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task WindowShowFolderDialog(InteractionContext<string, string?> interaction)
        {
            var dialog = new OpenFolderDialog
            {
                Directory = !string.IsNullOrEmpty(interaction?.Input) ? interaction.Input : AppDomain.CurrentDomain.BaseDirectory
            };

            var folderPath = await dialog.ShowAsync(this);
            interaction?.SetOutput(folderPath);
        }

        private async Task WindowShowFileDialog(InteractionContext<string, string?> interaction)
        {
            var dialog = new SaveFileDialog();

            if (!string.IsNullOrEmpty(interaction?.Input))
            {
                dialog.InitialFileName = Path.GetFileName(interaction.Input);
                dialog.Directory = Path.GetFullPath(interaction.Input);
            }

            else
            {
                dialog.InitialFileName = "serialdisk.log";
                dialog.Directory = AppDomain.CurrentDomain.BaseDirectory;
            }

            var filePath = await dialog.ShowAsync(this);
            interaction?.SetOutput(filePath);
        }
    }
}
