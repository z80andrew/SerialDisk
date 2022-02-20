using AtariST.SerialDisk.Models;
using ReactiveUI;
using System.IO.Ports;
using static AtariST.SerialDisk.Common.Constants;

namespace SerialDiskUI.Models
{
    public class SerialDiskUIModel : ReactiveObject
    {
        public UIApplicationSettings ApplicationSettings { get; set; }

        private string _virtualDiskFolder;
        public string VirtualDiskFolder
        {
            get => _virtualDiskFolder;
            set => this.RaiseAndSetIfChanged(ref _virtualDiskFolder, value);
        }

        private LoggingLevel _loggingLevel;
        public LoggingLevel LoggingLevel
        {
            get => _loggingLevel;
            set => this.RaiseAndSetIfChanged(ref _loggingLevel, value);
        }

        private bool _isLogFileEnabled;
        public bool IsLogFileEnabled
        {
            get => _isLogFileEnabled;
            set => this.RaiseAndSetIfChanged(ref _isLogFileEnabled, value);
        }

        private string _logFileName;
        public string LogFileName
        {
            get => _logFileName;
            set => this.RaiseAndSetIfChanged(ref _logFileName, value);
        }

        private string _comPortName;
        public string ComPortName
        {
            get => _comPortName;
            set => this.RaiseAndSetIfChanged(ref _comPortName, value);
        }

        private int _baudRate;
        public int BaudRate
        {
            get => _baudRate;
            set => this.RaiseAndSetIfChanged(ref _baudRate, value);
        }

        private int _dataBits;
        public int DataBits
        {
            get => _dataBits;
            set => this.RaiseAndSetIfChanged(ref _dataBits, value);
        }

        private Parity _parity;
        public Parity Parity
        {
            get => _parity;
            set => this.RaiseAndSetIfChanged(ref _parity, value);
        }

        private StopBits _stopBits;
        public StopBits StopBits
        {
            get => _stopBits;
            set => this.RaiseAndSetIfChanged(ref _stopBits, value);
        }

        private Handshake _handshake;
        public Handshake Handshake
        {
            get => _handshake;
            set => this.RaiseAndSetIfChanged(ref _handshake, value);
        }

        private bool _isOutputCompressionEnabled;
        public bool IsOutputCompressionEnabled
        {
            get => _isOutputCompressionEnabled;
            set => this.RaiseAndSetIfChanged(ref _isOutputCompressionEnabled, value);
        }

        private bool _isLogDisplayEnabled;
        public bool IsLogDisplayEnabled
        {
            get => _isLogDisplayEnabled;
            set => this.RaiseAndSetIfChanged(ref _isLogDisplayEnabled, value);
        }

        public SerialDiskUIModel(UIApplicationSettings appSettings)
        {
            ApplySettings(appSettings);
        }

        private void ApplySettings(UIApplicationSettings settings)
        {
            if (settings != null)
            {
                ApplicationSettings = settings;

                VirtualDiskFolder = settings.LocalDirectoryPath;

                //LoggingLevel = settings.LoggingLevel;
                LoggingLevel = LoggingLevel.All;
                LogFileName = settings.LogFileName;

                IsOutputCompressionEnabled = settings.IsCompressionEnabled;

                ComPortName = settings.SerialSettings.PortName;
                BaudRate = settings.SerialSettings.BaudRate;
                DataBits = settings.SerialSettings.DataBits;
                Parity = settings.SerialSettings.Parity;
                StopBits = settings.SerialSettings.StopBits;
                Handshake = settings.SerialSettings.Handshake;

                IsLogDisplayEnabled = settings.IsLogDisplayEnabled;
            }
        }
    }
}
