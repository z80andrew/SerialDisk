using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AtariST.SerialDisk.Storage
{
    public class Disk
    {
        public bool MediaChanged = true;

        public int DiskSize;
        public int BytesPerSector;
        public int SectorsPerCluster;
        public int BytesPerCluster;
        public int DiskClusters;
        public int FatEntriesPerSector;
        public int SectorsPerFat;
        public int RootDirectorySectors;

        public byte[] RootDirectoryBuffer;
        public byte[] FatBuffer;
        public ClusterInfo[] ClusterInfo;

        public List<LocalDirectoryContentInfo> LocalDirectoryContentInfo;
        public FileSystemWatcher FileSystemWatcher;

        public Disk(Settings applicationSettings)
        {
            CreateBiosParameterBlock(applicationSettings.DiskSizeMB);
            FatImportDirectoryContents(applicationSettings.LocalDirectoryName, 0);
            WatchLocalDirectory(applicationSettings.LocalDirectoryName);
        }

        public void WatchLocalDirectory(string localDirectoryName)
        {
            FileSystemWatcher = new FileSystemWatcher();

            FileSystemWatcher.Path = localDirectoryName;
            FileSystemWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            FileSystemWatcher.Filter = "";
            FileSystemWatcher.IncludeSubdirectories = true;
            FileSystemWatcher.Changed += new FileSystemEventHandler(FileChangedHandler);
            FileSystemWatcher.Created += new FileSystemEventHandler(FileChangedHandler);
            FileSystemWatcher.Deleted += new FileSystemEventHandler(FileChangedHandler);
            FileSystemWatcher.Renamed += new RenamedEventHandler(FileRenamedHandler);
            FileSystemWatcher.EnableRaisingEvents = true;
        }

        public void CreateBiosParameterBlock(int diskSizeMB)
        {
            DiskSize = diskSizeMB * 1024 * 1024;

            BytesPerSector = 512;

            while (BytesPerSector * 64 * 1024 < DiskSize)
                BytesPerSector *= 2;

            SectorsPerCluster = 2;
            BytesPerCluster = SectorsPerCluster * BytesPerSector;
            DiskClusters = DiskSize / BytesPerCluster;
            FatEntriesPerSector = BytesPerSector / 2;
            SectorsPerFat = (DiskClusters + FatEntriesPerSector - 1) / FatEntriesPerSector;
            RootDirectorySectors = 4;
        }

        public void SyncDirectoryContents(string localDirectoryName, int verbosity = 0, int DirectoryCluster = 0)
        {
            while (DirectoryCluster != 0xffff)
            {
                int EntryIndex = 0;
                byte[] DirectoryBuffer;

                if (DirectoryCluster == 0)
                    DirectoryBuffer = RootDirectoryBuffer;
                else
                    DirectoryBuffer = ClusterInfo[DirectoryCluster].DataBuffer;

                while (EntryIndex < DirectoryBuffer.Length && DirectoryBuffer[EntryIndex] != 0)
                {
                    if (DirectoryBuffer[EntryIndex] != 0x2e) // The entry is not "." or "..".
                    {
                        string ShortFileName = ASCIIEncoding.ASCII.GetString(DirectoryBuffer, EntryIndex, 8).Trim();
                        string ShortFileExtension = ASCIIEncoding.ASCII.GetString(DirectoryBuffer, EntryIndex + 8, 3).Trim();

                        if (ShortFileExtension != "")
                            ShortFileName += "." + ShortFileExtension;

                        int StartCluster = DirectoryBuffer[EntryIndex + 26] | (DirectoryBuffer[EntryIndex + 27] << 8);

                        // Find the matching local content and check what happened to it.

                        string LocalDirectoryContentName = "";

                        foreach (LocalDirectoryContentInfo directoryContentInfo in LocalDirectoryContentInfo)
                        {
                            if (directoryContentInfo.EntryIndex == EntryIndex && directoryContentInfo.Cluster == DirectoryCluster)
                            {
                                LocalDirectoryContentName = directoryContentInfo.ContentName;

                                if (directoryContentInfo.ShortFileName != ShortFileName)
                                {
                                    if (DirectoryBuffer[EntryIndex] == 0xe5) // Has the entry been deleted?
                                    {
                                        FileSystemWatcher.EnableRaisingEvents = false;

                                        if ((DirectoryBuffer[EntryIndex + 11] & 0x10) != 0) // Is it a directory?
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine("Deleting local directory \"" + directoryContentInfo.ContentName + "\".");

                                            Directory.Delete(directoryContentInfo.ContentName, true);
                                        }
                                        else // It's a file.
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine("Deleting local file \"" + directoryContentInfo.ContentName + "\".");

                                            File.Delete(directoryContentInfo.ContentName);
                                        }

                                        LocalDirectoryContentInfo.Remove(directoryContentInfo);

                                        FileSystemWatcher.EnableRaisingEvents = true;
                                    }
                                    else // Entry has been renamed.
                                    {
                                        FileSystemWatcher.EnableRaisingEvents = false;

                                        if ((DirectoryBuffer[EntryIndex + 11] & 0x10) != 0) // Is it a directory?
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine("Renaming local directory \"" + directoryContentInfo.ContentName + "\" to \"" + localDirectoryName + Path.DirectorySeparatorChar + ShortFileName + "\".");

                                            Directory.Move(directoryContentInfo.ContentName, localDirectoryName + Path.DirectorySeparatorChar + ShortFileName);
                                        }
                                        else // It's a file.
                                        {
                                            if (verbosity > 0)
                                                Console.WriteLine("Renaming local file \"" + directoryContentInfo.ContentName + "\" to \"" + localDirectoryName + Path.DirectorySeparatorChar + ShortFileName + "\".");

                                            File.Move(directoryContentInfo.ContentName, localDirectoryName + Path.DirectorySeparatorChar + ShortFileName);
                                        }

                                        directoryContentInfo.ContentName = localDirectoryName + Path.DirectorySeparatorChar + ShortFileName;
                                        directoryContentInfo.ShortFileName = ShortFileName;

                                        FileSystemWatcher.EnableRaisingEvents = true;
                                    }
                                }

                                break;
                            }
                        }

                        if (LocalDirectoryContentName == "" && DirectoryBuffer[EntryIndex] != 0xe5) // Is the content new but not been deleted?
                        {
                            LocalDirectoryContentName = localDirectoryName + Path.DirectorySeparatorChar + ShortFileName;

                            if ((DirectoryBuffer[EntryIndex + 11] & 0x10) != 0) // Is it a directory?
                            {
                                FileSystemWatcher.EnableRaisingEvents = false;

                                if (verbosity > 0)
                                    Console.WriteLine("Creating local directory \"" + LocalDirectoryContentName + "\".");

                                Directory.CreateDirectory(LocalDirectoryContentName);

                                FileSystemWatcher.EnableRaisingEvents = true;

                                LocalDirectoryContentInfo NewLocalDirectoryContentInfo = new LocalDirectoryContentInfo();

                                NewLocalDirectoryContentInfo.ContentName = LocalDirectoryContentName;
                                NewLocalDirectoryContentInfo.ShortFileName = ShortFileName;
                                NewLocalDirectoryContentInfo.EntryIndex = EntryIndex;
                                NewLocalDirectoryContentInfo.Cluster = DirectoryCluster;
                                NewLocalDirectoryContentInfo.StartCluster = StartCluster;

                                LocalDirectoryContentInfo.Add(NewLocalDirectoryContentInfo);
                            }
                            else // It's a file.
                            {
                                int Cluster = StartCluster;

                                // Check if the file has been completely written.

                                while (Cluster != 0 && Cluster != 0xffff)
                                    Cluster = FatGetNextCluster(Cluster);

                                if (Cluster != 0)
                                {
                                    FileSystemWatcher.EnableRaisingEvents = false;

                                    if (verbosity > 0)
                                        Console.WriteLine("Saving local file \"" + LocalDirectoryContentName + "\".");

                                    BinaryWriter FileBinaryWriter = new BinaryWriter(File.OpenWrite(LocalDirectoryContentName));

                                    int FileSize = DirectoryBuffer[EntryIndex + 28] | (DirectoryBuffer[EntryIndex + 29] << 8) | (DirectoryBuffer[EntryIndex + 30] << 16) | (DirectoryBuffer[EntryIndex + 31] << 24);

                                    Cluster = StartCluster;

                                    while (Cluster != 0xffff)
                                    {
                                        FileBinaryWriter.Write(ClusterInfo[Cluster].DataBuffer, 0, Math.Min(ClusterInfo[Cluster].DataBuffer.Length, FileSize));

                                        FileSize -= ClusterInfo[Cluster].DataBuffer.Length;

                                        Cluster = FatGetNextCluster(Cluster);
                                    }

                                    FileBinaryWriter.Close();

                                    FileSystemWatcher.EnableRaisingEvents = true;

                                    LocalDirectoryContentInfo NewLocalDirectoryContentInfo = new LocalDirectoryContentInfo();

                                    NewLocalDirectoryContentInfo.ContentName = LocalDirectoryContentName;
                                    NewLocalDirectoryContentInfo.ShortFileName = ShortFileName;
                                    NewLocalDirectoryContentInfo.EntryIndex = EntryIndex;
                                    NewLocalDirectoryContentInfo.Cluster = DirectoryCluster;
                                    NewLocalDirectoryContentInfo.StartCluster = StartCluster;

                                    LocalDirectoryContentInfo.Add(NewLocalDirectoryContentInfo);
                                }
                            }
                        }

                        if ((DirectoryBuffer[EntryIndex + 11] & 0x10) != 0 && DirectoryBuffer[EntryIndex] != 0xe5) // Is the content a non deleted directory?
                            SyncDirectoryContents(localDirectoryName, 0, StartCluster);
                    }

                    EntryIndex += 32;
                }

                if (EntryIndex < DirectoryBuffer.Length) // Have we found the last entry in this directory?
                    break;

                DirectoryCluster = FatGetNextCluster(DirectoryCluster);
            }
        }

        public string FatCreateShortFileName(string FileName)
        {
            Regex invalidCharactersRegex = new Regex("[^\\-A-Z0-9_\\.]");

            FileName = invalidCharactersRegex.Replace(FileName.ToUpper(), "_");

            string ShortFileName;

            int DotIndex = FileName.IndexOf(".");

            if (DotIndex == -1)
                ShortFileName = FileName;
            else
                ShortFileName = FileName.Substring(0, DotIndex);

            if (ShortFileName.Length > 8)
                ShortFileName = FileName.Substring(0, 8);

            DotIndex = FileName.LastIndexOf(".");

            if (DotIndex != -1)
            {
                string Extender = FileName.Substring(DotIndex + 1);

                if (Extender.Length > 3)
                    Extender = Extender.Substring(0, 3);

                ShortFileName += "." + Extender;
            }

            return ShortFileName;
        }

        public int FatGetNextCluster(int CurrentCluster)
        {
            return (FatBuffer[CurrentCluster * 2 + 1] << 8) | (FatBuffer[CurrentCluster * 2]);
        }

        public int FatGetFreeCluster(int CurrentCluster)
        {
            int NewCluster;

            for (NewCluster = 2; NewCluster < FatBuffer.Length / 2; NewCluster++)
            {
                if (FatBuffer[NewCluster * 2] == 0 && FatBuffer[NewCluster * 2 + 1] == 0)
                {
                    if (CurrentCluster > 0)
                    {
                        FatBuffer[CurrentCluster * 2] = (byte)(NewCluster & 0xff);
                        FatBuffer[CurrentCluster * 2 + 1] = (byte)((NewCluster >> 8) & 0xff);
                    }

                    FatBuffer[NewCluster * 2] = 0xff;
                    FatBuffer[NewCluster * 2 + 1] = 0xff;

                    break;
                }
            }

            return NewCluster;
        }

        public byte[] ReadSectors(int Sector, int NumberOfSectors)
        {
            byte[] DataBuffer = new byte[NumberOfSectors * BytesPerSector];
            int DataOffset = 0;

            while (NumberOfSectors > 0)
            {
                if (Sector < SectorsPerFat * 2)
                {
                    int ReadSector = Sector;

                    if (ReadSector >= SectorsPerFat)
                        ReadSector -= SectorsPerFat;

                    Array.Copy(FatBuffer, ReadSector * BytesPerSector, DataBuffer, DataOffset, BytesPerSector);
                }
                else if (Sector < SectorsPerFat * 2 + RootDirectorySectors)
                {
                    Array.Copy(RootDirectoryBuffer, (Sector - SectorsPerFat * 2) * BytesPerSector, DataBuffer, DataOffset, BytesPerSector);
                }
                else
                {
                    int ReadSector = Sector - (SectorsPerFat * 2 + RootDirectorySectors) + 2 * SectorsPerCluster;
                    int ClusterIndex = ReadSector / SectorsPerCluster;

                    if (ClusterInfo[ClusterIndex].DataBuffer != null)
                    {
                        Array.Copy(ClusterInfo[ClusterIndex].DataBuffer, (ReadSector - ClusterIndex * SectorsPerCluster) * BytesPerSector, DataBuffer, DataOffset, BytesPerSector);
                    }
                    else
                    {
                        byte[] FileClusterDataBuffer = new byte[BytesPerCluster];
                        FileStream FileStream = File.OpenRead(ClusterInfo[ClusterIndex].ContentName);
                        int BytesToRead = Math.Min(BytesPerCluster, (int)(FileStream.Length - ClusterInfo[ClusterIndex].FileOffset));

                        FileStream.Seek(ClusterInfo[ClusterIndex].FileOffset, SeekOrigin.Begin);

                        for (int Index = 0; Index < BytesToRead; Index++)
                            FileClusterDataBuffer[Index] = (byte)FileStream.ReadByte();

                        FileStream.Close();

                        Array.Copy(FileClusterDataBuffer, (ReadSector - ClusterIndex * SectorsPerCluster) * BytesPerSector, DataBuffer, DataOffset, BytesPerSector);
                    }
                }

                DataOffset += BytesPerSector;
                NumberOfSectors--;
                Sector++;
            }

            return DataBuffer;
        }

        public void WriteSectors(int StartSector, int receiveBufferLength, string localDirectoryName, byte[] DataBuffer)
        {
            int Sector = StartSector;
            int NumberOfSectors = receiveBufferLength / BytesPerSector;
            int DataOffset = 0;

            while (NumberOfSectors > 0)
            {
                if (Sector < SectorsPerFat * 2) // FAT area?
                {
                    int WriteSector = Sector;

                    if (WriteSector >= SectorsPerFat)
                        WriteSector -= SectorsPerFat;

                    Array.Copy(DataBuffer, DataOffset, FatBuffer, WriteSector * BytesPerSector, BytesPerSector);

                    SyncDirectoryContents(localDirectoryName);
                }
                else if (Sector < SectorsPerFat * 2 + RootDirectorySectors) // Root directory area?
                {
                    Array.Copy(DataBuffer, DataOffset, RootDirectoryBuffer, (Sector - SectorsPerFat * 2) * BytesPerSector, BytesPerSector);

                    SyncDirectoryContents(localDirectoryName);
                }
                else // Data area.
                {
                    int WriteSector = Sector - (SectorsPerFat * 2 + RootDirectorySectors) + 2 * SectorsPerCluster;
                    int ClusterIndex = WriteSector / SectorsPerCluster;

                    if (ClusterInfo[ClusterIndex] == null)
                        ClusterInfo[ClusterIndex] = new ClusterInfo();

                    if (ClusterInfo[ClusterIndex].DataBuffer == null)
                        ClusterInfo[ClusterIndex].DataBuffer = new byte[BytesPerCluster];

                    Array.Copy(DataBuffer, DataOffset, ClusterInfo[ClusterIndex].DataBuffer, (WriteSector - ClusterIndex * SectorsPerCluster) * BytesPerSector, BytesPerSector);
                }

                DataOffset += BytesPerSector;
                NumberOfSectors--;
                Sector++;
            }
        }

        public bool FatAddDirectoryEntry(int DirectoryCluster, string FullFileName, string ShortFileName, byte AttributeFlags, DateTime LastWriteDateTime, long FileSize, int StartCluster)
        {
            byte[] DirectoryBuffer;
            int EntryIndex = 0;

            if (DirectoryCluster == 0)
                DirectoryBuffer = RootDirectoryBuffer;
            else
                DirectoryBuffer = ClusterInfo[DirectoryCluster].DataBuffer;

            // Find a free entry.

            do
            {
                if (DirectoryCluster == 0)
                {
                    if (EntryIndex >= RootDirectoryBuffer.Length)
                        return false;
                }
                else if (EntryIndex >= BytesPerCluster)
                {
                    int NextDirectoryCluster = FatGetNextCluster(DirectoryCluster);

                    if (NextDirectoryCluster == 0xffff)
                    {
                        try
                        {
                            int NewDirectoryCluster = FatGetFreeCluster(DirectoryCluster);

                            ClusterInfo[NewDirectoryCluster] = new ClusterInfo();

                            ClusterInfo[NewDirectoryCluster].ContentName = ClusterInfo[DirectoryCluster].ContentName;
                            ClusterInfo[NewDirectoryCluster].FileOffset = -1;
                            ClusterInfo[NewDirectoryCluster].DataBuffer = new byte[BytesPerCluster];

                            DirectoryCluster = NewDirectoryCluster;
                        }

                        catch (IndexOutOfRangeException boundsEx)
                        {
                            Logger.LogError(boundsEx, $"Local directory is too large for the given virtual disk size ({DiskSize / 1024 / 1024} MB).");
                            throw boundsEx;
                        }
                    }
                    else
                    {
                        DirectoryCluster = NextDirectoryCluster;
                    }

                    DirectoryBuffer = ClusterInfo[DirectoryCluster].DataBuffer;
                    EntryIndex = 0;
                }

                while (EntryIndex < BytesPerCluster && DirectoryBuffer[EntryIndex] != 0)
                    EntryIndex += 32;
            }
            while (EntryIndex >= BytesPerCluster);

            // Remember which local content matches this entry.

            if (ShortFileName != "." && ShortFileName != "..")
            {
                LocalDirectoryContentInfo localDirectoryContentInfo = new LocalDirectoryContentInfo();

                localDirectoryContentInfo.ContentName = FullFileName;
                localDirectoryContentInfo.ShortFileName = ShortFileName;
                localDirectoryContentInfo.EntryIndex = EntryIndex;
                localDirectoryContentInfo.Cluster = DirectoryCluster;
                localDirectoryContentInfo.StartCluster = StartCluster;

                LocalDirectoryContentInfo.Add(localDirectoryContentInfo);
            }

            // File name.

            int Index;

            for (Index = 0; Index < (8 + 3); Index++)
                DirectoryBuffer[EntryIndex + Index] = 0x20;

            string[] NameAndExtender;
            byte[] AsciiName;
            byte[] AsciiExtender;

            if (ShortFileName == "." || ShortFileName == "..")
            {
                AsciiName = ASCIIEncoding.ASCII.GetBytes(ShortFileName);
                AsciiExtender = null;
            }
            else
            {
                NameAndExtender = ShortFileName.Split('.');
                AsciiName = ASCIIEncoding.ASCII.GetBytes(NameAndExtender[0]);
                AsciiExtender = NameAndExtender.Length == 2 ? ASCIIEncoding.ASCII.GetBytes(NameAndExtender[1]) : null;
            }

            for (Index = 0; Index < AsciiName.Length; Index++)
                DirectoryBuffer[EntryIndex + Index] = AsciiName[Index];

            if (AsciiExtender != null)
                for (Index = 0; Index < AsciiExtender.Length; Index++)
                    DirectoryBuffer[EntryIndex + 8 + Index] = AsciiExtender[Index];

            // File attribute flags.

            DirectoryBuffer[EntryIndex + 11] = AttributeFlags;

            // File write time and date (little endian).

            UInt16 FatFileWriteTime = 0;
            UInt16 FatFileWriteDate = 0;

            int TwoSeconds = LastWriteDateTime.Second / 2;
            int Minutes = LastWriteDateTime.Minute;
            int Hours = LastWriteDateTime.Hour;
            int DayOfMonth = LastWriteDateTime.Day;
            int Month = LastWriteDateTime.Month;
            int YearsSince1980 = LastWriteDateTime.Year - 1980;

            FatFileWriteTime |= (UInt16)TwoSeconds;
            FatFileWriteTime |= (UInt16)(Minutes << 5);
            FatFileWriteTime |= (UInt16)(Hours << 11);

            FatFileWriteDate |= (UInt16)DayOfMonth;
            FatFileWriteDate |= (UInt16)(Month << 5);
            FatFileWriteDate |= (UInt16)(YearsSince1980 << 9);

            DirectoryBuffer[EntryIndex + 22] = (byte)(FatFileWriteTime & 0xff);
            DirectoryBuffer[EntryIndex + 23] = (byte)((FatFileWriteTime >> 8) & 0xff);
            DirectoryBuffer[EntryIndex + 24] = (byte)(FatFileWriteDate & 0xff);
            DirectoryBuffer[EntryIndex + 25] = (byte)((FatFileWriteDate >> 8) & 0xff);

            // Cluster (little endian).

            DirectoryBuffer[EntryIndex + 26] = (byte)(StartCluster & 0xff);
            DirectoryBuffer[EntryIndex + 27] = (byte)((StartCluster >> 8) & 0xff);

            // File size (little endian).

            DirectoryBuffer[EntryIndex + 28] = (byte)(FileSize & 0xff);
            DirectoryBuffer[EntryIndex + 29] = (byte)((FileSize >> 8) & 0xff);
            DirectoryBuffer[EntryIndex + 30] = (byte)((FileSize >> 16) & 0xff);
            DirectoryBuffer[EntryIndex + 31] = (byte)((FileSize >> 24) & 0xff);

            return true;
        }

        public void FatAddDirectory(DirectoryInfo DirectoryInfo, int DirectoryCluster)
        {
            int NewDirectoryCluster = FatGetFreeCluster(0);

            ClusterInfo[NewDirectoryCluster] = new ClusterInfo();

            ClusterInfo[NewDirectoryCluster].ContentName = DirectoryInfo.FullName;
            ClusterInfo[NewDirectoryCluster].FileOffset = -1;
            ClusterInfo[NewDirectoryCluster].DataBuffer = new byte[BytesPerCluster];

            FatAddDirectoryEntry(DirectoryCluster, DirectoryInfo.FullName, FatCreateShortFileName(DirectoryInfo.Name), 0x10, DirectoryInfo.LastWriteTime, 0, NewDirectoryCluster);
            FatAddDirectoryEntry(NewDirectoryCluster, "", ".", 0x10, DirectoryInfo.LastWriteTime, 0, NewDirectoryCluster);
            FatAddDirectoryEntry(NewDirectoryCluster, "", "..", 0x10, DirectoryInfo.LastWriteTime, 0, DirectoryCluster);

            FatImportDirectoryContents(DirectoryInfo.FullName, NewDirectoryCluster);
        }

        public void FatAddFile(FileInfo FileInfo, int DirectoryCluster)
        {
            long FileOffset = 0;
            int FileStartCluster = 0;
            int NextFileCluster = 0;

            while (FileOffset < FileInfo.Length)
            {
                try
                {
                    NextFileCluster = FatGetFreeCluster(NextFileCluster);

                    if (FileStartCluster == 0)
                        FileStartCluster = NextFileCluster;

                    ClusterInfo[NextFileCluster] = new ClusterInfo();
                    ClusterInfo[NextFileCluster].ContentName = FileInfo.FullName;
                    ClusterInfo[NextFileCluster].FileOffset = FileOffset;

                    FileOffset += BytesPerCluster;
                }

                catch(IndexOutOfRangeException boundsEx)
                {
                    Logger.LogError(boundsEx, $"Local directory is too large for the given virtual disk size ({DiskSize / 1024 / 1024} MB)");
                    throw boundsEx;
                }
            }

            FatAddDirectoryEntry(DirectoryCluster, FileInfo.FullName, FatCreateShortFileName(FileInfo.Name), 0x00, FileInfo.LastWriteTime, FileInfo.Length, FileStartCluster);
        }

        public void FileChangedHandler(object Source, FileSystemEventArgs Args)
        {
            MediaChanged = true;
        }

        public void FileRenamedHandler(object Source, FileSystemEventArgs Args)
        {
            MediaChanged = true;
        }

        public void FatImportDirectoryContents(string DirectoryName, int DirectoryCluster)
        {
            if (DirectoryCluster == 0)
            {
                RootDirectoryBuffer = new byte[RootDirectorySectors * BytesPerSector];
                FatBuffer = new byte[SectorsPerFat * BytesPerSector];
                ClusterInfo = new ClusterInfo[DiskClusters];
                LocalDirectoryContentInfo = new List<LocalDirectoryContentInfo>();
            }

            DirectoryInfo DirectoryInfo = new DirectoryInfo(DirectoryName);

            foreach (DirectoryInfo SubDirectoryInfo in DirectoryInfo.GetDirectories())
                FatAddDirectory(SubDirectoryInfo, DirectoryCluster);

            foreach (FileInfo FileInfo in DirectoryInfo.GetFiles())
                FatAddFile(FileInfo, DirectoryCluster);
        }
    }
}
