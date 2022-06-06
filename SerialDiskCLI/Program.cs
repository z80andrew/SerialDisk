using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Storage;
using AtariST.SerialDisk.Utilities;
using AtariST.SerialDiskCLI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDiskCLI
{
    public static class Program
    {
        private static ApplicationSettings _applicationSettings;
        private static DiskParameters _diskParameters;

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

        private static void ConfigureServices(IServiceCollection serviceCollection, ApplicationSettings settings)
        {
            serviceCollection
                .AddSingleton<ILogger>(new Logger(_applicationSettings.LoggingLevel, _applicationSettings.LogFileName));
        }

        private static void ApplyConfiguration(string[] args)
        {
            try
            {
                _applicationSettings = ConfigurationHelper.GetDefaultApplicationSettings();

                var configBuilder = new ConfigurationBuilder();

                configBuilder.AddJsonFile(Common.Constants.configFileName, true, false)
                    .Build()
                    .Bind(_applicationSettings);

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
        }

        public static void Main(string[] args)
        {
            if (args.Any() && args.Where(arg => arg.ToLowerInvariant().StartsWith("--help")).Any())
            {
                PrintUsage(_applicationSettings);
                return;
            }

            ApplyConfiguration(args);

            if (String.IsNullOrEmpty(_applicationSettings.LocalDirectoryPath)
                || !Directory.Exists(_applicationSettings.LocalDirectoryPath))
            {
                Console.WriteLine($"Local directory path {_applicationSettings.LocalDirectoryPath} not found.");
                return;
            }

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, _applicationSettings);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger>();

            var cliApplication = new SerialDiskCLI(_applicationSettings, _diskParameters, logger);

            logger.Log($"Baud rate:{_applicationSettings.SerialSettings.BaudRate} | Data bits:{_applicationSettings.SerialSettings.DataBits}" +
                $" | Parity:{_applicationSettings.SerialSettings.Parity} | Stop bits:{_applicationSettings.SerialSettings.StopBits} | Flow control:{_applicationSettings.SerialSettings.Handshake}", LoggingLevel.Info);
            logger.Log($"Using local directory {_applicationSettings.LocalDirectoryPath} as a {_applicationSettings.DiskSettings.DiskSizeMiB}MiB virtual disk", LoggingLevel.Info);
            logger.Log($"Compression: " + (_applicationSettings.IsCompressionEnabled ? "Enabled" : "Disabled"), LoggingLevel.Info);
            logger.Log($"Logging level: { _applicationSettings.LoggingLevel} ", LoggingLevel.Info);

            Console.WriteLine("Press Ctrl-X to quit, Ctrl-R to reimport local disk content.");

            cliApplication.ListenForKeyboardCommand();
        }
    }
}
