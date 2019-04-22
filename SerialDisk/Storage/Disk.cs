using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AtariST.SerialDisk.Utilities;

namespace AtariST.SerialDisk.Storage
{
    public class Disk : IDisk
    {
        private int _rootDirectoryClusterIndex = 0;
        private byte[] _rootDirectoryBuffer;
        private byte[] _fatBuffer;

        private ClusterInfo[] _clusterInfos;
        private List<LocalDirectoryContentInfo> _localDirectoryContentInfos;
        private FileSystemWatcher _fileSystemWatcher { get; set; }
        private ILogger _logger;

        public DiskParameters Parameters { get; set; }
        public bool FileSystemWatcherEnabled
        {
            get => _fileSystemWatcher.EnableRaisingEvents;
            set => _fileSystemWatcher.EnableRaisingEvents = value;
        }
        public bool MediaChanged { get; set; } = true;

        public Disk(DiskParameters diskParams, ILogger log)
        {
            _logger = log;
            Parameters = diskParams;

            FatImportLocalDirectoryContents(Parameters.LocalDirectoryPath, _rootDirectoryClusterIndex);
            WatchLocalDirectory(Parameters.LocalDirectoryPath);
        }

        protected virtual void WatchLocalDirectory(string localDirectoryName)
        {
            _fileSystemWatcher = new FileSystemWatcher();

            _fileSystemWatcher.Path = localDirectoryName;
            _fileSystemWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _fileSystemWatcher.Filter = "";
            _fileSystemWatcher.IncludeSubdirectories = true;
            _fileSystemWatcher.Changed += new FileSystemEventHandler(FileChangedHandler);
            _fileSystemWatcher.Created += new FileSystemEventHandler(FileChangedHandler);
            _fileSystemWatcher.Deleted += new FileSystemEventHandler(FileChangedHandler);
            _fileSystemWatcher.Renamed += new RenamedEventHandler(FileChangedHandler);

            FileSystemWatcherEnabled = true;
        }

        public void SyncLocalDisk(int directoryClusterIndex, bool syncSubDirectoryContents = true)
        {
            while (!IsEndOfFile(directoryClusterIndex))
            {
                int directoryEntryIndex = 0;
                byte[] directoryBuffer;

                if (directoryClusterIndex == 0)
                    directoryBuffer = _rootDirectoryBuffer;
                else
                    directoryBuffer = _clusterInfos[directoryClusterIndex].DataBuffer;

                while (directoryEntryIndex < directoryBuffer.Length && directoryBuffer[directoryEntryIndex] != 0)
                {
                    if (directoryBuffer[directoryEntryIndex] != 0x2e) // The entry is not "." or "..".
                    {
                        string fileName = ASCIIEncoding.ASCII.GetString(directoryBuffer, directoryEntryIndex, 8).Trim();
                        string fileExtension = ASCIIEncoding.ASCII.GetString(directoryBuffer, directoryEntryIndex + 8, 3).Trim();

                        if (fileExtension != "")
                            fileName += "." + fileExtension;

                        int startClusterIndex = directoryBuffer[directoryEntryIndex + 26] | (directoryBuffer[directoryEntryIndex + 27] << 8);

                        // Find the matching local content and check what happened to it.

                        string LocalDirectoryContentName = "";

                        foreach (LocalDirectoryContentInfo directoryContentInfo in _localDirectoryContentInfos)
                        {
                            if (directoryContentInfo.EntryIndex == directoryEntryIndex && directoryContentInfo.DirectoryCluster == directoryClusterIndex)
                            {
                                LocalDirectoryContentName = directoryContentInfo.ContentName;

                                if (directoryContentInfo.ShortFileName != fileName)
                                {
                                    if (directoryBuffer[directoryEntryIndex] == 0xe5) // Has the entry been deleted?
                                    {
                                        if (directoryBuffer[directoryEntryIndex + 11] == 0x10) // Is it a directory?
                                        {
                                            _logger.Log($"Deleting local directory \"{ directoryContentInfo.ContentName}\".", Constants.LoggingLevel.Info);

                                            Directory.Delete(directoryContentInfo.ContentName, true);
                                        }

                                        else // It's a file
                                        {
                                            _logger.Log($"Deleting local file \"{directoryContentInfo.ContentName}\".", Constants.LoggingLevel.Info);

                                            File.Delete(directoryContentInfo.ContentName);
                                        }

                                        _localDirectoryContentInfos.Remove(directoryContentInfo);

                                        _clusterInfos
                                            .Where(ci => ci?.ContentName == directoryContentInfo.ContentName)
                                            .ToList()
                                            .ForEach(ci => ci.ContentName = null);
                                    }
                                    else // Entry has been renamed.
                                    {
                                        if (directoryBuffer[directoryEntryIndex + 11] == 0x10) // Is it a directory?
                                        {
                                            _logger.Log($"Renaming local directory \"{directoryContentInfo.ContentName}\" to \"{Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName)}\".",
                                                Constants.LoggingLevel.Info);

                                            Directory.Move(directoryContentInfo.ContentName, Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName));
                                        }

                                        else // It's a file
                                        {
                                            _logger.Log($"Renaming local file \"{directoryContentInfo.ContentName}\" to \"{Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName)}\".",
                                                Constants.LoggingLevel.Info);

                                            File.Move(directoryContentInfo.ContentName, Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName));
                                        }

                                        _clusterInfos
                                            .Where(ci => ci?.ContentName == directoryContentInfo.ContentName)
                                            .ToList()
                                            .ForEach(ci => ci.ContentName = Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName));


                                        directoryContentInfo.ContentName = Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName);
                                        directoryContentInfo.ShortFileName = fileName;
                                    }
                                }

                                break;
                            }
                        }

