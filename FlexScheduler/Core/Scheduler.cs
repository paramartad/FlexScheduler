using System;
using System.Collections.Generic;
using System.Linq;
using FlexScheduler.Model;
using FlexScheduler.Tools;

namespace FlexScheduler.Core
{
    public class Scheduler
    {
        private readonly IList<Employee> _employees;
        private readonly IList<TimeSlot> _schedule;
        private readonly ScheduleConfig _initialConfig;

        private readonly HeuristicsCalculator _heuristicsCalculator;
        private readonly SchedulerSettings _schedulerSettings;

        private int _resetCount = 0;

        protected List<ScheduleConfig> ConfigsHistory { get; set; } = new List<ScheduleConfig>();
        protected List<ScheduleConfig> KnownHighConfigs { get; set; } = new List<ScheduleConfig>();
        public ScheduleConfig CurrentMaximum { get; set; }
        public List<ScheduleConfig> Maximums { get; set; } = new List<ScheduleConfig>();

        public Scheduler(IList<Employee> employees, IList<TimeSlot> schedule)
            : this(employees, schedule, new HeuristicsCalculator(), SchedulerSettings.GetDefault(), null)
        {
        }

        public Scheduler(IList<Employee> employees, IList<TimeSlot> schedule, SchedulerSettings schedulerSettings)
            : this(employees, schedule, new HeuristicsCalculator(), schedulerSettings, null)
        {
        }

        public Scheduler(IList<Employee> employees, IList<TimeSlot> schedule, HeuristicsConstants heuristicsConstants)
            : this(employees, schedule, new HeuristicsCalculator(heuristicsConstants), SchedulerSettings.GetDefault(), null)
        {
        }

        public Scheduler(IList<Employee> employees, IList<TimeSlot> schedule, HeuristicsConstants heuristicsConstants,
            SchedulerSettings schedulerSettings)
            : this(employees, schedule, new HeuristicsCalculator(heuristicsConstants), schedulerSettings, null)
        {
        }

        public Scheduler(IList<Employee> employees, IList<TimeSlot> schedule, HeuristicsCalculator heuristicsCalculator,
            SchedulerSettings schedulerSettings, ScheduleConfig initialConfig)
        {
            _employees = employees;
            _schedule = schedule;
            _heuristicsCalculator = heuristicsCalculator;
            _schedulerSettings = schedulerSettings;
            _initialConfig = initialConfig;

            foreach (var ts in _schedule)
            {
                ts.GetHn = _heuristicsCalculator.Calculate;
            }
        }

        protected void Reset()
        {
            CurrentMaximum = null;
            ConfigsHistory = new List<ScheduleConfig>();
            KnownHighConfigs = new List<ScheduleConfig>();
        }

        public int Start(Action<int, ScheduleConfig, double> onIterationCompleted = null, Action onReset = null,
            Action<int, IList<TimeSlot>, ScheduleConfig> onSearchCompleted = null)
        {
            Reset();
            onReset?.Invoke();
            var initialConfig = _initialConfig ?? GetRandomInitialScheduleConfig(_schedule, _employees);
            var currentConfig = initialConfig;
            CurrentMaximum = currentConfig;
            
            var schedule = _schedule;
            var employees = _employees;

            var outerIteration = 0;
            var currentAnnealingCount = 0;
            while (outerIteration < _schedulerSettings.MinimumIteration ||
                   (currentAnnealingCount <= _schedulerSettings.MaximumSidewayStep(outerIteration) &&
                    outerIteration < _schedulerSettings.MaximumIteration))
            {
                var nextConfig = Iterate(schedule, employees, currentConfig);
                SetScheduleToConfig(schedule, nextConfig);

                var delta = nextConfig.Hn - currentConfig.Hn;
                currentAnnealingCount = delta > 0 ? 0 : currentAnnealingCount + 1;

                if (nextConfig.Hn >= CurrentMaximum.Hn)
                {
                    CurrentMaximum = nextConfig;
                }

                currentConfig = nextConfig;

                onIterationCompleted?.Invoke(outerIteration, currentConfig, delta);
                outerIteration++;

                if (outerIteration == _schedulerSettings.MinimumIteration / 2 &&
                    CurrentMaximum.Hn < _schedulerSettings.MinimumHn)
                    return Start(onIterationCompleted, onReset, onSearchCompleted);

                if (currentAnnealingCount > _schedulerSettings.MaximumSidewayStep(outerIteration) &&
                    outerIteration < _schedulerSettings.MinimumIteration &&
                    CurrentMaximum.Hn >= _schedulerSettings.MinimumHn)
                {
                    Maximums.Add(CurrentMaximum);
                    _resetCount++;
                    if (_resetCount >= _schedulerSettings.MaximumRandomRestart) break;

                    return Start(onIterationCompleted, onReset, onSearchCompleted);
                }
            }
            Maximums.Add(CurrentMaximum);
            CurrentMaximum = Maximums.OrderByDescending(x => x.Hn).First();
            SetScheduleToConfig(_schedule, CurrentMaximum);

            onSearchCompleted?.Invoke(outerIteration, _schedule, CurrentMaximum);

            return 1;
        }

