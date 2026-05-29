using System;

namespace SuperUI.Components
{
    /// <summary>Represents a column in the Kanban board.</summary>
    public class SgKanbanColumn
    {
        /// <summary>Unique identifier for this column.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>The display title of the column.</summary>
        public string Title { get; set; } = "";
        /// <summary>An optional color for the column header.</summary>
        public string? Color { get; set; }
        /// <summary>Maximum number of tasks allowed in this column. 0 or null means no limit.</summary>
        public int? WipLimit { get; set; }
    }
}
