using System;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Models;
using static Z80andrew.SerialDisk.Common.Constants;

namespace Z80andrew.SerialDisk.Interfaces
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