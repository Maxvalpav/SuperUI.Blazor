using System;
using System.Collections.Generic;

namespace SuperUI.Components
{
    public class SgKanbanTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Color { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? Assignee { get; set; }
        public DateTime? DueDate { get; set; }
        public string ColumnId { get; set; } = "";

        /// <summary>
        /// Optional swimlane name for grouping tasks horizontally.
        /// </summary>
        public string? Swimlane { get; set; }

        /// <summary>
        /// Order within the column.
        /// </summary>
        public int Order { get; set; }
    }
}
