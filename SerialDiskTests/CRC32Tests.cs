using NUnit.Framework;
using System.Linq;
using System.Text;
using Z80andrew.SerialDisk.Utilities;

namespace Z80andrew.SerialDisk.Tests
{
    [TestFixture]
    public class CRC32Tests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void CalculateCRC32ForLargeSector()
        {
            uint crc32 = CRC32.CalculateCRC32(Enumerable.Repeat((byte)0xFF, 0x2000).ToArray());
            Assert.AreEqual(0x7A4A44C9, crc32);
        }

        [TestCase(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 }, 0xFFFFFFFF)]
        [TestCase(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, (uint)0x091804D7)]
        [TestCase(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, (uint)0x5A600FE0)]
        [TestCase(new byte[] { 0x05, 0x04, 0x03, 0x02, 0x01 }, (uint)0x4CA921C5)]
        public void CalculateCRC32ForBytes(byte[] dataBytes, uint expectedCrc32Checksum)
        {
            Assert.AreEqual(expectedCrc32Checksum, CRC32.CalculateCRC32(dataBytes));
        }

        [TestCase("The quick brown fox jumps over the lazy dog", (uint)0x36B78081)]
        public void CalculateCRC32ForString(string dataString, uint expectedCrc32Checksum)
        {
            byte[] dataBytes = Encoding.ASCII.GetBytes(dataString);
            Assert.AreEqual(expectedCrc32Checksum, CRC32.CalculateCRC32(dataBytes));
        }
    }
}