using AtariST.SerialDisk.Models;
using ReactiveUI;
using System.IO.Ports;
using static AtariST.SerialDisk.Common.Constants;

namespace SerialDiskUI.Models
{
    public class SerialDiskUIModel : ReactiveObject
    {
        public ApplicationSettings ApplicationSettings { get; set; }

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

        //private bool _listening;
        //public bool Listening
        //{
        //    get => _listening;
        //    set => this.RaiseAndSetIfChanged(ref _listening, value);
        //}

        private bool _isOutputCompressionEnabled;
        public bool IsOutputCompressionEnabled
        {
            get => _isOutputCompressionEnabled;
            set => this.RaiseAndSetIfChanged(ref _isOutputCompressionEnabled, value);
        }

        public SerialDiskUIModel(ApplicationSettings appSettings)
        {
            ApplySettings(appSettings);
        }

        private void ApplySettings(ApplicationSettings settings)
        {
            if (settings != null)
            {
                // TODO: don't store entire settings model if possible
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
            }
        }
    }
}
