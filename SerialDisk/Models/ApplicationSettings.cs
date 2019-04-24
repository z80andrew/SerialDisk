using AtariST.SerialDisk.Shared;
using AtariST.SerialDisk.Utilities;
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
        private int _diskSizeMiB = FAT16Helper.MaxDiskSizeBytes / FAT16Helper.BytesPerMiB;

        public SerialPortSettings SerialSettings { get; set; }

        public LoggingLevel LoggingLevel { get; set; } = Constants.LoggingLevel.Info;

        public string LocalDirectoryName { get; set; } = ".";

        public int DiskSizeMiB {
            get
            {
                return _diskSizeMiB;
            }
            set
            {
                if (value * FAT16Helper.BytesPerMiB > FAT16Helper.MaxDiskSizeBytes) throw new ArgumentException($"{value} is larger than the maximum possible disk size ({FAT16Helper.MaxDiskSizeBytes / FAT16Helper.BytesPerMiB})");
                else _diskSizeMiB = value;
            }
        }

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
