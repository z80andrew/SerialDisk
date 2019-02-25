using System;
using System.IO.Ports;
using AtariST.SerialDisk.Storage;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using AtariST.SerialDisk.Shared;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Comm
{
    public class Serial : IDisposable
    {
        private SerialPort serialPort;

        private Logger logger;
        private Disk disk;

        public ReceiverState State { get; set; }

        private string localDirectoryName = ".";
        private int readTimeout = 100;

        private int ReceivedDataCounter = 0;

        private UInt32 ReceivedSectorIndex = 0;
        private UInt32 ReceivedSectorCount = 0;
        private byte[] ReceiverDataBuffer;
        private int ReceiverDataIndex = 0;

        byte[] buffer = new byte[4096];
        public Action kickoffRead = null;

        private DateTime TransferEndDateTime = DateTime.Now;
        private DateTime TransferStartDateTime = DateTime.Now;
        private long TransferSize = 0;

        public Serial(Settings applicationSettings, Disk localDisk, Logger log)
        {
            localDirectoryName = applicationSettings.LocalDirectoryName;

            disk = localDisk;
            logger = log;

            State = ReceiverState.ReceiveStartMagic;

            serialPort = new SerialPort(applicationSettings.SerialSettings.PortName);

            ConfigureSerialPort(applicationSettings.SerialSettings);

            serialPort.ReceivedBytesThreshold = 1;

            serialPort.WriteTimeout = -1;
            serialPort.ReadBufferSize = 64 * 1024;
            serialPort.WriteBufferSize = 64 * 1024;

            serialPort.Open();

            logger.Log($"Serial port {applicationSettings.SerialSettings.PortName} opened successfully.", LoggingLevel.Verbose);

            serialPort.DiscardOutBuffer();
            serialPort.DiscardInBuffer();

            serialPort.DataReceived += SerialPort_DataReceived;
            serialPort.ReceivedBytesThreshold = 1;
        }

        private void ConfigureSerialPort(SerialPortSettings serialSettings)
        {
            serialPort.Handshake = serialSettings.Handshake;
            serialPort.BaudRate = serialSettings.BaudRate;
            serialPort.DataBits = serialSettings.DataBits;
            serialPort.StopBits = serialSettings.StopBits;
            serialPort.Parity = serialSettings.Parity;
            serialPort.ReadTimeout = serialSettings.Timeout;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;

            while (port.BytesToRead > 0)
            {
                ProcessReceivedByte(Convert.ToByte(port.ReadByte()));
            }
        }

        private void ProcessReceivedByte(byte Data)
        {
            try
            {
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
                                        logger.Log("Received read command.", LoggingLevel.Verbose);
                                        State = ReceiverState.ReceiveReadSectorIndex;
                                        ReceivedSectorIndex = 0;
                                        break;

                                    case 1:
                                        logger.Log("Received write command.", LoggingLevel.Verbose);
                                        State = ReceiverState.ReceiveWriteSectorIndex;
                                        ReceivedSectorIndex = 0;
                                        break;

                                    case 2:
                                        logger.Log("Received media change command.", LoggingLevel.Verbose);
                                        State = ReceiverState.SendMediaChangeStatus;
                                        break;

                                    case 3:
                                        logger.Log("Received send BIOS parameter block command.", LoggingLevel.Verbose);
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
                                logger.Log($"Received read sector index command.", LoggingLevel.Verbose);
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
                                logger.Log($"Received read sector count command.", LoggingLevel.Verbose);
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
                                logger.Log($"Received write sector index command.", LoggingLevel.Verbose);
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
                                logger.Log($"Received write sector count command.", LoggingLevel.Verbose);
                                ReceivedSectorCount = (ReceivedSectorCount << 8) + Data;
                                State = ReceiverState.ReceiveWriteData;
                                ReceivedDataCounter = -1;
                                break;
                        }

                        break;

                    case ReceiverState.ReceiveWriteData:
                        int directoryCluster = 0;

                        if (ReceivedDataCounter == 0)
                        {

                            if (ReceivedSectorCount == 1)
                                logger.Log("Writing sector " + ReceivedSectorIndex + " (" + disk.BytesPerSector + " Bytes)... ", LoggingLevel.Info);
                            else
                                logger.Log("Writing sectors " + ReceivedSectorIndex + " - " + (ReceivedSectorIndex + ReceivedSectorCount - 1) + " (" + (ReceivedSectorCount * disk.BytesPerSector) + " Bytes)... ", LoggingLevel.Info);


                            ReceiverDataBuffer = new byte[ReceivedSectorCount * disk.BytesPerSector];
                            ReceiverDataIndex = 0;

                            TransferStartDateTime = DateTime.Now;
                        }

                        ReceiverDataBuffer[ReceiverDataIndex++] = Data;

                        if (ReceiverDataIndex == ReceivedSectorCount * disk.BytesPerSector)
                        {
                            logger.Log("Transfer done (" + (ReceiverDataBuffer.LongLength * 10000000 / (DateTime.Now.Ticks - TransferStartDateTime.Ticks)) + " Bytes/s).", LoggingLevel.Info);

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
                                logger.Log("Transfer done (" + (TransferSize * 10000000 / (DateTime.Now.Ticks - TransferStartDateTime.Ticks)) + " Bytes/s).", LoggingLevel.Info);

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
                            logger.Log("Media has been changed. Importing directory \"" + localDirectoryName + "\"... ", LoggingLevel.Info);

                            disk.FatImportDirectoryContents(localDirectoryName, 0);
                        }

                        byte[] MediaChangedBuffer = new byte[1];

                        MediaChangedBuffer[0] = disk.MediaChanged ? (byte)2 : (byte)0;
                        serialPort.Write(MediaChangedBuffer, 0, 1);

                        disk.MediaChanged = false;

                        State = ReceiverState.ReceiveStartMagic;

                        break;

                    case ReceiverState.SendBiosParameterBlock:
                        logger.Log($"Sending BIOS parameter block.", LoggingLevel.Verbose);

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
                        logger.Log("Sending data...", LoggingLevel.Verbose);

                        if (ReceivedSectorCount == 1)
                            logger.Log("Reading sector " + ReceivedSectorIndex + " (" + disk.BytesPerSector + " Bytes)... ", LoggingLevel.Info);
                        else
                            logger.Log("Reading sectors " + ReceivedSectorIndex + " - " + (ReceivedSectorIndex + ReceivedSectorCount - 1) + " (" + (ReceivedSectorCount * disk.BytesPerSector) + " Bytes)... ", LoggingLevel.Info);


                        byte[] SendDataBuffer = disk.ReadSectors((int)ReceivedSectorIndex, (int)ReceivedSectorCount);

                        TransferStartDateTime = DateTime.Now;
                        TransferSize = SendDataBuffer.LongLength;

                        for (int i = 0; i < SendDataBuffer.Length; i++)
                        {
                            serialPort.BaseStream.WriteByte(SendDataBuffer[i]);
                            string percentSent = ((Convert.ToDecimal(i + 1) / SendDataBuffer.Length) * 100).ToString("00.0");
                            Console.Write($"\rSent [{(i + 1).ToString("D" + SendDataBuffer.Length.ToString().Length)} / {SendDataBuffer.Length} Bytes] {percentSent}% ");
                        }
                        Console.WriteLine();

                        byte[] Crc32Buffer = new byte[4];
                        UInt32 Crc32Value = CRC32.CalculateCrc32(SendDataBuffer);

                        Crc32Buffer[0] = (byte)((Crc32Value >> 24) & 0xff);
                        Crc32Buffer[1] = (byte)((Crc32Value >> 16) & 0xff);
                        Crc32Buffer[2] = (byte)((Crc32Value >> 8) & 0xff);
                        Crc32Buffer[3] = (byte)(Crc32Value & 0xff);

                        logger.Log("Sending CRC32...", LoggingLevel.Verbose);

                        for (int i = 0; i < Crc32Buffer.Length; i++)
                        {
                            serialPort.BaseStream.WriteByte(Crc32Buffer[i]);
                            string percentSent = ((Convert.ToDecimal(i + 1) / Crc32Buffer.Length) * 100).ToString("00.0");
                            Console.Write($"\rSent [{(i + 1).ToString("D" + Crc32Buffer.Length.ToString().Length)} / {Crc32Buffer.Length} Bytes] {percentSent}% ");
                        }
                        Console.WriteLine();

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

                    logger.LogException(timeoutEx, "Serial port read timeout");

                    logger.Log("Transfer timeout. Retrying...", LoggingLevel.Info);

                    byte[] DummyBuffer = new byte[1];

                    serialPort.Write(DummyBuffer, 0, 1);
                }
            }

        }

        public void Dispose()
        {
            serialPort.Dispose();
        }
    }
}
