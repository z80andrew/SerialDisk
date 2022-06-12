using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using SerialDiskUI.ViewModels;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Reactive;
using System.IO;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Avalonia.Input.TextInput;

namespace SerialDiskUI.Views
{
    public class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
    {
        string inputDiskSize = string.Empty;

        public SettingsWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            this.WhenActivated(d => d(ViewModel.ApplySettingsCommand.Subscribe(Close)));
            this.WhenActivated(d => d(ViewModel.CloseSettingsCommand.Subscribe(Close)));
            this.WhenActivated(d => d(ViewModel.ShowFolderDialog.RegisterHandler(WindowShowFolderDialog)));
            this.WhenActivated(d => d(ViewModel.ShowFileDialog.RegisterHandler(WindowShowFileDialog)));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task WindowShowFolderDialog(InteractionContext<string, string?> interaction)
        {
            var dialog = new OpenFolderDialog();
            dialog.Directory = !string.IsNullOrEmpty(interaction?.Input) ? interaction.Input : AppDomain.CurrentDomain.BaseDirectory;

            var folderPath = await dialog.ShowAsync(this);
            interaction.SetOutput(folderPath);
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
            interaction.SetOutput(filePath);
        }
    }
}