        public ScheduleConfig Iterate(IList<TimeSlot> schedule, IList<Employee> employees, ScheduleConfig previousScheduleConfig)
        {
            var localClimbs = new List<ScheduleConfig>();

            var previousTimeSlots = previousScheduleConfig.TimeSlotConfigs.OrderBy(x => x.Hn).ToList();
            var tenth = (int)previousTimeSlots.Count / 20;
            var fifth = (int)previousTimeSlots.Count / 5;
            var half = (int) previousTimeSlots.Count / 2;
            
            var tenthClimbs = GetLocalClimbs(schedule, employees, previousScheduleConfig, 10, 0, tenth);
            var fifthClimbs = GetLocalClimbs(schedule, employees, previousScheduleConfig, 8, tenth, fifth - tenth);
            var lowerHalfClimbs = GetLocalClimbs(schedule, employees, previousScheduleConfig, 6, fifth, half - fifth);
            var upperHalfClimbs = GetLocalClimbs(schedule, employees, previousScheduleConfig, 6, half, half);

            localClimbs.AddRange(tenthClimbs);
            localClimbs.AddRange(fifthClimbs);
            localClimbs.AddRange(lowerHalfClimbs);
            localClimbs.AddRange(upperHalfClimbs);

            var configsPool = KnownHighConfigs.ToList();
            configsPool.AddRange(localClimbs);
            configsPool.Remove(previousScheduleConfig);
            if (configsPool.Count == 0)
            {
                var betterHistory = ConfigsHistory;
                var nextBacktrack = betterHistory[betterHistory.Count - 2];
                betterHistory.RemoveAt(betterHistory.Count - 1);
                return nextBacktrack;
            }

            ScheduleConfig nextClimb = null;
            var nextClimbId = 0;

            var rand = new Random();
            configsPool = configsPool.OrderByDescending(x => x.Hn).ToList();
            nextClimbId = rand.Next(0, configsPool.Count);
            nextClimb = configsPool[nextClimbId];
            configsPool.RemoveAt(nextClimbId);
            
            KnownHighConfigs = KnownHighConfigs
                .Union(configsPool)
                .OrderByDescending(x => x.Hn)
                .Take(_schedulerSettings.MaximumBeamCapacity)
                .ToList();
            ConfigsHistory.Add(nextClimb);

            return nextClimb;
        }

        protected IList<ScheduleConfig> GetLocalClimbs(IList<TimeSlot> schedule, IList<Employee> employees,
            ScheduleConfig previousScheduleConfig, int count, int min, int size)
        {
            var localClimbs = new List<ScheduleConfig>();

            var previousTimeSlots = previousScheduleConfig.TimeSlotConfigs.OrderBy(x => x.Hn).ToList();
            var ranges = Enumerable.Range(min, size).ToList();
            ranges.Shuffle();
            foreach (var slotPos in ranges)
            {
                var tsConfig = previousTimeSlots[slotPos];
                var ts = schedule.FirstOrDefault(x => x.Id == tsConfig.TimeSlotId);
                if (ts == null || !ts.IsOpen) continue;

                TryClimb(schedule, ts, _schedulerSettings.SearchDepth, s =>
                {
                    var newScheduleConfig = _heuristicsCalculator.Calculate(schedule, employees);
                    var delta = newScheduleConfig.Hn - previousScheduleConfig.Hn;
                    if (AcceptAnnealing(delta))
                    {
                        localClimbs.Add(newScheduleConfig);
                    }
                });

                if (localClimbs.Count >= count) break;
            }

            return localClimbs;
        }

