using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDiskCLI
{
    public class Logger : IDisposable, ILogger
    {
        private FileStream _fileStream;
        private string _logFilePath;
        public LoggingLevel LogLevel { get; set; }
        public LogMessage LogMessage { get; private set; }

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
                    fileWriter.WriteLineAsync($"{logMessage.TimeStamp.ToString(Constants.DATE_FORMAT)}\t{logMessage.TimeStamp.ToString(Constants.TIME_FORMAT)}\t{logMessage.Message}");
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

        public void SetLogFile(string folderPath, string fileName)
        {
            _logFilePath = Path.Combine(folderPath, fileName);

            try
            {
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                if (File.Exists(_logFilePath)) _fileStream = new FileStream(_logFilePath, FileMode.Append);
                else _fileStream = new FileStream(_logFilePath, FileMode.OpenOrCreate);
            }

            catch (Exception logException)
            {
                LogException(logException, $"ERROR: Unable to create log file {_logFilePath}");
            }
        }

        public void UnsetLogFile()
        {
            throw new NotImplementedException();
        }

        private void OutputLogMessage(LogMessage logMessage)
        {
            var message = String.Empty;
            if (LogLevel > LoggingLevel.Info) message = $"{LogMessage.TimeStamp}\t";
            message += logMessage.Message;
            Console.Write(message);
#if DEBUG
            Debug.WriteLine(message);
#endif
        }
    }
}