                        if (String.IsNullOrEmpty(LocalDirectoryContentName) && directoryBuffer[directoryEntryIndex] != 0xe5
                            && startClusterIndex != 0) // Is the content new but not been deleted, and has start cluster set
                        {
                            string newContentPath = "";
                            if (directoryClusterIndex != _rootDirectoryClusterIndex) newContentPath = Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName); // Subdirectory
                            else newContentPath = Path.Combine(Parameters.LocalDirectoryPath, fileName); // Root dir

                            try
                            {
                                if (directoryBuffer[directoryEntryIndex + 11] == 0x10) // Is it a directory with a valid start cluster?
                                {
                                    _logger.Log("Creating local directory \"" + newContentPath + "\".", Constants.LoggingLevel.Info);

                                    var CreatedLocalDirectory = Directory.CreateDirectory(newContentPath);

                                    _clusterInfos[startClusterIndex].FileOffset = -1;
                                    _clusterInfos[startClusterIndex].ContentName = newContentPath;

                                    _localDirectoryContentInfos.Add(new LocalDirectoryContentInfo
                                    {
                                        ContentName = newContentPath,
                                        ShortFileName = fileName,
                                        EntryIndex = directoryEntryIndex,
                                        DirectoryCluster = directoryClusterIndex,
                                        StartCluster = startClusterIndex,

                                    });
                                }

                                else // it's a file
                                {
                                    int fileClusterIndex = startClusterIndex;

                                    // Check if the file has been completely written.
                                    while (!IsEndOfClusterChain(fileClusterIndex))
                                    {
                                        fileClusterIndex = FatGetClusterValue(fileClusterIndex);
                                    }

                                    if (IsEndOfFile(fileClusterIndex))
                                    {
                                        try
                                        {
                                            _logger.Log("Saving local file \"" + newContentPath + "\".", Constants.LoggingLevel.Info);

                                            int fileSize = directoryBuffer[directoryEntryIndex + 28] | (directoryBuffer[directoryEntryIndex + 29] << 8) | (directoryBuffer[directoryEntryIndex + 30] << 16) | (directoryBuffer[directoryEntryIndex + 31] << 24);

                                            using (BinaryWriter FileBinaryWriter = new BinaryWriter(File.OpenWrite(newContentPath)))
                                            {
                                                fileClusterIndex = startClusterIndex;

                                                while (!IsEndOfFile(fileClusterIndex))
                                                {
                                                    _clusterInfos[fileClusterIndex].ContentName = newContentPath;

                                                    FileBinaryWriter.Write(_clusterInfos[fileClusterIndex].DataBuffer, 0, Math.Min(_clusterInfos[fileClusterIndex].DataBuffer.Length, fileSize));

                                                    fileSize -= _clusterInfos[fileClusterIndex].DataBuffer.Length;

                                                    fileClusterIndex = FatGetClusterValue(fileClusterIndex);
                                                }
                                            }

                                            _localDirectoryContentInfos.Add(new LocalDirectoryContentInfo
                                            {
                                                ContentName = newContentPath,
                                                ShortFileName = fileName,
                                                EntryIndex = directoryEntryIndex,
                                                DirectoryCluster = directoryClusterIndex,
                                                StartCluster = startClusterIndex,

                                            });
                                        }

                                        catch (Exception ex)
                                        {
                                            _logger.LogException(ex);
                                        }
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                _logger.LogException(ex);
                            }

                        }

                        // Recurse non-deleted directories
                        if (syncSubDirectoryContents
                            && directoryBuffer[directoryEntryIndex + 11] == 0x10
                            && directoryBuffer[directoryEntryIndex] != 0xe5)
                        {
                            SyncLocalDisk(startClusterIndex);
                        }
                    }

