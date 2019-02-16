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
		private static void PrintUsage()
		{
			Console.WriteLine();

			Console.WriteLine("Usage:");
			Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " --port=<serial_port_name> [Options] [<virtual_disk_path>]");
			Console.WriteLine();

			Console.WriteLine("Options (default):");
			Console.WriteLine("--disk-size=<disk_size_in_mb> (32)");
			Console.WriteLine("--baud-rate=<baud_rate> (115200)");
			Console.WriteLine("--parity=[N|O|E|M|S] (N)");
			Console.WriteLine("--stop-bits=[N|1|1.5|2] (1)");
			Console.WriteLine("--data-bits=<data_bits> (8)");
			Console.WriteLine("--handshake=[None|RTS|RTS_Xon_Xoff|Xon_Xoff] (RTS)");
			Console.WriteLine();

			Console.WriteLine("Serial ports available:");

			string[] SerialPortNames = SerialPort.GetPortNames();

			foreach (string Name in SerialPortNames)
				Console.Write(Name + " ");

			Console.WriteLine();
			Console.WriteLine();
		}

		public static void Main(string[] Arguments)
		{
			{
				Console.WriteLine("Serial Disk v" + Assembly.GetExecutingAssembly().GetName().Version);

				Settings applicationSettings = Parameters.ParseParameters(Arguments);

				if(applicationSettings.SerialSettings.PortName == null)
				{
					PrintUsage();

					return;
				}

                if (!Directory.Exists(applicationSettings.LocalDirectoryName))
                    throw new Exception("Local directory name invalid.");

                Disk disk = new Disk(applicationSettings);

                Serial serial = new Serial(applicationSettings, disk);

                Thread serialDataReceiverThread = new Thread(() => serial.SerialDataReceiver(applicationSettings.LocalDirectoryName, 
                    applicationSettings.SerialSettings.Timeout, applicationSettings.Verbosity));

                serialDataReceiverThread.Start();

                Console.WriteLine($"Listening on {applicationSettings.SerialSettings.PortName.ToUpperInvariant()}");

                Console.WriteLine($"Baud rate:{applicationSettings.SerialSettings.BaudRate} Data bits:{applicationSettings.SerialSettings.DataBits}" +
                    $" Parity:{applicationSettings.SerialSettings.Parity} Stop bits:{applicationSettings.SerialSettings.StopBits} Flow control:{applicationSettings.SerialSettings.Handshake}");
                Console.WriteLine($"Local directory: {applicationSettings.LocalDirectoryName}");

				Console.WriteLine("Press any key to quit.");
				Console.ReadKey();

				Console.WriteLine("Stopping receiver...");

                serial.StopListening();

                serialDataReceiverThread.Join();

                serial.serialPort.Close();
			}
		}
	}
}
