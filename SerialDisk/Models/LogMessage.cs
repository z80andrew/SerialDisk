﻿using AtariST.SerialDisk.Common;
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
        public LoggingLevel LogLevel { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; }

        public string TimeStampTime => TimeStamp.ToString(Constants.TIME_FORMAT);
        public LogMessageType MessageType { get; set; }

        public LogMessage(LoggingLevel level, string logMessage, DateTime timeStamp, LogMessageType messageType = LogMessageType.Message)
        {
            LogLevel = level;
            Message= logMessage;
            TimeStamp = timeStamp;
            MessageType = messageType;
        }
    }
}
