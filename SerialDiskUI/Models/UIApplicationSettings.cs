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
        public uint MainWindowHeight { get; set; }
        public uint MainWindowWidth { get; set; }

        public UIApplicationSettings(ApplicationSettings appSettings)
        {
            IsLogDisplayEnabled = false;
            IsLogFileEnabled = false;
            
            var logfile = !string.IsNullOrEmpty(appSettings.LogFileName) ? appSettings.LogFileName : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "serialdisk.log");
            LogFileName = logfile;

            //if (!string.IsNullOrEmpty(appSettings.LocalDirectoryPath)) LocalDirectorySizeInBytes = FAT16Helper.GetLocalDirectorySizeInBytes(appSettings.LocalDirectoryPath);
            LocalDirectorySizeInBytes = 0;
            MainWindowHeight = 0;
            MainWindowWidth = 0;

            SerialSettings = appSettings.SerialSettings;
            DiskSettings = appSettings.DiskSettings;
            LoggingLevel = appSettings.LoggingLevel;
            LocalDirectoryPath = appSettings.LocalDirectoryPath;
            IsCompressionEnabled = appSettings.IsCompressionEnabled;
        }

        public void WriteSettingsToDisk()
        {
            var settingsJson = JsonSerializer.Serialize(this, typeof(UIApplicationSettings), new JsonSerializerOptions { WriteIndented = true});
            File.WriteAllTextAsync(Settings.ConfigFilePath, settingsJson);
        }
    }
}