        protected void TryClimb(IList<TimeSlot> schedule, TimeSlot timeSlot, int depth, Action<IList<TimeSlot>> onEnd)
        {
            if (depth < 1)
            {
                onEnd(schedule);
                return;
            }

            var maxTsLocalIterations = _schedulerSettings.MaximumLocalSearchIterations;
            var maxIts = Math.Min(maxTsLocalIterations, timeSlot.PossibleCombinations.Count);
            
            timeSlot.PossibleCombinations.Shuffle();
            depth--;
            for (var i = 0; i < maxIts; i++)
            {
                timeSlot.PossibleCombinationId = i;


                if (depth < 1 || (_schedulerSettings.AdjacentDepthSearch && timeSlot.Previous == null && timeSlot.Next == null))
                {
                    TryClimb(schedule, timeSlot, depth, onEnd);
                }
                else
                {
                    var rand = new Random();
                    TimeSlot nextTimeSlot = null;
                    if (_schedulerSettings.AdjacentDepthSearch)
                    {
                        if (timeSlot.Previous == null)
                        {
                            nextTimeSlot = timeSlot.Next;
                        }
                        else if (timeSlot.Next == null)
                        {
                            nextTimeSlot = timeSlot.Previous;
                        }
                        else
                        {
                            nextTimeSlot = Convert.ToBoolean(rand.Next(0, 2)) ? timeSlot.Previous : timeSlot.Next;
                        }
                    }
                    else
                    {
                        nextTimeSlot = schedule[rand.Next(0, schedule.Count)];
                    }

                    onEnd(schedule);
                    TryClimb(schedule, nextTimeSlot, depth, onEnd);
                }
                
            }
        }

        protected bool AcceptAnnealing(double delta)
        {
            if (delta >= 0) return true;

            if (delta < _schedulerSettings.MaximumAnnealingDelta) return false;

            var rand = new Random();

            var annealingChance = Math.Pow(_schedulerSettings.AnnealingExponentBase, delta);
            var randomDouble = rand.NextDouble();
            return randomDouble <= annealingChance;
        }

        protected void SetScheduleToConfig(IList<TimeSlot> schedule, ScheduleConfig config)
        {
            foreach (var ts in schedule.Where(x => x.IsOpen))
            {
                var tsConfig = config.TimeSlotConfigs.FirstOrDefault(x => x.TimeSlotId == ts.Id);
                if (tsConfig == null) continue;

                var employeesCount = tsConfig.AssignedEmployeeIds.Count;
                var employeeIdsStr = string.Join("-", tsConfig.AssignedEmployeeIds.OrderBy(x => x));

                var comboId =
                    ts.PossibleCombinations.ToList()
                        .FindIndex(
                            x =>
                                x.Count == employeesCount &&
                                employeeIdsStr ==
                                string.Join("-", x.OrderBy(y => y.Employee.Id).Select(y => y.Employee.Id)));

                if (comboId == -1) continue;

                ts.PossibleCombinationId = comboId;
            }
        }

        public ScheduleConfig GetRandomInitialScheduleConfig(IList<TimeSlot> schedule, IList<Employee> employees)
        {
            var rand = new Random();
            foreach (var ts in schedule)
            {
                ts.PossibleCombinations =
                    PowerSet.GetPowerSet(ts.EmployeeAvailabilities, ts.MinimumSlot, ts.MaximumSlot)
                        .Select(x => x.ToList())
                        .ToList();
            }

            foreach (var emp in employees)
            {
                if (!emp.AbsoluteMinimum || (emp.MinimumHours < 0 || emp.MinimumHours < emp.Availabilities.Count))
                    continue;

                foreach (var av in emp.Availabilities)
                {
                    var tsId = av.TimeSlot.Id;
                    var ts = schedule.FirstOrDefault(x => x.Id == tsId);
                    if (ts == null) continue;

                    ts.PossibleCombinations.RemoveAll(x => x.All(y => y.Employee.Id != emp.Id));
                }
            }

            var leastAvailableEmployee =
                employees.OrderBy(
                    x =>
                        ((double) x.Availabilities.Count/
                         new[] {x.MinimumHours, x.PreferredHours, x.MaximumHours}.Max())).First();

            foreach (var av in leastAvailableEmployee.Availabilities)
            {
                var ts = schedule.First(x => x.Id == av.TimeSlot.Id);
                var chosenAssignment =
                    ts.PossibleCombinations.Where(x => x.Contains(av))
                        .OrderBy(x => Math.Abs(x.Count - ts.PreferredSlot))
                        .First();
                ts.PossibleCombinationId = ts.PossibleCombinations.IndexOf(chosenAssignment);
            }

            foreach (var ts in schedule)
            {
                var nextComboId = rand.Next(0, ts.PossibleCombinations.Count);
                ts.PossibleCombinationId = nextComboId;
            }

            var initialScheduleConfig = _heuristicsCalculator.Calculate(schedule, employees);
            return initialScheduleConfig;
        }
    }
}
