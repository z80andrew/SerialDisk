using System.IO.Ports;

namespace AtariST.SerialDisk.Models
{
    public class Settings
    {
        public SerialPortSettings SerialSettings { get; set; }

        public int Verbosity { get; set; }
        public string LocalDirectoryName { get; set; }
        public int DiskSizeMB { get; set; }
    }

    public class SerialPortSettings
    {
        public string PortName { get; set; }
        public Handshake Handshake { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public Parity Parity { get; set; }
        public int Timeout { get; set; }
    }
}
