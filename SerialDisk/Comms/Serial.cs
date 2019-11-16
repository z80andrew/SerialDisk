using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Utilities;
using System;
using System.IO;
using System.IO.Ports;
using static AtariST.SerialDisk.Common.Constants;

namespace AtariST.SerialDisk.Comms
{
    public class Serial : ISerial, IDisposable
    {
        private SerialPort _serialPort;

        private ILogger _logger;
        private IDisk _localDisk;

        private int _receivedDataCounter = 0;

        private UInt32 _receivedSectorIndex = 0;
        private UInt32 _receivedSectorCount = 0;
        private byte[] _receiverDataBuffer;
        private int _receiverDataIndex = 0;

        private DateTime _transferStartDateTime = DateTime.Now;
        private long _transferSize = 0;

        private ReceiverState _state = ReceiverState.ReceiveStartMagic;

        public Serial(SerialPortSettings serialPortSettings, IDisk disk, ILogger log)
        {
            _localDisk = disk;
            _logger = log;

            try
            {
                _serialPort = InitializeSerialPort(serialPortSettings);
                _serialPort.Open();
            }

            catch (Exception portException) when (portException is IOException || portException is UnauthorizedAccessException)
            {
                _logger.LogException(portException, $"Error opening serial port {serialPortSettings.PortName}");
                throw portException;
            }

            _logger.Log($"Serial port {serialPortSettings.PortName} opened successfully.", LoggingLevel.Verbose);

            _serialPort.DiscardOutBuffer();
            _serialPort.DiscardInBuffer();

            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        private SerialPort InitializeSerialPort(SerialPortSettings serialSettings)
        {
            SerialPort serialPort = new SerialPort()
            {
                PortName = serialSettings.PortName,
                Handshake = serialSettings.Handshake,
                BaudRate = serialSettings.BaudRate,
                DataBits = serialSettings.DataBits,
                StopBits = serialSettings.StopBits,
                Parity = serialSettings.Parity,
                ReceivedBytesThreshold = 1
            };

            bool useRts = serialSettings.Handshake == Handshake.RequestToSend || serialSettings.Handshake == Handshake.RequestToSendXOnXOff;

            try
            {
                serialPort.RtsEnable = useRts;
            }

            catch (Exception ex)
            {
                _logger.LogException(ex, "Serial error setting RTS");
            }

            try
            {
                serialPort.DtrEnable = useRts;
            }

            catch (Exception ex)
            {
                _logger.LogException(ex, "Serial error setting DTR");
            }

            return serialPort;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;

            while (port.BytesToRead > 0)
            {
                byte readByte = Convert.ToByte(port.BaseStream.ReadByte());
                ProcessReceivedByte(port, readByte);
            }
        }

        private void ProcessReceivedByte(SerialPort port, byte Data)
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

                                _logger.Log($"Receiver state: {_state.ToString()}", LoggingLevel.Verbose);

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
                                _receivedSectorIndex = (_receivedSectorIndex << 8) + Data;
                                _logger.Log($"Received read sector index command - sector {_receivedSectorIndex}", LoggingLevel.Verbose);
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
                                _receivedSectorCount = (_receivedSectorCount << 8) + Data;
                                _logger.Log($"Received read sector count command - {_receivedSectorCount} sector(s)", LoggingLevel.Verbose);
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
                                _receivedSectorIndex = (_receivedSectorIndex << 8) + Data;
                                _logger.Log($"Received write sector index command - sector {_receivedSectorIndex}", LoggingLevel.Verbose);
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
                                _logger.Log($"Received write sector count command  - {_receivedSectorCount} sector(s)", LoggingLevel.Verbose);
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

                        port.BaseStream.WriteByte(_localDisk.MediaChanged ? (byte)2 : (byte)0);

                        _localDisk.MediaChanged = false;

                        _state = ReceiverState.ReceiveStartMagic;

                        break;

                    case ReceiverState.SendBiosParameterBlock:
                        _logger.Log($"Sending BIOS parameter block.", LoggingLevel.Verbose);

                        port.BaseStream.Write(_localDisk.Parameters.BIOSParameterBlock, 0, _localDisk.Parameters.BIOSParameterBlock.Length);

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
                            port.BaseStream.WriteByte(sendDataBuffer[i]);
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

                        port.BaseStream.Write(crc32Buffer, 0, crc32Buffer.Length);

                        _state = ReceiverState.ReceiveStartMagic;

                        _logger.Log($"Receiver state: {_state.ToString()}", LoggingLevel.Verbose);

                        break;
                }
            }

            catch (Exception ex)
            {
                _logger.LogException(ex, "Serial port error");
            }

            if (_state == ReceiverState.ReceiveStartMagic)
            {
                _localDisk.FileSystemWatcherEnabled = true;
            }
        }

        public void Dispose()
        {
            _serialPort.Dispose();
        }
    }
}
