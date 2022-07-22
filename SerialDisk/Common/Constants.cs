using System.Collections.Generic;

namespace Z80andrew.SerialDisk.Common
{
    public static class Constants
    {
        public const string DATE_FORMAT = "yyyy-MM-dd";
        public const string TIME_FORMAT = "HH:mm:ss";

        public const string PROJECT_URL = @"https://www.github.com/z80andrew/serialdisk";

        public static Dictionary<string, string> ConsoleParameterMappings
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "--disk-size", "DiskSettings:DiskSizeMiB" },
                    { "--tos-version", "DiskSettings:DiskTOSCompatibility" },
                    { "--root-directory-sectors", "DiskSettings:RootDirectorySectors" },
                    { "--compression", "IsCompressionEnabled" },

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
