using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Splat;
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

        private async Task WindowShowFolderDialog(IInteractionContext<string, string?> interaction)
        {
			// Get top level from the current control. Alternatively, you can use Window reference instead.
			var topLevel = TopLevel.GetTopLevel(this);

			// Start async operation to open the dialog.
			var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select Folder",
				AllowMultiple = false,
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(!string.IsNullOrEmpty(interaction?.Input) ? interaction.Input : AppDomain.CurrentDomain.BaseDirectory)
			});

			if (folder.Count > 0)
			{
                interaction?.SetOutput(folder.First().Path.AbsolutePath);
            }
		}

        private async Task WindowShowFileDialog(IInteractionContext<string, string?> interaction)
        {
			// Get top level from the current control. Alternatively, you can use Window reference instead.
			var topLevel = TopLevel.GetTopLevel(this);

			// Start async operation to open the dialog.
			var file = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select File",
				AllowMultiple = false,
				SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(!string.IsNullOrEmpty(interaction?.Input) ? Path.GetFullPath(interaction.Input) : AppDomain.CurrentDomain.BaseDirectory),
				SuggestedFileName =!string.IsNullOrEmpty(interaction?.Input) ? Path.GetFileName(interaction?.Input) : "serialdisk.log"
			});

			if (file.Count > 0)
			{
				interaction?.SetOutput(file.First().Path.AbsolutePath);
			}
		}
    }
}
