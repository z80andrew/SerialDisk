using System.IO.Ports;

namespace AtariST.SerialDisk.Models
{
    public class SerialPortSettings
    {
        public string PortName { get; set; }
        public Handshake Handshake { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public Parity Parity { get; set; }
    }
}
