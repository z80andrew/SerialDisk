using System;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Models;

namespace Z80andrew.SerialDisk.Interfaces
{
    public interface ILogger : IDisposable
    {
        LogMessage LogMessage { get; }
        LoggingLevel LogLevel { get; set; }
        void Log(string message, LoggingLevel messageLogLevel);
        void LogException(Exception exception, string message = "");
        void SetLogFile(string filePath);
        void UnsetLogFile();
    }
}