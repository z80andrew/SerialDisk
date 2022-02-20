using Avalonia;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Reactive;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using SerialDiskUI.Services;
using System.Threading;
using SerialDiskUI.Models;
using System.IO.Ports;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using AtariST.SerialDisk.Models;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;

namespace SerialDiskUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private const double ICON_ENABLED_OPACITY = 1.0;
        private const double ICON_DISABLED_OPACITY = 0.15;

        private SerialDiskService _serialDiskService;
        private SerialDiskUIModel _model;
        private StatusService _statusService;

        private readonly ObservableAsPropertyHelper<string> _comPortName;
        public string ComPortName => _comPortName.Value;

        private readonly ObservableAsPropertyHelper<int> _baudRate;
        public int BaudRate => _baudRate.Value;

        private readonly ObservableAsPropertyHelper<int> _dataBits;
        public int DataBits => _dataBits.Value;

        private readonly ObservableAsPropertyHelper<StopBits> _stopBits;
        public StopBits StopBits => _stopBits.Value;

        private readonly ObservableAsPropertyHelper<Parity> _parity;
        public Parity Parity => _parity.Value;

        private readonly ObservableAsPropertyHelper<Handshake> _handshake;
        public Handshake Handshake => _handshake.Value;

        private readonly ObservableAsPropertyHelper<string> _virtualDiskFolder;
        public string VirtualDiskFolder => _virtualDiskFolder.Value;

        private readonly ObservableAsPropertyHelper<bool> _isOutputCompressionEnabled;
        public bool IsOutputCompressionEnabled => _isOutputCompressionEnabled.Value;

        private readonly ObservableAsPropertyHelper<string> _isOutputCompressionEnabledText;
        public string IsOutputCompressionEnabledText => _isOutputCompressionEnabledText.Value;

        private readonly ObservableAsPropertyHelper<bool> _isLogDisplayEnabled;
        public bool IsLogDisplayEnabled => _isLogDisplayEnabled.Value;

        // Status messages
        private readonly ObservableAsPropertyHelper<AtariST.SerialDisk.Common.Status.StatusKey> _status;
        public AtariST.SerialDisk.Common.Status.StatusKey Status => _status.Value;

        private readonly ObservableAsPropertyHelper<string> _statusText;
        public string StatusText => _statusText.Value;

        private readonly ObservableAsPropertyHelper<int> _totalBytes;
        public int TotalBytes=> _totalBytes.Value;

        private readonly ObservableAsPropertyHelper<int> _transferredBytes;
        public int TransferredBytes => _transferredBytes.Value;

        private readonly ObservableAsPropertyHelper<string> _transferPercent;
        public string TransferPercent => _transferPercent.Value;

        private readonly ObservableAsPropertyHelper<bool> _serialPortOpen;
        public bool SerialPortOpen => _serialPortOpen.Value;

        // private SourceList<LogMessage> _logMessages;
        //public SourceList<LogMessage> LogMessages => _logMessages;

        //private readonly ObservableAsPropertyHelper<SourceList<LogMessage>> LogMessages;

        public ObservableCollectionExtended<LogMessage> LogOutput;

        private double _sendIconOpacity;
        public double SendIconOpacity
        {
            get => _sendIconOpacity;
            set => this.RaiseAndSetIfChanged(ref _sendIconOpacity, value);
        }

        private double _receiveIconOpacity;
        public double ReceiveIconOpacity
        {
            get => _receiveIconOpacity;
            set => this.RaiseAndSetIfChanged(ref _receiveIconOpacity, value);
        }

        private string _logOutputString;
        public string LogOutputString
        {
            get => _logOutputString;
            set => this.RaiseAndSetIfChanged(ref _logOutputString, value);
        }

        public ICommand StartSerialDiskCommand { get; }
        public ICommand ShowVirtualDiskFolderCommand { get; }

        public ICommand ShowSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand HandleStatusChangeCommand  { get; }
        //public ICommand DisplayLogMessageCommand { get; }

        public Interaction<SettingsWindowViewModel, SerialDiskUIModel?> ShowSettingsDialog { get; }
        //public Interaction<bool, Unit> ToggleLogVisibility { get; }

        public MainWindowViewModel()
        {
            // Just for designer
        }

        public MainWindowViewModel(SerialDiskUIModel model, StatusService statusService)
        {
            _model = model;
            _statusService = statusService;

            SendIconOpacity = ICON_DISABLED_OPACITY;
            ReceiveIconOpacity = ICON_DISABLED_OPACITY;

            #region Configure model

            _comPortName = _model.WhenAnyValue(x => x.ComPortName).ToProperty(this, x => x.ComPortName);
            _baudRate = _model.WhenAnyValue(x => x.BaudRate).ToProperty(this, x => x.BaudRate);
            _dataBits = _model.WhenAnyValue(x => x.DataBits).ToProperty(this, x => x.DataBits);
            _stopBits = _model.WhenAnyValue(x => x.StopBits).ToProperty(this, x => x.StopBits);
            _parity = _model.WhenAnyValue(x => x.Parity).ToProperty(this, x => x.Parity);
            _handshake = _model.WhenAnyValue(x => x.Handshake).ToProperty(this, x => x.Handshake);

            _virtualDiskFolder = _model.WhenAnyValue(x => x.VirtualDiskFolder).ToProperty(this, x => x.VirtualDiskFolder);

            _status = _statusService.WhenAnyValue(x => x.Status).ToProperty(this, x => x.Status);
            _statusText = _statusService.WhenAnyValue(x => x.StatusWithMessage).ToProperty(this, x => x.StatusText);
            _transferredBytes = statusService.WhenAnyValue(x => x.TransferredBytes).ToProperty(this, x => x.TransferredBytes);
            _totalBytes = statusService.WhenAnyValue(x => x.TotalBytes).ToProperty(this, x => x.TotalBytes);

            _transferPercent = statusService.WhenAnyValue(x => x.TransferredBytes)
                .Select(x => GetTransferPercent())
                .ToProperty(this, x => x.TransferPercent);

            _serialPortOpen = statusService.WhenAnyValue(x => x.Status)
                .Select(x => x != AtariST.SerialDisk.Common.Status.StatusKey.Stopped)
                .ToProperty(this, x => x.SerialPortOpen);

            _totalBytes = statusService.WhenAnyValue(x => x.TotalBytes).ToProperty(this, x => x.TotalBytes);

            _isOutputCompressionEnabled = _model.WhenAnyValue(x => x.IsOutputCompressionEnabled).ToProperty(this, x => x.IsOutputCompressionEnabled);
            _isOutputCompressionEnabledText = _model.WhenAnyValue(x => x.IsOutputCompressionEnabled)
                .Select(x => x == true ? "Enabled" : "Disabled")
                .ToProperty(this, x => x.IsOutputCompressionEnabledText);

            _isLogDisplayEnabled = _model.WhenAnyValue(x => x.IsLogDisplayEnabled).ToProperty(this, x => x.IsLogDisplayEnabled);
            _model.IsLogDisplayEnabled = true;

            #endregion

            HandleStatusChangeCommand = ReactiveCommand.CreateFromTask<AtariST.SerialDisk.Common.Status.StatusKey, Unit>(UpdateStatus);

            _serialDiskService = new SerialDiskService();

            ShowSettingsDialog = new Interaction<SettingsWindowViewModel, SerialDiskUIModel?>();

            ShowSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settingsViewModel = new SettingsWindowViewModel(_model);
                var result = await ShowSettingsDialog.Handle(settingsViewModel);
                if (result != null) _model = result;
            });

            // Logging output
            LogOutput = new ObservableCollectionExtended<LogMessage>();

            _statusService.LogMessages
                .Connect()
                .Bind(LogOutput)
                .Subscribe();

            LogOutput.CollectionChanged += LogOutput_CollectionChanged;

            ExitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown(0);
            });

            ShowVirtualDiskFolderCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = VirtualDiskFolder};
                var process = Process.Start(startInfo);
            });

            StartSerialDiskCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_statusService.Status == AtariST.SerialDisk.Common.Status.StatusKey.Stopped)
                {
                    _serialDiskService.BeginSerialDisk(_model.ApplicationSettings, _statusService);
                }

                else
                {
                    _serialDiskService.EndSerialDisk();
                }
            });
        }

        private void LogOutput_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var hello = sender as ObservableCollectionExtended<LogMessage>;
            var lastLogMessage = hello[hello.Count - 1];
            LogOutputString += $"{lastLogMessage.TimeStamp}: [{lastLogMessage.MessageType}] {lastLogMessage.Message} \n";
        }

        private Task<Unit> UpdateStatus(AtariST.SerialDisk.Common.Status.StatusKey status)
        {
            switch (status)
            {
                case AtariST.SerialDisk.Common.Status.StatusKey.Receiving:
                    SendIconOpacity = ICON_DISABLED_OPACITY;
                    ReceiveIconOpacity = ICON_ENABLED_OPACITY;
                    break;

                case AtariST.SerialDisk.Common.Status.StatusKey.Sending:
                    ReceiveIconOpacity = ICON_DISABLED_OPACITY;
                    SendIconOpacity = ICON_ENABLED_OPACITY;
                    break;

                default:
                    ReceiveIconOpacity = ICON_DISABLED_OPACITY;
                    SendIconOpacity = ICON_DISABLED_OPACITY;
                    break;
            }

            return Task.FromResult<Unit>(Unit.Default);
        }

        private string GetTransferPercent()
        {
            var percentString = string.Empty;

            if(TotalBytes != 0 && TransferredBytes >= 0)
                percentString = (Convert.ToDecimal(TransferredBytes) / TotalBytes).ToString("0.0%", CultureInfo.CurrentCulture);

            return percentString;
        }
    }
}
