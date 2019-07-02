using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using DailyMark.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using DailyMark.Services;


namespace DailyMark.DAO
{
    public static class UsptoDAO
    {      
        private static readonly HttpClient httpClient = new HttpClient();
        private const string urlDateFormat = "yyyy-MM-dd";
        


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

        public static (List<TrademarkApplication> newApps, List<TrademarkApplication> deadApps) GetDailyTrademarkApplications(DateTime date, List<StatusCode> statusCodes, DateTime earliestFilingDate, int attempts) {
            var urlDownloadTask = GetDailyFileUrl(date);
            urlDownloadTask.Wait();
            string url = urlDownloadTask.Result;

            MemoryStream stream = TryDownloadDailyFileAsStream(url, attempts);

            var result = BdssParser.ParseXML(UnzipSingleFileAsStream(stream), statusCodes, earliestFilingDate);
            stream.Dispose();
            return result;
        }
    }
}
