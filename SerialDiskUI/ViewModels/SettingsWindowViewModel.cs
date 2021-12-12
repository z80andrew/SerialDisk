using AtariST.SerialDisk.Models;
using Avalonia.Controls;
using ReactiveUI;
using SerialDiskUI.Models;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static SerialDiskUI.Common.Settings;

namespace SerialDiskUI.ViewModels
{
    public class SettingsWindowViewModel : ViewModelBase
    {
        private SerialDiskUIModel _settings;
        //private OpenFolderDialog _dialog = new OpenFolderDialog();
        private string _selectedFolder;

        public KeyValuePair<string, int> SelectedBaud { get; set; }

        public KeyValuePair<string, int> SelectedDataBits { get; set; }
        public KeyValuePair<string, StopBits> SelectedStopBits { get; set; }
        public KeyValuePair<string, Handshake> SelectedHandshake { get; set; }
        public KeyValuePair<string, Parity> SelectedParity { get; set; }
        public string SelectedFolder 
        {
            get => _selectedFolder;
            set => this.RaiseAndSetIfChanged(ref _selectedFolder, value);
        }

        public KeyValuePair<string, int>[] BaudRateChoices { get; set; } = BaudRates;
        public KeyValuePair<string, int>[] DataBitsChoices { get; set; } = DataBitz;
        public KeyValuePair<string, StopBits>[] StopBitsChoices { get; set; } = StopBitz;
        public KeyValuePair<string, Handshake>[] HandshakeChoices { get; set; } = Handshakes;
        public KeyValuePair<string, Parity>[] ParityChoices { get; set; } = Parities;

        public SettingsWindowViewModel()
        {
        }

        public SettingsWindowViewModel(SerialDiskUIModel settings)
        {
            _settings = settings;
            ShowFolderDialog = new Interaction<Unit, string?>();

            ChooseFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
            ApplySettingsCommand = ReactiveCommand.CreateFromTask(ApplySettings);

            ApplySettingsValues(_settings);
        }

        private void ApplySettingsValues(SerialDiskUIModel settings)
        {
            SelectedBaud = BaudRateChoices.Where(x => x.Value == settings.BaudRate).FirstOrDefault();
            SelectedDataBits = DataBitsChoices.Where(x => x.Value == settings.DataBits).FirstOrDefault();
            SelectedStopBits = StopBitsChoices.Where(x => x.Value == settings.StopBits).FirstOrDefault();
            SelectedHandshake = HandshakeChoices.Where(x => x.Value == settings.Handshake).FirstOrDefault();
            SelectedParity = ParityChoices.Where(x => x.Value == settings.Parity).FirstOrDefault();

            SelectedFolder = settings.VirtualDiskFolder;
        }

        public ReactiveCommand<Unit, SerialDiskUIModel> ApplySettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ChooseFolderCommand { get; }
        public Interaction<Unit, string?> ShowFolderDialog { get; }

        private async Task<SerialDiskUIModel> ApplySettings()
        {
            if (_settings != null)
            {
                //_settings.LogFileName = "This was set in the dialog";
                _settings.BaudRate = SelectedBaud.Value;
                _settings.DataBits = SelectedDataBits.Value;
                _settings.StopBits = SelectedStopBits.Value;
                _settings.Handshake = SelectedHandshake.Value;
                _settings.Parity = SelectedParity.Value;

                _settings.VirtualDiskFolder = SelectedFolder;
            }

            return _settings;
        }

        private async Task OpenFolderAsync()
        {
            var folderName = await ShowFolderDialog.Handle(Unit.Default);

            if (folderName is object)
            {
                SelectedFolder = folderName;
            }
        }
    }
}