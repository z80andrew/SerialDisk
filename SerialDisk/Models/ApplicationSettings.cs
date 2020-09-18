using System;
using System.IO;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Models
{
    public class ApplicationSettings
    {
        private string _logfileName;

        public SerialPortSettings SerialSettings { get; set; }

        public AtariDiskSettings DiskSettings { get; set; }

        public LoggingLevel LoggingLevel { get; set; }

        public string LocalDirectoryName { get; set; }

        public bool IsCompressionEnabled { get; set; }

        public string LogFileName
        {
            get => _logfileName;
            set => _logfileName = String.Join("_", value.Split(Path.GetInvalidFileNameChars()));
        }

        public ApplicationSettings()
        {
            SerialSettings = new SerialPortSettings();
        }
    }
}
