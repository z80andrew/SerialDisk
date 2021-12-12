using AtariST.SerialDisk.Common;
using System;

namespace AtariST.SerialDisk.Interfaces
{
    public interface ILogger : IDisposable
    {
        void Log(string message, Constants.LoggingLevel messageLogLevel);
        void LogException(Exception exception, string message = "");
    }
}