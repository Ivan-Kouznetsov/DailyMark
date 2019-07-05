using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using DailyMark.Models;
using System.IO;
using DailyMark.DAO;

namespace DailyMark.Reports
{
    public static class ReportBuilder
    {
     
        private static string CreateReportName(ReportFormat reportFormat)
        {
            const string name = "DailyMarkReport_";
            string extention = "";

            switch (reportFormat) {
                case ReportFormat.Html: extention = ".html";
                    break;
                case ReportFormat.Json: extention = ".json";
                    break;
            }

            return name + DateTime.Now.ToString("MMM_dd_yyyy_HH_mm_ss") + extention;
        }

        private static string SaveReportAsJson(Settings settings, List<SearchResult> searchResults)
        {
            string json = JsonConvert.SerializeObject(searchResults);
            string filename = CreateReportName(settings.ReportFormat);
            File.WriteAllText(settings.ReportsDirectory + filename, json);
            return filename;
        }
               
        private static string HtmlTableRow(TrademarkApplication t)
        {
            return String.Format(@"<tr><td><a href=""http://tsdr.uspto.gov/#caseNumber={0}&caseSearchType=US_APPLICATION&caseType=SERIAL_NO&searchType=statusSearch"">{0}</a></td><td>{1}</td><td>{2}</td></tr>", t.SerialNumber, t.FilingDate.ToString("MMM dd, yyyy"),t.MarkLiteralElements);
        }

        private static string RenderReportAsHtml(List<SearchResult> searchResults)
        {
            StringBuilder stringBuilder = new StringBuilder();

            string title = "DailyMark Report for " + searchResults[0].From.ToString("MMM dd yyyy") + " - " + searchResults[0].To.ToString("MMM dd yyyy");

            stringBuilder.AppendFormat(@"<!doctype html><html lang=en><head><meta charset=utf-8><title>{0}</title><link href=""style.css"" rel=""stylesheet"" type=""text/css"" media=""all""></head><body><h1>{0}</h1>", title);

            foreach (SearchResult s in searchResults)
            {                
                stringBuilder.AppendFormat("<h2>Query: {0}</h2>", s.Name);
                if (s.TrademarkApplications.Count > 0)
                {
                    stringBuilder.Append("<table><tr><th>Serial Number</th><th>Filing Date</th><th>Mark Literal Elements</th></tr>");
                    foreach (TrademarkApplication t in s.TrademarkApplications)
                    {
                        stringBuilder.Append(HtmlTableRow(t));
                    }
                    stringBuilder.Append("</table>");
                }
                else {
                    stringBuilder.Append("No results for this query.");
                }                
            }

            stringBuilder.Append("</body></html>");
            return stringBuilder.ToString();
        }

        private static string SaveReportAsHtml(Settings settings, List<SearchResult> searchResults)
        {
            string html = RenderReportAsHtml(searchResults);
            string filename = CreateReportName(settings.ReportFormat);
            File.WriteAllText(settings.ReportsDirectory + Path.DirectorySeparatorChar + filename, html);
            return filename;
        }


        public static string SaveReport(Settings settings, List<SearchResult> searchResults)
        {
            if (searchResults.Count > 0)
            {
                switch (settings.ReportFormat)
                {
                    case ReportFormat.Json: return SaveReportAsJson(settings, searchResults);
                    case ReportFormat.Html: return SaveReportAsHtml(settings, searchResults);
                }
            }
            else {
                throw new ArgumentException("SaveReport: searchResults is empty");
            }
            throw new ArgumentException("SaveReport: Invalid value for ReportFormat");
        }

    }
}
