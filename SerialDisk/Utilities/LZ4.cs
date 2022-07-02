using K4os.Compression.LZ4;
using System.Linq;

namespace AtariST.SerialDisk.Utilities
{
    public static class LZ4
    {
        public static byte[] CompressAsStandardLZ4Block (byte[] data)
        {
            var compressedBytes = new byte[LZ4Codec.MaximumOutputSize(data.Length)];

            var encodedLength = LZ4Codec.Encode(
                data, 0, data.Length,
                compressedBytes, 0, compressedBytes.Length,
                LZ4Level.L12_MAX);

            return compressedBytes.Take(encodedLength).ToArray();
        }
    }
}
