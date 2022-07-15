using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.Models;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Z80andrew.SerialDisk.Common.Constants;

namespace Z80andrew.SerialDisk.SerialDiskUI.Services
{
    public class Logger : ReactiveObject, ILogger, IDisposable
    {
        private FileStream _fileStream;
        private string _logFilePath;
        public LoggingLevel LogLevel { get; set; }

        private LogMessage _logMessage;
        public LogMessage LogMessage
        {
            get => _logMessage;
            private set => this.RaiseAndSetIfChanged(ref _logMessage, value);
        }

        public Logger(LoggingLevel loggingLevel, string? logFilePath = null)
        {
            LogLevel = loggingLevel;

            if (logFilePath != null)
            {
                try
                {
                    SetLogFile(logFilePath);
                }

                catch(Exception ex)
                {
                    LogException(ex, LogMessage.Message);
                }
            }
        }

        public void SetLogFile(string logFilePath)
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    if (!string.Equals(logFilePath, _fileStream?.Name))
                    {
                        _fileStream?.Dispose();

                        if (File.Exists(logFilePath)) _fileStream = new FileStream(logFilePath, FileMode.Append);
                        else _fileStream = new FileStream(logFilePath, FileMode.OpenOrCreate);

                        _logFilePath = logFilePath;
                    }
                }

                catch (Exception logException)
                {
                    var logText = new StringBuilder()
                        .AppendLine($"Unable to set log file path {logFilePath}")
                        .AppendLine(logException.Message);

                    LogMessage = new LogMessage(LoggingLevel.Info, logText.ToString(), DateTime.Now);

                    throw;
                }
            }
        }

        public void UnsetLogFile()
        {
            try
            {
                _fileStream?.Dispose();
            }

            catch(Exception ex)
            {
                LogException(ex, "Could not un-set log file");
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
            if (String.IsNullOrEmpty(message)) message = exception.Message;
            message += $": {exception.StackTrace}";

            var logMessage = new LogMessage(LoggingLevel.Info, message, DateTime.Now);

            if (_fileStream != null) LogToFile(logMessage);

            OutputLogMessage(logMessage);
        }

        private void LogToFile(LogMessage logMessage)
        {
            if (_fileStream != null)
            {
                try
                {
                    using StreamWriter fileWriter = new StreamWriter(_fileStream, Encoding.UTF8, 1024, true);
                    fileWriter.WriteLineAsync($"{logMessage.TimeStamp.ToString(Constants.DATE_FORMAT)}\t{logMessage.TimeStamp.ToString(Constants.TIME_FORMAT)}\t{logMessage.Message}");
                }

                catch (Exception logException)
                {
                    Console.WriteLine($"WARNING! Unable to write to log file {_logFilePath}.");
                    Console.WriteLine(logException.Message);
                }
            }
        }

        private void OutputLogMessage(LogMessage logMessage)
        {
            LogMessage = logMessage;
#if DEBUG
            Debug.WriteLine(logMessage.Message);
#endif
        }

        public void Dispose()
        {
            if (_fileStream != null) _fileStream.Dispose();
        }
    }
}
