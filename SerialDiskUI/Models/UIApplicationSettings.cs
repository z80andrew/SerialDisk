using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SerialDiskUI.Common;

namespace SerialDiskUI.Models
{
    public class UIApplicationSettings : ApplicationSettings
    {
        public bool IsLogDisplayEnabled { get; set; }
        public bool IsLogFileEnabled { get; set; }
        public uint LocalDirectorySizeInBytes { get; set; }

        public int MainWindowHeight { get; set; }
        public int MainWindowWidth { get; set; }
        public int MainWindowX { get; set; }
        public int MainWindowY { get; set; }

        public UIApplicationSettings(ApplicationSettings appSettings)
        {
            IsLogDisplayEnabled = false;
            IsLogFileEnabled = false;

            LogFileName = appSettings.LogFileName;

            LocalDirectorySizeInBytes = 0;
            MainWindowHeight = -1;
            MainWindowWidth = -1;
            MainWindowX = -1;
            MainWindowY = -1;

            SerialSettings = appSettings.SerialSettings;
            DiskSettings = appSettings.DiskSettings;
            LoggingLevel = appSettings.LoggingLevel;
            LocalDirectoryPath = appSettings.LocalDirectoryPath;
            IsCompressionEnabled = appSettings.IsCompressionEnabled;
        }

        public void WriteSettingsToDisk()
        {
            var settingsJson = JsonSerializer.Serialize(this, typeof(UIApplicationSettings), new JsonSerializerOptions { WriteIndented = true});
            File.WriteAllTextAsync(Constants.ConfigFilePath, settingsJson);
        }
    }
}
