﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Utilities
{
    public static class FAT16Helper
    {
        public static int BytesPerMiB = 1024 * 1024;

        public static int MaxSectorSize(PartitionType partitionType)
        {
            return partitionType == PartitionType.GEM ? 512 : 8192;
        }

        public static int MaxDiskClusters(PartitionType partitionType, TOSVersion minimumTOSVersion)
        {
            return minimumTOSVersion == TOSVersion.TOS100 ? 0x3FFF : 0x7FFF;
        }

        public static int MaxDiskSizeBytes(PartitionType partitionType, TOSVersion tosVersion)
        {
            int maxDiskSizeBytes = MaxDiskClusters(partitionType, tosVersion) * (MaxSectorSize(partitionType) * 2); // 2 sectors per cluster

            return maxDiskSizeBytes;
        }

        public static string GetShortFileName(string fileName)
        {
            Regex invalidCharactersRegex = new Regex("[^\\-A-Z0-9_\\.~]");

            fileName = invalidCharactersRegex.Replace(fileName.ToUpper(), "_");

            string shortFileName;

            int dotIndex = fileName.IndexOf(".");

            if (dotIndex == -1)
                shortFileName = fileName;
            else
                shortFileName = fileName.Substring(0, dotIndex);

            if (shortFileName.Length > 8)
                shortFileName = fileName.Substring(0, 8);

            dotIndex = fileName.LastIndexOf(".");

            if (dotIndex != -1)
            {
                string Extender = fileName.Substring(dotIndex + 1);

                if (Extender.Length > 3)
                    Extender = Extender.Substring(0, 3);

                shortFileName += "." + Extender;
            }

            return shortFileName;
        }

        public static void ValidateLocalDirectory(string localDirectoryPath, int diskSizeBytes, int maxRootDirectoryEntries, PartitionType partitionType, TOSVersion tosVersion)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(localDirectoryPath);
                uint localDirectorySizeBytes = (uint)Directory.GetFiles(directoryInfo.FullName, "*", SearchOption.AllDirectories).Sum(file => (new FileInfo(file).Length));

                if(localDirectorySizeBytes > MaxDiskSizeBytes(partitionType, tosVersion))
                    throw new System.InsufficientMemoryException($"Local directory size is {localDirectorySizeBytes / BytesPerMiB} MiB, which is larger than the maximum allowable virtual disk size for a {partitionType} partition ({MaxDiskSizeBytes(partitionType, tosVersion) / BytesPerMiB} MiB)");

                else if (localDirectorySizeBytes > diskSizeBytes)
                    throw new System.InsufficientMemoryException($"Local directory size is {localDirectorySizeBytes / BytesPerMiB} MiB, which is too large for the given virtual disk size ({diskSizeBytes / BytesPerMiB} MiB)");

                int rootDirectoryEntries = Directory.GetFiles(directoryInfo.FullName, "*", SearchOption.TopDirectoryOnly).Count()
                    + Directory.GetDirectories(directoryInfo.FullName, "*", SearchOption.TopDirectoryOnly).Count();

                if (rootDirectoryEntries > maxRootDirectoryEntries)
                    throw new System.InsufficientMemoryException($"The root directory has {rootDirectoryEntries} files/directories, which is more than the maximum ({maxRootDirectoryEntries} allowed");
            }

            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}