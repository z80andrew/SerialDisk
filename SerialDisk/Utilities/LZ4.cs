using LZ4;
using System;

namespace AtariST.SerialDisk.Utilities
{
    public static class LZ4
    {
        public static byte[] CompressAsStandardLZ4Block (byte[] data)
        {
            var maximumLength = LZ4Codec.MaximumOutputLength(data.Length);
            var outputCompressedBuffer = new byte[maximumLength];

            var outputLength = LZ4Codec.EncodeHC(
                data, 0, data.Length,
                outputCompressedBuffer, 0, maximumLength);

            Array.Resize(ref outputCompressedBuffer, outputLength);

            return outputCompressedBuffer;
        }
    }
}
