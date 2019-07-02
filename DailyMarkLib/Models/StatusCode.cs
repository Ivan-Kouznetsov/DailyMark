

namespace DailyMark.Models
{
    public class StatusCode
    {
        public int Id { get; private set; }
        public string Indicator { get; private set; }
        public string Description { get; private set; }
      
        public bool IsDead {
            get { return Indicator.StartsWith("Dead/"); }
        }

        public bool IsNewApplication
        {
            get { return Description.StartsWith("NEW APPLICATION"); }
        }

        public StatusCode(int id, string indicator, string description) {
            Id = id;
            Indicator = indicator;
            Description = description;
        }
    }
}
