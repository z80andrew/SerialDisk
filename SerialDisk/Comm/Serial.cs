using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using System;
using System.IO.Ports;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Comm
{
    public class Serial : IDisposable, ISerial
    {
        private SerialPort _serialPort;

        private ILogger _logger;
        private IDisk _localDisk;

        private int _readTimeout = 100;

        private int _receivedDataCounter = 0;

        private UInt32 _receivedSectorIndex = 0;
        private UInt32 _receivedSectorCount = 0;
        private byte[] _receiverDataBuffer;
        private int _receiverDataIndex = 0;

        private byte[] _buffer = new byte[4096];

        private DateTime _transferEndDateTime = DateTime.Now;
        private DateTime _transferStartDateTime = DateTime.Now;
        private long _transferSize = 0;

        private ReceiverState _state = ReceiverState.ReceiveStartMagic;

        public Serial(SerialPortSettings serialPortSettings, IDisk disk, ILogger log)
        {
            _localDisk = disk;
            _logger = log;

            _serialPort = InitializeSerialPort(serialPortSettings);
            _serialPort.Open();

            _logger.Log($"Serial port {serialPortSettings.PortName} opened successfully.", LoggingLevel.Verbose);

            _serialPort.DiscardOutBuffer();
            _serialPort.DiscardInBuffer();

            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        private SerialPort InitializeSerialPort(SerialPortSettings serialSettings)
        {
            SerialPort serialPort = new SerialPort(serialSettings.PortName);
            serialPort.Handshake = serialSettings.Handshake;
            serialPort.BaudRate = serialSettings.BaudRate;
            serialPort.DataBits = serialSettings.DataBits;
            serialPort.StopBits = serialSettings.StopBits;
            serialPort.Parity = serialSettings.Parity;
            serialPort.ReceivedBytesThreshold = 1;
            serialPort.ReadTimeout = 100;
            serialPort.WriteTimeout = -1;
            serialPort.ReadBufferSize = 64 * 1024;
            serialPort.WriteBufferSize = 64 * 1024;
            serialPort.ReceivedBytesThreshold = 1;

            return serialPort;
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
            _localDisk.FileSystemWatcherEnabled = false;

            try
            {
                switch (_state)
                {
                    case ReceiverState.ReceiveStartMagic:
                        
                        switch (_receivedDataCounter)
                        {
                            case 0:
                                if (Data != 0x18)
                                    _receivedDataCounter = -1;
                                break;

                            case 1:
                                if (Data != 0x03)
                                    _receivedDataCounter = -1;
                                break;

                            case 2:
                                if (Data != 0x20)
                                    _receivedDataCounter = -1;
                                break;

                            case 3:
                                if (Data != 0x06)
                                    _receivedDataCounter = -1;
                                break;

                            case 4:
                                switch (Data)
                                {
                                    case 0:
                                        _logger.Log("Received read command.", LoggingLevel.Verbose);
                                        _state = ReceiverState.ReceiveReadSectorIndex;
                                        _receivedSectorIndex = 0;
                                        break;

                                    case 1:
                                        _logger.Log("Received write command.", LoggingLevel.Verbose);
                                        _state = ReceiverState.ReceiveWriteSectorIndex;
                                        _receivedSectorIndex = 0;
                                        break;

                                    case 2:
                                        _logger.Log("Received media change command.", LoggingLevel.Verbose);
                                        _state = ReceiverState.SendMediaChangeStatus;
                                        break;

                                    case 3:
                                        _logger.Log("Received send BIOS parameter block command.", LoggingLevel.Verbose);
                                        _state = ReceiverState.SendBiosParameterBlock;
                                        break;
                                }

                                _receivedDataCounter = -1;
                                break;
                        }

                        break;

                    case ReceiverState.ReceiveReadSectorIndex:
                        switch (_receivedDataCounter)
                        {
                            case 0:
                            case 1:
                            case 2:
                                _receivedSectorIndex = (_receivedSectorIndex << 8) + Data;
                                break;

                            case 3:
                                _logger.Log($"Received read sector index command.", LoggingLevel.Verbose);
                                _receivedSectorIndex = (_receivedSectorIndex << 8) + Data;
                                _state = ReceiverState.ReceiveReadSectorCount;
                                _receivedSectorCount = 0;
                                _receivedDataCounter = -1;
                                break;
                        }

                        break;

                    case ReceiverState.ReceiveReadSectorCount:
                        switch (_receivedDataCounter)
                        {
                            case 0:
                            case 1:
                            case 2:
                                _receivedSectorCount = (_receivedSectorCount << 8) + Data;
                                break;

                            case 3:
                                _logger.Log($"Received read sector count command.", LoggingLevel.Verbose);
                                _receivedSectorCount = (_receivedSectorCount << 8) + Data;
                                _state = ReceiverState.SendReadData;
                                _receivedDataCounter = -1;
                                break;
                        }

                        break;

                    case ReceiverState.ReceiveWriteSectorIndex:
                        switch (_receivedDataCounter)
                        {
                            case 0:
                            case 1:
                            case 2:
                                _receivedSectorIndex = (_receivedSectorIndex << 8) + Data;
                                break;

                            case 3:
                                _logger.Log($"Received write sector index command.", LoggingLevel.Verbose);
                                _receivedSectorIndex = (_receivedSectorIndex << 8) + Data;
                                _state = ReceiverState.ReceiveWriteSectorCount;
                                _receivedSectorCount = 0;
                                _receivedDataCounter = -1;
                                break;
                        }

                        break;

                    case ReceiverState.ReceiveWriteSectorCount:
                        switch (_receivedDataCounter)
                        {
                            case 0:
                            case 1:
                            case 2:
                                _receivedSectorCount = (_receivedSectorCount << 8) + Data;
                                break;

                            case 3:
                                _logger.Log($"Received write sector count command.", LoggingLevel.Verbose);
                                _receivedSectorCount = (_receivedSectorCount << 8) + Data;
                                _state = ReceiverState.ReceiveWriteData;
                                _receivedDataCounter = -1;
                                break;
                        }

                        break;

                    case ReceiverState.ReceiveWriteData:
                        if (_receivedDataCounter == 0)
                        {

                            if (_receivedSectorCount == 1)
                                _logger.Log("Writing sector " + _receivedSectorIndex + " (" + _localDisk.Parameters.BytesPerSector + " Bytes)... ", LoggingLevel.Info);
                            else
                                _logger.Log("Writing sectors " + _receivedSectorIndex + " - " + (_receivedSectorIndex + _receivedSectorCount - 1) + " (" + (_receivedSectorCount * _localDisk.Parameters.BytesPerSector) + " Bytes)... ", LoggingLevel.Info);


                            _receiverDataBuffer = new byte[_receivedSectorCount * _localDisk.Parameters.BytesPerSector];
                            _receiverDataIndex = 0;

                            _transferStartDateTime = DateTime.Now;
                        }

                        _receiverDataBuffer[_receiverDataIndex++] = Data;

                        string percentReceived = ((Convert.ToDecimal(_receiverDataIndex) / _receiverDataBuffer.Length) * 100).ToString("00.0");
                        Console.Write($"\rReceived [{_receiverDataIndex} / {_receiverDataBuffer.Length} Bytes] {percentReceived}% ");

                        if (_receiverDataIndex == _receivedSectorCount * _localDisk.Parameters.BytesPerSector)
                        {
                            Console.WriteLine();

                            _logger.Log("Transfer done (" + (_receiverDataBuffer.LongLength * 10000000 / (DateTime.Now.Ticks - _transferStartDateTime.Ticks)) + " Bytes/s).", LoggingLevel.Info);

                            _localDisk.WriteSectors(_receiverDataBuffer.Length, (int)_receivedSectorIndex, _receiverDataBuffer);

                            _state = ReceiverState.ReceiveStartMagic;
                            _receivedDataCounter = -1;
                        }

                        break;

                    case ReceiverState.ReceiveEndMagic:
                        _serialPort.ReadTimeout = _readTimeout;

                        switch (_receivedDataCounter)
                        {
                            case 0:
                                _logger.Log("Transfer done (" + (_transferSize * 10000000 / (DateTime.Now.Ticks - _transferStartDateTime.Ticks)) + " Bytes/s).", LoggingLevel.Info);

                                if (Data != 0x02)
                                    _receivedDataCounter = -1;
                                break;

                            case 1:
                                if (Data != 0x02)
                                    _receivedDataCounter = -1;
                                break;

                            case 2:
                                if (Data != 0x19)
                                    _receivedDataCounter = -1;
                                break;

                            case 3:
                                if (Data == 0x61)
                                    _state = ReceiverState.ReceiveStartMagic;

                                _receivedDataCounter = -1;

                                break;
                        }

                        break;
                }

                _receivedDataCounter++;

                switch (_state)
                {
                    case ReceiverState.SendMediaChangeStatus:
                        if (_localDisk.MediaChanged)
                        {
                            _logger.Log("Media has been changed. Importing directory \"" + _localDisk.Parameters.LocalDirectoryPath + "\"... ", LoggingLevel.Info);

                            _localDisk.FatImportLocalDirectoryContents(_localDisk.Parameters.LocalDirectoryPath, 0);
                        }

                        byte[] MediaChangedBuffer = new byte[1];

                        MediaChangedBuffer[0] = _localDisk.MediaChanged ? (byte)2 : (byte)0;
                        _serialPort.Write(MediaChangedBuffer, 0, 1);

                        _localDisk.MediaChanged = false;

                        _state = ReceiverState.ReceiveStartMagic;

                        break;

                    case ReceiverState.SendBiosParameterBlock:
                        _logger.Log($"Sending BIOS parameter block.", LoggingLevel.Verbose);

                        _serialPort.Write(_localDisk.Parameters.BIOSParameterBlock, 0, _localDisk.Parameters.BIOSParameterBlock.Length);

                        _state = ReceiverState.ReceiveStartMagic;

                        break;

                    case ReceiverState.SendReadData:
                        _logger.Log("Sending data...", LoggingLevel.Verbose);

                        if (_receivedSectorCount == 1)
                            _logger.Log("Reading sector " + _receivedSectorIndex + " (" + _localDisk.Parameters.BytesPerSector + " Bytes)... ", LoggingLevel.Info);
                        else
                            _logger.Log("Reading sectors " + _receivedSectorIndex + " - " + (_receivedSectorIndex + _receivedSectorCount - 1) + " (" + (_receivedSectorCount * _localDisk.Parameters.BytesPerSector) + " Bytes)... ", LoggingLevel.Info);


                        byte[] sendDataBuffer = _localDisk.ReadSectors((int)_receivedSectorIndex, (int)_receivedSectorCount);

                        _transferStartDateTime = DateTime.Now;
                        _transferSize = sendDataBuffer.LongLength;

                        for (int i = 0; i < sendDataBuffer.Length; i++)
                        {
                            _serialPort.BaseStream.WriteByte(sendDataBuffer[i]);
                            string percentSent = ((Convert.ToDecimal(i + 1) / sendDataBuffer.Length) * 100).ToString("00.0");
                            Console.Write($"\rSent [{(i + 1).ToString("D" + sendDataBuffer.Length.ToString().Length)} / {sendDataBuffer.Length} Bytes] {percentSent}% ");
                        }
                        Console.WriteLine();

                        byte[] crc32Buffer = new byte[4];
                        UInt32 crc32Value = CRC32.CalculateCRC32(sendDataBuffer);

                        crc32Buffer[0] = (byte)((crc32Value >> 24) & 0xff);
                        crc32Buffer[1] = (byte)((crc32Value >> 16) & 0xff);
                        crc32Buffer[2] = (byte)((crc32Value >> 8) & 0xff);
                        crc32Buffer[3] = (byte)(crc32Value & 0xff);

                        _logger.Log("Sending CRC32...", LoggingLevel.Verbose);

                        for (int i = 0; i < crc32Buffer.Length; i++)
                        {
                            _serialPort.BaseStream.WriteByte(crc32Buffer[i]);
                            string percentSent = ((Convert.ToDecimal(i + 1) / crc32Buffer.Length) * 100).ToString("00.0");
                            Console.Write($"\rSent [{(i + 1).ToString("D" + crc32Buffer.Length.ToString().Length)} / {crc32Buffer.Length} Bytes] {percentSent}% ");
                        }
                        Console.WriteLine();

                        _transferEndDateTime = DateTime.Now;

                        _state = ReceiverState.ReceiveEndMagic;

                        break;
                }
            }

            catch (TimeoutException timeoutEx)
            {
                if (_state == ReceiverState.ReceiveEndMagic)
                {
                    _serialPort.ReadTimeout = _readTimeout;

                    _logger.LogException(timeoutEx, "Serial port read timeout");

                    _logger.Log("Transfer timeout. Retrying...", LoggingLevel.Info);

                    byte[] DummyBuffer = new byte[1];

                    _serialPort.Write(DummyBuffer, 0, 1);
                }
            }

            if (_state == ReceiverState.ReceiveStartMagic)
                _localDisk.FileSystemWatcherEnabled = true;
        }

        public void Dispose()
        {
            _serialPort.Dispose();
        }
    }
}
