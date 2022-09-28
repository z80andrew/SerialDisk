using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.Models;
using static Z80andrew.SerialDisk.Common.Constants;

namespace Z80andrew.SerialDisk.SerialDiskCLI.Services
{
    public class Logger : IDisposable, ILogger
    {
        private FileStream _fileStream;
        private string _logFilePath;
        public LoggingLevel LogLevel { get; set; }
        public LogMessage LogMessage { get; private set; }

        public Logger(LoggingLevel loggingLevel, string logFilePath = null)
        {
            LogLevel = loggingLevel;

            if (logFilePath != null)
            {
                SetLogFile(logFilePath);
            }
        }

        public void Log(string message, LoggingLevel messageLogLevel)
        {
            var logMessage = new LogMessage(messageLogLevel, message, DateTime.Now);

            if (logMessage.LogLevel <= LogLevel)
            {
                OutputLogMessage(logMessage);
                LogToFile(logMessage);
            }
        }

        public void LogException(Exception exception, string message = "")
        {
            if (string.IsNullOrEmpty(message)) message = exception.Message;
            message += $": {exception.StackTrace}";

            var logMessage = new LogMessage(LoggingLevel.Info, message, DateTime.Now);

            if (_fileStream != null) LogToFile(logMessage);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{logMessage.TimeStamp}\t{logMessage.Message}");
            Console.ResetColor();
        }

        private void LogToFile(LogMessage logMessage)
        {
            if (_fileStream != null)
            {
                try
                {
                    using StreamWriter fileWriter = new StreamWriter(_fileStream, Encoding.UTF8, 1024, true);
                    fileWriter.WriteLineAsync($"{logMessage.TimeStamp.ToString(DATE_FORMAT)}\t{logMessage.TimeStamp.ToString(TIME_FORMAT)}\t{logMessage.Message}");
                }

                catch (Exception logException)
                {
                    Console.WriteLine($"WARNING! Unable to write to log file {_logFilePath}.");
                    Console.WriteLine(logException.Message);
                }
            }
        }

        public void Dispose()
        {
            if (_fileStream != null) _fileStream.Dispose();
        }

        public void SetLogFile(string logFilePath)
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    if (File.Exists(logFilePath)) _fileStream = new FileStream(logFilePath, FileMode.Append);
                    else _fileStream = new FileStream(logFilePath, FileMode.OpenOrCreate);

                    _logFilePath = logFilePath;
                }

                catch (Exception logException)
                {
                    LogException(logException, $"ERROR: Unable to create log file {logFilePath}");
                }
            }
        }

        public void UnsetLogFile()
        {
            throw new NotImplementedException();
        }

        private void OutputLogMessage(LogMessage logMessage)
        {
            var message = string.Empty;
            if (LogLevel > LoggingLevel.Info) message = $"{logMessage.TimeStamp}\t";
            message += logMessage.Message;
            Console.WriteLine(message);
#if DEBUG
            Debug.WriteLine(message);
#endif
        }
    }
}
