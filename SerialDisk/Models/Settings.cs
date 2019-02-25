using AtariST.SerialDisk.Shared;
using System;
using System.IO;
using System.IO.Ports;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Models
{
    public class Settings
    {
        private string _logfileName;

        public SerialPortSettings SerialSettings { get; set; }

        public LoggingLevel LoggingLevel { get; set; } = Constants.LoggingLevel.Error;
        public string LocalDirectoryName { get; set; } = null;
        public int DiskSizeMB { get; set; } = 24;
        public string LogFileName
        {
            get => _logfileName;
            set => _logfileName = String.Join("_", value.Split(Path.GetInvalidFileNameChars()));
        }
    }

    public class SerialPortSettings
    {
        public string PortName { get; set; } = "COM1";
        public Handshake Handshake { get; set; } = Handshake.None;
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Parity Parity { get; set; } = Parity.None;
        public int Timeout { get; set; } = 100;
    }
}
