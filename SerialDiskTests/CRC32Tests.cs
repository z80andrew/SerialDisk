using NUnit.Framework;
using AtariST.SerialDisk.Utilities;
using System;

namespace Tests
{
    [TestFixture]
    public class CRC32Tests
    {
        private byte[] _dataBuffer;

        [SetUp]
        public void Setup()
        {
            _dataBuffer = new byte[] { 1, 2, 3, 4, 5 };
        }

        [Test]
        public void CalculateCRC32ForTestData()
        {
            Assert.AreEqual(2778722335, CRC32.CalculateCRC32(_dataBuffer));
        }
    }
}