using System;
using System.Collections.Generic;
using System.Text;

namespace DailyMark.Models
{
    public class TrademarkApplication
    {
        public DateTime FilingDate { get; private set; }
        public DateTime DateAdded { get; private set; }
        public DateTime CaseFileDate { get; private set; }
        public int SerialNumber { get; private set; }
        public string MarkLiteralElements { get; private set; }               
        public StatusCode StatusCode { get; private set; }

        public TrademarkApplication(DateTime filingDate, DateTime caseFileDate, int serialNumber, string markLiteralElements, StatusCode statusCode)
        {
            FilingDate = filingDate;
            DateAdded = DateTime.Now;
            CaseFileDate = caseFileDate;
            SerialNumber = serialNumber;
            MarkLiteralElements = markLiteralElements;
            StatusCode = statusCode;
        }

        public TrademarkApplication(DateTime filingDate, DateTime dateAdded, DateTime caseFileDate, int serialNumber, string markLiteralElements, StatusCode statusCode)
        {
            FilingDate = filingDate;
            DateAdded = dateAdded;
            CaseFileDate = caseFileDate;
            SerialNumber = serialNumber;
            MarkLiteralElements = markLiteralElements;
            StatusCode = statusCode;
        }

        
    }
}
