using System;
using System.Collections.Generic;
using System.Linq;

namespace FlexScheduler.Model
{
    public class TimeSlot
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public bool IsOpen { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public TimeSlot Previous { get; set; }
        public TimeSlot Next { get; set; }

        public DayOfWeek Day
        {
            get { return StartTime.DayOfWeek; }
        }
        
        public int MinimumSlot { get; set; }
        public int PreferredSlot { get; set; }
        public int MaximumSlot { get; set; }

        public int PossibleCombinationId { get; set; }

        public List<Availability> Assignments
        {
            get { return PossibleCombinations[PossibleCombinationId].ToList(); }
        }

        public IList<Availability> EmployeeAvailabilities { get; set; } = new List<Availability>();
        public List<List<Availability>> PossibleCombinations { get; set; }

        private string _hnAddress;
        private double _hn;
        public double Hn
        {
            get
            {
                var currentAddress = GetAddress();
                if (_hnAddress == currentAddress) return _hn;

                _hn = GetHn(this);
                _hnAddress = currentAddress;

                return _hn;
            }
        }

        public Func<TimeSlot, double> GetHn { get; set; }

        protected string GetAddress()
        {
            var assignedAddress = string.Join("-", Assignments.Select(x => x.Employee.Id).OrderBy(x => x));
            return string.Format($"{Order}-{assignedAddress}");
        }
    }
}
