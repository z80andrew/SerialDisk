using AtariST.SerialDisk.Storage;
using System;
using System.IO;

namespace AtariST.SerialDisk.Interfaces
{
    public interface IDisk
    {
        bool MediaChanged { get; set; }
        DiskParameters Parameters { get; set; }

        void FatAddDirectory(DirectoryInfo directoryInfo, int directoryCluster);
        bool FatAddDirectoryEntry(int directoryClusterIndex, string fullFileName, string shortFileName, byte attributeFlags, DateTime lastWriteDateTime, long fileSize, int startClusterIndex);
        void FatAddFile(FileInfo FileInfo, int directoryClusterIndex);
        string FatCreateShortFileName(string fileName);
        int FatGetClusterValue(int clusterIndex, int directoryCluster = 0);
        int FatGetFreeCluster(int currentCluster);
        void FatImportLocalDirectoryContents(string directoryName, int directoryClusterIndex);
        void FileChangedHandler(object source, FileSystemEventArgs args);
        byte[] ReadSectors(int sector, int numberOfSectors);
        void SyncLocalDisk(int directoryClusterIndex, bool syncSubDirectoryContents = true);
        void WatchLocalDirectory(string localDirectoryName);
        int WriteSectors(int receiveBufferLength, int StartSector, byte[] DataBuffer);
    }
}