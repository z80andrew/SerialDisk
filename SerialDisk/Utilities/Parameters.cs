using AtariST.SerialDisk.Models;
using System.Collections.Specialized;
using System.IO;
using System.IO.Ports;

namespace AtariST.SerialDisk.Utilities
{
    public static class Parameters
    {
        public static Settings ParseParameters(string[] arguments)
        {
            NameValueCollection argumentNameValueCollection = new NameValueCollection();

            Settings applicationSettings = new Settings
            {
                SerialSettings = new SerialPortSettings()
            };

            foreach (string argument in arguments)
            {
                string[] NameValue = argument.Split('=');

                if (NameValue.Length == 2)
                    argumentNameValueCollection.Add(NameValue[0].ToLower().Trim(), NameValue[1].Trim());
                else
                    applicationSettings.LocalDirectoryName = argument.TrimEnd(Path.DirectorySeparatorChar);
            }

            applicationSettings.DiskSizeMB = argumentNameValueCollection["--disk-size"] == null ? 32 : int.Parse(argumentNameValueCollection["--disk-size"]);

            if (string.IsNullOrEmpty(applicationSettings.LocalDirectoryName)) applicationSettings.LocalDirectoryName = ".";

            applicationSettings.SerialSettings.PortName = argumentNameValueCollection["--port"];

            applicationSettings.SerialSettings.Timeout = 100;

            if (argumentNameValueCollection["--baud-rate"] == null)
                applicationSettings.SerialSettings.BaudRate = 19200;
            else
                applicationSettings.SerialSettings.BaudRate = int.Parse(argumentNameValueCollection["--baud-rate"]);

            switch (argumentNameValueCollection["--parity"]?.ToLowerInvariant())
            {
                case null:
                case "n":
                    applicationSettings.SerialSettings.Parity = Parity.None;

                    break;

                case "e":
                    applicationSettings.SerialSettings.Parity = Parity.Even;

                    break;

                case "m":
                    applicationSettings.SerialSettings.Parity = Parity.Mark;

                    break;

                case "o":
                    applicationSettings.SerialSettings.Parity = Parity.Odd;

                    break;

                case "s":
                    applicationSettings.SerialSettings.Parity = Parity.Space;

                    break;
            }

            switch (argumentNameValueCollection["--stop-bits"]?.ToLowerInvariant())
            {
                case null:
                case "1":
                    applicationSettings.SerialSettings.StopBits = StopBits.One;

                    break;

                case "1.5":
                    applicationSettings.SerialSettings.StopBits = StopBits.OnePointFive;

                    break;

                case "2":
                    applicationSettings.SerialSettings.StopBits = StopBits.Two;

                    break;

                case "n":
                    applicationSettings.SerialSettings.StopBits = StopBits.None;

                    break;
            }

            if (argumentNameValueCollection["--data-bits"] == null)
                applicationSettings.SerialSettings.DataBits = 8;
            else
                applicationSettings.SerialSettings.DataBits = int.Parse(argumentNameValueCollection["--data-bits"]);

            switch (argumentNameValueCollection["--handshake"]?.ToLowerInvariant())
            {
                case null:
                case "none":
                    applicationSettings.SerialSettings.Handshake = Handshake.None;

                    break;

                case "rts":
                    applicationSettings.SerialSettings.Handshake = Handshake.RequestToSend;

                    break;

                case "rts_xon_xoff":
                    applicationSettings.SerialSettings.Handshake = Handshake.RequestToSendXOnXOff;

                    break;

                case "xon_xoff":
                    applicationSettings.SerialSettings.Handshake = Handshake.XOnXOff;

                    break;
            }

            if (argumentNameValueCollection["--verbosity"] != null)
                applicationSettings.Verbosity = int.Parse(argumentNameValueCollection["--verbosity"]);

            return applicationSettings;
        }
    }
}
