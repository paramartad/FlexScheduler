using System.Collections.Generic;

namespace FlexScheduler.Model
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MinimumHours { get; set; }
        public int MaximumHours { get; set; }
        public int PreferredHours { get; set; }
        public bool AbsoluteMinimum { get; set; }
        public bool AbsoluteMaximum { get; set; }
        public IList<Availability> Availabilities { get; set; } = new List<Availability>();
    }
}
