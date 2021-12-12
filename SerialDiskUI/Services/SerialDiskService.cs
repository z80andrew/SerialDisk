using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Comms;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Storage;
using System.Threading;

namespace SerialDiskUI.Services
{
    public class SerialDiskService
    {
        public ILogger _logger;
        IDisk _disk;
        ISerial _serial;
        DiskParameters _diskParameters;
        CancellationTokenSource _cancellationToken;

        public SerialDiskService()
        {
        }

        public void BeginSerialDisk(ApplicationSettings appSettings, StatusService statusService)
        {
            _logger = new Logger(appSettings.LoggingLevel, statusService, appSettings.LogFileName);

            _diskParameters = new DiskParameters(appSettings.LocalDirectoryPath, appSettings.DiskSettings, _logger);
            _logger.Log($"Importing local directory contents from {appSettings.LocalDirectoryPath}", Constants.LoggingLevel.Debug);
            _disk = new Disk(_diskParameters, _logger);

            _cancellationToken = new CancellationTokenSource();
            _serial = new Serial(appSettings.SerialSettings, _disk, _logger, statusService, _cancellationToken, appSettings.IsCompressionEnabled);
        }

        public void EndSerialDisk()
        {
            _cancellationToken?.Cancel();
            _serial?.Dispose();
        }
    }
}
