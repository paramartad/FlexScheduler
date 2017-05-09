using System.Collections.Generic;

namespace FlexScheduler.Core
{
    public class ScheduleConfig
    {
        public IList<TimeSlotConfig> TimeSlotConfigs { get; set; } = new List<TimeSlotConfig>();
        public Summary Summary { get; set; }
        public double Hn { get; set; }
    }

    public class TimeSlotConfig
    {
        public int TimeSlotId { get; set; }
        public IList<int> AssignedEmployeeIds { get; set; } = new List<int>();
        public double Hn { get; set; }
    }
}
