using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Models;
using System;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Interfaces
{
    public interface ILogger : IDisposable
    {
        LogMessage LogMessage { get; }
        LoggingLevel LogLevel { get; set; }
        void Log(string message, Constants.LoggingLevel messageLogLevel);
        void LogException(Exception exception, string message = "");
        void SetLogFile(string filePath);
        void UnsetLogFile();
    }
}