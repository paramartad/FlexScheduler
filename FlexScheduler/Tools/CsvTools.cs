using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using FlexScheduler.Core;
using FlexScheduler.Model;

namespace FlexScheduler.Tools
{
    public static class CsvTools
    {
        public static IList<Employee> GetEmployees(string path)
        {
            TextReader textReader = File.OpenText(path);

            var csvConfig = new CsvConfiguration { HasHeaderRecord = true };

            var csvReader = new CsvReader(textReader, csvConfig);

            var employees = new List<Employee>();

            while (csvReader.Read())
            {
                var personnelId = csvReader.GetField<int>("PersonnelId");
                var name = csvReader.GetField<string>("Name");
                var minHours = csvReader.GetField<int>("MinHours");
                var maxHours = csvReader.GetField<int>("MaxHours");
                var preferredHours = csvReader.GetField<int>("PreferredHours");
                var absMin = csvReader.GetField<bool>("AbsMin");
                var absMax = csvReader.GetField<bool>("AbsMax");

                var emp = new Employee
                {
                    Id = personnelId,
                    Name = name,
                    MinimumHours = minHours,
                    MaximumHours = maxHours,
                    PreferredHours = preferredHours,
                    AbsoluteMinimum = absMin,
                    AbsoluteMaximum = absMax
                };

                employees.Add(emp);
            }

            textReader.Close();

            return employees;
        }

        public static IList<TimeSlot> GetBlankTemplate(string path)
        {
            TextReader textReader = File.OpenText(path);

            var csvConfig = new CsvConfiguration {HasHeaderRecord = true};

            var csvReader = new CsvReader(textReader, csvConfig);

            var template = new List<TimeSlot>();

            TimeSlot previousTimeSlot = null;
            var order = 0;
            while (csvReader.Read())
            {
                var availabilityId = csvReader.GetField<int>("AvailabilityId");

                var startTimeStr = csvReader.GetField<string>("StartTime");
                var endTimeStr = csvReader.GetField<string>("EndTime");

                var startTime = DateTime.Parse(startTimeStr);
                var endTime = DateTime.Parse(endTimeStr);

                var minimum = csvReader.GetField<int>("Minimum");
                var preferred = csvReader.GetField<int>("Preferred");
                var maximum = csvReader.GetField<int>("Maximum");

                var timeSlot = new TimeSlot()
                {
                    Id = order,
                    Order = order,
                    IsOpen = availabilityId > -1,
                    MinimumSlot = minimum,
                    PreferredSlot = preferred,
                    MaximumSlot = maximum,
                    StartTime = startTime,
                    EndTime = endTime,
                    Previous = previousTimeSlot
                };
                
                template.Add(timeSlot);

                if (previousTimeSlot != null)
                {
                    previousTimeSlot.Next = timeSlot;
                }

                previousTimeSlot = timeSlot;
                order++;
            }

            template = template.OrderBy(x => x.StartTime).ToList();

            textReader.Close();

            return template;
        }

        public static IList<TimeSlot> GetAvailabilitiesTemplate(string folderPath, string templatePath, string employeePath)
        {
            var template = GetBlankTemplate(templatePath);
            var employees = GetEmployees(employeePath);
            return GetAvailabilitiesTemplate(folderPath, template, employees);
        }

        public static IList<TimeSlot> GetAvailabilitiesTemplate(string folderPath, IList<TimeSlot> template, IList<Employee> employees)
        {
            var files = Directory.GetFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                TextReader textReader = File.OpenText(file);

                var csvConfig = new CsvConfiguration { HasHeaderRecord = true };

                var csvReader = new CsvReader(textReader, csvConfig);

                while (csvReader.Read())
                {
                    var availabilityId = csvReader.GetField<int>("AvailabilityId");
                    if (availabilityId < 1) continue;

                    var startTimeStr = csvReader.GetField<string>("StartTime");
                    var endTimeStr = csvReader.GetField<string>("EndTime");
                    var name = csvReader.GetField<string>("PersonnelName");

                    var startTime = DateTime.Parse(startTimeStr);
                    var endTime = DateTime.Parse(endTimeStr);

                    var timeSlot = template.FirstOrDefault(x => x.StartTime == startTime && x.EndTime == endTime);
                    if (timeSlot == null) continue;

                    var employee = employees.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (employee == null)
                    {
                        employee = new Employee() {Name = name};
                        employees.Add(employee);
                    }

                    var av = new Availability()
                    {
                        Employee = employee,
                        TimeSlot = timeSlot,
                        IsPreferred = availabilityId > 1
                    };

                    employee.Availabilities.Add(av);
                    timeSlot.EmployeeAvailabilities.Add(av);
                }

                textReader.Close();
            }

            return template;
        }

