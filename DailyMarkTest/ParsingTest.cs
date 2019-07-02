using Microsoft.VisualStudio.TestTools.UnitTesting;
using DailyMark;
using System.IO;
using System;

namespace DailyMarkTest
{
    [TestClass]
    public class ParsingTest
    {
        [TestMethod]
        public void TestXMLParsing()
        { 
            Stream stream =  File.Open("apc190401.xml",FileMode.Open);
            var results = DailyMark.Services.BdssParser.ParseXML(stream, DailyMark.DAO.LocalDAO.StatusCodes, DateTime.MinValue);
            Assert.AreEqual(results.deadApps.Count, 5892);
            Assert.AreEqual(results.newApps.Count, 4354);
        }
    }
}
