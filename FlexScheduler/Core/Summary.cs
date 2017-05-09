using System.Collections.Generic;
using System.Linq;
using FlexScheduler.Model;

namespace FlexScheduler.Core
{
    public class Summary
    {
        public IDictionary<int, int> EmployeeHours { get; set; }
        public IDictionary<int, double> EmployeeHns { get; set; }
        public IDictionary<int, double> EmployeeMaxHns { get; set; }

        public Summary(IList<Employee> employees)
        {
            EmployeeHours = employees.ToDictionary(x => x.Id, x => 0);
            EmployeeHns = employees.ToDictionary(x => x.Id, x => 0d);
            EmployeeMaxHns = employees.ToDictionary(x => x.Id, x => 0d);
        }

        public void AddHour(int employeeId)
        {
            AddHour(employeeId, 1);
        }

        public void DeductHour(int employeeId)
        {
            AddHour(employeeId, -1);
        }

        public void AddHour(int employeeId, int hours)
        {
            EmployeeHours[employeeId] += hours;
        }
    }
}
