using System;
using System.IO.Ports;
using AtariST.SerialDisk.Storage;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using AtariST.SerialDisk.Shared;

namespace AtariST.SerialDisk.Comm
{
    public class Serial
    {
        public SerialPort serialPort;

        private Disk disk;

        public ReceiverState State { get; set; }

        private bool ReceiverContinue = true;

        private int ReceivedDataCounter = 0;

        private UInt32 ReceivedSectorIndex = 0;
        private UInt32 ReceivedSectorCount = 0;
        private byte[] ReceiverDataBuffer;
        private int ReceiverDataIndex = 0;

        private int verbosity;

        public enum ReceiverState
        {
            ReceiveStartMagic = 0,
            ReceiveCommand,
            ReceiveReadSectorIndex,
            ReceiveReadSectorCount,
            SendReadData,
            SendReadCrc32,
            ReceiveWriteSectorIndex,
            ReceiveWriteSectorCount,
            ReceiveWriteData,
            SendWriteCrc32,
            SendMediaChangeStatus,
            SendBiosParameterBlock,
            ReceiveEndMagic
        };

        public Serial(Settings applicationSettings, Disk localDisk)
        {
            verbosity = applicationSettings.Verbosity;

            disk = localDisk;

            State = ReceiverState.ReceiveStartMagic;

            serialPort = new SerialPort(applicationSettings.SerialSettings.PortName);
            serialPort.Handshake = applicationSettings.SerialSettings.Handshake;
            serialPort.BaudRate = applicationSettings.SerialSettings.BaudRate;
            serialPort.DataBits = applicationSettings.SerialSettings.DataBits;
            serialPort.StopBits = applicationSettings.SerialSettings.StopBits;
            serialPort.Parity = applicationSettings.SerialSettings.Parity;
            serialPort.ReadTimeout = applicationSettings.SerialSettings.Timeout;

            serialPort.WriteTimeout = -1;
            serialPort.ReadBufferSize = 64 * 1024;
            serialPort.WriteBufferSize = 64 * 1024;

            serialPort.Open();
        }

        public void StopListening()
        {
            ReceiverContinue = false;
        }

