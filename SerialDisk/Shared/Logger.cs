using System;
using System.IO;
using System.Text;
using AtariST.SerialDisk.Interfaces;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Shared
{
    public class Logger : IDisposable, ILogger
    {
        private FileStream _fileStream;
        private string _logFilePath;
        private LoggingLevel _logLevel;

        public Logger(LoggingLevel loggingLevel, string logFileName = null)
        {
            _logLevel = loggingLevel;

            Console.CursorVisible = false;

            if (logFileName != null)
            {
                string folderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                string logFolderPath = Path.Combine(folderPath, "log");

                CreateLogFile(logFolderPath, logFileName);
            }
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
                Console.WriteLine($"WARNING! Unable to create log file.");
                Console.WriteLine(logException.Message);
            }
        }

        public void Log(string message, LoggingLevel messageLogLevel = LoggingLevel.Verbose, bool displayOnly = false)
        {
            if (!displayOnly && _fileStream != null) LogToFile(message);

            if (messageLogLevel >= _logLevel) Console.WriteLine($"{DateTime.Now} {message}");
        }

        public void LogException(Exception exception, string message = "")
        {
            if (String.IsNullOrEmpty(message)) message = exception.Message;
            if (_fileStream != null) LogToFile($"{message}: {exception.StackTrace}");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now}\t{message}");
            Console.ResetColor();
            Console.WriteLine(exception);
        }

        public void LogToFile(string message)
        {
            try
            {
                using (StreamWriter fileWriter = new StreamWriter(_fileStream, Encoding.UTF8, 1024, true))
                {
                    fileWriter.WriteLineAsync($"{DateTime.Now.ToString(Constants.DATE_FORMAT)}\t{DateTime.Now.ToString(Constants.TIME_FORMAT)}\t{message}");
                }
            }

            catch (Exception logException)
            {
                Console.WriteLine($"WARNING! Unable to write to log file {_logFilePath}.");
                Console.WriteLine(logException.Message);
            }
        }

        public void Dispose()
        {
            Console.CursorVisible = true;
            if (_fileStream != null) _fileStream.Dispose();
        }
    }
}
