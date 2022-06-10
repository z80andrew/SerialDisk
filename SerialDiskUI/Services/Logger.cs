using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using static AtariST.SerialDisk.Common.Constants;

namespace SerialDiskUI.Services
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

        public Logger(LoggingLevel loggingLevel, string logFileName = null)
        {
            LogLevel = loggingLevel;

            if (logFileName != null)
            {
                string folderPath = Path.GetDirectoryName(AppContext.BaseDirectory);
                string logFolderPath = Path.Combine(folderPath, "log");
                SetLogFile(logFolderPath, logFileName);
            }
        }

        public void SetLogFile(string folderPath, string fileName)
        {
            try
            {
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                _logFilePath = Path.Combine(folderPath, fileName);

                if (!string.Equals(_logFilePath, _fileStream?.Name))
                {
                    if (File.Exists(_logFilePath)) _fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    else _fileStream = new FileStream(_logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                }
            }

            catch (Exception logException)
            {
                var logText = new StringBuilder()
                    .AppendLine("WARNING! Unable to create log file.")
                    .AppendLine(logException.Message);

                var logMessage = new LogMessage(LoggingLevel.Info, logText.ToString(), DateTime.Now);

                throw logException;
            }
        }

        public void UnsetLogFile()
        {
            try
            {
                if (_fileStream != null) _fileStream.Dispose();
                _fileStream = null;
            }

            catch(Exception ex)
            {
                _fileStream = null;
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
