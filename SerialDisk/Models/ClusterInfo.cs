namespace Z80andrew.SerialDisk.Models
{
    public class ClusterInfo
    {
        public LocalDirectoryContentInfo LocalDirectoryContent;

        public long FileOffset;
        public byte[] DataBuffer;
    }
}
