using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.Models;
using Z80andrew.SerialDisk.Storage;
using Moq;
using NUnit.Framework;
using System;
using static Z80andrew.SerialDisk.Common.Constants;

namespace Z80andrew.SerialDisk.Tests
{
    [TestFixture]
    public class DiskParameterTests
    {
        private DiskParameters diskParams;

        public AtariDiskSettings _atariDiskSettings;
        private Mock<ILogger> _logger;

        [SetUp]
        public void Setup()
        {
            _logger = new Mock<ILogger>();
        }

        [TestCase(16, 512, TOSVersion.TOS104)]
        [TestCase(32, 512, TOSVersion.TOS104)]
        [TestCase(64, 1024, TOSVersion.TOS104)]
        [TestCase(128, 2048, TOSVersion.TOS104)]
        [TestCase(256, 4096, TOSVersion.TOS104)]
        [TestCase(512, 8192, TOSVersion.TOS104)]
        public void GetBytesPerSector(int diskSizeMiB, int expectedBytesPerSector, TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings, _logger.Object);

            int bytesPerSector = diskParams.BytesPerSector;

            Assert.AreEqual(expectedBytesPerSector, bytesPerSector);
        }

        [TestCase(16, 1024, TOSVersion.TOS104)]
        [TestCase(32, 1024, TOSVersion.TOS104)]
        [TestCase(64, 2048, TOSVersion.TOS104)]
        [TestCase(128, 4096, TOSVersion.TOS104)]
        [TestCase(256, 8192, TOSVersion.TOS104)]
        [TestCase(512, 16384, TOSVersion.TOS104)]
        public void GetBytesPerCluster(int diskSizeMiB, int expectedBytesPerCluster, TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings, _logger.Object);

            int bytesPerCluster = diskParams.BytesPerCluster;

            Assert.AreEqual(expectedBytesPerCluster, bytesPerCluster);
        }

        [TestCase(16, 0x3FFF, TOSVersion.TOS100)]
        [TestCase(16, 0x4000, TOSVersion.TOS104)]
        [TestCase(32, 0x7FFF, TOSVersion.TOS104)]
        [TestCase(64, 0x7FFF, TOSVersion.TOS104)]
        [TestCase(64, 0x3FFF, TOSVersion.TOS100)]
        [TestCase(128, 0x7FFF, TOSVersion.TOS104)]
        [TestCase(128, 0x3FFF, TOSVersion.TOS100)]
        [TestCase(256, 0x7FFF, TOSVersion.TOS104)]
        [TestCase(256, 0x3FFF, TOSVersion.TOS100)]
        [TestCase(512, 0x7FFF, TOSVersion.TOS104)]
        public void GetDiskClusters(int diskSizeMiB, int expectedClusters, TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings, _logger.Object);

            int diskClusters = diskParams.DiskClusters;

            Assert.AreEqual(expectedClusters, diskClusters);
        }

        [TestCase(512, TOSVersion.TOS100)]
        [TestCase(1024, TOSVersion.TOS104)]
        public void InvalidDiskSize(int diskSizeMiB, TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                RootDirectorySectors = 8
            };

            Assert.That(() => { new DiskParameters(".", diskSettings, _logger.Object); },
                Throws.TypeOf<ArgumentException>()
                    .With.Message.Contains($"{diskSizeMiB}MiB is larger than the maximum possible disk size"));
        }

        [TestCase(16, 256, TOSVersion.TOS104)]
        [TestCase(32, 256, TOSVersion.TOS104)]
        [TestCase(64, 512, TOSVersion.TOS104)]
        [TestCase(128, 1024, TOSVersion.TOS104)]
        [TestCase(256, 2048, TOSVersion.TOS104)]
        [TestCase(512, 4096, TOSVersion.TOS104)]
        public void GetFatEntriesPerSector(int diskSizeMiB, int expectedClusters, TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings, _logger.Object);

            int fatEntriesPerSector = diskParams.FatEntriesPerSector;

            Assert.AreEqual(expectedClusters, fatEntriesPerSector);
        }

        [TestCase(16, 64, TOSVersion.TOS104)]
        [TestCase(32, 128, TOSVersion.TOS104)]
        [TestCase(64, 64, TOSVersion.TOS104)]
        [TestCase(128, 32, TOSVersion.TOS104)]
        [TestCase(256, 16, TOSVersion.TOS104)]
        [TestCase(512, 8, TOSVersion.TOS104)]
        public void GetSectorsPerFat(int diskSizeMiB, int expectedSectorsPerFat, TOSVersion tosVersion)
        {
            AtariDiskSettings diskSettings = new AtariDiskSettings()
            {
                DiskSizeMiB = diskSizeMiB,
                DiskTOSCompatibility = tosVersion,
                RootDirectorySectors = 8
            };

            diskParams = new DiskParameters(".", diskSettings, _logger.Object);

            int sectorsPerFat = diskParams.SectorsPerFat;

            Assert.AreEqual(expectedSectorsPerFat, sectorsPerFat);
        }

    }
}