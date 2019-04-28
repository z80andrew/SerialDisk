using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using System;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Storage
{
    public class DiskParameters
    {
        private int _diskSizeTotalBytes = FAT16Helper.MaxDiskSizeBytes(PartitionType.GEM);
        private int _bytesPerSector = 256;
        private byte[] _biosParameterBlock;

        public int DiskTotalBytes
        {
            get => _diskSizeTotalBytes;
            set
            {
                if (value > FAT16Helper.MaxDiskSizeBytes(Type)) throw new ArgumentException($"{value / FAT16Helper.BytesPerMiB}MiB is larger than the maximum possible disk size for a {Type.ToString()} partition ({FAT16Helper.MaxDiskSizeBytes(Type) / FAT16Helper.BytesPerMiB}MiB)");
                else _diskSizeTotalBytes = value;
            }
        }

        public PartitionType Type { get; set; } = PartitionType.GEM;

        public string LocalDirectoryPath { get; set; }

        public int BytesPerSector
        {
            get
            {              
                if (_bytesPerSector == 256)
                {
                    if (Type == PartitionType.GEM) _bytesPerSector = 512;

                    else
                    {
                        // 0xFFFF is maximum number of sectors (word)
                        while (_bytesPerSector * 0x10000 < DiskTotalBytes)
                            _bytesPerSector *= 2;
                    }
                }

                return _bytesPerSector;
            }
        }

        // Must be 2 on TOS
        public readonly int SectorsPerCluster = 2;

        public int BytesPerCluster
        {
            get => SectorsPerCluster * BytesPerSector;
        }

        public int DiskClusters
        {
            get
            {
                int diskClusters = DiskTotalBytes / BytesPerCluster;
                if (Type == PartitionType.GEM && diskClusters == 0x8000) diskClusters = 0x7FFF; // Clamp GEM partition to 15-bit addresssing
                return diskClusters;
            }
        }

        public int FatEntriesPerSector
        {
            get => BytesPerSector / 2;
        }

        public int SectorsPerFat
        {
            get => (DiskClusters + FatEntriesPerSector - 1) / FatEntriesPerSector;
        }

        public int RootDirectorySectors { get; set; }

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

        public DiskParameters(string localDirectoryPath, AtariDiskSettings diskSettings)
        {
            LocalDirectoryPath = localDirectoryPath;
            Type = diskSettings.DiskPartitionType;
            DiskTotalBytes = diskSettings.DiskSizeMiB * FAT16Helper.BytesPerMiB;
            RootDirectorySectors = diskSettings.RootDirectorySectors;
        }
    }
}
