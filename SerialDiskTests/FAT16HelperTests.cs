using NUnit.Framework;
using AtariST.SerialDisk.Utilities;
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
    }
}