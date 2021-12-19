using AtariST.SerialDisk.Models;
using Avalonia.Controls;
using ReactiveUI;
using SerialDiskUI.Models;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static AtariST.SerialDisk.Common.Constants;
using static SerialDiskUI.Common.Settings;

namespace SerialDiskUI.ViewModels
{
    public class SettingsWindowViewModel : ViewModelBase
    {
        private SerialDiskUIModel _settings;
        //private OpenFolderDialog _dialog = new OpenFolderDialog();
        private string _selectedFolder;
        private bool _isLogDisplayEnabled;

        public KeyValuePair<string, string> SelectedCOMPort { get; set; }
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
        public KeyValuePair<string, LoggingLevel> SelectedLogLevel { get; set; }

        public KeyValuePair<string, string>[] COMPortChoices { get; set; }
        public KeyValuePair<string, int>[] BaudRateChoices { get; set; }
        public KeyValuePair<string, int>[] DataBitsChoices { get; set; }
        public KeyValuePair<string, StopBits>[] StopBitsChoices { get; set; }
        public KeyValuePair<string, Handshake>[] HandshakeChoices { get; set; }
        public KeyValuePair<string, Parity>[] ParityChoices { get; set; }
        public KeyValuePair<string, LoggingLevel>[] LogLevelChoices { get; set; }

        public bool IsLogDisplayEnabled
        {
            get => _isLogDisplayEnabled;
            set => this.RaiseAndSetIfChanged(ref _isLogDisplayEnabled, value);
        }

        public SettingsWindowViewModel()
        {
        }

        public SettingsWindowViewModel(SerialDiskUIModel settings)
        {
            _settings = settings;
            ShowFolderDialog = new Interaction<Unit, string?>();

            ChooseFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
            ApplySettingsCommand = ReactiveCommand.CreateFromTask(ApplySettings);

            InitChoices();
            ApplySettingsValues(_settings);
        }

        private void InitChoices()
        {
            BaudRateChoices = BaudRates;
            DataBitsChoices = DataBitz;
            StopBitsChoices = StopBitz;
            HandshakeChoices = Handshakes;
            ParityChoices = Parities;
            

            var logLevelChoices = new List<KeyValuePair<string, LoggingLevel>>();
            foreach (LoggingLevel logLevel in Enum.GetValues(typeof(LoggingLevel)))
            {
                var logChoice = new KeyValuePair<string, LoggingLevel>(logLevel.ToString(), logLevel);
                logLevelChoices.Add(logChoice);
            }

            LogLevelChoices = logLevelChoices.ToArray();

            var portChoices = new List<KeyValuePair<string, string>>();

            var availablePorts = SerialPort.GetPortNames();
            foreach (string portName in availablePorts)
            {
                var portChoice = new KeyValuePair<string, string>(portName, portName);
                portChoices.Add(portChoice);
            }

            portChoices.Add(new KeyValuePair<string, string>("Other", "Other"));

            COMPortChoices = portChoices.ToArray();
        }

        private void ApplySettingsValues(SerialDiskUIModel settings)
        {
            SelectedCOMPort = COMPortChoices.Where(x => x.Value == settings.ComPortName).FirstOrDefault();
            SelectedBaud = BaudRateChoices.Where(x => x.Value == settings.BaudRate).FirstOrDefault();
            SelectedDataBits = DataBitsChoices.Where(x => x.Value == settings.DataBits).FirstOrDefault();
            SelectedStopBits = StopBitsChoices.Where(x => x.Value == settings.StopBits).FirstOrDefault();
            SelectedHandshake = HandshakeChoices.Where(x => x.Value == settings.Handshake).FirstOrDefault();
            SelectedParity = ParityChoices.Where(x => x.Value == settings.Parity).FirstOrDefault();

            SelectedFolder = settings.VirtualDiskFolder;
            IsLogDisplayEnabled = settings.IsLogDisplayEnabled;
            SelectedLogLevel = LogLevelChoices.Where(x => x.Value == settings.LoggingLevel).FirstOrDefault();
        }

        public ReactiveCommand<Unit, SerialDiskUIModel> ApplySettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ChooseFolderCommand { get; }
        public Interaction<Unit, string?> ShowFolderDialog { get; }

        private async Task<SerialDiskUIModel> ApplySettings()
        {
            if (_settings != null)
            {
                //_settings.LogFileName = "This was set in the dialog";
                _settings.ComPortName = _settings.ApplicationSettings.SerialSettings.PortName = SelectedCOMPort.Value;
                _settings.BaudRate = _settings.ApplicationSettings.SerialSettings.BaudRate = SelectedBaud.Value;
                _settings.DataBits = _settings.ApplicationSettings.SerialSettings.DataBits = SelectedDataBits.Value;
                _settings.StopBits = _settings.ApplicationSettings.SerialSettings.StopBits = SelectedStopBits.Value;
                _settings.Handshake = _settings.ApplicationSettings.SerialSettings.Handshake = SelectedHandshake.Value;
                _settings.Parity = _settings.ApplicationSettings.SerialSettings.Parity = SelectedParity.Value;

                _settings.VirtualDiskFolder = _settings.ApplicationSettings.LocalDirectoryPath = SelectedFolder;
                _settings.IsLogDisplayEnabled = IsLogDisplayEnabled;
                _settings.LoggingLevel = _settings.ApplicationSettings.LoggingLevel = SelectedLogLevel.Value;
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