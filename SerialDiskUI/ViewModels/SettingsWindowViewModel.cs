using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
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
        private const string COMPORT_OTHER = "Other";

        private SerialDiskUIModel _settings;
        private bool _isCOMPortTextBoxVisible;
        private string _otherCOMPortName;
        private bool _isLogDisplayEnabled;

        private KeyValuePair<string, string> _selectedCOMPort;
        public KeyValuePair<string, string> SelectedCOMPort
        {
            get => _selectedCOMPort;

            set
            {
                _selectedCOMPort = value;
                IsCOMPortTextBoxVisible = String.Equals(_selectedCOMPort.Value, COMPORT_OTHER, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public KeyValuePair<string, int> SelectedBaud { get; set; }
        public KeyValuePair<string, int> SelectedDataBits { get; set; }
        public KeyValuePair<string, StopBits> SelectedStopBits { get; set; }
        public KeyValuePair<string, Handshake> SelectedHandshake { get; set; }
        public KeyValuePair<string, Parity> SelectedParity { get; set; }

        private string _selectedFolder;
        public string SelectedFolder
        {
            get => _selectedFolder;
            set =>  this.RaiseAndSetIfChanged(ref _selectedFolder, value);
        }

        private int _virtualDiskSizeMB;
        public int VirtualDiskSizeMB
        {
            get => _virtualDiskSizeMB;
            set => this.RaiseAndSetIfChanged(ref _virtualDiskSizeMB, value);
        }

        private bool _isLogFileEnabled;

        public bool IsLogFileEnabled
        {
            get => _isLogFileEnabled;
            set => this.RaiseAndSetIfChanged(ref _isLogFileEnabled, value);
        }

        private string _selectedFile;
        public string SelectedFile
        {
            get => _selectedFile;
            set => this.RaiseAndSetIfChanged(ref _selectedFile, value);
        }

        public bool IsCOMPortTextBoxVisible
        {
            get => _isCOMPortTextBoxVisible;
            set => this.RaiseAndSetIfChanged(ref _isCOMPortTextBoxVisible, value);
        }

        public string OtherCOMPortName
        {
            get => _otherCOMPortName;
            set => this.RaiseAndSetIfChanged(ref _otherCOMPortName, value);
        }

        private bool _isCompressionEnabled;

        public bool IsCompressionEnabled
        {
            get => _isCompressionEnabled;
            set => this.RaiseAndSetIfChanged(ref _isCompressionEnabled, value);
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
            ShowFolderDialog = new Interaction<string, string?>();
            ShowFileDialog = new Interaction<string, string?>();

            ChooseFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
            ChooseFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);
            ApplySettingsCommand = ReactiveCommand.CreateFromTask(ApplySettings);
            CloseSettingsCommand = ReactiveCommand.CreateFromTask(CloseSettings);

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

            portChoices.Add(new KeyValuePair<string, string>(COMPORT_OTHER, COMPORT_OTHER));

            COMPortChoices = portChoices.ToArray();
        }

        private void ApplySettingsValues(SerialDiskUIModel settings)
        {
            if (COMPortChoices.Where(x => x.Value == settings.ComPortName).Any())
            {
                SelectedCOMPort = COMPortChoices.Where(x => x.Value == settings.ComPortName).FirstOrDefault();
                IsCOMPortTextBoxVisible = false;
            }

            else
            {
                SelectedCOMPort = COMPortChoices.Where(x => x.Value == COMPORT_OTHER).First();
                IsCOMPortTextBoxVisible = true;
                OtherCOMPortName = settings.ComPortName;
            }

            SelectedBaud = BaudRateChoices.Where(x => x.Value == settings.BaudRate).FirstOrDefault();
            SelectedDataBits = DataBitsChoices.Where(x => x.Value == settings.DataBits).FirstOrDefault();
            SelectedStopBits = StopBitsChoices.Where(x => x.Value == settings.StopBits).FirstOrDefault();
            SelectedHandshake = HandshakeChoices.Where(x => x.Value == settings.Handshake).FirstOrDefault();
            SelectedParity = ParityChoices.Where(x => x.Value == settings.Parity).FirstOrDefault();

            SelectedFolder = settings.VirtualDiskFolder;
            VirtualDiskSizeMB = settings.VirtualDiskSizeMB;

            IsLogDisplayEnabled = settings.IsLogDisplayEnabled;
            SelectedLogLevel = LogLevelChoices.Where(x => x.Value == settings.LoggingLevel).FirstOrDefault();
            IsLogFileEnabled = settings.IsLogFileEnabled;
            SelectedFile = settings.LogFileName;

            IsCompressionEnabled = _settings.IsOutputCompressionEnabled;
        }

        public ReactiveCommand<Unit, SerialDiskUIModel> ApplySettingsCommand { get; }
        public ReactiveCommand<Unit, SerialDiskUIModel> CloseSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ChooseFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ChooseFileCommand { get; }
        public Interaction<string, string?> ShowFolderDialog { get; }
        public Interaction<string, string?> ShowFileDialog { get; }

        public ValidationContext ValidationContext { get; } = new ValidationContext();

        private async Task<SerialDiskUIModel> ApplySettings()
        {
            if (_settings != null)
            {
                var comPortName = String.Equals(SelectedCOMPort.Value, COMPORT_OTHER, StringComparison.CurrentCultureIgnoreCase) ? _otherCOMPortName : SelectedCOMPort.Value;


                // TODO: Remove the use of _settings properties if everything is in _settings.ApplicationSettings
                _settings.ComPortName = _settings.ApplicationSettings.SerialSettings.PortName = comPortName;
                _settings.BaudRate = _settings.ApplicationSettings.SerialSettings.BaudRate = SelectedBaud.Value;
                _settings.DataBits = _settings.ApplicationSettings.SerialSettings.DataBits = SelectedDataBits.Value;
                _settings.StopBits = _settings.ApplicationSettings.SerialSettings.StopBits = SelectedStopBits.Value;
                _settings.Handshake = _settings.ApplicationSettings.SerialSettings.Handshake = SelectedHandshake.Value;
                _settings.Parity = _settings.ApplicationSettings.SerialSettings.Parity = SelectedParity.Value;

                _settings.VirtualDiskFolder = _settings.ApplicationSettings.LocalDirectoryPath = SelectedFolder;
                _settings.VirtualDiskSizeMB = _settings.ApplicationSettings.DiskSettings.DiskSizeMiB = VirtualDiskSizeMB;

                _settings.IsLogDisplayEnabled = _settings.ApplicationSettings.IsLogDisplayEnabled = IsLogDisplayEnabled;
                _settings.LoggingLevel = _settings.ApplicationSettings.LoggingLevel = SelectedLogLevel.Value;
                _settings.IsLogFileEnabled = _settings.ApplicationSettings.IsLogFileEnabled = IsLogFileEnabled;
                _settings.LogFileName = _settings.ApplicationSettings.LogFileName = SelectedFile;

                _settings.IsOutputCompressionEnabled = _settings.ApplicationSettings.IsCompressionEnabled = IsCompressionEnabled;

                _settings.ApplicationSettings.WriteSettingsToDisk();
            }

            return _settings;
        }

        private async Task<SerialDiskUIModel> CloseSettings()
        {
            return null;
        }

        private async Task OpenFolderAsync()
        {
            var folderName = await ShowFolderDialog.Handle(_settings.VirtualDiskFolder);

            if (folderName is object)
            {
                SelectedFolder = folderName;
            }
        }

        private async Task OpenFileAsync()
        {
            var fileName = await ShowFileDialog.Handle(_settings.LogFileName);

            if (fileName is object)
            {
                SelectedFile = fileName;
            }
        }
    }
}