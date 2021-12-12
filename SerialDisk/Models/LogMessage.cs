using System;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Models
{
    public enum LogMessageType
    {
        Message,
        Exception
    }
    public class LogMessage
    {
        public LoggingLevel LogLevel;
        public string Message;
        public DateTime TimeStamp;
        public LogMessageType MessageType;

        public LogMessage(LoggingLevel level, string logMessage, DateTime timeStamp, LogMessageType messageType = LogMessageType.Message)
        {
            LogLevel = level;
            Message= logMessage;
            TimeStamp = timeStamp;
            MessageType = messageType;
        }
    }
}
