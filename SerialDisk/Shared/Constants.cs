using System.Collections.Generic;

namespace AtariST.SerialDisk.Shared
{
    public static class Constants
    {
        public const string DATE_FORMAT = "yyyy-MM-dd";
        public const string TIME_FORMAT = "HH:mm:ss";

        public static Dictionary<string, string> ConsoleParameterMappings
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "--disk-size", "DiskSizeMiB" },

                    { "--port", "SerialSettings:PortName" },
                    { "--baud-rate", "SerialSettings:BaudRate" },
                    { "--data-bits", "SerialSettings:DataBits" },
                    { "--stop-bits", "SerialSettings:StopBits" },
                    { "--parity", "SerialSettings:Parity" },
                    { "--handshake", "SerialSettings:Handshake" },

                    { "--logging-level", "LoggingLevel" },
                    { "--log-filename", "LogFileName" },
                };
            }
        }

        public enum ReceiverState
        {
            ReceiveStartMagic = 0,
            ReceiveCommand,
            ReceiveReadSectorIndex,
            ReceiveReadSectorCount,
            SendReadData,
            SendReadCrc32,
            ReceiveWriteSectorIndex,
            ReceiveWriteSectorCount,
            ReceiveWriteData,
            SendWriteCrc32,
            SendMediaChangeStatus,
            SendBiosParameterBlock,
            ReceiveEndMagic
        };

        public enum LoggingLevel
        {
            Verbose = 0,
            Info,
            Warn,
            Error
        };
    }
}
