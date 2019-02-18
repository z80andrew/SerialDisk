using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Reflection;
using AtariST.SerialDisk.Storage;
using AtariST.SerialDisk.Utilities;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Comm;

namespace AtariST.SerialDisk
{
    class MainClass
	{
		private static void PrintUsage(Settings applicationSettings)
		{
			Console.WriteLine();

			Console.WriteLine("Usage:");
			Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " --port=<serial_port_name> [Options] [<virtual_disk_path>]");
			Console.WriteLine();

			Console.WriteLine("Options (default):");
            Console.WriteLine($"{Parameters.diskSizeParam} <disk_size_in_MB> ({applicationSettings.DiskSizeMB})");
            Console.WriteLine($"{Parameters.portParam} [port_name] ({applicationSettings.SerialSettings.PortName})");
            Console.WriteLine($"{Parameters.baudRateParam} <baud_rate> ({applicationSettings.SerialSettings.BaudRate})");
			Console.WriteLine($"{Parameters.parityParam} [N|O|E|M|S] ({applicationSettings.SerialSettings.Parity})");
			Console.WriteLine($"{Parameters.stopBitsParam} [N|1|1.5|2] ({applicationSettings.SerialSettings.StopBits})");
			Console.WriteLine($"{Parameters.dataBitsParam} <data_bits> ({applicationSettings.SerialSettings.DataBits})");
			Console.WriteLine($"{Parameters.flowControlParam} [None|RTS|RTS-Xon-Xoff|Xon-Xoff] ({applicationSettings.SerialSettings.Handshake})");
            Console.WriteLine($"{Parameters.verbosityParam} [0-3] ({applicationSettings.LoggingLevel})");
            Console.WriteLine();

			Console.WriteLine("Serial ports available:");

			foreach (string Name in SerialPort.GetPortNames())
				Console.Write(Name + " ");

			Console.WriteLine();
			Console.WriteLine();
		}

		public static void Main(string[] Arguments)
		{
            {
                Console.WriteLine("Serial Disk v" + Assembly.GetExecutingAssembly().GetName().Version);

                Settings applicationSettings = Parameters.ParseParameters(Arguments);

                if ((bool)Arguments[0]?.ToLowerInvariant().StartsWith("--h"))
				{
					PrintUsage(applicationSettings);
					return;
				}

                if (!Directory.Exists(applicationSettings.LocalDirectoryName))
                    throw new Exception("Local directory name invalid.");

                Disk disk = new Disk(applicationSettings);

                Serial serial = new Serial(applicationSettings, disk);

                Console.WriteLine($"Listening on {applicationSettings.SerialSettings.PortName.ToUpperInvariant()}");

                Console.WriteLine($"Baud rate:{applicationSettings.SerialSettings.BaudRate} | Data bits:{applicationSettings.SerialSettings.DataBits}" +
                    $" | Parity:{applicationSettings.SerialSettings.Parity} | Stop bits:{applicationSettings.SerialSettings.StopBits} | Flow control:{applicationSettings.SerialSettings.Handshake}");
                Console.WriteLine($"Local directory: {applicationSettings.LocalDirectoryName}");
                Console.WriteLine($"Logging level: { applicationSettings.LoggingLevel} ");

				Console.WriteLine("Press any key to quit.");
				Console.ReadKey();

				Console.WriteLine("Stopping receiver...");

                serial.serialPort.Dispose();
			}
		}
	}
}
