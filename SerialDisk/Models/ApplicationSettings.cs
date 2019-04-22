using AtariST.SerialDisk.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Models
{
    public class ApplicationSettings
    {
        private string _logfileName;

        public SerialPortSettings SerialSettings { get; set; }

        public LoggingLevel LoggingLevel { get; set; } = Constants.LoggingLevel.Info;

        public string LocalDirectoryName { get; set; } = ".";

        public int DiskSizeMiB { get; set; } = 24;

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