        public void SerialDataReceiver(string localDirectoryName, int readTimeout, int verbosity)
        {
            DateTime TransferEndDateTime = DateTime.Now;
            DateTime TransferStartDateTime = DateTime.Now;
            long TransferSize = 0;

            while (ReceiverContinue)
            {
                try
                {
                    byte Data = (byte)serialPort.ReadByte();

                    switch (State)
                    {
                        case ReceiverState.ReceiveStartMagic:
                            switch (ReceivedDataCounter)
                            {
                                case 0:
                                    if (Data != 0x18)
                                        ReceivedDataCounter = -1;

                                    break;

                                case 1:
                                    if (Data != 0x03)
                                        ReceivedDataCounter = -1;

                                    break;

                                case 2:
                                    if (Data != 0x20)
                                        ReceivedDataCounter = -1;

                                    break;

                                case 3:
                                    if (Data != 0x06)
                                        ReceivedDataCounter = -1;

                                    break;

                                case 4:
                                    switch (Data)
                                    {
                                        case 0:
                                            State = ReceiverState.ReceiveReadSectorIndex;
                                            ReceivedSectorIndex = 0;

                                            break;

                                        case 1:
                                            State = ReceiverState.ReceiveWriteSectorIndex;
                                            ReceivedSectorIndex = 0;

                                            break;

                                        case 2:
                                            State = ReceiverState.SendMediaChangeStatus;

                                            break;

                                        case 3:
                                            State = ReceiverState.SendBiosParameterBlock;

                                            break;
                                    }

                                    ReceivedDataCounter = -1;

                                    break;
                            }

                            break;

                        case ReceiverState.ReceiveReadSectorIndex:
                            switch (ReceivedDataCounter)
                            {
                                case 0:
                                case 1:
                                case 2:
                                    ReceivedSectorIndex = (ReceivedSectorIndex << 8) + Data;

                                    break;

                                case 3:
                                    ReceivedSectorIndex = (ReceivedSectorIndex << 8) + Data;
                                    State = ReceiverState.ReceiveReadSectorCount;
                                    ReceivedSectorCount = 0;
                                    ReceivedDataCounter = -1;

                                    break;
                            }

                            break;

                        case ReceiverState.ReceiveReadSectorCount:
                            switch (ReceivedDataCounter)
                            {
                                case 0:
                                case 1:
                                case 2:
                                    ReceivedSectorCount = (ReceivedSectorCount << 8) + Data;

                                    break;

                                case 3:
                                    ReceivedSectorCount = (ReceivedSectorCount << 8) + Data;
                                    State = ReceiverState.SendReadData;
                                    ReceivedDataCounter = -1;

                                    break;
                            }

                            break;

                        case ReceiverState.ReceiveWriteSectorIndex:
                            switch (ReceivedDataCounter)
                            {
                                case 0:
                                case 1:
                                case 2:
                                    ReceivedSectorIndex = (ReceivedSectorIndex << 8) + Data;

                                    break;

                                case 3:
                                    ReceivedSectorIndex = (ReceivedSectorIndex << 8) + Data;
                                    State = ReceiverState.ReceiveWriteSectorCount;
                                    ReceivedSectorCount = 0;
                                    ReceivedDataCounter = -1;

                                    break;
                            }

                            break;

                        case ReceiverState.ReceiveWriteSectorCount:
                            switch (ReceivedDataCounter)
                            {
                                case 0:
                                case 1:
                                case 2:
                                    ReceivedSectorCount = (ReceivedSectorCount << 8) + Data;

                                    break;

                                case 3:
                                    ReceivedSectorCount = (ReceivedSectorCount << 8) + Data;
                                    State = ReceiverState.ReceiveWriteData;
                                    ReceivedDataCounter = -1;

                                    break;
                            }

                            break;

                        case ReceiverState.ReceiveWriteData:
                            if (ReceivedDataCounter == 0)
                            {
                                if (verbosity > 0)
                                {
                                    if (ReceivedSectorCount == 1)
                                        Console.WriteLine("Writing sector " + ReceivedSectorIndex + " (" + disk.BytesPerSector + " Bytes)... ");
                                    else
                                        Console.WriteLine("Writing sectors " + ReceivedSectorIndex + " - " + (ReceivedSectorIndex + ReceivedSectorCount - 1) + " (" + (ReceivedSectorCount * disk.BytesPerSector) + " Bytes)... ");
                                }

                                ReceiverDataBuffer = new byte[ReceivedSectorCount * disk.BytesPerSector];
                                ReceiverDataIndex = 0;

                                TransferStartDateTime = DateTime.Now;
                            }

                            ReceiverDataBuffer[ReceiverDataIndex++] = Data;

                            if (ReceiverDataIndex == ReceivedSectorCount * disk.BytesPerSector)
                            {
                                if (verbosity > 1)
                                    Console.WriteLine("Transfer done (" + (ReceiverDataBuffer.LongLength * 10000000 / (DateTime.Now.Ticks - TransferStartDateTime.Ticks)) + " Bytes/s).");

                                disk.WriteSectors(ReceiverDataBuffer.Length, (int)ReceivedSectorIndex, localDirectoryName, ReceiverDataBuffer);

                                State = ReceiverState.ReceiveStartMagic;
                                ReceivedDataCounter = -1;
                            }

                            break;

                        case ReceiverState.ReceiveEndMagic:
                            serialPort.ReadTimeout = readTimeout;

                            switch (ReceivedDataCounter)
                            {
                                case 0:
                                    if (verbosity > 1)
                                        Console.WriteLine("Transfer done (" + (TransferSize * 10000000 / (DateTime.Now.Ticks - TransferStartDateTime.Ticks)) + " Bytes/s).");

                                    if (Data != 0x02)
                                        ReceivedDataCounter = -1;

                                    break;

                                case 1:
                                    if (Data != 0x02)
                                        ReceivedDataCounter = -1;

                                    break;

                                case 2:
                                    if (Data != 0x19)
                                        ReceivedDataCounter = -1;

                                    break;

                                case 3:
                                    if (Data == 0x61)
                                        State = ReceiverState.ReceiveStartMagic;

                                    ReceivedDataCounter = -1;

                                    break;
                            }

                            break;
                    }

                    ReceivedDataCounter++;

                    switch (State)
                    {
                        case ReceiverState.SendMediaChangeStatus:
                            if (disk.MediaChanged)
                            {
                                if (verbosity > 0)
                                    Console.WriteLine("Media has been changed. Importing directory \"" + localDirectoryName + "\"... ");

                                disk.FatImportDirectoryContents(localDirectoryName, 0);
                            }

                            byte[] MediaChangedBuffer = new byte[1];

                            MediaChangedBuffer[0] = disk.MediaChanged ? (byte)2 : (byte)0;
                            serialPort.Write(MediaChangedBuffer, 0, 1);

                            disk.MediaChanged = false;

                            State = ReceiverState.ReceiveStartMagic;

                            break;

                        case ReceiverState.SendBiosParameterBlock:
                            byte[] BiosParameterBlock = new byte[18];

                            BiosParameterBlock[0] = (byte)((disk.BytesPerSector >> 8) & 0xff);
                            BiosParameterBlock[1] = (byte)(disk.BytesPerSector & 0xff);

                            BiosParameterBlock[2] = (byte)((disk.SectorsPerCluster >> 8) & 0xff);
                            BiosParameterBlock[3] = (byte)(disk.SectorsPerCluster & 0xff);

                            BiosParameterBlock[4] = (byte)((disk.BytesPerCluster >> 8) & 0xff);
                            BiosParameterBlock[5] = (byte)(disk.BytesPerCluster & 0xff);

                            BiosParameterBlock[6] = (byte)((disk.RootDirectorySectors >> 8) & 0xff);
                            BiosParameterBlock[7] = (byte)(disk.RootDirectorySectors & 0xff);

                            BiosParameterBlock[8] = (byte)((disk.SectorsPerFat >> 8) & 0xff);
                            BiosParameterBlock[9] = (byte)(disk.SectorsPerFat & 0xff);

                            BiosParameterBlock[10] = (byte)((disk.SectorsPerFat >> 8) & 0xff);
                            BiosParameterBlock[11] = (byte)(disk.SectorsPerFat & 0xff);

                            BiosParameterBlock[12] = (byte)(((disk.SectorsPerFat * 2 + disk.RootDirectorySectors) >> 8) & 0xff);
                            BiosParameterBlock[13] = (byte)((disk.SectorsPerFat * 2 + disk.RootDirectorySectors) & 0xff);

                            BiosParameterBlock[14] = (byte)((disk.DiskClusters >> 8) & 0xff);
                            BiosParameterBlock[15] = (byte)(disk.DiskClusters & 0xff);

                            BiosParameterBlock[16] = 0;
                            BiosParameterBlock[17] = 1;

                            serialPort.Write(BiosParameterBlock, 0, 18);

                            State = ReceiverState.ReceiveStartMagic;

                            break;

                        case ReceiverState.SendReadData:
                            if (verbosity > 0)
                            {
                                if (ReceivedSectorCount == 1)
                                    Console.WriteLine("Reading sector " + ReceivedSectorIndex + " (" + disk.BytesPerSector + " Bytes)... ");
                                else
                                    Console.WriteLine("Reading sectors " + ReceivedSectorIndex + " - " + (ReceivedSectorIndex + ReceivedSectorCount - 1) + " (" + (ReceivedSectorCount * disk.BytesPerSector) + " Bytes)... ");
                            }

                            byte[] SendDataBuffer = disk.ReadSectors((int)ReceivedSectorIndex, (int)ReceivedSectorCount);

                            TransferStartDateTime = DateTime.Now;
                            TransferSize = SendDataBuffer.LongLength;

                            serialPort.Write(SendDataBuffer, 0, SendDataBuffer.Length);

                            byte[] Crc32Buffer = new byte[4];
                            UInt32 Crc32Value = CRC32.CalculateCrc32(SendDataBuffer);

                            Crc32Buffer[0] = (byte)((Crc32Value >> 24) & 0xff);
                            Crc32Buffer[1] = (byte)((Crc32Value >> 16) & 0xff);
                            Crc32Buffer[2] = (byte)((Crc32Value >> 8) & 0xff);
                            Crc32Buffer[3] = (byte)(Crc32Value & 0xff);

                            serialPort.Write(Crc32Buffer, 0, Crc32Buffer.Length);

                            TransferEndDateTime = DateTime.Now;

                            State = ReceiverState.ReceiveEndMagic;

                            serialPort.ReadTimeout = (int)((TransferSize * 10 * 1000 * 1.5 / serialPort.BaudRate) - (TransferEndDateTime.Ticks - TransferStartDateTime.Ticks) / 10000); // Set the timeout to 1.5 times the estimated remaining transfer time.

                            break;
                    }
                }

                catch (TimeoutException timeoutEx)
                {
                    if (State == ReceiverState.ReceiveEndMagic)
                    {
                        serialPort.ReadTimeout = readTimeout;

                        Error.Log(timeoutEx, "Serial port read timeout");

                        if (verbosity > 0)
                            Console.WriteLine("Transfer timeout. Retrying...");

                        byte[] DummyBuffer = new byte[1];

                        serialPort.Write(DummyBuffer, 0, 1);
                    }
                }
            }
        }
    }
}
