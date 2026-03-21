using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Z80andrew.SerialDisk.SerialDiskUI.ViewModels;

namespace Z80andrew.SerialDisk.SerialDiskUI.Views
{
    public partial class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
    {
        public SettingsWindow()
        {
            InitializeComponent();

            this.WhenActivated(d => d(ViewModel.ApplySettingsCommand.Subscribe(CloseSettingsWindow)));
            this.WhenActivated(d => d(ViewModel.CloseSettingsCommand.Subscribe(CloseSettingsWindow)));
            this.WhenActivated(d => d(ViewModel.ShowFolderDialog.RegisterHandler(WindowShowFolderDialog)));
            this.WhenActivated(d => d(ViewModel.ShowFileDialog.RegisterHandler(WindowShowFileDialog)));
        }

		private void CloseSettingsWindow(Unit commandOutput)
        {
            this.Close();
        }

        private async Task WindowShowFolderDialog(IInteractionContext<string, string?> interaction)
        {
			var topLevel = TopLevel.GetTopLevel(this);

			var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select Folder",
				AllowMultiple = false,
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(!string.IsNullOrEmpty(interaction.Input) ? interaction.Input : AppDomain.CurrentDomain.BaseDirectory)
			});

            string? selectedFolderPath = folders.Count > 0 ? folders[0].Path.AbsolutePath : null;

            interaction.SetOutput(selectedFolderPath);
		}

        private async Task WindowShowFileDialog(IInteractionContext<string, string?> interaction)
        {
			var topLevel = TopLevel.GetTopLevel(this);

			var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select File",
				AllowMultiple = false,
				SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(!string.IsNullOrEmpty(interaction.Input) ? Path.GetFullPath(interaction.Input) : AppDomain.CurrentDomain.BaseDirectory),
				SuggestedFileName =!string.IsNullOrEmpty(interaction.Input) ? Path.GetFileName(interaction.Input) : "serialdisk.log"
			});

			string? selectedFilePath = files.Count > 0 ? files[0].Path.AbsolutePath : null;

            interaction.SetOutput(selectedFilePath);
		}
    }
}
