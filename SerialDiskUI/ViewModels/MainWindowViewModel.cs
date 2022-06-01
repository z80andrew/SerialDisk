using Avalonia;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Reactive;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using SerialDiskUI.Services;
using SerialDiskUI.Models;
using System.IO.Ports;
using System.Globalization;
using System.Threading.Tasks;
using AtariST.SerialDisk.Models;
using DynamicData;
using DynamicData.Binding;
using AtariST.SerialDisk.Interfaces;
using System.IO;
using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Utilities;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;

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


        public ObservableCollection<string> LogItems { get; }
        public int MaxLogLines = 512;

        
        private double _reloadIconOpacity;
        public double ReloadIconOpacity
        {
            get => _reloadIconOpacity;
            set => this.RaiseAndSetIfChanged(ref _reloadIconOpacity, value);
        }

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

        public ICommand StartSerialDiskCommand { get; }
        public ICommand ShowVirtualDiskFolderCommand { get; }

        public ICommand RefreshVirtualDiskFolderCommand { get; }

        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand HandleStatusChangeCommand  { get; }

        public ICommand ClearLogMessagesCommand { get; }

        public Interaction<SettingsWindowViewModel, SerialDiskUIModel?> ShowSettingsDialog { get; }

        public Interaction<AboutWindowViewModel, SimpleDialogModel> ShowAboutDialog { get; }

        public MainWindowViewModel()
        {
            LogItems = new ObservableCollection<string>();
            SendIconOpacity = ICON_DISABLED_OPACITY;
            ReceiveIconOpacity = ICON_DISABLED_OPACITY;

            var defaultApplicationSettings = ConfigurationHelper.GetDefaultApplicationSettings();
            UIApplicationSettings appSettings = new UIApplicationSettings(defaultApplicationSettings);
            appSettings.IsLogDisplayEnabled = true;

            var model = new SerialDiskUIModel(appSettings);

            var statusService = _statusService = new StatusService();

            _serialDiskService = new SerialDiskService();

            var logger = new Logger(Constants.LoggingLevel.All, statusService);

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
                .Select(x => x != AtariST.SerialDisk.Common.Status.StatusKey.Stopped && x != AtariST.SerialDisk.Common.Status.StatusKey.Error)
                .ToProperty(this, x => x.SerialPortOpen);

            statusService.WhenAnyValue(x => x.Status)
                .Subscribe(x => {
                    var isPortOpen = x != AtariST.SerialDisk.Common.Status.StatusKey.Stopped && x != AtariST.SerialDisk.Common.Status.StatusKey.Error;
                    ReloadIconOpacity = isPortOpen ? ICON_ENABLED_OPACITY : ICON_DISABLED_OPACITY;
                });

            _totalBytes = statusService.WhenAnyValue(x => x.TotalBytes).ToProperty(this, x => x.TotalBytes);

            _isOutputCompressionEnabled = _model.WhenAnyValue(x => x.IsOutputCompressionEnabled).ToProperty(this, x => x.IsOutputCompressionEnabled);
            _isOutputCompressionEnabledText = _model.WhenAnyValue(x => x.IsOutputCompressionEnabled)
                .Select(x => x == true ? "Enabled" : "Disabled")
                .ToProperty(this, x => x.IsOutputCompressionEnabledText);

            // Logger properties
            _model.WhenAnyValue(m => m.IsLogFileEnabled).Subscribe(isLogEnabled =>
            {
                if (isLogEnabled) logger.SetLogFile(Path.GetDirectoryName(_model.LogFileName), Path.GetFileName(_model.LogFileName));
                else logger.UnsetLogFile();
            });

            _model.WhenAnyValue(m => m.LoggingLevel).Subscribe(logLevel =>
            {
                logger.LogLevel = logLevel;
            });

            _model.WhenAnyValue(m => m.LogFileName).Subscribe(logFile =>
            {
                if (_model.IsLogFileEnabled) logger.SetLogFile(Path.GetDirectoryName(_model.LogFileName), Path.GetFileName(_model.LogFileName));
            });

            _isLogDisplayEnabled = _model.WhenAnyValue(x => x.IsLogDisplayEnabled).ToProperty(this, x => x.IsLogDisplayEnabled);

            #endregion

            #region Configure commands

            HandleStatusChangeCommand = ReactiveCommand.CreateFromTask<AtariST.SerialDisk.Common.Status.StatusKey, Unit>(UpdateStatus);

            _serialDiskService = new SerialDiskService();

            ShowSettingsDialog = new Interaction<SettingsWindowViewModel, SerialDiskUIModel?>();

            ShowSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settingsViewModel = new SettingsWindowViewModel(_model);
                var result = await ShowSettingsDialog.Handle(settingsViewModel);
                if (result != null) _model = result;
            });

            ShowAboutDialog = new Interaction<AboutWindowViewModel, SimpleDialogModel>();

            ShowAboutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var aboutViewModel = new AboutWindowViewModel();
                var result = await ShowAboutDialog.Handle(aboutViewModel);
            });

            // Logging output
            _statusService.WhenAnyValue(x => x.StatusWithMessage).
                Subscribe(x => {
                    if (LogItems.Count() > MaxLogLines) LogItems.RemoveAt(0);
                    LogItems.Add(DateTime.Now.ToString("G") + " " + x);
                });

            ExitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown(0);
            });

            ShowVirtualDiskFolderCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = VirtualDiskFolder };
                var process = Process.Start(startInfo);
            });

            RefreshVirtualDiskFolderCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                _serialDiskService.ReimportLocalDirectoryContents();
            });

            StartSerialDiskCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_statusService.Status == AtariST.SerialDisk.Common.Status.StatusKey.Stopped
                        || _statusService.Status == AtariST.SerialDisk.Common.Status.StatusKey.Error)
                {
                    _serialDiskService.BeginSerialDisk(_model.ApplicationSettings, _statusService, logger);
                }

                else
                {
                    _serialDiskService.EndSerialDisk();
                }
            });

            ClearLogMessagesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                LogItems.Clear();
            });

            #endregion
        }

        public MainWindowViewModel(SerialDiskUIModel model, StatusService statusService, ILogger logger)
        {
            LogItems = new ObservableCollection<string>();
            SendIconOpacity = ICON_DISABLED_OPACITY;
            ReceiveIconOpacity = ICON_DISABLED_OPACITY;

            _model = model;
            _statusService = statusService;

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
                .Select(x => x != AtariST.SerialDisk.Common.Status.StatusKey.Stopped && x != AtariST.SerialDisk.Common.Status.StatusKey.Error)
                .ToProperty(this, x => x.SerialPortOpen);

            statusService.WhenAnyValue(x => x.Status)
                .Subscribe(x => {
                    var isPortOpen = x != AtariST.SerialDisk.Common.Status.StatusKey.Stopped && x != AtariST.SerialDisk.Common.Status.StatusKey.Error;
                    ReloadIconOpacity = isPortOpen ? ICON_ENABLED_OPACITY : ICON_DISABLED_OPACITY;
                });

            _totalBytes = statusService.WhenAnyValue(x => x.TotalBytes).ToProperty(this, x => x.TotalBytes);

            _isOutputCompressionEnabled = _model.WhenAnyValue(x => x.IsOutputCompressionEnabled).ToProperty(this, x => x.IsOutputCompressionEnabled);
            _isOutputCompressionEnabledText = _model.WhenAnyValue(x => x.IsOutputCompressionEnabled)
                .Select(x => x == true ? "Enabled" : "Disabled")
                .ToProperty(this, x => x.IsOutputCompressionEnabledText);

            // Logger properties
            _model.WhenAnyValue(m => m.IsLogFileEnabled).Subscribe(isLogEnabled =>
            {
                if (isLogEnabled) logger.SetLogFile(Path.GetDirectoryName(_model.LogFileName), Path.GetFileName(_model.LogFileName));
                else logger.UnsetLogFile();
            });

            _model.WhenAnyValue(m => m.LoggingLevel).Subscribe(logLevel =>
            {
                logger.LogLevel = logLevel;
            });

            _model.WhenAnyValue(m => m.LogFileName).Subscribe(logFile =>
            {
                if(_model.IsLogFileEnabled) logger.SetLogFile(Path.GetDirectoryName(_model.LogFileName), Path.GetFileName(_model.LogFileName));
            });

            _isLogDisplayEnabled = _model.WhenAnyValue(x => x.IsLogDisplayEnabled).ToProperty(this, x => x.IsLogDisplayEnabled);

            #endregion

            #region Configure commands

            HandleStatusChangeCommand = ReactiveCommand.CreateFromTask<AtariST.SerialDisk.Common.Status.StatusKey, Unit>(UpdateStatus);

            _serialDiskService = new SerialDiskService();

            ShowSettingsDialog = new Interaction<SettingsWindowViewModel, SerialDiskUIModel?>();

            ShowSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settingsViewModel = new SettingsWindowViewModel(_model);
                var result = await ShowSettingsDialog.Handle(settingsViewModel);
                if (result != null) _model = result;
            });

            ShowAboutDialog = new Interaction<AboutWindowViewModel, SimpleDialogModel>();

            ShowAboutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var aboutViewModel = new AboutWindowViewModel();
                var result = await ShowAboutDialog.Handle(aboutViewModel);
            });

            // Logging output
            _statusService.WhenAnyValue(x => x.StatusWithMessage).
                Subscribe(x => {
                    if (LogItems.Count() > MaxLogLines) LogItems.RemoveAt(0);
                    LogItems.Add(DateTime.Now.ToString("G") + " " + x);
                });

            ExitCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).Shutdown(0);
            });

            ShowVirtualDiskFolderCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = VirtualDiskFolder};
                var process = Process.Start(startInfo);
            });

            RefreshVirtualDiskFolderCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                _serialDiskService.ReimportLocalDirectoryContents();
            });

            StartSerialDiskCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (_statusService.Status == AtariST.SerialDisk.Common.Status.StatusKey.Stopped
                        || _statusService.Status == AtariST.SerialDisk.Common.Status.StatusKey.Error)
                {
                    _serialDiskService.BeginSerialDisk(_model.ApplicationSettings, _statusService, logger);
                }

                else
                {
                    _serialDiskService.EndSerialDisk();
                }
            });

            ClearLogMessagesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                LogItems.Clear();
            });

            #endregion
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
