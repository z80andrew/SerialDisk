using System.Collections.Generic;

namespace AtariST.SerialDisk.Common
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
                    { "--disk-size", "DiskSettings:DiskSizeMiB" },
                    { "--tos-version", "DiskSettings:DiskTOSCompatibility" },
                    { "--root-directory-sectors", "DiskSettings:RootDirectorySectors" },
                    { "--compression", "CompressionIsEnabled" },

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

        public enum TOSVersion
        {
            TOS100,
            TOS104
        }

        public enum ReceiverState
        {
            ReceiveStartMagic = 0,
            ReceiveReadSectorIndex,
            ReceiveReadSectorCount,
            SendData,
            ReceiveWriteSectorIndex,
            ReceiveWriteSectorCount,
            ReceiveData,
            ReceiveCRC32,
            SendMediaChangeStatus,
            SendBiosParameterBlock
        };

        public enum LoggingLevel
        {
            Info = 0,
            Debug,
            All
        };

        public static class Flags
        {
            public const byte RLECompressionEnabled = 0x1F;
            public const byte CRC32Mismatch = 0x00;
            public const byte CRC32Match = 0x01;
        }
    }
}
