using System;
using System.Collections.Generic;
using System.Text;
using DailyMark.Models;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Globalization;

namespace DailyMark.Services
{
    public static class BdssParser
    {
        private const string xmlDateFormat = "yyyyMMdd";

        private static bool CheckCaseFileStatus(XElement e, List<int> statusCodeIds)
        {
            if (e.Element("status-code") == null) return false;
            int code = (int)e.Element("status-code");

            return statusCodeIds.Contains(code);
        }

        private static bool IsFilingDateTimely(XElement e, DateTime earliestFilingDate)
        {            
            if (e.Element("filing-date") == null) return false;
            DateTime date = DateTime.ParseExact((string)e.Element("filing-date"), xmlDateFormat, CultureInfo.InvariantCulture);
            return date.Date >= earliestFilingDate.Date;
        }

        public static (List<TrademarkApplication> newApps, List<TrademarkApplication> deadApps) ParseXML(Stream s, List<StatusCode> statusCodes, DateTime earliestFilingDate)
        {
            List<StatusCode> searchStatusCodes = statusCodes.FindAll(x => x.IsNewApplication | x.IsDead);
            List<int> searchStatusCodeIds = new List<int>();

            foreach (StatusCode statusCode in searchStatusCodes) {
                searchStatusCodeIds.Add(statusCode.Id);
            }


            XElement xElement = XElement.Load(s);
            IEnumerable<XElement> searchResults = from case_file in xElement.Descendants("case-file")
                                                  where (case_file.Element("case-file-header") != null &&
                                                         case_file.Element("case-file-header").Element("mark-identification") != null &&
                                                         IsFilingDateTimely(case_file.Element("case-file-header"), earliestFilingDate) &&
                                                         CheckCaseFileStatus(case_file.Element("case-file-header"), searchStatusCodeIds))
                                                  select case_file;

            IEnumerable<XElement> dateElement = from creation_datetime in xElement.Descendants("creation-datetime")
                                                select creation_datetime;

            DateTime dateTimeOfFile = DateTime.ParseExact(((string)dateElement.First()).Substring(0,8), xmlDateFormat, CultureInfo.InvariantCulture);

            List<TrademarkApplication> newApplications = new List<TrademarkApplication>();
            List<TrademarkApplication> deadApplications = new List<TrademarkApplication>();

            foreach (XElement e in searchResults)
            {               
                TrademarkApplication app =  new TrademarkApplication(DateTime.ParseExact((string)e.Element("case-file-header").Element("filing-date"), xmlDateFormat, CultureInfo.InvariantCulture),
                                                                     dateTimeOfFile,
                                                                     (int)e.Element("serial-number"),
                                                                     (string)e.Element("case-file-header").Element("mark-identification"),
                                                                     statusCodes.Find(status => status.Id== (int)e.Element("case-file-header").Element("status-code")));
                if (app.StatusCode.IsDead) {
                    deadApplications.Add(app);
                } else if (app.StatusCode.IsNewApplication) {
                    newApplications.Add(app);
                }
            }

            s.Dispose();
            return (newApplications, deadApplications);
        }
    }
}
