using System.IO;

namespace Z80andrew.SerialDisk.Models
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
        public int FinalCluster { get; set; }
        public bool WriteInProgress { get; set; }
    }
}