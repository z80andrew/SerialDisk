﻿using System.IO;
using System.Text.Json;
using Z80andrew.SerialDisk.Models;
using Z80andrew.SerialDisk.SerialDiskUI.Common;

namespace Z80andrew.SerialDisk.SerialDiskUI.Models
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
            LogFileName = appSettings.LogFileName;
        }

        public void WriteSettingsToDisk()
        {
            var settingsJson = JsonSerializer.Serialize(this, typeof(UIApplicationSettings), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllTextAsync(Constants.ConfigFilePath, settingsJson);
        }
    }
}
