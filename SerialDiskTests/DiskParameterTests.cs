using NUnit.Framework;
using AtariST.SerialDisk.Storage;
using AtariST.SerialDisk.Models;
using static AtariST.SerialDisk.Common.Constants;
using System;

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

        }

        [TestCase(16, 512, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(32, 512, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(64, 1024, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(128, 2048, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(256, 4096, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(512, 8192, PartitionType.BGM, TOSVersion.TOS104)]
        public void GetBytesPerSector(int diskSizeMiB, int expectedBytesPerSector, PartitionType partitionType,
            TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                DiskPartitionType = partitionType,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings);

            int bytesPerSector = diskParams.BytesPerSector;

            Assert.AreEqual(expectedBytesPerSector, bytesPerSector);
        }

        [TestCase(16, 1024, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(32, 1024, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(64, 2048, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(128, 4096, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(256, 8192, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(512, 16384, PartitionType.BGM, TOSVersion.TOS104)]
        public void GetBytesPerCluster(int diskSizeMiB, int expectedBytesPerCluster, PartitionType partitionType,
            TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                DiskPartitionType = partitionType,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings);

            int bytesPerCluster = diskParams.BytesPerCluster;

            Assert.AreEqual(expectedBytesPerCluster, bytesPerCluster);
        }

        [TestCase(16, 0x4000, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(32, 0x7FFF, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(64, 0x7FFF, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(128, 0x7FFF, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(256, 0x7FFF, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(512, 0x7FFF, PartitionType.BGM, TOSVersion.TOS104)]
        public void GetDiskClusters(int diskSizeMiB, int expectedClusters, PartitionType partitionType,
            TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                DiskPartitionType = partitionType,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings);

            int diskClusters = diskParams.DiskClusters;

            Assert.AreEqual(expectedClusters, diskClusters);
        }

        [TestCase(32, PartitionType.GEM, TOSVersion.TOS100)]
        [TestCase(64, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(1024, PartitionType.BGM, TOSVersion.TOS104)]
        public void InvalidDiskSize(int diskSizeMiB, PartitionType partitionType,
            TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                DiskPartitionType = partitionType,
                RootDirectorySectors = 8
            };

            Assert.That(() => { new DiskParameters(".", diskSettings); },
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains($"{diskSizeMiB}MiB is larger than the maximum possible disk size"));
        }

        [TestCase(16, 256, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(32, 256, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(64, 512, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(128, 1024, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(256, 2048, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(512, 4096, PartitionType.BGM, TOSVersion.TOS104)]
        public void GetFatEntriesPerSector(int diskSizeMiB, int expectedClusters, PartitionType partitionType,
            TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                DiskPartitionType = partitionType,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings);

            int fatEntriesPerSector = diskParams.FatEntriesPerSector;

            Assert.AreEqual(expectedClusters, fatEntriesPerSector);
        }

        [TestCase(16, 64, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(32, 128, PartitionType.GEM, TOSVersion.TOS104)]
        [TestCase(64, 64, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(128, 32, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(256, 16, PartitionType.BGM, TOSVersion.TOS104)]
        [TestCase(512, 8, PartitionType.BGM, TOSVersion.TOS104)]
        public void GetSectorsPerFat(int diskSizeMiB, int expectedSectorsPerFat, PartitionType partitionType,
            TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                DiskPartitionType = partitionType,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings);

            int sectorsPerFat = diskParams.SectorsPerFat;

            Assert.AreEqual(expectedSectorsPerFat, sectorsPerFat);
        }

    }
}