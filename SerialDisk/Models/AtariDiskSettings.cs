using AtariST.SerialDisk.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Models
{
    public class AtariDiskSettings
    {
        public TOSVersion DiskTOSCompatibility { get; set; }
        public int RootDirectorySectors { get; set; }
        public int DiskSizeMiB { get; set; }
    }
}