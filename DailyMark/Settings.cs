using System;
using System.Collections.Generic;
using System.Text;

namespace DailyMark
{
    public enum ReportFormat {
        Json,
        Html
    }

    public class Settings
    {
        public string ReportsDirectory { get; private set; }
        public DateTime LastSuccessfulReportDate { get; set; }
        public DateTime LastDownloadDate { get; set; }
        public DateTime EarliestFilingDate { get; set; }
        public ReportFormat ReportFormat { get; private set; }
        public int DownloadAttempts { get; private set; }

        public Settings(string reportsDirectory,  DateTime lastSuccessfulReportDate, DateTime lastDownloadDate,
                        DateTime earliestFilingDate, ReportFormat reportFormat, int downloadAttempts)
        {
            ReportsDirectory = reportsDirectory;
            LastSuccessfulReportDate = lastSuccessfulReportDate;
            LastDownloadDate = lastDownloadDate;
            EarliestFilingDate = earliestFilingDate;
            ReportFormat = reportFormat;
            DownloadAttempts = downloadAttempts;
        }
    }
}
