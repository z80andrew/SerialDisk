using Z80andrew.SerialDisk.Models;
using Z80andrew.SerialDisk.Storage;
using System.Collections.Generic;

namespace Z80andrew.SerialDisk.Interfaces
{
    public interface IDisk
    {
        DiskParameters Parameters { get; }
        void WriteSectors(int receiveBufferLength, int startSector, byte[] dataBuffer);
        byte[] ReadSectors(int sector, int numberOfSectors);
        void ReimportLocalDirectoryContents();
    }
}