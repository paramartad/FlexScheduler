namespace FlexScheduler.Core
{
    public class SchedulerSettings
    {
        public int SearchDepth { get; set; }
        public bool AdjacentDepthSearch { get; set; }
        public int MinimumIteration { get; set; }
        public int MaximumIteration { get; set; }
        public double MinimumHn { get; set; }
        public int MaximumLocalSearchIterations { get; set; }
        public int MaximumBeamCapacity { get; set; }
        public int InitialMaximumSidewayStep { get; set; }
        public double FinalMaximumSidewayStepRatio { get; set; }

        public double MaximumAnnealingDelta { get; set; }
        public double AnnealingExponentBase { get; set; }
        public int MaximumRandomRestart { get; set; }

        public double MaximumSidewayStep(int currentIteration)
        {
            return InitialMaximumSidewayStep * (FinalMaximumSidewayStepRatio + (1 - FinalMaximumSidewayStepRatio)  * (MaximumIteration - currentIteration) / MaximumIteration);
        }

        public static SchedulerSettings GetDefault()
        {
            return new SchedulerSettings()
            {
                SearchDepth = 2,
                AdjacentDepthSearch = false,
                MinimumIteration = 300,
                MaximumIteration = 500,
                MinimumHn = 0,
                MaximumLocalSearchIterations = 7,
                MaximumBeamCapacity = 25,
                InitialMaximumSidewayStep = 15,
                FinalMaximumSidewayStepRatio = 0.2,
                MaximumAnnealingDelta = -150,
                AnnealingExponentBase = 1.03,
                MaximumRandomRestart = 1
            };
        }
    }
}
