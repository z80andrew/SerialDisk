using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Z80andrew.SerialDisk.Comms;
using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.Models;
using Z80andrew.SerialDisk.SerialDiskCLI.Services;
using Z80andrew.SerialDisk.Utilities;
using static Z80andrew.SerialDisk.Common.Constants;

namespace Z80andrew.SerialDisk.SerialDiskCLI
{
    public static class Program
    {
        private static ApplicationSettings _applicationSettings;

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

        private static void PrintUsage(ApplicationSettings applicationSettings)
        {
            Console.WriteLine();

            Console.WriteLine("Usage:");
            Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " [Command] [Options] [virtual_disk_path]");
            Console.WriteLine();

            List<String> parameters = Common.Constants.ConsoleParameterMappings.Keys.ToList();

            Console.WriteLine("Commands");
            Console.WriteLine("--help (Lists available command-line options)");
            Console.WriteLine("--update-check (Checks for new version of the program)");
            Console.WriteLine();
            Console.WriteLine("Options (default value):");
            Console.WriteLine($"{parameters[0]} <disk_size_in_MiB> ({applicationSettings.DiskSettings.DiskSizeMiB})");
            Console.WriteLine($"{parameters[1]} [{FormatEnumParams(typeof(TOSVersion))}] ({applicationSettings.DiskSettings.DiskTOSCompatibility})");
            Console.WriteLine($"{parameters[2]} <sectors> ({applicationSettings.DiskSettings.RootDirectorySectors})");
            Console.WriteLine($"{parameters[3]} [True|False] ({applicationSettings.IsCompressionEnabled})");

            Console.WriteLine($"{parameters[4]} [port_name] ({applicationSettings.SerialSettings.PortName})");
            Console.WriteLine($"{parameters[5]} <baud_rate> ({applicationSettings.SerialSettings.BaudRate})");
            Console.WriteLine($"{parameters[6]} <data_bits> ({applicationSettings.SerialSettings.DataBits})");
            Console.WriteLine($"{parameters[7]} [{FormatEnumParams(typeof(StopBits))}] ({applicationSettings.SerialSettings.StopBits})");
            Console.WriteLine($"{parameters[8]} [{FormatEnumParams(typeof(Parity))}] ({applicationSettings.SerialSettings.Parity})");
            Console.WriteLine($"{parameters[9]} [{FormatEnumParams(typeof(Handshake))}] ({applicationSettings.SerialSettings.Handshake})");

            Console.WriteLine($"{parameters[10]} [{FormatEnumParams(typeof(LoggingLevel))}] ({applicationSettings.LoggingLevel})");
            Console.WriteLine($"{parameters[11]} [log_file_name]");
            Console.WriteLine();

            Console.WriteLine("Serial ports available:");

            foreach (string portName in SerialPort.GetPortNames())
                Console.Write(portName + " ");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static void CheckIfNewVersionAvailable()
        {
            Console.WriteLine("Checking for new version...");

            try
            {
                var releasesInfo = Network.GetReleases().GetAwaiter().GetResult();
                var latestVersionUrl = ConfigurationHelper.GetLatestVersionUrl(releasesInfo);
                var isNewVersionAvailable = ConfigurationHelper.IsNewVersionAvailable(releasesInfo);

                if (isNewVersionAvailable)
                    Console.WriteLine($"New version is available at {latestVersionUrl}");
                else
                    Console.WriteLine("No new version available");
            }

            catch (Exception ex)
            {
                Console.WriteLine("Could not check for new version");
                Console.WriteLine(ex.Message + ": " + ex.StackTrace);
            }
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
                .AddSingleton<ILogger>(new Logger(settings.LoggingLevel, settings.LogFileName));
        }

        private static ApplicationSettings ApplyConfigurationToApplicationSettings(string[] args, ApplicationSettings defaultApplicationSettings)
        {
            var applicationSettings = defaultApplicationSettings;

            var configBuilder = new ConfigurationBuilder();

            if (File.Exists(Common.Constants.CONFIG_FILE_NAME))
            {
                configBuilder.AddJsonFile(Common.Constants.CONFIG_FILE_NAME, true, false)
                    .Build()
                    .Bind(applicationSettings);
            }

            configBuilder.AddCommandLine(args, Common.Constants.ConsoleParameterMappings)
                .Build()
                .Bind(applicationSettings);

            applicationSettings.LocalDirectoryPath = ParseLocalDirectoryPath(applicationSettings.LocalDirectoryPath, args);

            return applicationSettings;
        }

        public static void Main(string[] args)
        {
            var defaultApplicationSettings = ConfigurationHelper.GetDefaultApplicationSettings();

            if (args.Any() && args.Where(arg => arg.ToLowerInvariant().StartsWith("--help")).Any())
            {
                PrintUsage(defaultApplicationSettings);
                return;
            }

            else if (args.Any() && args.Where(arg => arg.ToLowerInvariant().StartsWith("--update-check")).Any())
            {
                CheckIfNewVersionAvailable();
                return;
            }

            try
            {
                _applicationSettings = ApplyConfigurationToApplicationSettings(args, defaultApplicationSettings);
            }

            catch (Exception parameterException)
            {
                Console.WriteLine($"Error parsing parameters: {parameterException.Message}");
                return;
            }

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, _applicationSettings);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger>();

            if (String.IsNullOrEmpty(_applicationSettings.LocalDirectoryPath)
                || !Directory.Exists(_applicationSettings.LocalDirectoryPath))
            {
                Console.WriteLine($"Local directory path {_applicationSettings.LocalDirectoryPath} not found.");
                return;
            }

            var cliApplication = new SerialDiskCLI(_applicationSettings, logger);

            logger.Log($"Baud rate:{_applicationSettings.SerialSettings.BaudRate} | Data bits:{_applicationSettings.SerialSettings.DataBits}" +
                $" | Parity:{_applicationSettings.SerialSettings.Parity} | Stop bits:{_applicationSettings.SerialSettings.StopBits} | Flow control:{_applicationSettings.SerialSettings.Handshake}", LoggingLevel.Info);
            logger.Log($"Using local directory {_applicationSettings.LocalDirectoryPath} as a {_applicationSettings.DiskSettings.DiskSizeMiB}MiB virtual disk", LoggingLevel.Info);
            logger.Log($"Compression: " + (_applicationSettings.IsCompressionEnabled ? "Enabled" : "Disabled"), LoggingLevel.Info);
            logger.Log($"Logging level: {_applicationSettings.LoggingLevel} ", LoggingLevel.Info);

            Console.WriteLine("Press Ctrl-X to quit, Ctrl-R to reimport local disk content.");

            cliApplication.ListenForKeyboardCommand();
        }
    }
}
