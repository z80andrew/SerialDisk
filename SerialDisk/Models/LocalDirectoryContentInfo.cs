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
                if (ParentDirectory != null) return Path.Combine(ParentDirectory.LocalPath, LocalFileName);
                else return LocalFileName;
            }
        }

        public LocalDirectoryContentInfo ParentDirectory { get; set; }
        public string LocalFileName { get; set; }
        public string TOSFileName { get; set; }
        public int DirectoryCluster { get; set; }
        public int EntryIndex { get; set; }
        public int StartCluster { get; set; }
        public bool WriteInProgress { get; set; } = false;
    }
}