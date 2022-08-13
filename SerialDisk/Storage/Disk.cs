using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.Models;
using Z80andrew.SerialDisk.Utilities;

namespace Z80andrew.SerialDisk.Storage
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

            ImportLocalDiskContent(_localDirectoryContentInfos, Parameters.LocalDirectoryPath, _rootDirectoryClusterIndex);
        }

        private void InitDiskContentVariables()
        {
            _previousFreeClusterIndex = 1;
            _localDirectoryContentInfos = new List<LocalDirectoryContentInfo>();

            _rootDirectoryBuffer = new byte[Parameters.RootDirectorySectors * Parameters.BytesPerSector];
            _fatBuffer = new byte[Parameters.SectorsPerFat * Parameters.BytesPerSector];
            _clusterInfos = Enumerable.Range(0, Parameters.DiskClusters).Select(n => new ClusterInfo(Parameters.BytesPerCluster)).ToArray();
        }

        #region FAT16 helper methods

        private string GetAbsolutePath(string relativePath)
        {
            return Path.Combine(Parameters.LocalDirectoryPath, relativePath);
        }

        private byte[] GetDirectoryClusterData(int directoryClusterIndex)
        {
            byte[] directoryClusterData;

            if (directoryClusterIndex == _rootDirectoryClusterIndex)
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


        private int AddNextFreeClusterToChain(int? clusterToExtend = null)
        {
            int newClusterIndex = GetNextFreeClusterIndex();

            if (clusterToExtend.HasValue && clusterToExtend.Value != _rootDirectoryClusterIndex)
            {
                _fatBuffer[clusterToExtend.Value * 2] = (byte)(newClusterIndex & 0xff);
                _fatBuffer[clusterToExtend.Value * 2 + 1] = (byte)((newClusterIndex >> 8) & 0xff);
            }

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

        private static LocalDirectoryContentInfo FindLocalDirectoryContentInfo(List<LocalDirectoryContentInfo> localDirectoryContentInfos, int directoryClusterIndex, int directoryEntryIndex, int entryStartClusterIndex)
        {
            return localDirectoryContentInfos.Where(ldci => ldci.EntryIndex == directoryEntryIndex
                                                && ldci.DirectoryCluster == directoryClusterIndex
                                                && ldci.StartCluster == entryStartClusterIndex
                                                && ldci.WriteInProgress == false).SingleOrDefault();
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
                    _logger.Log($"Reading data from FAT area", Constants.LoggingLevel.All);
                    int readSector = sector;

                    if (readSector >= Parameters.SectorsPerFat)
                        readSector -= Parameters.SectorsPerFat;

                    Array.Copy(_fatBuffer, readSector * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                }

                // Root directory
                else if (sector < Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors)
                {
                    _logger.Log($"Reading data from root directory", Constants.LoggingLevel.All);
                    Array.Copy(_rootDirectoryBuffer, (sector - Parameters.SectorsPerFat * 2) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                }

                // DATA area
                else
                {
                    int readSector = sector - (Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) + 2 * Parameters.SectorsPerCluster;
                    int clusterIndex = readSector / Parameters.SectorsPerCluster;

                    // Directory clusters are not read from disk
                    if (_clusterInfos[clusterIndex].IsDirectoryCluster)
                    {
                        _logger.Log($"Reading directory data cluster {clusterIndex}", Constants.LoggingLevel.All);
                        Array.Copy(_clusterInfos[clusterIndex].DataBuffer, (readSector - clusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                    }

                    else if (_clusterInfos[clusterIndex].LocalDirectoryContent?.LocalPath != null)
                    {
                        if (firstSector == sector)
                        {
                            _logger.Log($"Reading local file {_clusterInfos[clusterIndex].LocalDirectoryContent.LocalPath}", Constants.LoggingLevel.Info);
                        }

                        byte[] fileClusterDataBuffer = new byte[Parameters.BytesPerCluster];

                        try
                        {
                            using (FileStream fileStream = File.OpenRead(GetAbsolutePath(_clusterInfos[clusterIndex].LocalDirectoryContent.LocalPath)))
                            {
                                int bytesToRead = Math.Min(Parameters.BytesPerCluster, (int)(fileStream.Length - _clusterInfos[clusterIndex].FileOffset));

                                fileStream.Seek(_clusterInfos[clusterIndex].FileOffset, SeekOrigin.Begin);

                                for (int Index = 0; Index < bytesToRead; Index++)
                                    fileClusterDataBuffer[Index] = (byte)fileStream.ReadByte();

                                Array.Copy(fileClusterDataBuffer, (readSector - (clusterIndex * Parameters.SectorsPerCluster)) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                            }
                        }

                        catch (Exception ex)
                        {
                            _logger.LogException(ex, "Error reading disk sectors");
                        }
                    }

                    // DATA area not yet written to disk (e.g. during incomplete file transfers)
                    else
                    {
                        _logger.Log($"Reading incomplete local file / sector from data buffer at cluster {clusterIndex}, offset {_clusterInfos[clusterIndex].FileOffset}", Constants.LoggingLevel.All);
                        Array.Copy(_clusterInfos[clusterIndex].DataBuffer, (readSector - clusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
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

                    // Directory entries are updated in FAT on their final cluster; directories on their first
                    var localDirectoryContentInfos = _localDirectoryContentInfos.Where(ldci =>
                        (ldci.FinalCluster <= upperFATClusterBound && ldci.FinalCluster >= lowerFATClusterBound)
                        || (ldci.StartCluster <= upperFATClusterBound && ldci.StartCluster >= lowerFATClusterBound));

                    foreach (var directoryInfo in localDirectoryContentInfos)
                    {
                        int directoryClusterIndex = _clusterInfos[directoryInfo.StartCluster].IsDirectoryCluster ? directoryInfo.StartCluster : directoryInfo.DirectoryCluster;
                        if (!localDirectoryContentToUpdate.Contains(directoryClusterIndex)) localDirectoryContentToUpdate.Add(directoryClusterIndex);
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

                    Array.Copy(dataBuffer, dataOffset, _clusterInfos[clusterIndex].DataBuffer, (WriteSector - clusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, Parameters.BytesPerSector);

                    // Empty files are not written to the FAT so must be synced via their containing directory
                    SyncDirectoryClusterToLocalDisk(_localDirectoryContentInfos, clusterIndex);
                }

                dataOffset += Parameters.BytesPerSector;
                numberOfSectors--;
                sector++;
            }
        }


        #endregion

        #region CRUD methods

        private void SyncDirectoryClusterToLocalDisk(List<LocalDirectoryContentInfo> localDirectoryContentInfos, int clusterIndex)
        {
            byte[] directoryData;

            directoryData = GetDirectoryClusterData(clusterIndex);

            // Only check for changes if this cluster contains directory entry information
            if (clusterIndex == _rootDirectoryClusterIndex || _clusterInfos[clusterIndex].IsDirectoryCluster)
            {
                bool continueCheckingEntries = true;

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
                                        RenameLocalDirectoryOrFile(directoryData, directoryEntryIndex, localContent, fileName);
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
                        continueCheckingEntries = false;
                    }

                    else
                    {
                        var nextClusterIndex = FatGetClusterValue(clusterIndex);
                        _logger.Log($"Directory cluster {clusterIndex} continues in cluster {nextClusterIndex}", Constants.LoggingLevel.All);

                        // If the next directory cluster is new, ensure it has a LocalDirectoryContentInfo assigned
                        if (_clusterInfos[nextClusterIndex].LocalDirectoryContent == null)
                        {
                            _clusterInfos[nextClusterIndex].LocalDirectoryContent = _clusterInfos[clusterIndex].LocalDirectoryContent;
                            _clusterInfos[nextClusterIndex].FileOffset = FAT16Helper.DirectoryFileOffset;
                            _logger.Log($"Directory cluster {clusterIndex} extended to cluster {nextClusterIndex}", Constants.LoggingLevel.All);
                        }

                        clusterIndex = nextClusterIndex;

                        directoryData = GetDirectoryClusterData(clusterIndex);
                    }

                } while (continueCheckingEntries);
            }
        }

        private void DeleteLocalDirectoryOrFile(List<LocalDirectoryContentInfo> localDirectoryContentInfos, byte[] directoryData, int directoryEntryIndex, LocalDirectoryContentInfo directoryContentInfo)
        {
            if (directoryData[directoryEntryIndex + 11] == FAT16Helper.DirectoryIdentifier)
            {
                _logger.Log($"Deleting local directory \"{directoryContentInfo.LocalPath}\"", Constants.LoggingLevel.Info);
                Directory.Delete(GetAbsolutePath(directoryContentInfo.LocalPath), true);
            }

            // It's a file
            else
            {
                _logger.Log($"Deleting local file \"{directoryContentInfo.LocalPath}\"", Constants.LoggingLevel.Info);
                File.Delete(GetAbsolutePath(directoryContentInfo.LocalPath));
            }

            var clustersToDelete = _clusterInfos.Where(_ => _.LocalDirectoryContent?.LocalPath == directoryContentInfo.LocalPath);

            foreach (var cluster in clustersToDelete)
            {
                _logger.Log($"Removing local cluster data for {cluster.LocalDirectoryContent.LocalPath} (offset {cluster.FileOffset})", Constants.LoggingLevel.All);
                cluster.LocalDirectoryContent = null;
            }

            localDirectoryContentInfos.Remove(directoryContentInfo);
        }

        private void RenameLocalDirectoryOrFile(byte[] directoryData, int directoryEntryIndex, LocalDirectoryContentInfo directoryContentInfo, string newContentName)
        {
            string oldContentPath = directoryContentInfo.LocalPath;

            directoryContentInfo.LocalFileName = newContentName;
            directoryContentInfo.TOSFileName = newContentName;

            if (directoryData[directoryEntryIndex + 11] == FAT16Helper.DirectoryIdentifier)
            {
                _logger.Log($"Renaming local directory \"{oldContentPath}\" to \"{directoryContentInfo.LocalPath}\"", Constants.LoggingLevel.Info);
                Directory.Move(GetAbsolutePath(oldContentPath), GetAbsolutePath(directoryContentInfo.LocalPath));
            }

            // It's a file
            else
            {
                _logger.Log($"Renaming local file \"{oldContentPath}\" to \"{directoryContentInfo.LocalPath}\"", Constants.LoggingLevel.Info);
                File.Move(GetAbsolutePath(oldContentPath), GetAbsolutePath(directoryContentInfo.LocalPath));
            }
        }

        private void UpdateLocalDirectoryOrFile(List<LocalDirectoryContentInfo> localDirectoryContentInfos, byte[] directoryData,
            int directoryClusterIndex, int directoryEntryIndex, int entryStartClusterIndex, string newContentName)
        {
            string newContentPath = null;

            if (directoryClusterIndex != _rootDirectoryClusterIndex)
            {
                string newPathDirectory = _clusterInfos[directoryClusterIndex].LocalDirectoryContent.LocalPath;
                newContentPath = Path.Combine(newPathDirectory, newContentName);
            }

            else newContentPath = newContentName;

            try
            {
                if (directoryData[directoryEntryIndex + 11] == FAT16Helper.DirectoryIdentifier)
                {
                    // Is it a directory with a valid start cluster?
                    if (entryStartClusterIndex != 0)
                    {
                        _logger.Log("Creating local directory \"" + newContentPath + "\"", Constants.LoggingLevel.Info);

                        var CreatedLocalDirectory = Directory.CreateDirectory(GetAbsolutePath(newContentPath));

                        var newLocalDirectoryContent = new LocalDirectoryContentInfo
                        {
                            ParentDirectory = _clusterInfos[directoryClusterIndex].LocalDirectoryContent,
                            LocalFileName = newContentName,
                            TOSFileName = newContentName,
                            EntryIndex = directoryEntryIndex,
                            DirectoryCluster = directoryClusterIndex,
                            StartCluster = entryStartClusterIndex,
                            FinalCluster = entryStartClusterIndex,
                            WriteInProgress = false
                        };

                        localDirectoryContentInfos.Add(newLocalDirectoryContent);

                        _clusterInfos[entryStartClusterIndex].LocalDirectoryContent = newLocalDirectoryContent;
                        _clusterInfos[entryStartClusterIndex].FileOffset = FAT16Helper.DirectoryFileOffset;
                    }
                }

                // it's a file
                else
                {
                    _logger.Log($"Checking if local file {newContentPath} should be updated", Constants.LoggingLevel.Debug);

                    int fileSize = directoryData[directoryEntryIndex + 28] | (directoryData[directoryEntryIndex + 29] << 8) | (directoryData[directoryEntryIndex + 30] << 16) | (directoryData[directoryEntryIndex + 31] << 24);

                    _logger.Log("File size to write:" + fileSize, Constants.LoggingLevel.All);

                    int fileClusterIndex = entryStartClusterIndex;

                    _logger.Log("Finding existing content info for local file \"" + newContentPath + "\".", Constants.LoggingLevel.All);

                    var localDirectoryContent = localDirectoryContentInfos.Where(ldci => ldci.LocalPath == newContentPath).FirstOrDefault();

#if DEBUG
                    if (localDirectoryContentInfos.Where(ldci => ldci.LocalPath == newContentPath).Count() > 1) Debugger.Break();
#endif

                    if (localDirectoryContent == null)
                    {
                        _logger.Log($"Creating local file: {newContentPath}", Constants.LoggingLevel.Info);

                        File.Create(GetAbsolutePath(newContentPath)).Dispose();

                        var newLocalDirectoryContent = new LocalDirectoryContentInfo
                        {
                            ParentDirectory = _clusterInfos[directoryClusterIndex].LocalDirectoryContent,
                            LocalFileName = newContentName,
                            TOSFileName = newContentName,
                            EntryIndex = directoryEntryIndex,
                            DirectoryCluster = directoryClusterIndex,
                            StartCluster = entryStartClusterIndex,
                            FinalCluster = -1
                        };

                        localDirectoryContentInfos.Add(newLocalDirectoryContent);

                        localDirectoryContent = newLocalDirectoryContent;
                    }

                    localDirectoryContent.StartCluster = entryStartClusterIndex;
                    _logger.Log("File start cluster: " + localDirectoryContent.StartCluster + " (" + FatGetClusterValue(localDirectoryContent.StartCluster) + ")", Constants.LoggingLevel.All);

                    // Entry cluster will be assigned if this is a non-empty file
                    if (entryStartClusterIndex != 0)
                    {
                        localDirectoryContent.WriteInProgress = true;
                        _logger.Log("Content marked for write: " + localDirectoryContent.WriteInProgress, Constants.LoggingLevel.All);

                        int remainingBytes = fileSize;

                        // Check if the file has been completely written.
                        // Number of bytes in the cluster chain must be checked in case this file is being written
                        // in multiple passes due to lack of available RAM on the Atari
                        while (!FAT16Helper.IsEndOfClusterChain(fileClusterIndex))
                        {
                            // Keep final cluster updated in case this file is being written in multiple passes
                            localDirectoryContent.FinalCluster = fileClusterIndex;
                            remainingBytes -= Parameters.BytesPerCluster;
                            fileClusterIndex = FatGetClusterValue(fileClusterIndex);
                        }

                        bool allBytesAvailable = remainingBytes <= 0 && FAT16Helper.IsEndOfFile(fileClusterIndex);

                        _logger.Log("All bytes available?: " + allBytesAvailable, Constants.LoggingLevel.All);

                        // Final FAT cluster value in chain matches a fully written file
                        if (allBytesAvailable)
                        {
                            localDirectoryContent.WriteInProgress = false;

                            try
                            {
                                _logger.Log("Finding existing content info for local file \"" + newContentPath + "\".", Constants.LoggingLevel.All);

                                localDirectoryContent = localDirectoryContentInfos.Where(ldci => ldci.LocalPath == newContentPath).Single();

                                localDirectoryContent.StartCluster = entryStartClusterIndex;


                                _logger.Log("Writing to local file \"" + newContentPath + "\".", Constants.LoggingLevel.Info);

                                using (BinaryWriter FileBinaryWriter = new BinaryWriter(File.OpenWrite(GetAbsolutePath(newContentPath))))
                                {
                                    fileClusterIndex = entryStartClusterIndex;
                                    remainingBytes = fileSize;
                                    int fileOffset = 0;

                                    while (!FAT16Helper.IsEndOfFile(fileClusterIndex))
                                    {
                                        _clusterInfos[fileClusterIndex].LocalDirectoryContent = localDirectoryContent;
                                        _clusterInfos[fileClusterIndex].FileOffset = fileOffset;

                                        FileBinaryWriter.Write(_clusterInfos[fileClusterIndex].DataBuffer, 0, Math.Min(_clusterInfos[fileClusterIndex].DataBuffer.Length, remainingBytes));

                                        remainingBytes -= _clusterInfos[fileClusterIndex].DataBuffer.Length;
                                        fileOffset += _clusterInfos[fileClusterIndex].DataBuffer.Length;

                                        // Buffer has been written to disk; free up RAM
                                        _clusterInfos[fileClusterIndex].DeallocateBuffer();

                                        localDirectoryContent.FinalCluster = fileClusterIndex;
                                        fileClusterIndex = FatGetClusterValue(fileClusterIndex);
                                    }

                                    _logger.Log("Bytes remaining after write:" + remainingBytes, Constants.LoggingLevel.All);
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

            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        #endregion

        #region Local disk content import methods

        private bool FatAddDirectoryEntry(List<LocalDirectoryContentInfo> localDirectoryContentInfos,
            int directoryClusterIndex, int entryStartClusterIndex,
            string directoryPath, string fileName, string TOSFileName,
            byte attributeFlags, DateTime lastWriteDateTime, long fileSize)
        {
            byte[] directoryBuffer;
            int entryIndex = 0;

            int maxEntryIndex = directoryClusterIndex == _rootDirectoryClusterIndex ? _rootDirectoryBuffer.Length : Parameters.BytesPerCluster;

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
                            int newDirectoryCluster = AddNextFreeClusterToChain(directoryClusterIndex);

                            _clusterInfos[newDirectoryCluster] = new ClusterInfo(Parameters.BytesPerCluster)
                            {
                                FileOffset = Constants.DirectoryClusterOffset,
                                LocalDirectoryContent = _clusterInfos[directoryClusterIndex].LocalDirectoryContent
                        };

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

            if (TOSFileName[0] != '.')
            {
                LocalDirectoryContentInfo newLocalDirectoryContentInfo = new LocalDirectoryContentInfo()
                {
                    ParentDirectory = _clusterInfos[directoryClusterIndex].LocalDirectoryContent,
                    LocalFileName = fileName,
                    TOSFileName = TOSFileName,
                    EntryIndex = entryIndex,
                    DirectoryCluster = directoryClusterIndex,
                    StartCluster = entryStartClusterIndex
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

        private void ImportLocalDirectory(List<LocalDirectoryContentInfo> localDirectoryContentInfos, DirectoryInfo directoryInfo, int parentDirectoryCluster)
        {
            int newDirectoryClusterIndex = AddNextFreeClusterToChain();

            _clusterInfos[newDirectoryClusterIndex].FileOffset = Constants.DirectoryClusterOffset;

            FatAddDirectoryEntry(localDirectoryContentInfos,
                parentDirectoryCluster,
                newDirectoryClusterIndex,
                directoryInfo.Parent.FullName,
                directoryInfo.Name,
                FAT16Helper.GetShortFileName(directoryInfo.Name),
                FAT16Helper.DirectoryIdentifier,
                directoryInfo.LastWriteTime,
                0);

            FatAddDirectoryEntry(localDirectoryContentInfos, newDirectoryClusterIndex, newDirectoryClusterIndex, directoryInfo.FullName, ".", ".", FAT16Helper.DirectoryIdentifier, directoryInfo.LastWriteTime, 0);
            FatAddDirectoryEntry(localDirectoryContentInfos, newDirectoryClusterIndex, parentDirectoryCluster, directoryInfo.Parent.FullName, "..", "..", FAT16Helper.DirectoryIdentifier, directoryInfo.LastWriteTime, 0);

            ImportLocalDiskContent(localDirectoryContentInfos, directoryInfo.FullName, newDirectoryClusterIndex);
        }

        private void ImportLocalFile(List<LocalDirectoryContentInfo> localDirectoryContentInfos, FileInfo fileInfo, int directoryClusterIndex)
        {
            long fileOffset = 0;
            int fileentryStartClusterIndex = 0;
            int nextFileClusterIndex = AddNextFreeClusterToChain();

            while (fileOffset < fileInfo.Length)
            {
                try
                {
                    nextFileClusterIndex = AddNextFreeClusterToChain(nextFileClusterIndex);

                    if (fileentryStartClusterIndex == _rootDirectoryClusterIndex)
                        fileentryStartClusterIndex = nextFileClusterIndex;

                    _clusterInfos[nextFileClusterIndex].FileOffset = fileOffset;

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

        private void ImportLocalDiskContent(List<LocalDirectoryContentInfo> localDirectoryContentInfos, string directoryPath, int directoryClusterIndex)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

            foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles())
                ImportLocalFile(localDirectoryContentInfos, fileInfo, directoryClusterIndex);

            foreach (DirectoryInfo subDirectoryInfo in directoryInfo.EnumerateDirectories())
                ImportLocalDirectory(localDirectoryContentInfos, subDirectoryInfo, directoryClusterIndex);
        }

        public void ReimportLocalDirectoryContents()
        {
            _logger.Log($"Reimporting local directory contents from {Parameters.LocalDirectoryPath}", Constants.LoggingLevel.Info);

            InitDiskContentVariables();
            ImportLocalDiskContent(_localDirectoryContentInfos, Parameters.LocalDirectoryPath, _rootDirectoryClusterIndex);

            _logger.Log($"Import complete", Constants.LoggingLevel.Info);
        }

        #endregion
    }
}