using AtariST.SerialDisk.Common;
using System;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Interfaces
{
    public interface ILogger : IDisposable
    {
        LoggingLevel LogLevel { get; set; }
        void Log(string message, Constants.LoggingLevel messageLogLevel);
        void LogException(Exception exception, string message = "");
        void SetLogFile(string folderPath, string fileName);
        void UnsetLogFile();
    }
}