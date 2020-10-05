using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Utilities
{
    public static class FAT16Helper
    {
        public const int BytesPerMiB = 1024 * 1024;

        public const int BytesPerDirectoryEntry = 32;

        public static int MaxDiskClusters(TOSVersion minimumTOSVersion)
        {
            return minimumTOSVersion == TOSVersion.TOS100 ? 0x3FFF : 0x7FFF;
        }

        public static int MaxDiskSizeBytes(TOSVersion tosVersion)
        {
            int maxDiskSizeBytes = MaxDiskClusters(tosVersion) * (MaxSectorSize * 2); // 2 sectors per cluster

            return maxDiskSizeBytes;
        }

        public static string GetShortFileName(string fileName)
        {
            Regex invalidCharactersRegex = new Regex("[^\\-A-Z0-9_\\.~]");

            fileName = invalidCharactersRegex.Replace(fileName.ToUpper(), "_");

            int dotIndex = fileName.LastIndexOf(".");

            // Filenames cannot start with . so if this is the only one in the name, replace it
            if (dotIndex == 0)
            {
                fileName = fileName.Replace('.', '_');
                dotIndex = -1;
            }

            // Replace all except the final .
            else if (dotIndex != -1) fileName = fileName.Substring(0, dotIndex).Replace('.', '_') + fileName[dotIndex..];

            string shortFileName;

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

        /// <summary>
        /// Returns true if the given cluster value matches a free, bad or EOF identifier
        /// </summary>
        /// <remarks>
        /// 0x0000 free cluster
        /// 0xFFF8–0xFFFF last FAT entry for a file
        /// 0xFFF0-0xFFF7 bad sector (GEMDOS)
        /// </remarks>
        /// <param name="clusterValue">The value of the cluster to check</param>
        /// <returns>True if this cluster value does not correspond to a cluster index, otherwise false</returns>
        public static bool IsEndOfClusterChain(int clusterValue)
        {
            return clusterValue == 0 || clusterValue >= 0xfff8 || (clusterValue >= 0xfff0 && clusterValue <= 0xfff7);
        }

        /// <summary>
        /// Returns true if the given cluster value matches an EOF identifier
        /// </summary> 
        /// <remarks>
        /// 0xFFF8–0xFFFF last FAT entry for a file
        /// </remarks>
        /// <param name="clusterValue">The value of the cluster to check</param>
        /// <returns>True if this cluster value matches an EOF identifier, otherwise false</returns>
        public static bool IsEndOfFile(int clusterValue)
        {
            return clusterValue >= 0xfff8;
        }

        public static void ValidateLocalDirectory(string localDirectoryPath, int diskSizeBytes, int maxRootDirectoryEntries, TOSVersion tosVersion)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(localDirectoryPath);
                uint localDirectorySizeBytes = (uint)Directory.GetFiles(directoryInfo.FullName, "*", SearchOption.AllDirectories).Sum(file => (new FileInfo(file).Length));

                if (localDirectorySizeBytes > MaxDiskSizeBytes(tosVersion))
                    throw new System.InsufficientMemoryException($"Local directory size is {localDirectorySizeBytes / BytesPerMiB} MiB, which is larger than the maximum allowable virtual disk size ({MaxDiskSizeBytes(tosVersion) / BytesPerMiB} MiB)");

                else if (localDirectorySizeBytes > diskSizeBytes)
                    throw new System.InsufficientMemoryException($"Local directory size is {localDirectorySizeBytes / BytesPerMiB} MiB, which is too large for the given virtual disk size ({diskSizeBytes / BytesPerMiB} MiB)");

                int rootDirectoryEntries = Directory.GetFiles(directoryInfo.FullName, "*", SearchOption.TopDirectoryOnly).Count()
                    + Directory.GetDirectories(directoryInfo.FullName, "*", SearchOption.TopDirectoryOnly).Count();

                if (rootDirectoryEntries > maxRootDirectoryEntries)
                    throw new System.InsufficientMemoryException($"The root directory has {rootDirectoryEntries} files/directories, which is more than the maximum ({maxRootDirectoryEntries} allowed");
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
