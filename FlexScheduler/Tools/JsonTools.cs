using System;
using System.Collections.Generic;
using System.Linq;
using FlexScheduler.Core;
using FlexScheduler.Model;
using Newtonsoft.Json;

namespace FlexScheduler.Tools
{
    public static class JsonTools
    {
        public static string GenerateScheduleToJson(string schedule, string employees, string availabilities)
        {
            var scheduleObj = JsonConvert.DeserializeObject<List<JsonClasses.TimeSlot>>(schedule);
            var employeesObj = JsonConvert.DeserializeObject<List<JsonClasses.Employee>>(employees);
            var availabilitiesObj = JsonConvert.DeserializeObject<List<JsonClasses.Availability>>(availabilities);

            return GenerateScheduleToJson(scheduleObj, employeesObj, availabilitiesObj, new JsonClasses.InputOptions());
        }

        public static string GenerateScheduleToJson(IList<JsonClasses.TimeSlot> schedule,
            IList<JsonClasses.Employee> employees, IList<JsonClasses.Availability> availabilities, JsonClasses.InputOptions options)
        {
            var template = schedule.Select(ToEntity).ToList();
            var employeesList = employees.Select(ToEntity).ToList();

            foreach (var ts in template)
            {
                var tsAvs = availabilities.Where(x => x.TimeslotId == ts.Id);
                ts.EmployeeAvailabilities = tsAvs.Select(tsAv =>
                {
                    var emp = employeesList.FirstOrDefault(x => x.Id == tsAv.EmployeeId);
                    if (emp == null) return null;

                    return new Availability
                    {
                        TimeSlot = ts,
                        Employee = emp,
                        IsPreferred = tsAv.IsPreferred
                    };
                }).ToList();
            }

            var scheduler = new Scheduler(employeesList, template);

            IList<JsonClasses.TimeSlotResult> result = null;
            if (options.EnableProd)
            {
                scheduler.Start(null, null, (iterationCount, finalSchedule, finalConfig) =>
                {
                    result = finalConfig.TimeSlotConfigs.Select(ToJson).ToList();
                });
            }
            else
            {
                var config = scheduler.GetRandomInitialScheduleConfig(template, employeesList);
                result = config.TimeSlotConfigs.Select(ToJson).ToList();
            }

            return result != null ? JsonConvert.SerializeObject(result) : null;
        }

        public static Employee ToEntity(this JsonClasses.Employee empJson)
        {
            var ent = new Employee
            {
                Id = empJson.Id,
                Name = empJson.Name,
                MinimumHours = empJson.MinimumHours,
                MaximumHours = empJson.MaximumHours,
                PreferredHours = empJson.PreferredHours,
                AbsoluteMinimum = empJson.AbsoluteMinimum,
                AbsoluteMaximum = empJson.AbsoluteMaximum
            };

            return ent;
        }

        public static TimeSlot ToEntity(this JsonClasses.TimeSlot tsJson)
        {
            var ent = new TimeSlot()
            {
                Id = tsJson.Id,
                Order = tsJson.Order,
                IsOpen = tsJson.IsOpen,
                StartTime = tsJson.StartTime,
                EndTime = tsJson.EndTime,
                MinimumSlot = tsJson.MinimumSlot,
                MaximumSlot = tsJson.MaximumSlot,
                PreferredSlot = tsJson.PreferredSlot
            };

            return ent;
        }

        public static JsonClasses.TimeSlot ToJson(this TimeSlot ts)
        {
            return new JsonClasses.TimeSlot()
            {
                Id = ts.Id,
                Order = ts.Order,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                IsOpen = ts.IsOpen,
                MinimumSlot = ts.MinimumSlot,
                MaximumSlot = ts.MaximumSlot,
                PreferredSlot = ts.PreferredSlot
            };
        }

        public static JsonClasses.Employee ToJson(this Employee emp)
        {
            return new JsonClasses.Employee()
            {
                Id = emp.Id,
                Name = emp.Name,
                MinimumHours = emp.MinimumHours,
                MaximumHours = emp.MaximumHours,
                PreferredHours = emp.MaximumHours,
                AbsoluteMinimum = emp.AbsoluteMinimum,
                AbsoluteMaximum = emp.AbsoluteMaximum
            };
        }

        public static JsonClasses.TimeSlotResult ToJson(this TimeSlotConfig tsConfig)
        {
            return new JsonClasses.TimeSlotResult()
            {
                TimeslotId = tsConfig.TimeSlotId,
                EmployeeIds = tsConfig.AssignedEmployeeIds
            };
        }

        public static JsonClasses.Availability ToJson(this Availability availability)
        {
            return new JsonClasses.Availability()
            {
                TimeslotId = availability.TimeSlot.Id,
                EmployeeId = availability.Employee.Id,
                IsPreferred = availability.IsPreferred
            };
        }

        public static string GetAvailabilitiesJsonStringFromTemplate(IList<TimeSlot> template)
        {
            var availabilities = template.SelectMany(x => x.EmployeeAvailabilities).Select(ToJson).ToList();
            return JsonConvert.SerializeObject(availabilities);
        }

        public static string GetJsonString(this IList<TimeSlot> template)
        {
            var jsonString = JsonConvert.SerializeObject(template.Select(ToJson).ToList());
            return jsonString;
        }

        public static string GetJsonString(this IList<Employee> employees)
        {
            var jsonString = JsonConvert.SerializeObject(employees.Select(ToJson).ToList());
            return jsonString;
        }
    }

    public class JsonClasses
    {
        public class Input
        {
            public List<JsonClasses.TimeSlot> Template { get; set; }
            public List<JsonClasses.Employee> Employees { get; set; }
            public List<JsonClasses.Availability> Availabilities { get; set; }
            public InputOptions Options { get; set; } = new InputOptions();
        }

        public class InputOptions
        {
            public bool EnableProd { get; set; }
        }
        public class Availability
        {
            public int EmployeeId { get; set; }
            public int TimeslotId { get; set; }
            public bool IsPreferred { get; set; }
        }

        public class Employee
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int MinimumHours { get; set; }
            public int MaximumHours { get; set; }
            public int PreferredHours { get; set; }
            public bool AbsoluteMaximum { get; set; }
            public bool AbsoluteMinimum { get; set; }
        }

        public class TimeSlot
        {
            public int Id { get; set; }
            public int Order { get; set; }
            public bool IsOpen { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }

            public int MinimumSlot { get; set; }
            public int PreferredSlot { get; set; }
            public int MaximumSlot { get; set; }
        }

        public class TimeSlotResult
        {
            public int TimeslotId { get; set; }
            public IList<int> EmployeeIds { get; set; }
        }
    }
}
