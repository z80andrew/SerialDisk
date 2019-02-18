using System;

namespace AtariST.SerialDisk.Utilities
{
    public static class CRC32
    {
        public static UInt32[] Crc32Table;

        public static void CreateCrc32Table()
        {
            Crc32Table = new UInt32[256];

            UInt32 Crc32Poly = 0x04c11db7;

            for (int ByteIndex = 0; ByteIndex < 256; ByteIndex++)
            {
                UInt32 Crc32Value = (UInt32)(ByteIndex << 24);

                for (int BitIndex = 0; BitIndex < 8; BitIndex++)
                {
                    if ((Crc32Value & (1 << 31)) != 0)
                        Crc32Value = (Crc32Value << 1) ^ Crc32Poly;
                    else
                        Crc32Value = (Crc32Value << 1);
                }

                Crc32Table[ByteIndex] = Crc32Value;
            }
        }

        public static UInt32 CalculateCrc32(byte[] Buffer)
        {
            if (Crc32Table == null) CreateCrc32Table();

            UInt32 Crc32Value = 0;

            for (int Index = 0; Index < Buffer.Length; Index++)
                Crc32Value = (Crc32Value << 8) ^ Crc32Table[Buffer[Index] ^ ((Crc32Value >> 24) & 0xff)];

            return Crc32Value;
        }
    }
}
