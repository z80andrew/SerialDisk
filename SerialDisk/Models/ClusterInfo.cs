namespace AtariST.SerialDisk.Models
{
    public class ClusterInfo
    {
        public string ContentName;
        public long FileOffset;
        public byte[] DataBuffer;
    }
}
