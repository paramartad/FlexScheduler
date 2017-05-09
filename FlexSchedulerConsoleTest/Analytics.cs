using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlexScheduler.Core;
using FlexScheduler.Model;

namespace FlexSchedulerConsoleTest
{
    public class Analytics
    {
        public static int GetNumberOfNonContinuousAssignments(IList<TimeSlot> schedule)
        {
            var nonContinuousAssignment = 0;

            foreach (var ts in schedule.Where(x => x.IsOpen))
            {
                var prevTs = ts.Previous;
                var nextTs = ts.Next;
                foreach (var assignment in ts.Assignments)
                {
                    var employeeId = assignment.Employee.Id;
                    var hasPrev = prevTs != null && prevTs.IsOpen && prevTs.Assignments.Any(x => x.Employee.Id == employeeId);
                    var hasNext = nextTs != null && nextTs.IsOpen && nextTs.Assignments.Any(x => x.Employee.Id == employeeId);

                    if (!hasPrev && !hasNext)
                        nonContinuousAssignment++;
                }
            }

            return nonContinuousAssignment;
        }

        public static double GetAverageEmployeePreferredHoursDiff(ScheduleConfig config, IList<Employee> employees)
        {
            var differences = employees.Select(emp =>
            {
                var empTotalHours = config.Summary.EmployeeHours[emp.Id];

                if (emp.PreferredHours > 0)
                {
                    return Math.Abs(empTotalHours - emp.PreferredHours);
                }
                if (emp.MaximumHours > 0 && empTotalHours > emp.MaximumHours)
                {
                    return empTotalHours - emp.MaximumHours;
                }
                if (emp.MinimumHours > 0 && empTotalHours < emp.MinimumHours)
                {
                    return emp.MinimumHours - empTotalHours;
                }
                return 0;
            }).ToList();

            return differences.Average();
        }

        public static double GetAverageEmployeePreferredSlotRatio(IList<Employee> employees, IList<TimeSlot> schedule)
        {
            var allAssignments = schedule.SelectMany(x => x.Assignments).ToList();
            var ratios = employees.Select(emp =>
            {
                var assignedSlots = allAssignments.Where(x => x.Employee.Id == emp.Id).ToList();
                var preferredAssignedSlots = assignedSlots.Where(x => x.IsPreferred);

                return (double) preferredAssignedSlots.Count()/assignedSlots.Count;
            });

            return ratios.Average();
        }

        public static double GetTimeSlotPreferredNumberRatio(IList<TimeSlot> schedule)
        {
            var openTs = schedule.Where(x => x.IsOpen).ToList();
            var nonPreferredNumberTs = openTs.Where(x => x.PreferredSlot != x.Assignments.Count);

            return (double) nonPreferredNumberTs.Count()/openTs.Count;
        }
    }
}
