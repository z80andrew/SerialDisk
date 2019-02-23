using System;
using System.IO;
using System.Text;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Shared
{
    public class Logger : IDisposable
    {
        private FileStream fileStream;
        private string logFilePath;
        private LoggingLevel logLevel;

        public Logger(LoggingLevel loggingLevel, string logFileName = null)
        {
            logLevel = loggingLevel;

            Console.CursorVisible = false;

            if (logFileName != null)
            {
                string folderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                string logFolderPath = Path.Combine(folderPath, "log");

                // logFolderPath = Path.GetFullPath(logFolderPath);

                CreateLogFile(logFolderPath, logFileName);
            }
        }

        public void CreateLogFile(string folderPath, string fileName)
        {
            try
            {
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                logFilePath = Path.Combine(folderPath, fileName);

                if (File.Exists(logFilePath)) fileStream = new FileStream(logFilePath, FileMode.Append);
                else fileStream = new FileStream(logFilePath, FileMode.OpenOrCreate);
            }

            catch (Exception logException)
            {
                Console.WriteLine($"WARNING! Unable to create log file.");
                Console.WriteLine(logException.Message);
            }
        }

        public void Log(string message, LoggingLevel messageLogLevel = LoggingLevel.Verbose)
        {
            if (fileStream != null) LogToFile(message);

            if(messageLogLevel >= logLevel) Console.WriteLine($"{DateTime.Now} {message}");
        }

        public void LogException(Exception exception, string message = "")
        {
            if(String.IsNullOrEmpty(message)) message = exception.Message;
            if (fileStream != null) LogToFile($"{message}: {exception.StackTrace}");

            Console.WriteLine($"{DateTime.Now}\t{message}");
            Console.WriteLine(exception);
        }

        public void LogToFile(string message)
        {
            try
            {
                using (StreamWriter fileWriter = new StreamWriter(fileStream,Encoding.UTF8, 1024, true))
                {
                    fileWriter.WriteLineAsync($"{DateTime.Now.ToString(Constants.DATE_FORMAT)}\t{DateTime.Now.ToString(Constants.TIME_FORMAT)}\t{message}");
                }
            }

            catch (Exception logException)
            {
                Console.WriteLine($"WARNING! Unable to write to log file {logFilePath}.");
                Console.WriteLine(logException.Message);
            }
        }

        public void Dispose()
        {
            Console.CursorVisible = true;
            if(fileStream != null) fileStream.Dispose();
        }
    }
}
