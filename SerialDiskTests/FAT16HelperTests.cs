using NUnit.Framework;
using AtariST.SerialDisk.Utilities;
using static AtariST.SerialDisk.Common.Constants;

namespace Tests
{
    [TestFixture]
    public class FAT16HelperTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [TestCase("SerialDisk.tos", "SERIALDI.TOS")]
        [TestCase("Hello There.mpeg", "HELLO_TH.MPE")]
        [TestCase(@"*+,/:;<=.æøå", "________.___")]
        [TestCase(@">?\[]|.^'¨","______.___")]
        public void CreateShortFileNameFromLongFileName(string longFileName, string expectedShortFileName)
        {
            var shortFileName = FAT16Helper.GetShortFileName(longFileName);

            Assert.AreEqual(expectedShortFileName, shortFileName);
        }

        [TestCase(TOSVersion.TOS100, 0x3FFF * 8192 * 2)]
        [TestCase(TOSVersion.TOS104, 0x7FFF * 8192 * 2)]
        public void ValidDiskSizes(TOSVersion tosVersion, int expectedMaxDiskSizeBytes)
        {
            var maxDiskSizeBytes = FAT16Helper.MaxDiskSizeBytes(tosVersion);

            Assert.AreEqual(expectedMaxDiskSizeBytes, maxDiskSizeBytes);
        }

        public void InvalidDiskSizes()
        {

        }
    }
}