using System;

namespace SuperUI.Components
{
    public class SgCalendarEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public string? Color { get; set; } // hex or css variable
        public string? Icon { get; set; }
        
        public bool IsAllDay { get; set; } = true;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        /// Recurrence rule in iCal format (e.g., "FREQ=WEEKLY;BYDAY=MO,WE,FR").
        /// </summary>
        public string? RecurrenceRule { get; set; }
    }
}
