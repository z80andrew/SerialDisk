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
using ReactiveUI.Validation.Extensions;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SerialDiskUI.Views
{
    public class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
    {
        private static Key[] NumericKeys = { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
            Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,
            Key.Delete, Key.Back };

        private static Key[] ControlKeys = { Key.Delete, Key.Back, Key.Clear, Key.Left, Key.Right };

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

            var diskSizeInput = this.FindControl<NumericUpDown>("DiskSizeInput");
            diskSizeInput.AddHandler(InputElement.KeyDownEvent, TextBox_KeyDown, RoutingStrategies.Tunnel);
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            var textInput = sender as NumericUpDown;

            var bob = (char)e.Key;

            if (!Array.Exists(NumericKeys, k => k == e.Key) && !Array.Exists(ControlKeys, k => k == e.Key))
            {
                e.Handled = true;
            }

            else if(textInput.Text.Length >= 3 && !Array.Exists(ControlKeys, k => k == e.Key))
            {
                e.Handled = true;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task WindowShowFolderDialog(InteractionContext<string, string?> interaction)
        {
            var dialog = new OpenFolderDialog();
            dialog.Directory = !string.IsNullOrEmpty(interaction.Input) ? interaction.Input : AppDomain.CurrentDomain.BaseDirectory;
            var folderPath = await dialog.ShowAsync(this);
            interaction.SetOutput(folderPath);
        }

        private async Task WindowShowFileDialog(InteractionContext<string, string?> interaction)
        {
            var dialog = new SaveFileDialog();

            if (!string.IsNullOrEmpty(interaction.Input))
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
