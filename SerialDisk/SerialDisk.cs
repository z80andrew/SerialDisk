using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Comms;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Storage;
using AtariST.SerialDisk.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk
{
    public class SerialDisk
    {
        private static ApplicationSettings _applicationSettings;
        private static Logger _logger;
        private static DiskParameters _diskParameters;
        private static Disk _disk;
        private static Serial _serial;

        private static string FormatEnumParams(Type enumerationType)
        {
            StringBuilder enumString = new StringBuilder();

            foreach (var item in Enum.GetNames(enumerationType))
            {
                enumString.Append(item);
                enumString.Append('|');
            }

            enumString.Remove(enumString.Length - 1, 1);

            return enumString.ToString();
        }

        private static void PrintUsage(ApplicationSettings _applicationSettings)
        {
            Console.WriteLine();

            Console.WriteLine("Usage:");
            Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " [Options] [virtual_disk_path]");
            Console.WriteLine();

            List<String> parameters = Constants.ConsoleParameterMappings.Keys.ToList();

            Console.WriteLine("Options (default):");
            Console.WriteLine($"{parameters[0]} <disk_size_in_MiB> ({_applicationSettings.DiskSettings.DiskSizeMiB})");
            Console.WriteLine($"{parameters[1]} [{FormatEnumParams(typeof(TOSVersion))}] ({_applicationSettings.DiskSettings.DiskTOSCompatibility})");
            Console.WriteLine($"{parameters[2]} <sectors> ({_applicationSettings.DiskSettings.RootDirectorySectors})");
            Console.WriteLine($"{parameters[3]} [True|False] ({_applicationSettings.IsCompressionEnabled})");

            Console.WriteLine($"{parameters[4]} [port_name] ({_applicationSettings.SerialSettings.PortName})");
            Console.WriteLine($"{parameters[5]} <baud_rate> ({_applicationSettings.SerialSettings.BaudRate})");
            Console.WriteLine($"{parameters[6]} <data_bits> ({_applicationSettings.SerialSettings.DataBits})");
            Console.WriteLine($"{parameters[7]} [{FormatEnumParams(typeof(StopBits))}] ({_applicationSettings.SerialSettings.StopBits})");
            Console.WriteLine($"{parameters[8]} [{FormatEnumParams(typeof(Parity))}] ({_applicationSettings.SerialSettings.Parity})");
            Console.WriteLine($"{parameters[9]} [{FormatEnumParams(typeof(Handshake))}] ({_applicationSettings.SerialSettings.Handshake})");

            Console.WriteLine($"{parameters[10]} [{FormatEnumParams(typeof(Constants.LoggingLevel))}] ({_applicationSettings.LoggingLevel})");
            Console.WriteLine($"{parameters[11]} [log_file_name]");
            Console.WriteLine();

            Console.WriteLine("Serial ports available:");

            foreach (string portName in SerialPort.GetPortNames())
                Console.Write(portName + " ");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static string ParseLocalDirectoryPath(string _applicationSettingsPath, string[] args)
        {
            string localDirectoryPath;

            // args length is odd, assume final arg is a path
            if (args.Length % 2 != 0)
            {
                if (Directory.Exists(args.Last()))
                    localDirectoryPath = args.Last();

                else
                    throw new Exception($"Could not find path {args.Last()}");
            }

            else
            {
                if (Directory.Exists(_applicationSettingsPath))
                    localDirectoryPath = _applicationSettingsPath;

                else
                    throw new Exception($"Could not find path {_applicationSettingsPath}");
            }


            DirectoryInfo localDirectoryInfo = new DirectoryInfo(localDirectoryPath);
            return localDirectoryInfo.FullName;
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IDisk, Disk>();
            serviceCollection.AddSingleton<ISerial, Serial>();
            serviceCollection.AddSingleton<ILogger, Logger>();
        }

        private static Task ListenForConsoleKeypress()
        {
            return Task.Factory.StartNew(() =>
            {
                var keyInfo = new ConsoleKeyInfo();
                do
                {
                    keyInfo = Console.ReadKey(true);
                    if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0 && keyInfo.Key == ConsoleKey.R) _disk.ReimportLocalDirectoryContents();

                } while ((keyInfo.Modifiers & ConsoleModifiers.Control) == 0 || keyInfo.Key != ConsoleKey.X);
            });
        }

        public static void Main(string[] args)
        {
            var versionMessage = "Serial Disk v" + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            Console.WriteLine(versionMessage);
           
            #region Dependency injection

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            #endregion

            #region Application settings

            try
            {
                var defaultConfigResourceName = $"AtariST.SerialDisk.Resources.default_config_{OSHelper.OperatingSystemName.ToLower()}.json";

                using (var defaultConfigStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(defaultConfigResourceName))
                {
                    DataContractJsonSerializer appSettingsSerializer = new DataContractJsonSerializer(typeof(ApplicationSettings));
                    _applicationSettings = (ApplicationSettings)appSettingsSerializer.ReadObject(defaultConfigStream);
                }

                var configBuilder = new ConfigurationBuilder();

                configBuilder.AddJsonFile("serialdisk.config", true, false)
                    .Build()
                    .Bind(_applicationSettings);

                if (args.Any() && args.Where(arg => arg.ToLowerInvariant().StartsWith("--help")).Any())
                {
                    PrintUsage(_applicationSettings);
                    return;
                }

                configBuilder.AddCommandLine(args, Constants.ConsoleParameterMappings)
                    .Build()
                    .Bind(_applicationSettings);

                _applicationSettings.LocalDirectoryPath = ParseLocalDirectoryPath(_applicationSettings.LocalDirectoryPath, args);
            }

            catch (Exception parameterException)
            {
                Console.WriteLine($"Error parsing parameters: {parameterException.Message}");
                return;
            }


            if (String.IsNullOrEmpty(_applicationSettings.LocalDirectoryPath)
                || !Directory.Exists(_applicationSettings.LocalDirectoryPath))
            {
                Console.WriteLine($"Local directory path {_applicationSettings.LocalDirectoryPath} not found.");
                return;
            }

            #endregion

            _logger = new Logger(_applicationSettings.LoggingLevel, _applicationSettings.LogFileName);

            _logger.LogToFile(versionMessage);

            var json = JsonSerializer.Serialize(_applicationSettings, typeof(ApplicationSettings));

            _logger.Log(json, LoggingLevel.All);

            _logger.Log($"Operating system: {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture} {System.Runtime.InteropServices.RuntimeInformation.OSDescription}", LoggingLevel.Debug);
            _logger.Log($"Framework version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}", LoggingLevel.Debug);

            _diskParameters = new DiskParameters(_applicationSettings.LocalDirectoryPath, _applicationSettings.DiskSettings, _logger);

            _logger.Log($"Importing local directory contents from {_applicationSettings.LocalDirectoryPath}", Constants.LoggingLevel.Debug);

            _disk = new Disk(_diskParameters, _logger);

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

            _serial = new Serial(_applicationSettings.SerialSettings, _disk, _logger, cancelTokenSource, _applicationSettings.IsCompressionEnabled);

            _logger.Log($"Baud rate:{_applicationSettings.SerialSettings.BaudRate} | Data bits:{_applicationSettings.SerialSettings.DataBits}" +
                $" | Parity:{_applicationSettings.SerialSettings.Parity} | Stop bits:{_applicationSettings.SerialSettings.StopBits} | Flow control:{_applicationSettings.SerialSettings.Handshake}", LoggingLevel.Info);
            _logger.Log($"Using local directory {_applicationSettings.LocalDirectoryPath} as a {_applicationSettings.DiskSettings.DiskSizeMiB}MiB virtual disk", LoggingLevel.Info);
            _logger.Log($"Compression: " + (_applicationSettings.IsCompressionEnabled ? "Enabled" : "Disabled"), LoggingLevel.Info);
            _logger.Log($"Logging level: { _applicationSettings.LoggingLevel} ", LoggingLevel.Info);

            Console.WriteLine("Press Ctrl-X to quit, Ctrl-R to reimport local disk content.");

            Task keyboardListener = ListenForConsoleKeypress();

            try
            {
                keyboardListener.Wait(cancelTokenSource.Token);
            }

            catch (OperationCanceledException ex)
            {
                _logger.Log("Thread cancellation requested", LoggingLevel.Debug);
                _logger.Log(ex.Message, LoggingLevel.Debug);
            }

            _serial.Dispose();
            _logger.Dispose();

            Console.ResetColor();
        }
    }
}