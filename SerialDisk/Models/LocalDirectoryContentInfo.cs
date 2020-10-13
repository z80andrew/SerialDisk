using AtariST.SerialDisk.Utilities;
using System.IO;

namespace AtariST.SerialDisk.Models
{
    public class LocalDirectoryContentInfo
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

        public string TOSFileName { get; set; }
        public int DirectoryCluster { get; set; }
        public int EntryIndex { get; set; }
        public int StartCluster { get; set; }
    }
}