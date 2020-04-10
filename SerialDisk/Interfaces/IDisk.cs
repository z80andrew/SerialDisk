using AtariST.SerialDisk.Storage;

namespace AtariST.SerialDisk.Interfaces
{
    public interface IDisk
    {
        bool FileSystemWatcherEnabled { get; set; }
        bool MediaChanged { get; set; }
        DiskParameters Parameters { get; set; }

        int WriteSectors(int receiveBufferLength, int startSector, byte[] dataBuffer);

        void SyncLocalDisk(int directoryClusterIndex, bool syncSubDirectoryContents = true);

        void FatImportLocalDirectoryContents(string directoryName, int directoryClusterIndex);

        byte[] ReadSectors(int sector, int numberOfSectors);
    }
}