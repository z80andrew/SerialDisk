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
        private readonly LoggingLevel _logLevel;
        private readonly StatusService _statusService;

        public Logger(LoggingLevel loggingLevel, StatusService statusService, string logFileName = null)
        {
            _logLevel = loggingLevel;
            _statusService = statusService;

            if (logFileName != null)
            {
                string folderPath = Path.GetDirectoryName(AppContext.BaseDirectory);
                string logFolderPath = Path.Combine(folderPath, "log");
                CreateLogFile(logFolderPath, logFileName);
            }
        }

        public void LogReceiveProgress(int totalBytes, int receivedBytes)
        {
            //string percentReceived = ((Convert.ToDecimal(receivedBytes) / totalBytes) * 100).ToString("00.00");
            //string formattedBytesReceived = receivedBytes.ToString().PadLeft(totalBytes.ToString().Length, '0');
            //OutputLogMessage($"\rReceived [{formattedBytesReceived} / {totalBytes}] bytes {percentReceived}% ");
        }

        public void LogSendProgress(int totalBytes, int sentBytes)
        {
            //string percentSent = ((Convert.ToDecimal(sentBytes) / totalBytes) * 100).ToString("00.00");
            //OutputLogMessage($"\rSent [{(sentBytes).ToString("D" + totalBytes.ToString().Length)} / {totalBytes} Bytes] {percentSent}% ");
        }

        public void CreateLogFile(string folderPath, string fileName)
        {
            try
            {
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                _logFilePath = Path.Combine(folderPath, fileName);

                if (File.Exists(_logFilePath)) _fileStream = new FileStream(_logFilePath, FileMode.Append);
                else _fileStream = new FileStream(_logFilePath, FileMode.OpenOrCreate);
            }

            catch (Exception logException)
            {
                var logText = new StringBuilder()
                    .AppendLine("WARNING! Unable to create log file.")
                    .AppendLine(logException.Message);

                var logMessage = new LogMessage(LoggingLevel.Info, logText.ToString(), DateTime.Now);
            }
        }

        public void Log(string message, LoggingLevel messageLogLevel)
        {
            if (messageLogLevel <= _logLevel)
            {
                var logMessage = new LogMessage(messageLogLevel, message, DateTime.Now);
                OutputLogMessage(logMessage);
                LogToFile(message);
            }
        }

        public void LogException(Exception exception, string message = "")
        {
            if (String.IsNullOrEmpty(message)) message = exception.Message;
            if (_fileStream != null) LogToFile($"{message}: {exception.StackTrace}");

            var logText = new StringBuilder()
                .AppendLine(message)
                .AppendLine(exception.StackTrace);

            var logMessage = new LogMessage(LoggingLevel.Info, logText.ToString(), DateTime.Now);

            OutputLogMessage(logMessage);
        }

        public void LogToFile(string message)
        {
            if (_fileStream != null)
            {
                try
                {
                    using StreamWriter fileWriter = new StreamWriter(_fileStream, Encoding.UTF8, 1024, true);
                    fileWriter.WriteLineAsync($"{DateTime.Now.ToString(Constants.DATE_FORMAT)}\t{DateTime.Now.ToString(Constants.TIME_FORMAT)}\t{message}");
                }

                catch (Exception logException)
                {
                    var logText = new StringBuilder()
                        .AppendLine($"WARNING! Unable to write to log file {_logFilePath}.")
                        .AppendLine(logException.Message);
                }
            }
        }

        private void OutputLogMessage(LogMessage logMessage)
        {
            _statusService.AddLogEntry(logMessage);
#if DEBUG
            Debug.Write(logMessage.Message);
#endif
        }

        public void Dispose()
        {
            if (_fileStream != null) _fileStream.Dispose();
        }
    }
}
