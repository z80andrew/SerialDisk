using Z80andrew.SerialDisk.Common;

namespace Z80andrew.SerialDisk.Models
{
    public class AtariDiskSettings
    {
        public TOSVersion DiskTOSCompatibility { get; set; }
        public int RootDirectorySectors { get; set; }
        public int DiskSizeMiB { get; set; }
    }
}