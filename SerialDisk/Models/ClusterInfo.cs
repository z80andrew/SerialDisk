using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AtariST.SerialDisk.Models
{
    public class ClusterInfo
    {
        public string ContentName;
        public long FileOffset;
        public byte[] DataBuffer;
    }
}
