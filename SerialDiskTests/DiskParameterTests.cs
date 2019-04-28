using NUnit.Framework;
using AtariST.SerialDisk.Storage;
using AtariST.SerialDisk.Models;
using static AtariST.SerialDisk.Common.Constants;

namespace Tests
{
    [TestFixture]
    public class DiskParameterTests
    {
        private DiskParameters diskParams;

        public AtariDiskSettings _atariDiskSettings;

        [SetUp]
        public void Setup()
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = 32,
                DiskPartitionType = PartitionType.GEM,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings);
        }

        [TestCase(16, 512, PartitionType.GEM)]
        [TestCase(32, 512, PartitionType.GEM)]
        [TestCase(64, 1024, PartitionType.BGM)]
        [TestCase(128, 2048, PartitionType.BGM)]
        [TestCase(256, 4096, PartitionType.BGM)]
        [TestCase(512, 8192, PartitionType.BGM)]
        public void GetBytesPerSector(int diskSizeMiB, int expectedBytesPerSector, PartitionType partitionType)
        {
            diskParams.Type = partitionType;
            diskParams.DiskTotalBytes = diskSizeMiB * 1024 * 1024;

            int bytesPerSector = diskParams.BytesPerSector;

            Assert.AreEqual(expectedBytesPerSector, bytesPerSector);
        }

        [TestCase(16, 1024, PartitionType.GEM)]
        [TestCase(32, 1024, PartitionType.GEM)]
        [TestCase(64, 2048, PartitionType.BGM)]
        [TestCase(128, 4096, PartitionType.BGM)]
        [TestCase(256, 8192, PartitionType.BGM)]
        [TestCase(512, 16384, PartitionType.BGM)]
        public void GetBytesPerCluster(int diskSizeMiB, int expectedBytesPerCluster, PartitionType partitionType)
        {
            diskParams.Type = partitionType;
            diskParams.DiskTotalBytes = diskSizeMiB * 1024 * 1024;

            int bytesPerCluster = diskParams.BytesPerCluster;

            Assert.AreEqual(expectedBytesPerCluster, bytesPerCluster);
        }

        [TestCase(16, 0x4000, PartitionType.GEM)]
        [TestCase(32, 0x7FFF, PartitionType.GEM)]
        [TestCase(64, 0x8000, PartitionType.BGM)]
        [TestCase(128, 0x8000, PartitionType.BGM)]
        [TestCase(256, 0x8000, PartitionType.BGM)]
        [TestCase(512, 0x8000, PartitionType.BGM)]
        public void GetDiskClusters(int diskSizeMiB, int expectedClusters, PartitionType partitionType)
        {
            diskParams.Type = partitionType;
            diskParams.DiskTotalBytes = diskSizeMiB * 1024 * 1024;

            int diskClusters = diskParams.DiskClusters;

            Assert.AreEqual(expectedClusters, diskClusters);
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

            Assert.AreEqual(128, sectorsPerFat);
        }

    }
}