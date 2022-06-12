using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Comms;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Storage;
using System;
using System.Threading;

namespace SerialDiskUI.Services
{
    public class SerialDiskService
    {
        IDisk _disk;
        ISerial _serial;
        DiskParameters _diskParameters;
        CancellationTokenSource _cancellationToken;

        public SerialDiskService()
        {
        }

        public void BeginSerialDisk(ApplicationSettings appSettings, IStatusService statusService, ILogger logger)
        {
            _diskParameters = new DiskParameters(appSettings.LocalDirectoryPath, appSettings.DiskSettings, logger);
            logger.Log($"Importing local directory contents from {appSettings.LocalDirectoryPath}", Constants.LoggingLevel.Debug);

            try
            {
                _disk = new Disk(_diskParameters, logger);

                _cancellationToken = new CancellationTokenSource();
                _serial = new Serial(appSettings.SerialSettings, _disk, logger, statusService, _cancellationToken, appSettings.IsCompressionEnabled);
            }

            catch (Exception ex)
            {
                switch (ex.Source)
                {
                    case "System.IO.Ports":
                        statusService.SetStatus(Status.StatusKey.Error, $"could not open serial port - {ex.Message}");
                        break;
                    default:
                        statusService.SetStatus(Status.StatusKey.Error, ex.Message);
                        break;
                }
            }
        }

        public void ReimportLocalDirectoryContents()
        {
            _disk.ReimportLocalDirectoryContents();
        }

        public void EndSerialDisk()
        {
            _cancellationToken?.Cancel();
            _serial?.Dispose();
        }
    }
}
