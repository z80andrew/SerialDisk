﻿using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Interfaces;
using System;
using System.IO;
using System.Text;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDiskCLI
{
    public class Logger : IDisposable, ILogger
    {
        private FileStream _fileStream;
        private string _logFilePath;
        private readonly LoggingLevel _logLevel;

        public Logger(LoggingLevel loggingLevel, string logFileName = null)
        {
            _logLevel = loggingLevel;

            if (logFileName != null)
            {
                string folderPath = Path.GetDirectoryName(AppContext.BaseDirectory);

                string logFolderPath = Path.Combine(folderPath, "log");

                CreateLogFile(logFolderPath, logFileName);
            }
        }

        public void Log(string message, LoggingLevel messageLogLevel)
        {
            if (messageLogLevel <= _logLevel)
            {
                if (_logLevel >= LoggingLevel.Debug) Console.Write($"{DateTime.Now}\t");
                Console.Write($"{message}\r\n");
                LogToFile(message);
            }
        }

        public void LogException(Exception exception, string message = "")
        {
            if (String.IsNullOrEmpty(message)) message = exception.Message;
            if (_fileStream != null) LogToFile($"{message}: {exception.StackTrace}");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now}\t{message}");
            Console.ResetColor();
            if (_logLevel > LoggingLevel.Info)
            {
                Console.WriteLine(exception);
                Console.WriteLine(exception.StackTrace);
            }
        }

        private void LogToFile(string message)
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
                    Console.WriteLine($"WARNING! Unable to write to log file {_logFilePath}.");
                    Console.WriteLine(logException.Message);
                }
            }
        }

        public void Dispose()
        {
            if (_fileStream != null) _fileStream.Dispose();
        }

        private void CreateLogFile(string folderPath, string fileName)
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
                Console.WriteLine($"WARNING! Unable to create log file.");
                Console.WriteLine(logException.Message);
            }
        }
    }
}
