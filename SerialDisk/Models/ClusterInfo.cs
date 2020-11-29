using System.IO;

namespace AtariST.SerialDisk.Models
{
    public class ClusterInfo
    {
        public LocalDirectoryContentInfo LocalDirectoryContent;

        public long FileOffset;
        public byte[] DataBuffer;
    }
}
