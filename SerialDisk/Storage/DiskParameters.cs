using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using System;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Storage
{
    public class DiskParameters
    {
        private int _diskSizeTotalBytes;
        private int _bytesPerSector;
        private byte[] _biosParameterBlock;

        private readonly ILogger _logger;

        public int DiskTotalBytes
        {
            get => _diskSizeTotalBytes;
            set
            {
                try
                {
                    if (value - (MaxSectorSize * 2) > FAT16Helper.MaxDiskSizeBytes(TOS, SectorsPerCluster)) // Allow for an extra cluster so max size can exactly match 32/512MiB despite 14/15 bit addressing limitation
                        throw new ArgumentException($"{value / FAT16Helper.BytesPerMiB}MiB is larger than the maximum possible disk size " +
                            $"({(FAT16Helper.MaxDiskSizeBytes(TOS, SectorsPerCluster) + (MaxSectorSize * 2)) / FAT16Helper.BytesPerMiB}MiB)");

                    else
                    {
                        _diskSizeTotalBytes = value;
                    }
                }

                catch (ArgumentException argEx)
                {
                    _logger.LogException(argEx);
                    throw argEx;
                }
            }
        }

        public int MaxClusters
        {
            get
            {
                return TOS == TOSVersion.TOS100 ? 0x3FFF : 0x7FFF;
            }
        }

        public TOSVersion TOS { get; set; }

        public string LocalDirectoryPath { get; set; }

        public int BytesPerSector
        {
            get
            {
                if (_bytesPerSector == 0)
                {
                    _bytesPerSector = 512;

                    // MaxClusters + 1 because max clusters on the Atari is 32767 when 1024-byte boundaries
                    // would normally end on 32768
                    while ((_bytesPerSector * 2) * (MaxClusters + 1) < DiskTotalBytes)
                        _bytesPerSector *= 2;
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
                if (diskClusters > MaxClusters) diskClusters = MaxClusters; // Clamp to 14/15-bit addresssing
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

        public DiskParameters(string localDirectoryPath, AtariDiskSettings diskSettings, ILogger logger)
        {
            _logger = logger;
            LocalDirectoryPath = localDirectoryPath;
            TOS = diskSettings.DiskTOSCompatibility;
            DiskTotalBytes = diskSettings.DiskSizeMiB * FAT16Helper.BytesPerMiB;
            RootDirectorySectors = diskSettings.RootDirectorySectors;
        }
    }
}
