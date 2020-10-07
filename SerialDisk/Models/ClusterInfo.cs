using System.IO;

namespace AtariST.SerialDisk.Models
{
    public class ClusterInfo
    {
        public string LocalPath
        {
            get
            {
                if (LocalFileName != null) return Path.Combine(LocalDirectory, LocalFileName);
                else return LocalDirectory;
            }
        }

        public string LocalDirectory { get; set; }

        public string LocalFileName { get; set; }

        public long FileOffset;
        public byte[] DataBuffer;
    }
}
