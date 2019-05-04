using System.Collections.Generic;

namespace AtariST.SerialDisk.Common
{
    public static class Constants
    {
        public const string DATE_FORMAT = "yyyy-MM-dd";
        public const string TIME_FORMAT = "HH:mm:ss";

        public enum TOSVersion
        {
            TOS100,
            TOS104
        }

        public enum PartitionType
        {
            GEM,
            BGM
        }

        public static Dictionary<string, string> ConsoleParameterMappings
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "--disk-size", "DiskSettings:DiskSizeMiB" },
                    { "--partition-type", "DiskSettings:DiskPartitionType" },
                    { "--root-directory-sectors", "DiskSettings:RootDirectorySettings" },

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
