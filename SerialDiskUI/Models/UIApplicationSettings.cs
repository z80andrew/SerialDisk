using AtariST.SerialDisk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDiskUI.Models
{
    public class UIApplicationSettings : ApplicationSettings
    {
        public bool IsLogDisplayEnabled { get; set; }

        public UIApplicationSettings(ApplicationSettings appSettings)
        {
            IsLogDisplayEnabled = true;

            SerialSettings = appSettings.SerialSettings;
            DiskSettings = appSettings.DiskSettings;
            LoggingLevel = appSettings.LoggingLevel;
            LocalDirectoryPath = appSettings.LocalDirectoryPath;
            IsCompressionEnabled = appSettings.IsCompressionEnabled;
        }
    }
}
