using AtariST.SerialDisk.Utilities;
using NUnit.Framework;
using static AtariST.SerialDisk.Common.Constants;

namespace Tests
{
    [TestFixture]
    public class FAT16HelperTests
    {
        private const int _sectorsPerCluster = 2;

        [SetUp]
        public void Setup()
        {

        }

        [TestCase("SerialDisk.tos", "SERIALDI.TOS")]
        [TestCase("Hello There.mpeg", "HELLO_TH.MPE")]
        [TestCase(@"*+,/:;<=.æøå", "________.___")]
        [TestCase(@">?\[]|.^'¨", "______.___")]
        [TestCase("tst.dot.exe", "TST_DOT.EXE")]
        public void CreateShortFileNameFromLongFileName(string longFileName, string expectedShortFileName)
        {
            var shortFileName = FAT16Helper.GetShortFileName(longFileName);

            Assert.AreEqual(expectedShortFileName, shortFileName);
        }

        [TestCase(".htaccess","_HTACCES")]
        [TestCase(".filename.jpeg","_FILENAM.JPE")]
        public void CreateShortFileNameFromInvalidFileName(string invalidFileName, string expectedShortFileName)
        {
            var shortFileName = FAT16Helper.GetShortFileName(invalidFileName);

            Assert.AreEqual(expectedShortFileName, shortFileName);
        }

        [TestCase(TOSVersion.TOS100, 0x3FFF * 8192 * 2)]
        [TestCase(TOSVersion.TOS104, 0x7FFF * 8192 * 2)]
        public void ValidDiskSizes(TOSVersion tosVersion, int expectedMaxDiskSizeBytes)
        {
            var maxDiskSizeBytes = FAT16Helper.MaxDiskSizeBytes(tosVersion, _sectorsPerCluster);

            Assert.AreEqual(expectedMaxDiskSizeBytes, maxDiskSizeBytes);
        }

        public void InvalidDiskSizes()
        {

        }
    }
}