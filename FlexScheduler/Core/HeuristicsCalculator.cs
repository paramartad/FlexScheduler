using System;
using System.Collections.Generic;
using System.Linq;
using FlexScheduler.Model;

namespace FlexScheduler.Core
{
    public class HeuristicsCalculator
    {
        private readonly HeuristicsConstants _heuristicsConstants;

        public HeuristicsCalculator() : this(HeuristicsConstants.GetDefault()){ }
        public HeuristicsCalculator(HeuristicsConstants heuristicsConstants)
        {
            _heuristicsConstants = heuristicsConstants;
        }

        public double Calculate(TimeSlot ts)
        {
            var hc = _heuristicsConstants;

            var hn = ts.Assignments.Sum(assignment => Calculate(assignment, ts));

            if (ts.Assignments.Count == ts.PreferredSlot) hn += hc.TimeSlotPreferredNumberBonus;

            return hn;
        }

        public double Calculate(Availability assignment, TimeSlot ts)
        {
            var hc = _heuristicsConstants;
            var hn = 0d;

            var employee = assignment.Employee;
            if (assignment.IsPreferred) hn += hc.EmployeePreferredSlotBonus;

            var prevTs = ts.Previous;
            var nextTs = ts.Next;
            var hasPrev = prevTs != null && prevTs.IsOpen && prevTs.Assignments.Any(x => x.Employee.Id == employee.Id);
            var hasNext = nextTs != null && nextTs.IsOpen && nextTs.Assignments.Any(x => x.Employee.Id == employee.Id);
            if (hasPrev)
            {
                hn += hc.EmployeeContinuousSlotBonus;
            }
            if (hasNext)
            {
                hn += hc.EmployeeContinuousSlotBonus;
            }
            if (!hasPrev && !hasNext)
            {
                hn -= hc.EmployeeNonContinuousSlotPenalty;
            }

            return hn;
        }

        public ScheduleConfig Calculate(IList<TimeSlot> schedule, IList<Employee> employees)
        {
            var hc = _heuristicsConstants;

            var scheduleConfig = new ScheduleConfig();
            var individualTimeSlotsHn = 0d;

            var summary = new Summary(employees);
            foreach (var ts in schedule.Where(x => x.IsOpen))
            {
                var tsHn = ts.Hn;
                var tsConfig = new TimeSlotConfig()
                {
                    TimeSlotId = ts.Id,
                    AssignedEmployeeIds = ts.Assignments.Select(x => x.Employee.Id).OrderBy(x => x).ToList(),
                    Hn = tsHn
                };
                individualTimeSlotsHn += tsHn;

                foreach (var assignment in ts.Assignments)
                {
                    summary.AddHour(assignment.Employee.Id);
                }

                scheduleConfig.TimeSlotConfigs.Add(tsConfig);
            }

            var hoursHn = 0d;
            var normalizedMax = hc.TotalHoursMaximumNormalizedHn;
            foreach (var emp in employees)
            {
                var empHours = summary.EmployeeHours[emp.Id];
                var empHn = Calculate(emp, empHours);
                var empMaxHn = GetEmployeeMaxHn(emp);

                var normalizedEmpHn = empHn/empMaxHn*normalizedMax;

                summary.EmployeeHns[emp.Id] = normalizedEmpHn;
                summary.EmployeeMaxHns[emp.Id] = empMaxHn;
                hoursHn += empHn;
            }

            scheduleConfig.Summary = summary;
            scheduleConfig.Hn = individualTimeSlotsHn + hoursHn;

            return scheduleConfig;
        }

        public double Calculate(Employee employee, double totalHours)
        {
            var hc = _heuristicsConstants;
            var empHn = 0d;

            if (employee.AbsoluteMaximum && totalHours > employee.MaximumHours) empHn -= hc.TotalHoursAbsoluteMaximumViolationPenalty;
            if (employee.AbsoluteMinimum && totalHours < employee.MinimumHours) empHn -= hc.TotalHoursAbsoluteMinimumViolationPenalty;

            if (employee.PreferredHours > 0)
                empHn += hc.TotalHoursPreferredBonus *
                         (2 -
                          Math.Pow(hc.TotalHoursPreferredBonusExponentBase, Math.Abs(employee.PreferredHours - totalHours)));

            if (employee.MaximumHours > 0 && totalHours <= employee.MaximumHours &&
                (employee.MinimumHours < 0 || employee.MinimumHours <= totalHours)) empHn += hc.TotalHoursMaximumBonus;

            if (employee.MinimumHours > 0)
                empHn += Math.Min(employee.MinimumHours, totalHours) / employee.MinimumHours * hc.TotalHoursMinimumBonus;


            return empHn;
        }


        public double GetEmployeeMaxHn(Employee employee)
        {
            var hc = _heuristicsConstants;
            var maxHn = 0d;

            if (employee.PreferredHours > -1) maxHn += hc.TotalHoursPreferredBonus;
            if (employee.MaximumHours > -1) maxHn += hc.TotalHoursMaximumBonus;
            if (employee.MinimumHours > 1) maxHn += hc.TotalHoursMinimumBonus;

            return maxHn;
        }
    }
}
