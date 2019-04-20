using NUnit.Framework;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Storage;
using System.Linq;
using Moq;

namespace Tests
{
    [TestFixture]
    public class DiskTests
    {
        private DiskParameters diskParams;
        private Mock<Disk> _disk;
        private Mock<ILogger> _logger;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();

            diskParams = new DiskParameters(".", 24 * 1024 * 1024);

            _disk = new Mock<Disk>(diskParams, _logger.Object);
            _disk.Setup(d => d.WatchLocalDirectory(It.IsAny<string>())).Callback(() => { });
            _disk.Setup(d => d.FatImportLocalDirectoryContents(It.IsAny<string>(), It.IsAny<int>())).Callback(() => { }); ;
        }

        [TestCase("SerialDisk.tos","SERIALDI.TOS")]
        [TestCase("Hello There.mpeg", "HELLO_TH.MPE")]
        [TestCase(@"*+,/:;<=.ווו", "________.___")]
        [TestCase(@">?\[]|.ררר", "______.___")]
        public void CreateShortFileNameFromLongFileName(string longFileName, string expectedShortFileName)
        {
            var shortFileName =_disk.Object.FatCreateShortFileName(longFileName);

            Assert.AreEqual(expectedShortFileName, shortFileName);
        }

    }
}