                    directoryEntryIndex += 32;
                }

                if (directoryEntryIndex < directoryBuffer.Length)
                    break;

                directoryClusterIndex = FatGetClusterValue(directoryClusterIndex);
            }
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
        private bool IsEndOfClusterChain(int clusterValue)
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
        private bool IsEndOfFile(int clusterValue)
        {
            return clusterValue >= 0xfff8;
        }

        private int FatGetClusterValue(int clusterIndex, int directoryCluster = 0)
        {
            int cluster = clusterIndex * 2;
            if (directoryCluster != 0) cluster -= Parameters.RootDirectorySectors;
            return _fatBuffer[cluster + 1] << 8 | _fatBuffer[cluster];
        }

        private int FatGetFreeCluster(int currentCluster)
        {
            int newCluster;

            for (newCluster = 2; newCluster < _fatBuffer.Length / 2; newCluster++)
            {
                if (_fatBuffer[newCluster * 2] == 0 && _fatBuffer[newCluster * 2 + 1] == 0)
                {
                    if (currentCluster > 0)
                    {
                        _fatBuffer[currentCluster * 2] = (byte)(newCluster & 0xff);
                        _fatBuffer[currentCluster * 2 + 1] = (byte)((newCluster >> 8) & 0xff);
                    }

                    _fatBuffer[newCluster * 2] = 0xff;
                    _fatBuffer[newCluster * 2 + 1] = 0xff;

                    break;
                }
            }

            return newCluster;
        }

        public byte[] ReadSectors(int sector, int numberOfSectors)
        {
            byte[] dataBuffer = new byte[numberOfSectors * Parameters.BytesPerSector];
            int dataOffset = 0;

            while (numberOfSectors > 0)
            {
                if (sector < Parameters.SectorsPerFat * 2)
                {
                    int readSector = sector;

                    if (readSector >= Parameters.SectorsPerFat)
                        readSector -= Parameters.SectorsPerFat;

                    Array.Copy(_fatBuffer, readSector * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                }
                else if (sector < Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors)
                {
                    Array.Copy(_rootDirectoryBuffer, (sector - Parameters.SectorsPerFat * 2) * Parameters.BytesPerSector, dataBuffer, dataOffset, Parameters.BytesPerSector);
                }
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
                            if (_clusterInfos[clusterIndex].ContentName != null)
                            {
                                byte[] fileClusterDataBuffer = new byte[Parameters.BytesPerCluster];

                                try
                                {
                                    using (FileStream fileStream = File.OpenRead(_clusterInfos[clusterIndex].ContentName))
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

        public int WriteSectors(int receiveBufferLength, int startSector, byte[] dataBuffer)
        {
            int sector = startSector;
            int numberOfSectors = (int)Math.Ceiling((decimal)receiveBufferLength / Parameters.BytesPerSector);
            int dataOffset = 0;
            int clusterIndex = 0;

            while (numberOfSectors > 0)
            {
                if (sector < Parameters.SectorsPerFat * 2) // FAT area?
                {
                    int WriteSector = sector;

                    if (WriteSector >= Parameters.SectorsPerFat)
                        WriteSector -= Parameters.SectorsPerFat;

                    Array.Copy(dataBuffer, dataOffset, _fatBuffer, WriteSector * Parameters.BytesPerSector, Parameters.BytesPerSector);

                    SyncLocalDisk(_rootDirectoryClusterIndex);
                }

                else if (sector < Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) // Root directory area?
                {
                    Array.Copy(dataBuffer, dataOffset, _rootDirectoryBuffer, (sector - Parameters.SectorsPerFat * 2) * Parameters.BytesPerSector, Parameters.BytesPerSector);

                    SyncLocalDisk(_rootDirectoryClusterIndex, false); // Root directory must be synced independently
                }

                else // Data area.
                {
                    int WriteSector = sector - (Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) + 2 * Parameters.SectorsPerCluster;
                    clusterIndex = WriteSector / Parameters.SectorsPerCluster;

                    if (_clusterInfos[clusterIndex] == null) _clusterInfos[clusterIndex] = new ClusterInfo();

                    if (_clusterInfos[clusterIndex].DataBuffer == null) _clusterInfos[clusterIndex].DataBuffer = new byte[Parameters.BytesPerCluster];

                    Array.Copy(dataBuffer, dataOffset, _clusterInfos[clusterIndex].DataBuffer, (WriteSector - clusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, Parameters.BytesPerSector);
                }

                dataOffset += Parameters.BytesPerSector;
                numberOfSectors--;
                sector++;
            }

            return clusterIndex;
        }

        private bool FatAddDirectoryEntry(int directoryClusterIndex, string fullFileName, string shortFileName, byte attributeFlags, DateTime lastWriteDateTime, long fileSize, int startClusterIndex)
        {
            byte[] directoryBuffer;
            int entryIndex = 0;

            if (directoryClusterIndex == _rootDirectoryClusterIndex)
                directoryBuffer = _rootDirectoryBuffer;
            else
                directoryBuffer = _clusterInfos[directoryClusterIndex].DataBuffer;

            // Find a free entry.
            do
            {
                if (directoryClusterIndex == 0)
                {
                    if (entryIndex >= _rootDirectoryBuffer.Length)
                        return false;
                }
                else if (entryIndex >= Parameters.BytesPerCluster)
                {
                    int nextDirectoryClusterIndex = FatGetClusterValue(directoryClusterIndex);
                    directoryClusterIndex = FatGetClusterValue(directoryClusterIndex);

                    if (IsEndOfFile(directoryClusterIndex))
                    {
                        try
                        {
                            int newDirectoryCluster = FatGetFreeCluster(directoryClusterIndex);

                            _clusterInfos[newDirectoryCluster] = new ClusterInfo();

                            _clusterInfos[newDirectoryCluster].ContentName = _clusterInfos[directoryClusterIndex].ContentName;
                            _clusterInfos[newDirectoryCluster].FileOffset = -1;
                            _clusterInfos[newDirectoryCluster].DataBuffer = new byte[Parameters.BytesPerCluster];
                        }

                        catch (IndexOutOfRangeException outOfRangeEx)
                        {
                            int localDirectorySizeMiB = (int)Directory.GetFiles(Parameters.LocalDirectoryPath, "*", SearchOption.AllDirectories).Sum(file => (new FileInfo(file).Length)) / 1024 / 1024;
                            _logger.LogException(outOfRangeEx, $"Local directory size is {localDirectorySizeMiB} MiB, which is too large for the given virtual disk size ({Parameters.DiskTotalBytes / 1024 / 1024} MiB)");
                            throw outOfRangeEx;
                        }
                    }

                    else
                    {
                        directoryClusterIndex = nextDirectoryClusterIndex;
                    }

                    directoryBuffer = _clusterInfos[directoryClusterIndex].DataBuffer;
                    entryIndex = 0;
                }

                while (entryIndex < Parameters.BytesPerCluster && directoryBuffer[entryIndex] != 0)
                    entryIndex += 32;

            } while (entryIndex >= Parameters.BytesPerCluster);

            // Remember which local content matches this entry.

            if (shortFileName != "." && shortFileName != "..")
            {
                LocalDirectoryContentInfo newLocalDirectoryContentInfo = new LocalDirectoryContentInfo();

                newLocalDirectoryContentInfo.ContentName = fullFileName;
                newLocalDirectoryContentInfo.ShortFileName = shortFileName;
                newLocalDirectoryContentInfo.EntryIndex = entryIndex;
                newLocalDirectoryContentInfo.DirectoryCluster = directoryClusterIndex;
                newLocalDirectoryContentInfo.StartCluster = startClusterIndex;

                _localDirectoryContentInfos.Add(newLocalDirectoryContentInfo);
            }

            // File name.

            int fileNameIndex;

            for (fileNameIndex = 0; fileNameIndex < (8 + 3); fileNameIndex++)
                directoryBuffer[entryIndex + fileNameIndex] = 0x20;

            string[] nameAndExtender;
            byte[] asciiName;
            byte[] asciiExtender;

            if (shortFileName == "." || shortFileName == "..")
            {
                asciiName = ASCIIEncoding.ASCII.GetBytes(shortFileName);
                asciiExtender = null;
            }
            else
            {
                nameAndExtender = shortFileName.Split('.');
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

            directoryBuffer[entryIndex + 26] = (byte)(startClusterIndex & 0xff);
            directoryBuffer[entryIndex + 27] = (byte)((startClusterIndex >> 8) & 0xff);

            // File size (little endian).

            directoryBuffer[entryIndex + 28] = (byte)(fileSize & 0xff);
            directoryBuffer[entryIndex + 29] = (byte)((fileSize >> 8) & 0xff);
            directoryBuffer[entryIndex + 30] = (byte)((fileSize >> 16) & 0xff);
            directoryBuffer[entryIndex + 31] = (byte)((fileSize >> 24) & 0xff);

            return true;
        }

        private void FatAddDirectory(DirectoryInfo directoryInfo, int directoryCluster)
        {
            int newDirectoryClusterIndex = FatGetFreeCluster(0);

            _clusterInfos[newDirectoryClusterIndex] = new ClusterInfo();

            _clusterInfos[newDirectoryClusterIndex].ContentName = directoryInfo.FullName;
            _clusterInfos[newDirectoryClusterIndex].FileOffset = -1;
            _clusterInfos[newDirectoryClusterIndex].DataBuffer = new byte[Parameters.BytesPerCluster];

            FatAddDirectoryEntry(directoryCluster, directoryInfo.FullName, FAT16Helper.GetShortFileName(directoryInfo.Name), 0x10, directoryInfo.LastWriteTime, 0, newDirectoryClusterIndex);
            FatAddDirectoryEntry(newDirectoryClusterIndex, "", ".", 0x10, directoryInfo.LastWriteTime, 0, newDirectoryClusterIndex);
            FatAddDirectoryEntry(newDirectoryClusterIndex, "", "..", 0x10, directoryInfo.LastWriteTime, 0, directoryCluster);

            FatImportLocalDirectoryContents(directoryInfo.FullName, newDirectoryClusterIndex);
        }

        private void FatAddFile(FileInfo fileInfo, int directoryClusterIndex)
        {
            long fileOffset = 0;
            int fileStartClusterIndex = 0;
            int nextFileClusterIndex = 0;

            while (fileOffset < fileInfo.Length)
            {
                try
                {
                    nextFileClusterIndex = FatGetFreeCluster(nextFileClusterIndex);

                    if (fileStartClusterIndex == _rootDirectoryClusterIndex)
                        fileStartClusterIndex = nextFileClusterIndex;

                    _clusterInfos[nextFileClusterIndex] = new ClusterInfo();
                    _clusterInfos[nextFileClusterIndex].ContentName = fileInfo.FullName;
                    _clusterInfos[nextFileClusterIndex].FileOffset = fileOffset;

                    fileOffset += Parameters.BytesPerCluster;
                }

                catch (IndexOutOfRangeException outOfRangeEx)
                {
                    int localDirectorySizeMiB = (int)Directory.GetFiles(Parameters.LocalDirectoryPath, "*", SearchOption.AllDirectories).Sum(file => (new FileInfo(file).Length)) / 1024 / 1024;
                    _logger.LogException(outOfRangeEx, $"Local directory size is {localDirectorySizeMiB} MiB, which is too large for the given virtual disk size ({Parameters.DiskTotalBytes / 1024 / 1024} MiB)");
                    throw outOfRangeEx;
                }
            }

            FatAddDirectoryEntry(directoryClusterIndex, fileInfo.FullName, FAT16Helper.GetShortFileName(fileInfo.Name), 0x00, fileInfo.LastWriteTime, fileInfo.Length, fileStartClusterIndex);
        }

        private void FileChangedHandler(object source, FileSystemEventArgs args)
        {
            MediaChanged = true;
        }

        public void FatImportLocalDirectoryContents(string directoryName, int directoryClusterIndex)
        {
            if (directoryClusterIndex == _rootDirectoryClusterIndex)
            {
                _rootDirectoryBuffer = new byte[Parameters.RootDirectorySectors * Parameters.BytesPerSector];
                _fatBuffer = new byte[Parameters.SectorsPerFat * Parameters.BytesPerSector];
                _clusterInfos = new ClusterInfo[Parameters.DiskClusters];
                _localDirectoryContentInfos = new List<LocalDirectoryContentInfo>();
            }

            DirectoryInfo DirectoryInfo = new DirectoryInfo(directoryName);

            foreach (DirectoryInfo SubDirectoryInfo in DirectoryInfo.GetDirectories())
                FatAddDirectory(SubDirectoryInfo, directoryClusterIndex);

            foreach (FileInfo FileInfo in DirectoryInfo.GetFiles())
                FatAddFile(FileInfo, directoryClusterIndex);
        }
    }
}
