using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlexScheduler.Core;
using FlexScheduler.Model;
using FlexScheduler.Tools;

namespace FlexSchedulerConsoleTest
{
    public class Program
    {
        static ReaderWriterLock locker = new ReaderWriterLock();

        static void Main(string[] args)
        {
            var heuristicsConstants = GetHeuristicsConstantFromConfig();
            var schedulerSettings = GetSchedulerSettingsFromConfig();
            
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;

            var epsLevel = Convert.ToInt32(appSettings["Settings.Test.NumberOfLevels.EmployeePreferredSlotBonus"]);
            var encLevel = Convert.ToInt32(appSettings["Settings.Test.NumberOfLevels.EmployeeNonContinuousSlotPenalty"]);
            var ecsLevel = Convert.ToInt32(appSettings["Settings.Test.NumberOfLevels.EmployeeContinuousSlotBonus"]);
            var tpnLevel = Convert.ToInt32(appSettings["Settings.Test.NumberOfLevels.TimeSlotPreferredNumberBonus"]);
            var numberOfReplicates = Convert.ToInt32(appSettings["Settings.Test.NumberOfReplicates"]);
            var totalRuns = epsLevel*encLevel*ecsLevel*tpnLevel*numberOfReplicates;

            var masterTimer = Stopwatch.StartNew();
            var runs = new List<Action>();
            
            var employeePreferredSlotBonus = heuristicsConstants.EmployeePreferredSlotBonus;
            for (var eps = 0; eps < epsLevel; eps++)
            {
                var employeeNonContinuousSlotPenalty = heuristicsConstants.EmployeeNonContinuousSlotPenalty;
                for (var enc = 0; enc < encLevel; enc++)
                {
                    var employeeContinuousSlotBonus = heuristicsConstants.EmployeeContinuousSlotBonus;
                    for (var ecs = 0; ecs < ecsLevel; ecs++)
                    {
                        var timeSlotPreferredNumberBonus = heuristicsConstants.TimeSlotPreferredNumberBonus;
                        for (var tpn = 0; tpn < tpnLevel; tpn++)
                        {
                            var innerHc = heuristicsConstants.Copy();
                            innerHc.EmployeePreferredSlotBonus = employeePreferredSlotBonus;
                            innerHc.EmployeeNonContinuousSlotPenalty = employeeNonContinuousSlotPenalty;
                            innerHc.EmployeeContinuousSlotBonus = employeeContinuousSlotBonus;
                            innerHc.TimeSlotPreferredNumberBonus = timeSlotPreferredNumberBonus;
                            for (var i = 0; i < numberOfReplicates; i++)
                            {
                                runs.Add(() => OneRun(innerHc, schedulerSettings));
                            }

                            timeSlotPreferredNumberBonus += 5;
                        }
                        employeeContinuousSlotBonus += 5;
                    }
                    employeeNonContinuousSlotPenalty += 5;
                }
                employeePreferredSlotBonus += 5;
            }

            var parallelOptions = new ParallelOptions() {MaxDegreeOfParallelism = Environment.ProcessorCount};
            Parallel.Invoke(parallelOptions, runs.ToArray());

            masterTimer.Stop();

            Console.WriteLine("------------------------");
            Console.WriteLine($"Completed {runs.Count} after {masterTimer.ElapsedMilliseconds} ms");
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }

        static void OneRun(HeuristicsConstants heuristicsConstants, SchedulerSettings schedulerSettings)
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;

            var blankTemplateFilePath = appSettings["Input.BlankTemplateFilePath"];
            var employeeInfoFilePath = appSettings["Input.EmployeeInfoFilePath"];
            var availabilityFolderPath = appSettings["Input.AvailabilityFolderPath"];
            var finalScheduleFilePath = appSettings["Output.FinalScheduleFilePath"];
            var iterationsFilePath = appSettings["Output.IterationsFilePath"];
            var timeSlotFilePath = appSettings["Output.TimeSlotFilePath"];
            var summaryFilePath = appSettings["Output.SummaryFilePath"];

            var employees = CsvTools.GetEmployees(employeeInfoFilePath);
            var blankTemplate = CsvTools.GetBlankTemplate(blankTemplateFilePath);
            var template = CsvTools.GetAvailabilitiesTemplate(availabilityFolderPath, blankTemplate, employees);

            var scheduler = new Scheduler(employees, template, heuristicsConstants, schedulerSettings);
            IList<TimeSlot> finalSchedule = null;
            IList<IterationOutputRow> iterationsOutput = new List<IterationOutputRow>();

