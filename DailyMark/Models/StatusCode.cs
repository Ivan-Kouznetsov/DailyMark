using System;
using System.Collections.Generic;
using System.Text;

namespace DailyMark.Models
{
    public class StatusCode
    {
        public int Id { get; private set; }
        public string Indicator { get; private set; }
        public string Description { get; private set; }
        public bool PartialState {
            get {
                return Indicator == null || Description==null;
            }
        }
        public bool IsLive {
            get {
                if (PartialState) throw new Exception("This status code object is incomplete, save it and load it for a database to have all values set");
                return Indicator.StartsWith("Live/");
            }
        }

        public bool IsNewApplication
        {
            get
            {
                if (PartialState) throw new Exception("This status code object is incomplete, save it and load it for a database to have all values set");
                return Description.StartsWith("NEW APPLICATION");
            }
        }

        public StatusCode(int id, string indicator, string description) {
            Id = id;
            Indicator = indicator;
            Description = description;
        }
    }
}
