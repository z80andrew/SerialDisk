using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Comms;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Storage;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDiskCLI
{
    public class SerialDiskCLI : IDisposable
    {
        private readonly ILogger _logger;
        private IDisk _disk;
        private ISerial _serial;
        private IStatusService _statusService;

        private CancellationTokenSource _cancelTokenSource;

        public SerialDiskCLI(ApplicationSettings applicationSettings, DiskParameters diskParameters, ILogger logger)
        {
            _logger = logger;
            _cancelTokenSource = new CancellationTokenSource();

            Init(applicationSettings, diskParameters, _cancelTokenSource);
        }

        private void Init(ApplicationSettings applicationSettings, DiskParameters diskParameters, CancellationTokenSource cancelTokenSource)
        {
            var versionMessage = "Serial Disk v" + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            _logger.Log(versionMessage, LoggingLevel.Info);

            var json = JsonSerializer.Serialize(applicationSettings, typeof(ApplicationSettings));

            _logger.Log(json, LoggingLevel.All);

            _logger.Log($"Operating system: {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture} {System.Runtime.InteropServices.RuntimeInformation.OSDescription}", LoggingLevel.Debug);
            _logger.Log($"Framework version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}", LoggingLevel.Debug);

            diskParameters = new DiskParameters(applicationSettings.LocalDirectoryPath, applicationSettings.DiskSettings, _logger);

            _logger.Log($"Importing local directory contents from {applicationSettings.LocalDirectoryPath}", Constants.LoggingLevel.Debug);

            _statusService = new StatusService();

            _disk = new Disk(diskParameters, _logger);

            //if (applicationSettings.LoggingLevel > LoggingLevel.Info) _statusService.ShowDateTime = true;

            _serial = new Serial(applicationSettings.SerialSettings, _disk, _logger, _statusService, _cancelTokenSource, applicationSettings.IsCompressionEnabled);
        }

        private Task ListenForConsoleKeypress()
        {
            return Task.Factory.StartNew(() =>
            {
                var keyInfo = new ConsoleKeyInfo();
                do
                {
                    keyInfo = Console.ReadKey(true);
                    if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0 && keyInfo.Key == ConsoleKey.R) _disk.ReimportLocalDirectoryContents(); // Ctrl-R

                } while ((keyInfo.Modifiers & ConsoleModifiers.Control) == 0 || keyInfo.Key != ConsoleKey.X); // Ctrl-X
            });
        }

        public void ListenForKeyboardCommand()
        {
            Task keyboardListener = ListenForConsoleKeypress();

            try
            {
                keyboardListener.Wait(_cancelTokenSource.Token);
            }

            catch (OperationCanceledException ex)
            {
                _logger.Log("Thread cancellation requested", LoggingLevel.Debug);
                _logger.Log(ex.Message, LoggingLevel.Debug);
            }

            Console.ResetColor();
        }


        public void Dispose()
        {
            _serial.Dispose();
            _logger.Dispose();
        }
    }
}