            var timer = Stopwatch.StartNew();
            scheduler.Start((iteration, config, delta) =>
                {
                    Console.WriteLine("--------------");
                    Console.WriteLine($"Iteration: {iteration}");
                    Console.WriteLine($"Current Hn: {config.Hn:0.00} --- Delta: {delta:0.00}");
                    iterationsOutput.Add(new IterationOutputRow()
                    {
                        Iteration = iteration,
                        EmployeesHn = config.Summary.EmployeeHns.Sum(x => x.Value),
                        ScheduleHn = config.TimeSlotConfigs.Sum(x => x.Hn),
                        TotalHn = config.Hn,
                    });

                }, () =>
                {
                    timer.Restart();
                }
                , (iterationCount, schedule, finalConfig) =>
                {
                    timer.Stop();
                    var elapsedMs = timer.ElapsedMilliseconds;
                    var finishedTime = DateTime.Now;
                    finalSchedule = schedule;

                    Console.WriteLine("---------------");
                    Console.WriteLine("----FINISHED----");
                    Console.WriteLine("---------------");

                    foreach (var emp in employees)
                    {
                        var empHours = finalConfig.Summary.EmployeeHours[emp.Id];
                        var empHn = finalConfig.Summary.EmployeeHns[emp.Id];
                        Console.WriteLine($"Employee Name: {emp.Name} -- Hour: {empHours} -- Hn: {empHn:0.00}");
                    }

                    Console.WriteLine("---------------");
                    Console.WriteLine("FINAL CONFIG");
                    Console.WriteLine($"Total Hn: {finalConfig.Hn:0.00}");
                    Console.WriteLine($"Min Hn: {finalConfig.TimeSlotConfigs.Min(x => x.Hn):0.00}");
                    Console.WriteLine($"Max Hn: {finalConfig.TimeSlotConfigs.Max(x => x.Hn):0.00}");
                    Console.WriteLine($"Iterations: {iterationCount}");
                    Console.WriteLine($"Time: {elapsedMs} ms");
                    Console.WriteLine("---------------");

                    var runtimeAppend = $"-{finishedTime.Ticks}.csv";

                    finalScheduleFilePath += runtimeAppend;
                    CsvTools.GenerateScheduleToCsv(finalSchedule, finalScheduleFilePath);

                    iterationsFilePath += runtimeAppend;
                    foreach (var iterationRow in iterationsOutput)
                    {
                        iterationRow.RunTime = finishedTime;
                    }
                    CsvTools.GenerateIterationsToCsv(iterationsOutput, iterationsFilePath);

                    timeSlotFilePath += runtimeAppend;
                    var timeSlotOutputRow = finalSchedule.Where(x => x.IsOpen).Select(x =>
                    {
                        var tsHn = finalConfig.TimeSlotConfigs.FirstOrDefault(c => c.TimeSlotId == x.Id);
                        if (tsHn == null) return null;

                        var outputRow = new TimeSlotOutputRow
                        {
                            RunTimeTicks = finishedTime.Ticks.ToString(),
                            RunTime = finishedTime,
                            Id = x.Id,
                            StartTime = x.StartTime,
                            TotalHn = tsHn.Hn,
                            MinimumEmployees = x.MinimumSlot,
                            MaximumEmployees = x.MaximumSlot,
                            PreferredEmployees = x.PreferredSlot,
                            AssignedEmployees = x.Assignments.Count,
                            AssignedEmployeesWithPreferred = x.Assignments.Count(a => a.IsPreferred),
                            AvailableEmployeesWithPreferred = x.EmployeeAvailabilities.Count(a => a.IsPreferred)
                        };

                        return outputRow;
                    }).ToList();
                    CsvTools.GenerateTimeSlotToCsv(timeSlotOutputRow, timeSlotFilePath);

                    var numberOfNonContinuousAssignments = Analytics.GetNumberOfNonContinuousAssignments(finalSchedule);
                    var averageEmployeePreferredHoursDiff = Analytics.GetAverageEmployeePreferredHoursDiff(finalConfig,
                        employees);
                    var averageEmployeePreferredSlotRatio = Analytics.GetAverageEmployeePreferredSlotRatio(employees,
                        finalSchedule);
                    var timeSlotPreferredNumberRatio = Analytics.GetTimeSlotPreferredNumberRatio(finalSchedule);

                    try
                    {
                        locker.AcquireWriterLock(3000);
                        CsvTools.AddSummaryToCsv(new SummaryRow
                        {
                            RunTimeTicks = finishedTime.Ticks.ToString(),
                            RunTime = finishedTime,
                            EmployeesHn = finalConfig.Summary.EmployeeHns.Sum(x => x.Value),
                            ScheduleHn = finalConfig.TimeSlotConfigs.Sum(x => x.Hn),
                            TotalHn = finalConfig.Hn,
                            Iteration = iterationCount,
                            Time = elapsedMs,
                            NumberOfNonContinuousAssignments = numberOfNonContinuousAssignments,
                            AverageEmployeePreferredHoursDiff = averageEmployeePreferredHoursDiff,
                            AverageEmployeePreferredSlotRatio = averageEmployeePreferredSlotRatio,
                            TimeSlotPreferredNumberRatio = timeSlotPreferredNumberRatio,
                            HeuristicsConstants = heuristicsConstants,
                            SchedulerSettings = schedulerSettings
                        }, summaryFilePath);
                    }
                    finally
                    {
                        locker.ReleaseWriterLock();
                    }
                });
        }

