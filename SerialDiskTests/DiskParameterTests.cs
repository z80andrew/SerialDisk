using NUnit.Framework;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Storage;
using System.Linq;
using Moq;

namespace Tests
{
    [TestFixture]
    public class DiskParameterTests
    {
        private DiskParameters diskParams;
        int diskTotalBytes = 25165824; // 24MiB

        [SetUp]
        public void Setup()
        {
            diskParams = new DiskParameters(".", diskTotalBytes);
        }

        [Test]
        public void GetBytesPerSector()
        {
            int bytesPerCluster = diskParams.BytesPerSector;

            Assert.AreEqual(512, bytesPerCluster);
        }

        [Test]
        public void GetBytesPerCluster()
        {
            int bytesPerCluster = diskParams.BytesPerCluster;

            Assert.AreEqual(1024, bytesPerCluster);
        }

        [Test]
        public void GetDiskClusters()
        {
            int diskClusters = diskParams.DiskClusters;

            Assert.AreEqual(diskTotalBytes/1024, diskClusters);
        }

        [Test]
        public void GetFatEntriesPerSector()
        {
            int fatEntriesPerSector = diskParams.FatEntriesPerSector;

            Assert.AreEqual(256, fatEntriesPerSector);
        }

        [Test]
        public void GetSectorsPerFat()
        {
            int sectorsPerFat = diskParams.SectorsPerFat;

            Assert.AreEqual(96, sectorsPerFat);
        }

    }
}