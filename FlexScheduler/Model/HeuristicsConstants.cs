namespace FlexScheduler.Model
{
    public class HeuristicsConstants
    {
        public int EmployeePreferredSlotBonus { get; set; }
        public int EmployeeNonContinuousSlotPenalty { get; set; }
        public int EmployeeContinuousSlotBonus { get; set; }

        public int TimeSlotPreferredNumberBonus { get; set; }

        public int TotalHoursAbsoluteMaximumViolationPenalty { get; set; }
        public int TotalHoursAbsoluteMinimumViolationPenalty { get; set; }
        public int TotalHoursPreferredBonus { get; set; }
        public int TotalHoursMaximumBonus { get; set; }
        public int TotalHoursMinimumBonus { get; set; }

        public double TotalHoursPreferredBonusExponentBase { get; set; }

        public double TotalHoursMaximumNormalizedHn { get; set; }

        public static HeuristicsConstants GetDefault()
        {
            return new HeuristicsConstants
            {
                EmployeePreferredSlotBonus = 20,
                EmployeeNonContinuousSlotPenalty = 75,
                EmployeeContinuousSlotBonus = 30,

                TimeSlotPreferredNumberBonus = 40,

                TotalHoursAbsoluteMaximumViolationPenalty = 100000,
                TotalHoursAbsoluteMinimumViolationPenalty = 100000,
                TotalHoursPreferredBonus = 1000,
                TotalHoursMaximumBonus = 500,
                TotalHoursMinimumBonus = 500,

                TotalHoursPreferredBonusExponentBase = 1.1,

                TotalHoursMaximumNormalizedHn = 1000
            };
        }

        public HeuristicsConstants Copy()
        {
            var newHc = new HeuristicsConstants
            {
                EmployeePreferredSlotBonus = this.EmployeePreferredSlotBonus,
                EmployeeNonContinuousSlotPenalty = this.EmployeeNonContinuousSlotPenalty,
                EmployeeContinuousSlotBonus = this.EmployeeContinuousSlotBonus,

                TimeSlotPreferredNumberBonus = this.TimeSlotPreferredNumberBonus,

                TotalHoursAbsoluteMaximumViolationPenalty = this.TotalHoursAbsoluteMaximumViolationPenalty,
                TotalHoursAbsoluteMinimumViolationPenalty = this.TotalHoursAbsoluteMinimumViolationPenalty,
                TotalHoursPreferredBonus = this.TotalHoursPreferredBonus,
                TotalHoursMaximumBonus = this.TotalHoursMaximumBonus,
                TotalHoursMinimumBonus = this.TotalHoursMinimumBonus,

                TotalHoursPreferredBonusExponentBase = this.TotalHoursPreferredBonusExponentBase,

                TotalHoursMaximumNormalizedHn = this.TotalHoursMaximumNormalizedHn
            };

            return newHc;
        }
    }
}
