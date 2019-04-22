namespace AtariST.SerialDisk.Models
{
    public class LocalDirectoryContentInfo
    {
        public string ContentName { get; set; }
        public string ShortFileName { get; set; }
        public int DirectoryCluster { get; set; }
        public int EntryIndex { get; set; }
        public int StartCluster { get; set; }
    }
}