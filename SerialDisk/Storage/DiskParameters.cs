namespace AtariST.SerialDisk.Storage
{
    public class DiskParameters
    {
        private int _bytesPerSector = 1;
        private byte[] _biosParameterBlock;

        public int DiskTotalBytes { get; set; }

        public DiskParameters(string localDirectoryPath, int diskTotalBytes)
        {
            LocalDirectoryPath = localDirectoryPath;
            DiskTotalBytes = diskTotalBytes;
        }

        public string LocalDirectoryPath { get; set; }

        public int BytesPerSector
        {
            get
            {
                if (_bytesPerSector == 1)
                {
                    while (_bytesPerSector * 64 * 1024 < DiskTotalBytes)
                        _bytesPerSector *= 2;
                }

                return _bytesPerSector;
            }
        }

        public readonly int SectorsPerCluster = 2;

        public int BytesPerCluster
        {
            get => SectorsPerCluster * BytesPerSector;
        }

        public int DiskClusters
        {
            get => DiskTotalBytes / BytesPerCluster;
        }

        public int FatEntriesPerSector
        {
            get => BytesPerSector / 2;
        }

        public int SectorsPerFat
        {
            get => (DiskClusters + FatEntriesPerSector - 1) / FatEntriesPerSector;
        }

        public readonly int RootDirectorySectors = 4;

        public byte[] BIOSParameterBlock
        {
            get
            {
                if (_biosParameterBlock == null)
                {
                    _biosParameterBlock = new byte[18];

                    _biosParameterBlock[0] = (byte)((BytesPerSector >> 8) & 0xff);
                    _biosParameterBlock[1] = (byte)(BytesPerSector & 0xff);

                    _biosParameterBlock[2] = (byte)((SectorsPerCluster >> 8) & 0xff);
                    _biosParameterBlock[3] = (byte)(SectorsPerCluster & 0xff);

                    _biosParameterBlock[4] = (byte)((BytesPerCluster >> 8) & 0xff);
                    _biosParameterBlock[5] = (byte)(BytesPerCluster & 0xff);

                    _biosParameterBlock[6] = (byte)((RootDirectorySectors >> 8) & 0xff);
                    _biosParameterBlock[7] = (byte)(RootDirectorySectors & 0xff);

                    _biosParameterBlock[8] = (byte)((SectorsPerFat >> 8) & 0xff);
                    _biosParameterBlock[9] = (byte)(SectorsPerFat & 0xff);

                    _biosParameterBlock[10] = (byte)((SectorsPerFat >> 8) & 0xff);
                    _biosParameterBlock[11] = (byte)(SectorsPerFat & 0xff);

                    _biosParameterBlock[12] = (byte)(((SectorsPerFat * 2 + RootDirectorySectors) >> 8) & 0xff);
                    _biosParameterBlock[13] = (byte)((SectorsPerFat * 2 + RootDirectorySectors) & 0xff);

                    _biosParameterBlock[14] = (byte)((DiskClusters >> 8) & 0xff);
                    _biosParameterBlock[15] = (byte)(DiskClusters & 0xff);

                    _biosParameterBlock[16] = 0;
                    _biosParameterBlock[17] = 1;
                }

                return _biosParameterBlock;
            }
        }
    }
}
