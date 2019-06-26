using System;
using System.Collections.Generic;
using DailyMark.Models;


namespace DailyMark.DAO
{
    public class SearchResult
    {
        public string Name { get; private set; }
        public string SearchPattern { get; private set; }
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public List<TrademarkApplication> TrademarkApplications { get; private set; }

        public SearchResult(string name, string searchPattern, DateTime from, DateTime to, List<TrademarkApplication> trademarkApplications) {
            Name = name;
            SearchPattern = searchPattern;
            From = from;
            To = to;
            TrademarkApplications = trademarkApplications;
        }

    }
}
