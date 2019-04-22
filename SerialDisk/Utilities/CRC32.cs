using System;

namespace AtariST.SerialDisk.Utilities
{
    public static class CRC32
    {
        private static UInt32[] _crc32Table;

        private static void CreateCrc32Table()
        {
            _crc32Table = new UInt32[256];

            UInt32 Crc32Poly = 0x04c11db7;

            for (int ByteIndex = 0; ByteIndex < 256; ByteIndex++)
            {
                UInt32 crc32Value = (UInt32)(ByteIndex << 24);

                for (int BitIndex = 0; BitIndex < 8; BitIndex++)
                {
                    if ((crc32Value & (1 << 31)) != 0)
                        crc32Value = (crc32Value << 1) ^ Crc32Poly;
                    else
                        crc32Value = (crc32Value << 1);
                }

                _crc32Table[ByteIndex] = crc32Value;
            }
        }

        public static UInt32 CalculateCRC32(byte[] buffer)
        {
            if (_crc32Table == null) CreateCrc32Table();

            UInt32 crc32Value = 0;

            for (int Index = 0; Index < buffer.Length; Index++)
                crc32Value = (crc32Value << 8) ^ _crc32Table[buffer[Index] ^ ((crc32Value >> 24) & 0xff)];

            return crc32Value;
        }
    }
}
