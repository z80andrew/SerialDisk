using static Z80andrew.SerialDisk.Common.Constants;

namespace Z80andrew.SerialDisk.Models
{
    public class ApplicationSettings
    {
        public SerialPortSettings SerialSettings { get; set; }

        public AtariDiskSettings DiskSettings { get; set; }

        public LoggingLevel LoggingLevel { get; set; }

        public string LocalDirectoryPath { get; set; }

        public bool IsCompressionEnabled { get; set; }

        public string LogFileName { get; set; }

        public ApplicationSettings()
        {
            SerialSettings = new SerialPortSettings();
        }
    }
}
