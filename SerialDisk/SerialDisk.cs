using AtariST.SerialDisk.Comms;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Common;
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
using System.Text;
using System.Runtime.Serialization.Json;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk
{
    public class SerialDisk
    {
        private static string FormatEnumParams(Type enumerationType)
        {
            StringBuilder enumString = new StringBuilder();

            foreach (var item in Enum.GetNames(enumerationType))
            {
                enumString.Append(item);
                enumString.Append("|");
            }

            enumString.Remove(enumString.Length - 1, 1);

            return enumString.ToString();
        }

        private static void PrintUsage(ApplicationSettings applicationSettings)
        {
            Console.WriteLine();

            Console.WriteLine("Usage:");
            Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " [Options] [virtual_disk_path]");
            Console.WriteLine();

            List<String> parameters = Constants.ConsoleParameterMappings.Keys.ToList();

            Console.WriteLine("Options (default):");
            Console.WriteLine($"{parameters[0]} <disk_size_in_MiB> ({applicationSettings.DiskSettings.DiskSizeMiB})");
            Console.WriteLine($"{parameters[1]} [{FormatEnumParams(typeof(TOSVersion))}] ({applicationSettings.DiskSettings.DiskTOSCompatibility})");
            Console.WriteLine($"{parameters[2]} <sectors> ({applicationSettings.DiskSettings.RootDirectorySectors})");

            Console.WriteLine($"{parameters[3]} [port_name] ({applicationSettings.SerialSettings.PortName})");
            Console.WriteLine($"{parameters[4]} <baud_rate> ({applicationSettings.SerialSettings.BaudRate})");
            Console.WriteLine($"{parameters[5]} <data_bits> ({applicationSettings.SerialSettings.DataBits})");
            Console.WriteLine($"{parameters[6]} [{FormatEnumParams(typeof(StopBits))}] ({applicationSettings.SerialSettings.StopBits})");
            Console.WriteLine($"{parameters[7]} [{FormatEnumParams(typeof(Parity))}] ({applicationSettings.SerialSettings.Parity})");
            Console.WriteLine($"{parameters[8]} [{FormatEnumParams(typeof(Handshake))}] ({applicationSettings.SerialSettings.Handshake})");

            Console.WriteLine($"{parameters[9]} [{FormatEnumParams(typeof(Constants.LoggingLevel))}] ({applicationSettings.LoggingLevel})");
            Console.WriteLine($"{parameters[10]} [log_file_name]");
            Console.WriteLine();

            Console.WriteLine("Serial ports available:");

            foreach (string portName in SerialPort.GetPortNames())
                Console.Write(portName + " ");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static string ParseLocalDirectoryPath(string applicationSettingsPath, string[] args)
        {
            string localDirectoryPath = ".";

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
                if (Directory.Exists(applicationSettingsPath))
                    localDirectoryPath = applicationSettingsPath;

                else
                    throw new Exception($"Could not find path {applicationSettingsPath}");
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

        public static void Main(string[] args)
        {
            Console.WriteLine("\n\rSerial Disk v" + Assembly.GetExecutingAssembly().GetName().Version);

            #region Dependency injection

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            #endregion

            #region Application settings

            ApplicationSettings applicationSettings;

            try
            {
                using (var defaultConfigStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AtariST.SerialDisk.Resources.default_config.json"))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ApplicationSettings));
                    applicationSettings = (ApplicationSettings)ser.ReadObject(defaultConfigStream);
                }

                if (args.Any() && args.Where(arg => arg.ToLowerInvariant().StartsWith("--help")).Any())
                {
                    PrintUsage(applicationSettings);
                    return;
                }

                new ConfigurationBuilder()
                    .AddJsonFile("serialdisk.config", true, false)
                    .AddCommandLine(args, Constants.ConsoleParameterMappings)
                    .Build()
                    .Bind(applicationSettings);

                applicationSettings.LocalDirectoryName = ParseLocalDirectoryPath(applicationSettings.LocalDirectoryName, args);
            }

            catch (Exception parameterException)
            {
                Console.WriteLine($"Error parsing parameters: {parameterException.Message}");
                return;
            }


            if (String.IsNullOrEmpty(applicationSettings.LocalDirectoryName)
                || !Directory.Exists(applicationSettings.LocalDirectoryName))
            {
                Console.WriteLine($"Local directory path {applicationSettings.LocalDirectoryName} not found.");
                return;
            }

            #endregion

            using Logger logger = new Logger(applicationSettings.LoggingLevel, applicationSettings.LogFileName);

            logger.Log($"Operating system: {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture} {System.Runtime.InteropServices.RuntimeInformation.OSDescription}", LoggingLevel.Verbose);
            logger.Log($"Framework version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}", LoggingLevel.Verbose);

            try
            {
                DiskParameters diskParameters = new DiskParameters(applicationSettings.LocalDirectoryName, applicationSettings.DiskSettings, logger);

                logger.Log($"Importing local directory contents from {applicationSettings.LocalDirectoryName}", Constants.LoggingLevel.Verbose);

                Disk disk = new Disk(diskParameters, logger);

                Serial serial = new Serial(applicationSettings.SerialSettings, disk, logger);
            }

            catch (ArgumentException argEx)
            {
                // If there was an initialization error, quit
                return;
            }

            logger.Log($"Listening on {applicationSettings.SerialSettings.PortName.ToUpperInvariant()}", LoggingLevel.Info);

            logger.Log($"Baud rate:{applicationSettings.SerialSettings.BaudRate} | Data bits:{applicationSettings.SerialSettings.DataBits}" +
                $" | Parity:{applicationSettings.SerialSettings.Parity} | Stop bits:{applicationSettings.SerialSettings.StopBits} | Flow control:{applicationSettings.SerialSettings.Handshake}", LoggingLevel.Info);
            logger.Log($"Using local directory {applicationSettings.LocalDirectoryName} as a {applicationSettings.DiskSettings.DiskSizeMiB}MiB virtual disk", LoggingLevel.Info);
            logger.Log($"Logging level: { applicationSettings.LoggingLevel} ", LoggingLevel.Info);

            Console.WriteLine("Press Ctrl-X to quit.");

            var keyInfo = new ConsoleKeyInfo();

            do
            {
                keyInfo = Console.ReadKey(true);
            } while ((keyInfo.Modifiers & ConsoleModifiers.Control) == 0 || keyInfo.Key != ConsoleKey.X);            
        }
    }
}
