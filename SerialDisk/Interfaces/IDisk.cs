using Z80andrew.SerialDisk.Storage;

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