using System.Collections.Generic;

namespace Z80andrew.SerialDisk.SerialDiskCLI.Common
{
    internal class Constants
    {
        public const string CONFIG_FILE_NAME = "serialdisk.config";

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
    }
}
