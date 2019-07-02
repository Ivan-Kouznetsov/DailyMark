using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyMark;
using System.IO;
using System.IO.Compression;
using System;

namespace DailyMarkTest
{
    [TestClass]
    public class ParsingTest
    {
        [TestMethod]
        public void TestXMLParsing()
        {
            using (Stream zipFileStream = File.Open("apc190401.zip", FileMode.Open))
            {
                ZipArchive zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read);

                var results = DailyMark.Services.BdssParser.ParseXML(zipArchive.Entries[0].Open(), DailyMark.DAO.LocalDAO.StatusCodes, DateTime.MinValue);
                Assert.AreEqual(results.deadApps.Count, 5892);
                Assert.AreEqual(results.newApps.Count, 4354);
            }
        }
    }
}
