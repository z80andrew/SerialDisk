using AtariST.SerialDisk.Shared;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Models
{
    public class ApplicationSettings
    {
        private string _logfileName;

        public SerialPortSettings SerialSettings { get; set; }

        public LoggingLevel LoggingLevel { get; set; } = Constants.LoggingLevel.Error;
        public string LocalDirectoryName { get; set; } = null;
        public int DiskSizeMiB { get; set; } = 24;
        public string LogFileName
        {
            get => _logfileName;
            set => _logfileName = String.Join("_", value.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
