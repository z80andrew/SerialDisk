using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Storage;
using System.Collections.Generic;

namespace AtariST.SerialDisk.Interfaces
{
    public interface IDisk
    {
        DiskParameters Parameters { get; set; }

        void WriteSectors(int receiveBufferLength, int startSector, byte[] dataBuffer);

        void FatImportLocalDirectoryContents(List<LocalDirectoryContentInfo> localDirectoryContentInfos, string directoryName, int directoryClusterIndex);

        byte[] ReadSectors(int sector, int numberOfSectors);
    }
}