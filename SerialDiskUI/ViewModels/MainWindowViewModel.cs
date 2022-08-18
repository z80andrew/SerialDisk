using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.Models;
using Z80andrew.SerialDisk.SerialDiskUI.Models;
using Z80andrew.SerialDisk.SerialDiskUI.Services;
using Z80andrew.SerialDisk.Utilities;

namespace Z80andrew.SerialDisk.SerialDiskUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private const double ICON_ENABLED_OPACITY = 1.0;
        private const double ICON_DISABLED_OPACITY = 0.15;

        private readonly SerialDiskService _serialDiskService;
        private SerialDiskUIModel _model;
        private readonly IStatusService _statusService;

        private string _releasesJson;

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
        private readonly ObservableAsPropertyHelper<Status.StatusKey> _status;
        public Status.StatusKey Status => _status.Value;

        private readonly ObservableAsPropertyHelper<string> _statusText;
        public string StatusText => _statusText.Value;

        private readonly ObservableAsPropertyHelper<int> _totalBytes;
        public int TotalBytes => _totalBytes.Value;

        private readonly ObservableAsPropertyHelper<int> _transferredBytes;
        public int TransferredBytes => _transferredBytes.Value;

        private readonly ObservableAsPropertyHelper<string> _transferPercent;
        public string TransferPercent => _transferPercent.Value;

        private readonly ObservableAsPropertyHelper<bool> _serialPortOpen;
        public bool SerialPortOpen => _serialPortOpen.Value;


        public ObservableCollection<LogMessage> LogItems { get; }
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

        public ICommand ClearLogMessagesCommand { get; }

        public Interaction<SettingsWindowViewModel, SerialDiskUIModel> ShowSettingsDialog { get; }

        public Interaction<AboutWindowViewModel, string> ShowAboutDialog { get; }

        public MainWindowViewModel(SerialDiskUIModel model, IStatusService statusService, ILogger logger)
        {
            LogItems = new ObservableCollection<LogMessage>();
            SendIconOpacity = ICON_DISABLED_OPACITY;
            ReceiveIconOpacity = ICON_DISABLED_OPACITY;

            _releasesJson = string.Empty;

            // Parameters are null at design-time
            if (model == null)
            {
                var defaultApplicationSettings = ConfigurationHelper.GetDefaultApplicationSettings();
                var appSettings = new UIApplicationSettings(defaultApplicationSettings)
                {
                    IsLogDisplayEnabled = true
                };

                model = new SerialDiskUIModel(appSettings);
            }

            if (statusService == null) 
                statusService = new StatusService();

            if (logger == null) 
                logger = new Logger(Constants.LoggingLevel.All);

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
                .Select(x => x != Z80andrew.SerialDisk.Common.Status.StatusKey.Stopped && x != Z80andrew.SerialDisk.Common.Status.StatusKey.Error)
                .ToProperty(this, x => x.SerialPortOpen);

            _statusService.WhenAnyValue(x => x.Status)
                .Subscribe(x =>
                {
                    var isPortOpen = x != Z80andrew.SerialDisk.Common.Status.StatusKey.Stopped && x != Z80andrew.SerialDisk.Common.Status.StatusKey.Error;
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
                try
                {
                    if (isLogEnabled) logger.SetLogFile(_model.LogFilePath);
                    else logger.UnsetLogFile();
                }

                catch (Exception logException)
                {
                    _statusService.SetStatus(Z80andrew.SerialDisk.Common.Status.StatusKey.Error, logException.Message);
                }
            });

            _model.WhenAnyValue(m => m.LoggingLevel).Subscribe(logLevel =>
            {
                logger.LogLevel = logLevel;
            });

            _model.WhenAnyValue(m => m.LogFilePath).Subscribe(logFile =>
            {
                try
                {
                    if (_model.IsLogFileEnabled) logger.SetLogFile(_model.LogFilePath);
                }

                catch (Exception logException)
                {
                    _statusService.SetStatus(Z80andrew.SerialDisk.Common.Status.StatusKey.Error, logException.Message);
                }
            });

            _isLogDisplayEnabled = _model.WhenAnyValue(x => x.IsLogDisplayEnabled).ToProperty(this, x => x.IsLogDisplayEnabled);

            #endregion

            #region Configure commands

            _serialDiskService = new SerialDiskService();

            ShowSettingsDialog = new Interaction<SettingsWindowViewModel, SerialDiskUIModel>();

            ShowSettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var settingsViewModel = new SettingsWindowViewModel(_model);
                var result = await ShowSettingsDialog.Handle(settingsViewModel);
                if (result != null) _model = result;
            });

            ShowAboutDialog = new Interaction<AboutWindowViewModel, string>();

            ShowAboutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var aboutViewModel = new AboutWindowViewModel(logger, _releasesJson);
                _releasesJson = await ShowAboutDialog.Handle(aboutViewModel);
            });

            // Logging output
            logger.WhenAnyValue(x => x.LogMessage).
                Subscribe(x =>
                {
                    if (LogItems.Count > MaxLogLines) LogItems.RemoveAt(0);
                    LogItems.Add(x);
                });

            _statusService.WhenAnyValue(x => x.Status).
                Subscribe(x =>
                {
                    UpdateStatus(x);
                });

            ExitCommand = ReactiveCommand.Create(() =>
            {
                (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow.Close();
            });

            ShowVirtualDiskFolderCommand = ReactiveCommand.Create(() =>
            {
                var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = VirtualDiskFolder };
                var process = Process.Start(startInfo);
            });

            RefreshVirtualDiskFolderCommand = ReactiveCommand.Create(() =>
            {
                _serialDiskService.ReimportLocalDirectoryContents();
                _statusService.SetStatus(Z80andrew.SerialDisk.Common.Status.StatusKey.OperationComplete, "refreshing disk contents");
            });

            StartSerialDiskCommand = ReactiveCommand.Create(() =>
            {
                if (_statusService.Status == Z80andrew.SerialDisk.Common.Status.StatusKey.Stopped
                        || _statusService.Status == Z80andrew.SerialDisk.Common.Status.StatusKey.Error)
                {
                    _serialDiskService.BeginSerialDisk(_model.ApplicationSettings, _statusService, logger);
                }

                else
                {
                    _serialDiskService.EndSerialDisk();
                }
            });

            ClearLogMessagesCommand = ReactiveCommand.Create(() =>
            {
                LogItems.Clear();
            });

            #endregion
        }

        private Task<Unit> UpdateStatus(Status.StatusKey status)
        {
            switch (status)
            {
                case SerialDisk.Common.Status.StatusKey.Receiving:
                    SendIconOpacity = ICON_DISABLED_OPACITY;
                    ReceiveIconOpacity = ICON_ENABLED_OPACITY;
                    break;

                case SerialDisk.Common.Status.StatusKey.Sending:
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

            if (TotalBytes != 0 && TransferredBytes >= 0)
                percentString = (Convert.ToDecimal(TransferredBytes) / TotalBytes).ToString("0.0%", CultureInfo.CurrentCulture);

            return percentString;
        }
    }
}
