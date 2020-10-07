using System.IO;

namespace AtariST.SerialDisk.Models
{
    public class ClusterInfo
    {
        public string LocalPath
        {
            get
            {
                if (LocalDirectory == null) return null;
                else if (LocalFileName == null) return LocalDirectory;
                else return Path.Combine(LocalDirectory, LocalFileName);
            }
        }

        public string LocalDirectory { get; set; }

        public string LocalFileName { get; set; }

        public long FileOffset;
        public byte[] DataBuffer;
    }
}
