using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Storage;
using System.Collections.Generic;

namespace AtariST.SerialDisk.Interfaces
{
    public interface IDisk
    {
        DiskParameters Parameters { get; }
        void WriteSectors(int receiveBufferLength, int startSector, byte[] dataBuffer);
        byte[] ReadSectors(int sector, int numberOfSectors);
        void ReimportLocalDirectoryContents();
    }
}