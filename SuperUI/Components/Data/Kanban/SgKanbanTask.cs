using System;
using System.Collections.Generic;

namespace SuperUI.Components
{
    /// <summary>Represents a task card on the Kanban board.</summary>
    public class SgKanbanTask
    {
        /// <summary>Unique identifier for this task.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>The title of the task card.</summary>
        public string Title { get; set; } = "";
        /// <summary>A detailed description of the task.</summary>
        public string Description { get; set; } = "";
        /// <summary>An optional color for the task card.</summary>
        public string? Color { get; set; }
        /// <summary>Tags associated with this task.</summary>
        public List<string> Tags { get; set; } = new();
        /// <summary>The assignee of this task.</summary>
        public string? Assignee { get; set; }
        /// <summary>The due date for this task.</summary>
        public DateTime? DueDate { get; set; }
        /// <summary>The ID of the column this task belongs to.</summary>
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
