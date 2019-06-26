using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using DailyMark.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;

namespace DailyMark.DAO
{
    public static class UsptoDAO
    {      
        private static readonly HttpClient httpClient = new HttpClient();
        private const string urlDateFormat = "yyyy-MM-dd";
        private const string xmlDateFormat = "yyyyMMdd";


        static UsptoDAO() {
            httpClient.Timeout = new TimeSpan(0, 30, 0);
        }
        private static async Task<string> GetDailyFileUrl(DateTime date) {
            string result = null; ;

            string url = @"https://bulkdata.uspto.gov/BDSS-API/products/TRTDXFAP?fromDate=" + date.ToString(urlDateFormat) + "&toDate=" + date.ToString(urlDateFormat);

            string response = await httpClient.GetStringAsync(url);

            dynamic parsedResponse = JsonConvert.DeserializeObject(response);

            if (parsedResponse.productFiles != null)
            {
                result = parsedResponse.productFiles[0].fileDownloadUrl.ToString();
            }
            return result;
        }

        private static async Task<MemoryStream> GetDailyFileAsStream(string url) {
            MemoryStream memoryStream = new MemoryStream();

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await response.Content.CopyToAsync(memoryStream);
            return memoryStream;            
        }

        private static Stream UnzipSingleFileAsStream(MemoryStream s) {
            ZipArchive zipArchive = new ZipArchive(s,ZipArchiveMode.Read);
            return zipArchive.Entries[0].Open();
        }
        private static bool CheckCaseFileStatus(XElement e, List<int> statusCodeIds) {
            //.Element("status-code")
            if (e == null) return false;
            if (e.Element("status-code")==null) return false;
            int code = (int)e.Element("status-code");

            return statusCodeIds.Contains(code);
        }

        private static bool IsFilingDateTimely(XElement e, DateTime earliestFilingDate) {
            if (e == null) return false;
            if (e.Element("filing-date") == null) return false;
            DateTime date = DateTime.ParseExact((string)e.Element("filing-date"), xmlDateFormat, CultureInfo.InvariantCulture);
            return date.Date >= earliestFilingDate.Date;
        }
    
        private static List<TrademarkApplication> ParseXML(Stream s, List<int> statusCodes, DateTime earliestFilingDate) {
         
            XElement xElement = XElement.Load(s);
            IEnumerable<XElement> searchResults = from case_file in xElement.Descendants("case-file")
                                                  where CheckCaseFileStatus(case_file.Element("case-file-header"), statusCodes) &&
                                                        IsFilingDateTimely(case_file.Element("case-file-header"), earliestFilingDate)
                                                  select case_file;
            
            List<TrademarkApplication> results = new List<TrademarkApplication>();

            foreach (XElement e in searchResults)
            {
               results.Add(new TrademarkApplication(DateTime.ParseExact((string)e.Element("case-file-header").Element("filing-date"), xmlDateFormat, CultureInfo.InvariantCulture), (int)e.Element("serial-number"), (string)e.Element("case-file-header").Element("mark-identification"), (int)e.Element("case-file-header").Element("status-code")));
            }            
           
           
            return results;
        }


        private static MemoryStream TryDownloadDailyFileAsStream(string url, int attempts) {
            if (attempts == 0) throw new TimeoutException("Download failed, make sure you're connected to the internet and that the USPTO's servers are not down for maintenance and try again.");
            try
            {
                var fileDownloadTask = GetDailyFileAsStream(url);
                fileDownloadTask.Wait();
                MemoryStream stream = fileDownloadTask.Result;
                return stream;
            }
            catch {
                //here the wrong exception (TaskCanceledException) is thrown by httpclient, despite the error (most likely) being a timeout
                Task.Delay(5000).Wait();
               
                return TryDownloadDailyFileAsStream(url, attempts - 1);
            }

        }

        public static List<TrademarkApplication> GetDailyTrademarkApplications(DateTime date, List<int> statusCodeIds, DateTime earliestFilingDate, int attempts) {
            var urlDownloadTask = GetDailyFileUrl(date);
            urlDownloadTask.Wait();
            string url = urlDownloadTask.Result;

            MemoryStream stream = TryDownloadDailyFileAsStream(url, attempts);

            List <TrademarkApplication> result = ParseXML(UnzipSingleFileAsStream(stream), statusCodeIds, earliestFilingDate);
            stream.Dispose();
            return result;

        }
    }
}
