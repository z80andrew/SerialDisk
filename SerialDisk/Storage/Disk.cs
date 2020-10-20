using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace AtariST.SerialDisk.Storage
{
    public class Disk : IDisk
    {
        private readonly int _rootDirectoryClusterIndex;
        private byte[] _rootDirectoryBuffer;
        private byte[] _fatBuffer;
        private int _previousFreeClusterIndex;

        private ClusterInfo[] _clusterInfos;
        private List<LocalDirectoryContentInfo> _localDirectoryContentInfos;
        
        public DiskParameters Parameters { get; }
        private readonly ILogger _logger;

        public Disk(DiskParameters diskParams, ILogger logger)
        {
            _logger = logger;
            Parameters = diskParams;
            _rootDirectoryClusterIndex = 0;

            try
            {
                InitDiskContentVariables();

                int maxRootDirectoryEntries = ((diskParams.RootDirectorySectors * diskParams.BytesPerSector) / 32) - 2; // Each entry is 32 bytes, 2 entries reserved for . and ..
                FAT16Helper.ValidateLocalDirectory(diskParams.LocalDirectoryPath, diskParams.DiskTotalBytes, maxRootDirectoryEntries, diskParams.SectorsPerCluster, diskParams.TOS);
            }

            catch (Exception ex)
            {
                _logger.LogException(ex, ex.Message);
                throw;
            }

            FatImportLocalDirectoryContents(_localDirectoryContentInfos, Parameters.LocalDirectoryPath, _rootDirectoryClusterIndex);
        }

        private void InitDiskContentVariables()
        {
            _previousFreeClusterIndex = 1;
            _localDirectoryContentInfos = new List<LocalDirectoryContentInfo>();

            _rootDirectoryBuffer = new byte[Parameters.RootDirectorySectors * Parameters.BytesPerSector];
            _fatBuffer = new byte[Parameters.SectorsPerFat * Parameters.BytesPerSector];
            _clusterInfos = new ClusterInfo[Parameters.DiskClusters];
        }

        private LocalDirectoryContentInfo FindLocalDirectoryContentInfo(List<LocalDirectoryContentInfo> localDirectoryContentInfos, int directoryClusterIndex, int directoryEntryIndex, int entryStartClusterIndex)
        {
            return localDirectoryContentInfos.Where(ldci => ldci.EntryIndex == directoryEntryIndex
                                                && ldci.DirectoryCluster == directoryClusterIndex
                                                && ldci.StartCluster == entryStartClusterIndex
                                                && ldci.WriteInProgress == false).SingleOrDefault();
        }

        private void SyncDirectoryClusterToLocalDisk(List<LocalDirectoryContentInfo> localDirectoryContentInfos, int clusterIndex)
        {
            byte[] directoryData;

            directoryData = GetDirectoryClusterData(clusterIndex);

            // Only check for changes if this cluster contains directory entry information
            if (clusterIndex == 0 || _clusterInfos[clusterIndex].FileOffset == -1)
            {
                bool localContentChangeFound = false;

                do
                {
                    _logger.Log($"Updating directory cluster {clusterIndex}", Constants.LoggingLevel.All);
                    int directoryEntryIndex = 0;

                    while (directoryEntryIndex < directoryData.Length && directoryData[directoryEntryIndex] != 0)
                    {
                        // The entry is not "." or "..".
                        if (directoryData[directoryEntryIndex] != 0x2e)
                        {
                            string fileName = ASCIIEncoding.ASCII.GetString(directoryData, directoryEntryIndex, 8).Trim();
                            string fileExtension = ASCIIEncoding.ASCII.GetString(directoryData, directoryEntryIndex + 8, 3).Trim();

                            if (fileExtension != "")
                                fileName += "." + fileExtension;

                            int entryStartClusterIndex = directoryData[directoryEntryIndex + 26] | (directoryData[directoryEntryIndex + 27] << 8);

                            // Find the matching local content and check what happened to it.
                            var localContent = FindLocalDirectoryContentInfo(localDirectoryContentInfos, clusterIndex, directoryEntryIndex, entryStartClusterIndex);

                            if (localContent != null)
                            {
                                if (localContent.TOSFileName != fileName)
                                {
                                    if (directoryData[directoryEntryIndex] == FAT16Helper.DeletedEntryIdentifier)
                                    {
                                        DeleteLocalDirectoryOrFile(localDirectoryContentInfos, directoryData, directoryEntryIndex, localContent);
                                    }

                                    else
                                    {
                                        RenameLocalDirectoryOrFile(localDirectoryContentInfos, directoryData, directoryEntryIndex, localContent, fileName);
                                    }
                                }
                            }

                            // Entry is new
                            else if (directoryData[directoryEntryIndex] != FAT16Helper.DeletedEntryIdentifier)
                            {
                                UpdateLocalDirectoryOrFile(localDirectoryContentInfos, directoryData, clusterIndex, directoryEntryIndex, entryStartClusterIndex, fileName);
                            }
                        }

                        directoryEntryIndex += 32;
                    }

                    if (directoryEntryIndex < directoryData.Length || FAT16Helper.IsEndOfClusterChain(FatGetClusterValue(clusterIndex)))
                    {
                        localContentChangeFound = true;
                    }

                    else
                    {
                        var nextClusterIndex = FatGetClusterValue(clusterIndex);
                        _logger.Log($"Directory cluster {clusterIndex} continues in cluster {nextClusterIndex}", Constants.LoggingLevel.All);

                        // If the next directory cluster is new, ensure it has a LocalDirectoryContentInfo assigned
                        if (_clusterInfos[nextClusterIndex].LocalDirectoryContent == null) _clusterInfos[nextClusterIndex].LocalDirectoryContent = _clusterInfos[clusterIndex].LocalDirectoryContent;
                        clusterIndex = nextClusterIndex;

                        directoryData = GetDirectoryClusterData(clusterIndex);
                    }

                } while (!localContentChangeFound);
            }
        }

        private void DeleteLocalDirectoryOrFile(List<LocalDirectoryContentInfo> localDirectoryContentInfos, byte[] directoryData, int directoryEntryIndex, LocalDirectoryContentInfo directoryContentInfo)
        {
            if (directoryData[directoryEntryIndex + 11] == FAT16Helper.DirectoryIdentifier)
            {
                _logger.Log($"Deleting local directory \"{ directoryContentInfo.LocalPath}\".", Constants.LoggingLevel.Info);

                Directory.Delete(directoryContentInfo.LocalPath, true);
            }

            // It's a file
            else
            {
                _logger.Log($"Deleting local file \"{directoryContentInfo.LocalPath}\".", Constants.LoggingLevel.Info);

                File.Delete(directoryContentInfo.LocalPath);
            }

            var clusterIndexesToDelete = _clusterInfos
                .Select((ci, index) => new { ci, index })
                .Where(_ => _.ci?.LocalDirectoryContent == directoryContentInfo)
                .Select(_ => _.index);

            foreach (int clusterIndex in clusterIndexesToDelete)
            {
                _logger.Log($"Removing local data for cluster {clusterIndex}.", Constants.LoggingLevel.All);
                _clusterInfos[clusterIndex] = null;
            }

            localDirectoryContentInfos.Remove(directoryContentInfo);
        }

        private void RenameLocalDirectoryOrFile(List<LocalDirectoryContentInfo> localDirectoryContentInfos, byte[] directoryData, int directoryEntryIndex, LocalDirectoryContentInfo directoryContentInfo, string newContentName)
        {
            string oldContentPath = directoryContentInfo.LocalPath;

            if (directoryData[directoryEntryIndex + 11] == FAT16Helper.DirectoryIdentifier)
            {
                directoryContentInfo.LocalFileName = newContentName;
                directoryContentInfo.TOSFileName = newContentName;

                _logger.Log($"Renaming local directory \"{oldContentPath}\" to \"{directoryContentInfo.LocalPath}\".", Constants.LoggingLevel.Info);

                localDirectoryContentInfos
                    .Where(ldci => ldci != null && ldci.LocalDirectory.StartsWith(oldContentPath))
                    .ToList()
                    .ForEach(ldci => ldci.LocalDirectory = ldci.LocalDirectory.Replace(oldContentPath, directoryContentInfo.LocalPath));

                Directory.Move(oldContentPath, directoryContentInfo.LocalPath);
            }

            // It's a file
            else
            {
                directoryContentInfo.LocalFileName = newContentName;
                directoryContentInfo.TOSFileName = newContentName;

                _logger.Log($"Renaming local file \"{oldContentPath}\" to \"{directoryContentInfo.LocalPath}\".", Constants.LoggingLevel.Info);

                File.Move(oldContentPath, directoryContentInfo.LocalPath);
            }
        }

        private void UpdateLocalDirectoryOrFile(List<LocalDirectoryContentInfo> localDirectoryContentInfos, byte[] directoryData, 
            int directoryClusterIndex, int directoryEntryIndex, int entryStartClusterIndex, string newContentName)
        {
            string newPathDirectory;
            if (directoryClusterIndex != _rootDirectoryClusterIndex) newPathDirectory = _clusterInfos[directoryClusterIndex].LocalDirectoryContent.LocalPath; // Subdirectory
            else newPathDirectory = Parameters.LocalDirectoryPath; // Root dir

            string newContentPath = Path.Combine(newPathDirectory, newContentName);

            try
            {
                if (directoryData[directoryEntryIndex + 11] == FAT16Helper.DirectoryIdentifier)
                {
                    // Is it a directory with a valid start cluster?
                    if (entryStartClusterIndex != 0)
                    {
                        _logger.Log("Creating local directory \"" + newContentPath + "\".", Constants.LoggingLevel.Info);

                        var CreatedLocalDirectory = Directory.CreateDirectory(newContentPath);

                        var newLocalDirectoryContent = new LocalDirectoryContentInfo
                        {
                            LocalDirectory = newPathDirectory,
                            LocalFileName = newContentName,
                            TOSFileName = newContentName,
                            EntryIndex = directoryEntryIndex,
                            DirectoryCluster = directoryClusterIndex,
                            StartCluster = entryStartClusterIndex,
                            WriteInProgress = false
                        };

                        localDirectoryContentInfos.Add(newLocalDirectoryContent);

                        _clusterInfos[entryStartClusterIndex].LocalDirectoryContent = newLocalDirectoryContent;
                        _clusterInfos[entryStartClusterIndex].FileOffset = -1;
                    }
                }

                // it's a file
                else
                {
                    _logger.Log($"Checking if local file {newContentPath} should be updated", Constants.LoggingLevel.Info);

                    int fileClusterIndex = entryStartClusterIndex;

                    _logger.Log("Finding existing content info for local file \"" + newContentPath + "\".", Constants.LoggingLevel.All);

                    var localDirectoryContent = localDirectoryContentInfos.Where(ldci => ldci.LocalPath == newContentPath).SingleOrDefault();

                    if (localDirectoryContent == null)
                    {
                        _logger.Log($"Creating local file: {newContentPath}", Constants.LoggingLevel.Info);
                        File.Create(newContentPath).Dispose();

                        var newLocalDirectoryContent = new LocalDirectoryContentInfo
                        {
                            LocalDirectory = newPathDirectory,
                            LocalFileName = newContentName,
                            TOSFileName = newContentName,
                            EntryIndex = directoryEntryIndex,
                            DirectoryCluster = directoryClusterIndex,
                            StartCluster = entryStartClusterIndex,
                            WriteInProgress = false
                        };

                        localDirectoryContentInfos.Add(newLocalDirectoryContent);

                        localDirectoryContent = newLocalDirectoryContent;
                    }

                    // Entry cluster will be assigned if this is a non-empty file
                    if (entryStartClusterIndex != 0)
                    {
                        // A cluster has been assigned to the file, 
                        // but the corresponding data and FAT entry haven't been written yet
                        if (FatGetClusterValue(entryStartClusterIndex) == 0)
                        {
                            localDirectoryContent.StartCluster = entryStartClusterIndex;
                            localDirectoryContent.WriteInProgress = true;
                        }

                        else
                        {
                            // Check if the file has been completely written.
                            while (!FAT16Helper.IsEndOfClusterChain(fileClusterIndex))
                            {
                                fileClusterIndex = FatGetClusterValue(fileClusterIndex);
                            }


                            // FAT cluster value matches a fully written file
                            if (FAT16Helper.IsEndOfFile(fileClusterIndex))
                            {
                                localDirectoryContent.WriteInProgress = false;

                                try
                                {
                                    _logger.Log("Finding existing content info for local file \"" + newContentPath + "\".", Constants.LoggingLevel.All);

                                    var newLocalDirectoryContent = localDirectoryContentInfos.Where(ldci => ldci.LocalPath == newContentPath).Single();

                                    newLocalDirectoryContent.StartCluster = entryStartClusterIndex;

                                    _logger.Log("Updating local file \"" + newContentPath + "\".", Constants.LoggingLevel.Info);

                                    using (BinaryWriter FileBinaryWriter = new BinaryWriter(File.OpenWrite(newContentPath)))
                                    {
                                        fileClusterIndex = entryStartClusterIndex;

                                        int fileSize = directoryData[directoryEntryIndex + 28] | (directoryData[directoryEntryIndex + 29] << 8) | (directoryData[directoryEntryIndex + 30] << 16) | (directoryData[directoryEntryIndex + 31] << 24);

                                        while (!FAT16Helper.IsEndOfFile(fileClusterIndex))
                                        {
                                            _clusterInfos[fileClusterIndex].LocalDirectoryContent = newLocalDirectoryContent;

                                            FileBinaryWriter.Write(_clusterInfos[fileClusterIndex].DataBuffer, 0, Math.Min(_clusterInfos[fileClusterIndex].DataBuffer.Length, fileSize));

                                            fileSize -= _clusterInfos[fileClusterIndex].DataBuffer.Length;

                                            // Buffer has been written to disk; free up RAM
                                            _clusterInfos[fileClusterIndex].DataBuffer = null;

                                            fileClusterIndex = FatGetClusterValue(fileClusterIndex);
                                        }
                                    }
                                }

                                catch (Exception ex)
                                {
                                    _logger.LogException(ex);
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private byte[] GetDirectoryClusterData(int directoryClusterIndex)
        {
            byte[] directoryClusterData;

            if (directoryClusterIndex == 0)
                directoryClusterData = _rootDirectoryBuffer;
            else
                directoryClusterData = _clusterInfos[directoryClusterIndex].DataBuffer;

            return directoryClusterData;
        }

        private int FatGetClusterValue(int clusterIndex, int directoryClusterIndex = 0)
        {
            int cluster = clusterIndex * 2;
            if (directoryClusterIndex != 0) cluster -= Parameters.RootDirectorySectors;
            return _fatBuffer[cluster + 1] << 8 | _fatBuffer[cluster];
        }

        private int GetNextFreeClusterIndexAndAssignToCluster(int clusterToAssign)
        {
            int newClusterIndex = GetNextFreeClusterIndex();

            _fatBuffer[clusterToAssign * 2] = (byte)(newClusterIndex & 0xff);
            _fatBuffer[clusterToAssign * 2 + 1] = (byte)((newClusterIndex >> 8) & 0xff);

            _fatBuffer[newClusterIndex * 2] = 0xff;
            _fatBuffer[newClusterIndex * 2 + 1] = 0xff;

            return newClusterIndex;
        }

        private int GetNextFreeClusterIndex()
        {
            int maxClusterIndex = _fatBuffer.Length / 2;
            int newClusterIndex = _previousFreeClusterIndex; // Start check at previous index for performance
            int newClusterValue = 0xFFFF;

            try
            {
                while (newClusterValue != 0)
                {
                    newClusterIndex++;
                    if (newClusterIndex > maxClusterIndex) newClusterIndex = 2; // End of buffer reached, loop back to beginning
                    else if (newClusterIndex == _previousFreeClusterIndex) throw new Exception("Could not find a free cluster in FAT"); // We have looped without finding a free cluster
                    newClusterValue = FatGetClusterValue(newClusterIndex);
                }

                _previousFreeClusterIndex = newClusterIndex;
            }

            catch (Exception ex)
            {
                _logger.LogException(ex);
                newClusterIndex = -1;
            }

            return newClusterIndex;
        }

        public byte[] ReadSectors(int sector, int numberOfSectors)
        {
            int firstSector = sector;
            byte[] dataBuffer = new byte[numberOfSectors * Parameters.BytesPerSector];
            int dataOffset = 0;

            while (numberOfSectors > 0)
            {
                // FAT area
                if (sector < Parameters.SectorsPerFat * 2)
                {
                    int readSector = sector;

                    if (readSector >= Parameters.SectorsPerFat)
                        readSector -= Parameters.SectorsPerFat;

                    Array.Copy(_fatBuffer, readSector * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                }

                // Root directory
                else if (sector < Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors)
                {
                    Array.Copy(_rootDirectoryBuffer, (sector - Parameters.SectorsPerFat * 2) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                }

                // DATA area
                else
                {
                    int readSector = sector - (Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) + 2 * Parameters.SectorsPerCluster;
                    int clusterIndex = readSector / Parameters.SectorsPerCluster;

                    if (_clusterInfos[clusterIndex] != null)
                    {
                        if (_clusterInfos[clusterIndex].DataBuffer != null)
                        {
                            Array.Copy(_clusterInfos[clusterIndex].DataBuffer, (readSector - clusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                        }
                        else
                        {
                            if (_clusterInfos[clusterIndex].LocalDirectoryContent.LocalPath != null)
                            {
                                if (firstSector == sector) _logger.Log($"Reading local file {_clusterInfos[clusterIndex].LocalDirectoryContent.LocalPath}", Constants.LoggingLevel.Info);

                                byte[] fileClusterDataBuffer = new byte[Parameters.BytesPerCluster];

                                try
                                {
                                    using (FileStream fileStream = File.OpenRead(_clusterInfos[clusterIndex].LocalDirectoryContent.LocalPath))
                                    {
                                        int bytesToRead = Math.Min(Parameters.BytesPerCluster, (int)(fileStream.Length - _clusterInfos[clusterIndex].FileOffset));

                                        fileStream.Seek(_clusterInfos[clusterIndex].FileOffset, SeekOrigin.Begin);

                                        for (int Index = 0; Index < bytesToRead; Index++)
                                            fileClusterDataBuffer[Index] = (byte)fileStream.ReadByte();

                                        Array.Copy(fileClusterDataBuffer, (readSector - clusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                                    }
                                }

                                catch (Exception ex)
                                {
                                    _logger.LogException(ex, "Error reading sectors");
                                }
                            }
                        }
                    }
                }

                dataOffset += Parameters.BytesPerSector;
                numberOfSectors--;
                sector++;
            }

            return dataBuffer;
        }

        public void WriteSectors(int receiveBufferLength, int startSector, byte[] dataBuffer)
        {
            int sector = startSector;
            int numberOfSectors = (int)Math.Ceiling((decimal)receiveBufferLength / Parameters.BytesPerSector);
            int dataOffset = 0;
            int clusterIndex = 0;

            while (numberOfSectors > 0)
            {
                // FAT area?
                if (sector < Parameters.SectorsPerFat * 2)
                {
                    int WriteSector = sector;

                    // Force all writes to the first FAT
                    if (WriteSector >= Parameters.SectorsPerFat) WriteSector -= Parameters.SectorsPerFat;

                    _logger.Log($"Updating FAT sector {WriteSector}", Constants.LoggingLevel.All);

                    Array.Copy(dataBuffer, dataOffset, _fatBuffer, WriteSector * Parameters.BytesPerSector, Parameters.BytesPerSector);

                    var lowerFATClusterBound = (WriteSector * Parameters.BytesPerSector) / 2;
                    var upperFATClusterBound = ((WriteSector * Parameters.BytesPerSector) + Parameters.BytesPerSector) / 2;

                    _logger.Log($"FAT entries for clusters {lowerFATClusterBound}-{upperFATClusterBound} updated", Constants.LoggingLevel.All);

                    var localDirectoryContentToUpdate = new List<int>();

                    // Find all unique directory clusters relating to FAT cluster entries that are in the update range
                    for (int index = lowerFATClusterBound; index <= upperFATClusterBound; index++)
                    {
                        var localDirectoryContentInfos = _localDirectoryContentInfos.Where(ldci => ldci.StartCluster == index);

                        foreach (var directoryInfo in localDirectoryContentInfos)
                        {
                            var isDirectoryCluster = _clusterInfos[directoryInfo.StartCluster]?.FileOffset == -1;

                            int directoryClusterIndex = isDirectoryCluster ? directoryInfo.StartCluster : directoryInfo.DirectoryCluster;

                            if (!localDirectoryContentToUpdate.Contains(directoryClusterIndex)) localDirectoryContentToUpdate.Add(directoryClusterIndex);
                        }
                    }

                    foreach (var directoryClusterIndex in localDirectoryContentToUpdate)
                    {
                        _logger.Log($"Syncing directory cluster {directoryClusterIndex} via FAT update", Constants.LoggingLevel.All);
                        SyncDirectoryClusterToLocalDisk(_localDirectoryContentInfos, directoryClusterIndex);
                    }
                }

                // Root directory area?
                else if (sector < Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors)
                {
                    _logger.Log($"Updating ROOT directory sector {sector}", Constants.LoggingLevel.All);

                    Array.Copy(dataBuffer, dataOffset, _rootDirectoryBuffer, (sector - Parameters.SectorsPerFat * 2) * Parameters.BytesPerSector, Parameters.BytesPerSector);

                    // Root directory must be synced independently
                    SyncDirectoryClusterToLocalDisk(_localDirectoryContentInfos, _rootDirectoryClusterIndex); 
                }

                // Data area, used for files and non-root directories
                else
                {
                    int WriteSector = sector - (Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) + 2 * Parameters.SectorsPerCluster;

                    clusterIndex = WriteSector / Parameters.SectorsPerCluster;

                    _logger.Log($"Updating DATA sector {WriteSector}, cluster {clusterIndex}", Constants.LoggingLevel.All);

                    if (_clusterInfos[clusterIndex] == null)
                    {
                        _clusterInfos[clusterIndex] = new ClusterInfo
                        {
                            DataBuffer = new byte[Parameters.BytesPerCluster],
                        };
                    }

                    Array.Copy(dataBuffer, dataOffset, _clusterInfos[clusterIndex].DataBuffer, (WriteSector - clusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, Parameters.BytesPerSector);

                    // Empty files are not written to the FAT so must be synced via their containing directory
                    SyncDirectoryClusterToLocalDisk(_localDirectoryContentInfos, clusterIndex);
                }

                dataOffset += Parameters.BytesPerSector;
                numberOfSectors--;
                sector++;
            }
        }

        private bool FatAddDirectoryEntry(List<LocalDirectoryContentInfo> localDirectoryContentInfos, 
            int directoryClusterIndex, int entryStartClusterIndex,
            string directoryPath, string fileName, string TOSFileName, 
            byte attributeFlags, DateTime lastWriteDateTime, long fileSize)
        {
            byte[] directoryBuffer;
            int entryIndex = 0;

            int maxEntryIndex = directoryClusterIndex == 0 ? _rootDirectoryBuffer.Length : Parameters.BytesPerCluster;

            if (directoryClusterIndex == _rootDirectoryClusterIndex)
                directoryBuffer = _rootDirectoryBuffer;
            else
                directoryBuffer = _clusterInfos[directoryClusterIndex].DataBuffer;

            // Check whether there is any space left in the cluster
            do
            {
                // No space left
                if (entryIndex >= maxEntryIndex)
                {
                    int nextDirectoryClusterIndex = FatGetClusterValue(directoryClusterIndex);

                    // This is the final cluster, allocate new cluster
                    if (FAT16Helper.IsEndOfFile(nextDirectoryClusterIndex))
                    {
                        try
                        {
                            int newDirectoryCluster = GetNextFreeClusterIndexAndAssignToCluster(directoryClusterIndex);

                            _clusterInfos[newDirectoryCluster] = new ClusterInfo()
                            {
                                FileOffset = -1,
                                DataBuffer = new byte[Parameters.BytesPerCluster]
                            };

                            _clusterInfos[newDirectoryCluster].LocalDirectoryContent = _clusterInfos[directoryClusterIndex].LocalDirectoryContent;

                            entryIndex = 0;
                        }

                        catch (IndexOutOfRangeException outOfRangeEx)
                        {
                            int localDirectorySizeMiB = (int)Directory.GetFiles(Parameters.LocalDirectoryPath, "*", SearchOption.AllDirectories).Sum(file => (new FileInfo(file).Length)) / FAT16Helper.BytesPerMiB;
                            _logger.LogException(outOfRangeEx, $"Local directory size is {localDirectorySizeMiB} MiB, which is too large for the given virtual disk size ({Parameters.DiskTotalBytes / FAT16Helper.BytesPerMiB} MiB)");
                            throw;
                        }
                    }

                    else
                    {
                        directoryClusterIndex = nextDirectoryClusterIndex;
                    }

                    directoryBuffer = _clusterInfos[directoryClusterIndex].DataBuffer;
                    entryIndex = 0;
                }

                // Find next unused entry in directory
                while (entryIndex < maxEntryIndex && directoryBuffer[entryIndex] != 0)
                    entryIndex += 32;

                if (entryIndex >= maxEntryIndex)
                {
                    if (directoryClusterIndex == _rootDirectoryClusterIndex)
                    {
                        Exception outofIndexesException = new Exception($"Exceeded available directory entries in {directoryPath}. There may be too many files in directory (max {(maxEntryIndex / 32) - 2} items).");
                        _logger.LogException(outofIndexesException, outofIndexesException.Message);
                        throw outofIndexesException;
                    }
                }

            } while (entryIndex >= maxEntryIndex);

            // Remember which local content matches this entry.

            if (TOSFileName != "." && TOSFileName != "..")
            {
                LocalDirectoryContentInfo newLocalDirectoryContentInfo = new LocalDirectoryContentInfo()
                {
                    LocalDirectory = directoryPath,
                    LocalFileName = fileName,
                    TOSFileName = TOSFileName,
                    EntryIndex = entryIndex,
                    DirectoryCluster = directoryClusterIndex,
                    StartCluster = entryStartClusterIndex,
                    WriteInProgress = false
                };

                localDirectoryContentInfos.Add(newLocalDirectoryContentInfo);

                // Cluster index 0 indicates empty file, so no data clusters to update
                if (entryStartClusterIndex != 0)
                {
                    _clusterInfos[entryStartClusterIndex].LocalDirectoryContent = newLocalDirectoryContentInfo;

                    int clusterValue = FatGetClusterValue(entryStartClusterIndex);

                    while (!FAT16Helper.IsEndOfFile(clusterValue))
                    {
                        _clusterInfos[clusterValue].LocalDirectoryContent = newLocalDirectoryContentInfo;
                        clusterValue = FatGetClusterValue(clusterValue);
                    }
                }
            }

            // File name.
            int fileNameIndex;

            for (fileNameIndex = 0; fileNameIndex < (8 + 3); fileNameIndex++)
                directoryBuffer[entryIndex + fileNameIndex] = 0x20;

            string[] nameAndExtender;
            byte[] asciiName;
            byte[] asciiExtender;

            if (TOSFileName == "." || TOSFileName == "..")
            {
                asciiName = ASCIIEncoding.ASCII.GetBytes(TOSFileName);
                asciiExtender = null;
            }
            else
            {
                nameAndExtender = TOSFileName.Split('.');
                asciiName = ASCIIEncoding.ASCII.GetBytes(nameAndExtender[0]);
                asciiExtender = nameAndExtender.Length == 2 ? ASCIIEncoding.ASCII.GetBytes(nameAndExtender[1]) : null;
            }

            for (fileNameIndex = 0; fileNameIndex < asciiName.Length; fileNameIndex++)
                directoryBuffer[entryIndex + fileNameIndex] = asciiName[fileNameIndex];

            if (asciiExtender != null)
                for (fileNameIndex = 0; fileNameIndex < asciiExtender.Length; fileNameIndex++)
                    directoryBuffer[entryIndex + 8 + fileNameIndex] = asciiExtender[fileNameIndex];

            // File attribute flags.

            directoryBuffer[entryIndex + 11] = attributeFlags;

            // File write time and date (little endian).

            UInt16 fatFileWriteTime = 0;
            UInt16 fatFileWriteDate = 0;

            int TwoSeconds = lastWriteDateTime.Second / 2;
            int Minutes = lastWriteDateTime.Minute;
            int Hours = lastWriteDateTime.Hour;
            int DayOfMonth = lastWriteDateTime.Day;
            int Month = lastWriteDateTime.Month;
            int YearsSince1980 = lastWriteDateTime.Year - 1980;

            fatFileWriteTime |= (UInt16)TwoSeconds;
            fatFileWriteTime |= (UInt16)(Minutes << 5);
            fatFileWriteTime |= (UInt16)(Hours << 11);

            fatFileWriteDate |= (UInt16)DayOfMonth;
            fatFileWriteDate |= (UInt16)(Month << 5);
            fatFileWriteDate |= (UInt16)(YearsSince1980 << 9);

            directoryBuffer[entryIndex + 22] = (byte)(fatFileWriteTime & 0xff);
            directoryBuffer[entryIndex + 23] = (byte)((fatFileWriteTime >> 8) & 0xff);
            directoryBuffer[entryIndex + 24] = (byte)(fatFileWriteDate & 0xff);
            directoryBuffer[entryIndex + 25] = (byte)((fatFileWriteDate >> 8) & 0xff);

            // Cluster (little endian).

            directoryBuffer[entryIndex + 26] = (byte)(entryStartClusterIndex & 0xff);
            directoryBuffer[entryIndex + 27] = (byte)((entryStartClusterIndex >> 8) & 0xff);

            // File size (little endian).

            directoryBuffer[entryIndex + 28] = (byte)(fileSize & 0xff);
            directoryBuffer[entryIndex + 29] = (byte)((fileSize >> 8) & 0xff);
            directoryBuffer[entryIndex + 30] = (byte)((fileSize >> 16) & 0xff);
            directoryBuffer[entryIndex + 31] = (byte)((fileSize >> 24) & 0xff);

            return true;
        }

        private void FatAddDirectory(List<LocalDirectoryContentInfo> localDirectoryContentInfos, DirectoryInfo directoryInfo, int parentDirectoryCluster)
        {
            int newDirectoryClusterIndex = GetNextFreeClusterIndexAndAssignToCluster(0); // Is there is a cleaner way to do this?

            _clusterInfos[newDirectoryClusterIndex] = new ClusterInfo()
            {
                FileOffset = -1,
                DataBuffer = new byte[Parameters.BytesPerCluster]
            };

            FatAddDirectoryEntry(localDirectoryContentInfos,
                parentDirectoryCluster,
                newDirectoryClusterIndex,
                directoryInfo.Parent.FullName,
                directoryInfo.Name, 
                FAT16Helper.GetShortFileName(directoryInfo.Name), 
                FAT16Helper.DirectoryIdentifier, 
                directoryInfo.LastWriteTime, 
                0);
            
            FatAddDirectoryEntry(localDirectoryContentInfos, newDirectoryClusterIndex, newDirectoryClusterIndex, directoryInfo.FullName, ".",".", FAT16Helper.DirectoryIdentifier, directoryInfo.LastWriteTime, 0);
            FatAddDirectoryEntry(localDirectoryContentInfos, newDirectoryClusterIndex, parentDirectoryCluster, directoryInfo.Parent.FullName, "..","..", FAT16Helper.DirectoryIdentifier, directoryInfo.LastWriteTime, 0);

            FatImportLocalDirectoryContents(localDirectoryContentInfos, directoryInfo.FullName, newDirectoryClusterIndex);
        }

        private void FatAddFile(List<LocalDirectoryContentInfo> localDirectoryContentInfos, FileInfo fileInfo, int directoryClusterIndex)
        {
            long fileOffset = 0;
            int fileentryStartClusterIndex = 0;
            int nextFileClusterIndex = 0;

            while (fileOffset < fileInfo.Length)
            {
                try
                {
                    nextFileClusterIndex = GetNextFreeClusterIndexAndAssignToCluster(nextFileClusterIndex);

                    if (fileentryStartClusterIndex == _rootDirectoryClusterIndex)
                        fileentryStartClusterIndex = nextFileClusterIndex;

                    _clusterInfos[nextFileClusterIndex] = new ClusterInfo()
                    {
                        FileOffset = fileOffset
                    };

                    fileOffset += Parameters.BytesPerCluster;
                }

                catch (IndexOutOfRangeException outOfRangeEx)
                {
                    int localDirectorySizeMiB = (int)Directory.GetFiles(Parameters.LocalDirectoryPath, "*", SearchOption.AllDirectories).Sum(file => (new FileInfo(file).Length)) / FAT16Helper.BytesPerMiB;
                    _logger.LogException(outOfRangeEx, $"Local directory size is {localDirectorySizeMiB} MiB, which is too large for the given virtual disk size ({Parameters.DiskTotalBytes / FAT16Helper.BytesPerMiB} MiB)");
                    throw;
                }
            }

            // handle duplicate short filenames
            string TOSFileName = FAT16Helper.GetShortFileName(fileInfo.Name);
            int duplicateId = 1;

            while (localDirectoryContentInfos.Where(ldi => ldi.TOSFileName.Equals(TOSFileName, StringComparison.InvariantCultureIgnoreCase) &&
                 ldi.DirectoryCluster == directoryClusterIndex).Any())
            {
                int numberStringLength = duplicateId.ToString().Length + 1; // +1 for ~
                int replaceIndex = TOSFileName.LastIndexOf('.') != -1 ? TOSFileName.LastIndexOf('.') : TOSFileName.Length;
                replaceIndex -= numberStringLength;
                TOSFileName = TOSFileName.Remove(replaceIndex, numberStringLength).Insert(replaceIndex, $"~{duplicateId}");
                duplicateId++;
            }

            FatAddDirectoryEntry(localDirectoryContentInfos, directoryClusterIndex, fileentryStartClusterIndex, 
                fileInfo.DirectoryName, fileInfo.Name, TOSFileName, 0x00, fileInfo.LastWriteTime, fileInfo.Length);
        }

        private void FatImportLocalDirectoryContents(List<LocalDirectoryContentInfo> localDirectoryContentInfos, string directoryPath, int directoryClusterIndex)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

            foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles())
                FatAddFile(localDirectoryContentInfos, fileInfo, directoryClusterIndex);

            foreach (DirectoryInfo subDirectoryInfo in directoryInfo.EnumerateDirectories())
                FatAddDirectory(localDirectoryContentInfos, subDirectoryInfo, directoryClusterIndex);
        }

        public void ReimportLocalDirectoryContents()
        {
            _logger.Log($"Reimporting local directory contents from {Parameters.LocalDirectoryPath}", Constants.LoggingLevel.Info);

            InitDiskContentVariables();
            FatImportLocalDirectoryContents(_localDirectoryContentInfos, Parameters.LocalDirectoryPath, _rootDirectoryClusterIndex);

            _logger.Log($"Import complete", Constants.LoggingLevel.Info);
        }
    }
}