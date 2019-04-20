using AtariST.SerialDisk.Shared;
using System;

namespace AtariST.SerialDisk.Interfaces
{
    public interface ILogger
    {
        void CreateLogFile(string folderPath, string fileName);
        void Dispose();
        void Log(string message, Constants.LoggingLevel messageLogLevel = Constants.LoggingLevel.Verbose, bool displayOnly = false);
        void LogException(Exception exception, string message = "");
        void LogToFile(string message);
    }
}