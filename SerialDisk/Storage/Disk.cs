using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AtariST.SerialDisk.Storage
{
    public class Disk
    {
        public bool MediaChanged = true;

        private int _rootDirectoryClusterIndex = 0;

        private byte[] _rootDirectoryBuffer;
        private byte[] _fatBuffer;
        private ClusterInfo[] _clusterInfos;
        public DiskParameters Parameters { get; set; }

        private List<LocalDirectoryContentInfo> _localDirectoryContentInfos;
        private FileSystemWatcher _fileSystemWatcher;

        private Logger _logger;

        public Disk(DiskParameters diskParams, Logger log)
        {
            _logger = log;
            Parameters = diskParams;

            FatImportLocalDirectoryContents(diskParams.LocalDirectoryPath, 0);
            WatchLocalDirectory(diskParams.LocalDirectoryPath);
        }

        public void WatchLocalDirectory(string localDirectoryName)
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
            _fileSystemWatcher.EnableRaisingEvents = true;
        }



        public void SyncLocalDisk(int directoryClusterIndex = 0)
        {
            int verbosity = 3;

            while (directoryClusterIndex != 0xffff)
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
                                        _fileSystemWatcher.EnableRaisingEvents = false;

                                        if (directoryBuffer[directoryEntryIndex + 11] == 0x10) // Is it a directory?
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine($"Deleting local directory \"{ directoryContentInfo.ContentName}\".");

                                            Directory.Delete(directoryContentInfo.ContentName, true);

                                            _localDirectoryContentInfos.Remove(directoryContentInfo);
                                            _clusterInfos[startClusterIndex] = null;
                                        }

                                        else // It's a file
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine($"Deleting local file \"{directoryContentInfo.ContentName}\".");

                                            File.Delete(directoryContentInfo.ContentName);

                                            _localDirectoryContentInfos.Remove(directoryContentInfo);

                                            _clusterInfos
                                                .Where(ci => ci?.ContentName == directoryContentInfo.ContentName)
                                                .ToList()
                                                .ForEach(ci => ci.ContentName = null);
                                        }

                                        _localDirectoryContentInfos.Remove(directoryContentInfo);

                                        _fileSystemWatcher.EnableRaisingEvents = true;
                                    }
                                    else // Entry has been renamed.
                                    {
                                        _fileSystemWatcher.EnableRaisingEvents = false;

                                        if (directoryBuffer[directoryEntryIndex + 11] == 0x10) // Is it a directory?
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine($"Renaming local directory \"{directoryContentInfo.ContentName}\" to \"{Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName)}\".");

                                            Directory.Move(directoryContentInfo.ContentName, Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName));
                                        }

                                        else // It's a file
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine($"Renaming local file \"{directoryContentInfo.ContentName}\" to \"{Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName)}\".");

                                            File.Move(directoryContentInfo.ContentName, Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName));
                                        }

                                        directoryContentInfo.ContentName = Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName);
                                        directoryContentInfo.ShortFileName = fileName;

                                        _fileSystemWatcher.EnableRaisingEvents = true;
                                    }
                                }

                                break;
                            }
                        }

                        if (String.IsNullOrEmpty(LocalDirectoryContentName) && directoryBuffer[directoryEntryIndex] != 0xe5) // Is the content new but not been deleted?
                        {
                            string newContentPath = "";
                            if (directoryClusterIndex != _rootDirectoryClusterIndex) newContentPath = Path.Combine(_clusterInfos[directoryClusterIndex].ContentName, fileName); // Subdirectory
                            else newContentPath = Path.Combine(Parameters.LocalDirectoryPath, fileName); // Root dir

                            _clusterInfos[startClusterIndex].ContentName = newContentPath;

                            try
                            {
                                if (directoryBuffer[directoryEntryIndex + 11] == 0x10 && startClusterIndex != 0) // Is it a directory with a valid start cluster?
                                {
                                    _fileSystemWatcher.EnableRaisingEvents = false;

                                    if (verbosity > 0)
                                        Console.WriteLine("Creating local directory \"" + newContentPath + "\".");

                                    var CreatedLocalDirectory = Directory.CreateDirectory(newContentPath);

                                    _fileSystemWatcher.EnableRaisingEvents = true;
                                }

                                else // it's a file
                                {
                                    int fileClusterIndex = startClusterIndex;

                                    // Check if the file has been completely written.
                                    while (!IsEndOfClusterChain(fileClusterIndex))
                                    {
                                        fileClusterIndex = FatGetClusterValue(fileClusterIndex, directoryClusterIndex);
                                    }

                                    if (IsEndOfFileMarker(fileClusterIndex))
                                    {
                                        _fileSystemWatcher.EnableRaisingEvents = false;

                                        try
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine("Saving local file \"" + newContentPath + "\".");

                                            int fileSize = directoryBuffer[directoryEntryIndex + 28] | (directoryBuffer[directoryEntryIndex + 29] << 8) | (directoryBuffer[directoryEntryIndex + 30] << 16) | (directoryBuffer[directoryEntryIndex + 31] << 24);

                                            using (BinaryWriter FileBinaryWriter = new BinaryWriter(File.OpenWrite(newContentPath)))
                                            {
                                                fileClusterIndex = startClusterIndex;

                                                while (!IsEndOfClusterChain(fileClusterIndex))
                                                {
                                                    _clusterInfos[fileClusterIndex].ContentName = newContentPath;

                                                    FileBinaryWriter.Write(_clusterInfos[fileClusterIndex].DataBuffer, 0, Math.Min(_clusterInfos[fileClusterIndex].DataBuffer.Length, fileSize));

                                                    fileSize -= _clusterInfos[fileClusterIndex].DataBuffer.Length;

                                                    fileClusterIndex = FatGetClusterValue(fileClusterIndex);
                                                }
                                            }
                                        }

                                        catch (Exception ex)
                                        {
                                            _logger.LogException(ex);
                                        }

                                        _fileSystemWatcher.EnableRaisingEvents = true;
                                    }
                                }

                                LocalDirectoryContentInfo newlocalDirectoryContentInfo = new LocalDirectoryContentInfo();

                                newlocalDirectoryContentInfo.ContentName = newContentPath;
                                newlocalDirectoryContentInfo.ShortFileName = fileName;
                                newlocalDirectoryContentInfo.EntryIndex = directoryEntryIndex;
                                newlocalDirectoryContentInfo.DirectoryCluster = directoryClusterIndex;
                                newlocalDirectoryContentInfo.StartCluster = startClusterIndex;

                                _localDirectoryContentInfos.Add(newlocalDirectoryContentInfo);
                            }

                            catch (Exception ex)
                            {
                                _logger.LogException(ex);
                            }

                        }

                        if (directoryBuffer[directoryEntryIndex + 11] == 0x10 && directoryBuffer[directoryEntryIndex] != 0xe5) // Is the content a non deleted directory?
                            SyncLocalDisk(startClusterIndex);
                    }

                    directoryEntryIndex += 32;
                }

                if (directoryEntryIndex < directoryBuffer.Length)
                    break;

                directoryClusterIndex = FatGetClusterValue(directoryClusterIndex);
            }
        }

        private bool IsEndOfClusterChain(int clusterValue)
        {
            // 0x00 free cluster
            // 0xFFF8–0xFFFF indicates last FAT entry for a file
            // 0xFFF0-0xFFF7 marks a bad sector in GEMDOS
            return clusterValue == 0 || clusterValue >= 0xfff8 || (clusterValue >= 0xfff0 && clusterValue <= 0xfff7);
        }

        private bool IsEndOfFileMarker(int clusterValue)
        {
            // 0xFFF8–0xFFFF indicates last FAT entry for a file
            return clusterValue >= 0xfff8;
        }

        public string FatCreateShortFileName(string fileName)
        {
            Regex invalidCharactersRegex = new Regex("[^\\-A-Z0-9_\\.]");

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

        public int FatGetClusterValue(int clusterIndex, int directoryCluster = 0)
        {
            int cluster = clusterIndex * 2;
            if (directoryCluster != 0) cluster -= Parameters.RootDirectorySectors;
            return _fatBuffer[cluster + 1] << 8 | _fatBuffer[cluster];
        }

        public int FatGetFreeCluster(int CurrentCluster)
        {
            int NewCluster;

            for (NewCluster = 2; NewCluster < _fatBuffer.Length / 2; NewCluster++)
            {
                if (_fatBuffer[NewCluster * 2] == 0 && _fatBuffer[NewCluster * 2 + 1] == 0)
                {
                    if (CurrentCluster > 0)
                    {
                        _fatBuffer[CurrentCluster * 2] = (byte)(NewCluster & 0xff);
                        _fatBuffer[CurrentCluster * 2 + 1] = (byte)((NewCluster >> 8) & 0xff);
                    }

                    _fatBuffer[NewCluster * 2] = 0xff;
                    _fatBuffer[NewCluster * 2 + 1] = 0xff;

                    break;
                }
            }

            return NewCluster;
        }

        public byte[] ReadSectors(int Sector, int NumberOfSectors)
        {
            byte[] DataBuffer = new byte[NumberOfSectors * Parameters.BytesPerSector];
            int DataOffset = 0;

            while (NumberOfSectors > 0)
            {
                if (Sector < Parameters.SectorsPerFat * 2)
                {
                    int ReadSector = Sector;

                    if (ReadSector >= Parameters.SectorsPerFat)
                        ReadSector -= Parameters.SectorsPerFat;

                    Array.Copy(_fatBuffer, ReadSector * Parameters.BytesPerSector, DataBuffer, DataOffset, Parameters.BytesPerSector);
                }
                else if (Sector < Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors)
                {
                    Array.Copy(_rootDirectoryBuffer, (Sector - Parameters.SectorsPerFat * 2) * Parameters.BytesPerSector, DataBuffer, DataOffset, Parameters.BytesPerSector);
                }
                else
                {
                    int ReadSector = Sector - (Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) + 2 * Parameters.SectorsPerCluster;
                    int ClusterIndex = ReadSector / Parameters.SectorsPerCluster;

                    if (_clusterInfos[ClusterIndex] != null)
                    {
                        if (_clusterInfos[ClusterIndex].DataBuffer != null)
                        {
                            Array.Copy(_clusterInfos[ClusterIndex].DataBuffer, (ReadSector - ClusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, DataBuffer, DataOffset, Parameters.BytesPerSector);
                        }
                        else
                        {
                            if (_clusterInfos[ClusterIndex].ContentName != null)
                            {
                                byte[] FileClusterDataBuffer = new byte[Parameters.BytesPerCluster];
                                try
                                {
                                    using (FileStream FileStream = File.OpenRead(_clusterInfos[ClusterIndex].ContentName))
                                    {
                                        int BytesToRead = Math.Min(Parameters.BytesPerCluster, (int)(FileStream.Length - _clusterInfos[ClusterIndex].FileOffset));

                                        FileStream.Seek(_clusterInfos[ClusterIndex].FileOffset, SeekOrigin.Begin);

                                        for (int Index = 0; Index < BytesToRead; Index++)
                                            FileClusterDataBuffer[Index] = (byte)FileStream.ReadByte();

                                        Array.Copy(FileClusterDataBuffer, (ReadSector - ClusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, DataBuffer, DataOffset, Parameters.BytesPerSector);
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

                DataOffset += Parameters.BytesPerSector;
                NumberOfSectors--;
                Sector++;
            }

            return DataBuffer;
        }

        public int WriteSectors(int receiveBufferLength, int StartSector, byte[] DataBuffer)
        {
            int Sector = StartSector;
            int NumberOfSectors = (int)Math.Ceiling((decimal)receiveBufferLength / Parameters.BytesPerSector);
            int DataOffset = 0;
            int ClusterIndex = 0;

            while (NumberOfSectors > 0)
            {
                if (Sector < Parameters.SectorsPerFat * 2) // FAT area?
                {
                    int WriteSector = Sector;

                    if (WriteSector >= Parameters.SectorsPerFat)
                        WriteSector -= Parameters.SectorsPerFat;

                    Array.Copy(DataBuffer, DataOffset, _fatBuffer, WriteSector * Parameters.BytesPerSector, Parameters.BytesPerSector);

                    SyncLocalDisk();
                }

                else if (Sector < Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) // Root directory area?
                {
                    Array.Copy(DataBuffer, DataOffset, _rootDirectoryBuffer, (Sector - Parameters.SectorsPerFat * 2) * Parameters.BytesPerSector, Parameters.BytesPerSector);
                }

                else // Data area.
                {
                    int WriteSector = Sector - (Parameters.SectorsPerFat * 2 + Parameters.RootDirectorySectors) + 2 * Parameters.SectorsPerCluster;
                    ClusterIndex = WriteSector / Parameters.SectorsPerCluster;

                    if (_clusterInfos[ClusterIndex] == null) _clusterInfos[ClusterIndex] = new ClusterInfo();

                    if (_clusterInfos[ClusterIndex].DataBuffer == null) _clusterInfos[ClusterIndex].DataBuffer = new byte[Parameters.BytesPerCluster];

                    Array.Copy(DataBuffer, DataOffset, _clusterInfos[ClusterIndex].DataBuffer, (WriteSector - ClusterIndex * Parameters.SectorsPerCluster) * Parameters.BytesPerSector, Parameters.BytesPerSector);
                }

                DataOffset += Parameters.BytesPerSector;
                NumberOfSectors--;
                Sector++;
            }

            return ClusterIndex;
        }

        public bool FatAddDirectoryEntry(int directoryClusterIndex, string fullFileName, string shortFileName, byte attributeFlags, DateTime lastWriteDateTime, long fileSize, int startClusterIndex)
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
                    int NextDirectoryCluster = FatGetClusterValue(directoryClusterIndex);

                    if (NextDirectoryCluster == 0xffff)
                    {
                        try
                        {
                            int NewDirectoryCluster = FatGetFreeCluster(directoryClusterIndex);

                            _clusterInfos[NewDirectoryCluster] = new ClusterInfo();

                            _clusterInfos[NewDirectoryCluster].ContentName = _clusterInfos[directoryClusterIndex].ContentName;
                            _clusterInfos[NewDirectoryCluster].FileOffset = -1;
                            _clusterInfos[NewDirectoryCluster].DataBuffer = new byte[Parameters.BytesPerCluster];

                            directoryClusterIndex = NewDirectoryCluster;
                        }

                        catch (IndexOutOfRangeException outOfRangeEx)
                        {
                            _logger.LogException(outOfRangeEx, $"Local directory is too large for the given virtual disk size ({Parameters.DiskTotalBytes / 1024 / 1024} MB).");
                            throw outOfRangeEx;
                        }
                    }
                    else
                    {
                        directoryClusterIndex = NextDirectoryCluster;
                    }

                    directoryBuffer = _clusterInfos[directoryClusterIndex].DataBuffer;
                    entryIndex = 0;
                }

                while (entryIndex < Parameters.BytesPerCluster && directoryBuffer[entryIndex] != 0)
                    entryIndex += 32;
            }
            while (entryIndex >= Parameters.BytesPerCluster);

            // Remember which local content matches this entry.

            if (shortFileName != "." && shortFileName != "..")
            {
                LocalDirectoryContentInfo localDirectoryContentInfo = new LocalDirectoryContentInfo();

                localDirectoryContentInfo.ContentName = fullFileName;
                localDirectoryContentInfo.ShortFileName = shortFileName;
                localDirectoryContentInfo.EntryIndex = entryIndex;
                localDirectoryContentInfo.DirectoryCluster = directoryClusterIndex;
                localDirectoryContentInfo.StartCluster = startClusterIndex;

                _localDirectoryContentInfos.Add(localDirectoryContentInfo);
            }

            // File name.

            int Index;

            for (Index = 0; Index < (8 + 3); Index++)
                directoryBuffer[entryIndex + Index] = 0x20;

            string[] NameAndExtender;
            byte[] AsciiName;
            byte[] AsciiExtender;

            if (shortFileName == "." || shortFileName == "..")
            {
                AsciiName = ASCIIEncoding.ASCII.GetBytes(shortFileName);
                AsciiExtender = null;
            }
            else
            {
                NameAndExtender = shortFileName.Split('.');
                AsciiName = ASCIIEncoding.ASCII.GetBytes(NameAndExtender[0]);
                AsciiExtender = NameAndExtender.Length == 2 ? ASCIIEncoding.ASCII.GetBytes(NameAndExtender[1]) : null;
            }

            for (Index = 0; Index < AsciiName.Length; Index++)
                directoryBuffer[entryIndex + Index] = AsciiName[Index];

            if (AsciiExtender != null)
                for (Index = 0; Index < AsciiExtender.Length; Index++)
                    directoryBuffer[entryIndex + 8 + Index] = AsciiExtender[Index];

            // File attribute flags.

            directoryBuffer[entryIndex + 11] = attributeFlags;

            // File write time and date (little endian).

            UInt16 FatFileWriteTime = 0;
            UInt16 FatFileWriteDate = 0;

            int TwoSeconds = lastWriteDateTime.Second / 2;
            int Minutes = lastWriteDateTime.Minute;
            int Hours = lastWriteDateTime.Hour;
            int DayOfMonth = lastWriteDateTime.Day;
            int Month = lastWriteDateTime.Month;
            int YearsSince1980 = lastWriteDateTime.Year - 1980;

            FatFileWriteTime |= (UInt16)TwoSeconds;
            FatFileWriteTime |= (UInt16)(Minutes << 5);
            FatFileWriteTime |= (UInt16)(Hours << 11);

            FatFileWriteDate |= (UInt16)DayOfMonth;
            FatFileWriteDate |= (UInt16)(Month << 5);
            FatFileWriteDate |= (UInt16)(YearsSince1980 << 9);

            directoryBuffer[entryIndex + 22] = (byte)(FatFileWriteTime & 0xff);
            directoryBuffer[entryIndex + 23] = (byte)((FatFileWriteTime >> 8) & 0xff);
            directoryBuffer[entryIndex + 24] = (byte)(FatFileWriteDate & 0xff);
            directoryBuffer[entryIndex + 25] = (byte)((FatFileWriteDate >> 8) & 0xff);

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

        public void FatAddDirectory(DirectoryInfo directoryInfo, int directoryCluster)
        {
            int newDirectoryClusterIndex = FatGetFreeCluster(0);

            _clusterInfos[newDirectoryClusterIndex] = new ClusterInfo();

            _clusterInfos[newDirectoryClusterIndex].ContentName = directoryInfo.FullName;
            _clusterInfos[newDirectoryClusterIndex].FileOffset = -1;
            _clusterInfos[newDirectoryClusterIndex].DataBuffer = new byte[Parameters.BytesPerCluster];

            FatAddDirectoryEntry(directoryCluster, directoryInfo.FullName, FatCreateShortFileName(directoryInfo.Name), 0x10, directoryInfo.LastWriteTime, 0, newDirectoryClusterIndex);
            FatAddDirectoryEntry(newDirectoryClusterIndex, "", ".", 0x10, directoryInfo.LastWriteTime, 0, newDirectoryClusterIndex);
            FatAddDirectoryEntry(newDirectoryClusterIndex, "", "..", 0x10, directoryInfo.LastWriteTime, 0, directoryCluster);

            FatImportLocalDirectoryContents(directoryInfo.FullName, newDirectoryClusterIndex);
        }

        public void FatAddFile(FileInfo FileInfo, int directoryClusterIndex)
        {
            long fileOffset = 0;
            int fileStartClusterIndex = 0;
            int NextFileClusterIndex = 0;

            while (fileOffset < FileInfo.Length)
            {
                try
                {
                    NextFileClusterIndex = FatGetFreeCluster(NextFileClusterIndex);

                    if (fileStartClusterIndex == _rootDirectoryClusterIndex)
                        fileStartClusterIndex = NextFileClusterIndex;

                    _clusterInfos[NextFileClusterIndex] = new ClusterInfo();
                    _clusterInfos[NextFileClusterIndex].ContentName = FileInfo.FullName;
                    _clusterInfos[NextFileClusterIndex].FileOffset = fileOffset;

                    fileOffset += Parameters.BytesPerCluster;
                }

                catch (IndexOutOfRangeException boundsEx)
                {
                    _logger.LogException(boundsEx, $"Local directory is too large for the given virtual disk size ({Parameters.DiskTotalBytes / 1024 / 1024} MiB)");
                    throw boundsEx;
                }
            }

            FatAddDirectoryEntry(directoryClusterIndex, FileInfo.FullName, FatCreateShortFileName(FileInfo.Name), 0x00, FileInfo.LastWriteTime, FileInfo.Length, fileStartClusterIndex);
        }

        public void FileChangedHandler(object source, FileSystemEventArgs args)
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
