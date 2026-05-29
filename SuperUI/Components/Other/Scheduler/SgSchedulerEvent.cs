using System;

namespace SuperUI.Components
{
    /// <summary>Represents a single event in the scheduler.</summary>
    public class SgSchedulerEvent
    {
        /// <summary>Unique event identifier.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Event title displayed in the scheduler.</summary>
        public string Title { get; set; } = "";
        /// <summary>Optional event description.</summary>
        public string? Description { get; set; }
        /// <summary>Event start date/time.</summary>
        public DateTime Start { get; set; }
        /// <summary>Event end date/time.</summary>
        public DateTime End { get; set; }
        /// <summary>Whether this is an all-day event.</summary>
        public bool IsAllDay { get; set; }
        /// <summary>Event color for the scheduler UI.</summary>
        public string? Color { get; set; }
        /// <summary>Optional icon displayed on the event.</summary>
        public string? Icon { get; set; }
        /// <summary>Recurrence rule (RRULE format) for repeating events.</summary>
        public string? RecurrenceRule { get; set; }
        /// <summary>Custom data payload attached to the event.</summary>
        public object? Data { get; set; }
    }
}
