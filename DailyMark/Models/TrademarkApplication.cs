using System;
using System.Collections.Generic;
using System.Text;

namespace DailyMark.Models
{
    public class TrademarkApplication
    {
        public DateTime FilingDate { get; private set; }
        public DateTime DateAdded { get; private set; }
        public int SerialNumber { get; private set; }
        public string MarkLiteralElements { get; private set; }               
        public StatusCode StatusCode { get; private set; }

        public TrademarkApplication(DateTime filingDate, int serialNumber, string markLiteralElements, int statusCode) {
            FilingDate = filingDate;
            DateAdded = DateTime.Now;
            SerialNumber = serialNumber;
            MarkLiteralElements = markLiteralElements;
            StatusCode = new StatusCode(statusCode,null,null);
        }

        public TrademarkApplication(DateTime filingDate, DateTime dateAdded, int serialNumber, string markLiteralElements, StatusCode statusCode)
        {
            FilingDate = filingDate;
            DateAdded = dateAdded;
            SerialNumber = serialNumber;
            MarkLiteralElements = markLiteralElements;
            StatusCode = statusCode;
        }

        
    }
}
