using AtariST.SerialDisk.Storage;

namespace AtariST.SerialDisk.Interfaces
{
    public interface IDisk
    {
        DiskParameters Parameters { get; set; }

        void WriteSectors(int receiveBufferLength, int startSector, byte[] dataBuffer);

        void FatImportLocalDirectoryContents(string directoryName, int directoryClusterIndex);

        byte[] ReadSectors(int sector, int numberOfSectors);
    }
}