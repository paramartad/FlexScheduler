namespace FlexScheduler.Model
{
    public class Availability
    {
        public Employee Employee { get; set; }
        public TimeSlot TimeSlot { get; set; }
        public bool IsPreferred { get; set; }
    }
}