        public static void GenerateScheduleToCsv(IList<TimeSlot> schedule, string path)
        {
            var directoryName = Path.GetDirectoryName(path);
            if (directoryName == null) return;

            Directory.CreateDirectory(directoryName);
            TextWriter textWriter = File.CreateText(path);

            var csvConfig = new CsvConfiguration { HasHeaderRecord = true};

            var csvWriter = new CsvWriter(textWriter, csvConfig);

            var transformedSchedule = schedule.Select(x => new ScheduleCsvOutputRow()
            {
                StartTime = x.StartTime,
                IsOpen = x.IsOpen,
                Employee1 = x.Assignments.Count > 0 ? x.Assignments[0].Employee.Name : "",
                Employee2 = x.Assignments.Count > 1 ? x.Assignments[1].Employee.Name : "",
                Employee3 = x.Assignments.Count > 2 ? x.Assignments[2].Employee.Name : "",
            });

            csvWriter.WriteHeader<ScheduleCsvOutputRow>();
            csvWriter.WriteRecords(transformedSchedule);
            textWriter.Flush();
            textWriter.Close();
        }

        public static void GenerateIterationsToCsv(IList<IterationOutputRow> iterations, string path)
        {
            var directoryName = Path.GetDirectoryName(path);
            if (directoryName == null) return;

            Directory.CreateDirectory(directoryName);
            TextWriter textWriter = File.CreateText(path);

            var csvConfig = new CsvConfiguration { HasHeaderRecord = true };

            var csvWriter = new CsvWriter(textWriter, csvConfig);

            csvWriter.WriteHeader<IterationOutputRow>();
            csvWriter.WriteRecords(iterations);
            textWriter.Flush();
            textWriter.Close();
        }

        public static void GenerateTimeSlotToCsv(IList<TimeSlotOutputRow> timeSlotOutput, string path)
        {
            var directoryName = Path.GetDirectoryName(path);
            if (directoryName == null) return;

            Directory.CreateDirectory(directoryName);
            TextWriter textWriter = File.CreateText(path);

            var csvConfig = new CsvConfiguration { HasHeaderRecord = true };

            var csvWriter = new CsvWriter(textWriter, csvConfig);

            csvWriter.WriteHeader<TimeSlotOutputRow>();
            csvWriter.WriteRecords(timeSlotOutput);
            textWriter.Flush();
            textWriter.Close();
        }

        public static void AddSummaryToCsv(SummaryRow row, string path)
        {
            var directoryName = Path.GetDirectoryName(path);
            if (directoryName == null) return;

            var exists = File.Exists(path);

            Directory.CreateDirectory(directoryName);
            TextWriter textWriter = exists ? File.AppendText(path) : File.CreateText(path);

            var csvConfig = new CsvConfiguration { HasHeaderRecord = true };

            var csvWriter = new CsvWriter(textWriter, csvConfig);

            if (!exists)
            {
                csvWriter.WriteHeader<SummaryRow>();
            }
            csvWriter.WriteRecord(row);
            textWriter.Flush();
            textWriter.Close();
        }
    }

    public class ScheduleCsvOutputRow
    {
        public DateTime StartTime { get; set; }
        public bool IsOpen { get; set; }
        public string Employee1 { get; set; }
        public string Employee2 { get; set; }
        public string Employee3 { get; set; }
    }

    public class SummaryRow
    {
        public string RunTimeTicks { get; set; }
        public DateTime RunTime { get; set; }
        public double EmployeesHn { get; set; }
        public double ScheduleHn { get; set; }
        public double TotalHn { get; set; }
        public int Iteration { get; set; }
        public long Time { get; set; }
        public int NumberOfNonContinuousAssignments { get; set; }
        public double AverageEmployeePreferredHoursDiff { get; set; }
        public double AverageEmployeePreferredSlotRatio { get; set; }
        public double TimeSlotPreferredNumberRatio { get; set; }
        public HeuristicsConstants HeuristicsConstants { get; set; }
        public SchedulerSettings SchedulerSettings { get; set; }
    }

    public class IterationOutputRow
    {
        public DateTime RunTime { get; set; }
        public int Iteration { get; set; }
        public double EmployeesHn { get; set; }
        public double ScheduleHn { get; set; }
        public double TotalHn { get; set; }

        //public void DoSOmething()
        //{
        //    textbox2.Text = "some text";
        //    var n = Convert.ToInt32(textbox1.Text);
        //}
    }

    public class TimeSlotOutputRow
    {
        public string RunTimeTicks { get; set; }
        public DateTime RunTime { get; set; }
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public double TotalHn { get; set; }
        public int MinimumEmployees { get; set; }
        public int PreferredEmployees { get; set; }
        public int MaximumEmployees { get; set; }
        public int AssignedEmployees { get; set; }
        public int AssignedEmployeesWithPreferred { get; set; }
        public int AvailableEmployeesWithPreferred { get; set; }
    }
}
