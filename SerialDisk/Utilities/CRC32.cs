namespace Z80andrew.SerialDisk.Utilities
{
    public static class CRC32
    {
        private readonly static uint _polynomial = 0x04c11db7;

        private static uint[] _crc32Table;

        private static uint[] Crc32Table
        {
            get
            {
                if (_crc32Table == null)
                {
                    _crc32Table = new uint[256];

                    for (uint byteIndex = 0; byteIndex < _crc32Table.Length; byteIndex++)
                    {
                        uint crc32Value = byteIndex << 24;

                        for (byte bitIndex = 0; bitIndex < 8; bitIndex++)
                        {
                            if ((crc32Value & 0x80000000) != 0)
                                crc32Value = (crc32Value << 1) ^ _polynomial;
                            else
                                crc32Value <<= 1;
                        }

                        _crc32Table[byteIndex] = crc32Value;
                    }
                }

                return _crc32Table;
            }
        }

        /// <summary>
        /// Generates a CRC32/POSIX checksum
        /// Polynomial: 0x04C11DB7	
        /// Checksum initial value: 0x00000000	
        /// Relect input: false	
        /// Reflect output: false	
        /// XOR result with: 0xFFFFFFFF
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static uint CalculateCRC32(byte[] buffer)
        {
            uint crc32 = 0;

            foreach (byte data in buffer)
            {
                crc32 = (crc32 << 8) ^ Crc32Table[data ^ ((crc32 >> 24) & 0xFF)];
            }

            return ~crc32;
        }
    }
}