        static SchedulerSettings GetSchedulerSettingsFromConfig()
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            var settings = new SchedulerSettings()
            {
                SearchDepth = Convert.ToInt32(appSettings["Settings.SearchDepth"]),
                AdjacentDepthSearch = Convert.ToBoolean(appSettings["Settings.AdjacentDepthSearch"]),
                MinimumIteration = Convert.ToInt32(appSettings["Settings.MinimumIteration"]),
                MaximumIteration = Convert.ToInt32(appSettings["Settings.MaximumIteration"]),
                MinimumHn = Convert.ToInt32(appSettings["Settings.MinimumHn"]),
                MaximumLocalSearchIterations = Convert.ToInt32(appSettings["Settings.MaximumLocalSearchIterations"]),
                MaximumBeamCapacity = Convert.ToInt32(appSettings["Settings.MaximumBeamCapacity"]),
                InitialMaximumSidewayStep = Convert.ToInt32(appSettings["Settings.InitialMaximumSidewayStep"]),
                FinalMaximumSidewayStepRatio = Convert.ToDouble(appSettings["Settings.FinalMaximumSidewayStepRatio"]),
                MaximumAnnealingDelta = Convert.ToInt32(appSettings["Settings.MaximumAnnealingDelta"]),
                AnnealingExponentBase = Convert.ToDouble(appSettings["Settings.AnnealingExponentBase"]),
                MaximumRandomRestart = Convert.ToInt32(appSettings["Settings.MaximumRandomRestart"])
            };

            return settings;
        }

        static HeuristicsConstants GetHeuristicsConstantFromConfig()
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            var hc = new HeuristicsConstants
            {
                EmployeePreferredSlotBonus = Convert.ToInt32(appSettings["Heuristics.EmployeePreferredSlotBonus"]),
                EmployeeNonContinuousSlotPenalty = Convert.ToInt32(appSettings["Heuristics.EmployeeNonContinuousSlotPenalty"]),
                EmployeeContinuousSlotBonus = Convert.ToInt32(appSettings["Heuristics.EmployeeContinuousSlotBonus"]),

                TimeSlotPreferredNumberBonus = Convert.ToInt32(appSettings["Heuristics.TimeSlotPreferredNumberBonus"]),

                TotalHoursAbsoluteMaximumViolationPenalty =
                    Convert.ToInt32(appSettings["Heuristics.TotalHoursAbsoluteMaximumViolationPenalty"]),
                TotalHoursAbsoluteMinimumViolationPenalty =
                    Convert.ToInt32(appSettings["Heuristics.TotalHoursAbsoluteMinimumViolationPenalty"]),
                TotalHoursPreferredBonus = Convert.ToInt32(appSettings["Heuristics.TotalHoursPreferredBonus"]),
                TotalHoursMaximumBonus = Convert.ToInt32(appSettings["Heuristics.TotalHoursMaximumBonus"]),
                TotalHoursMinimumBonus = Convert.ToInt32(appSettings["Heuristics.TotalHoursMinimumBonus"]),

                TotalHoursPreferredBonusExponentBase =
                    Convert.ToDouble(appSettings["Heuristics.TotalHoursPreferredBonusExponentBase"]),

                TotalHoursMaximumNormalizedHn =
                    Convert.ToDouble(appSettings["Heuristics.TotalHoursMaximumNormalizedHn"])
            };

            return hc;
        }
    }
}
