using System;

namespace SuperUI.Components
{
    public class SgSchedulerEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAllDay { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string? RecurrenceRule { get; set; }
        public object? Data { get; set; }
    }
}
