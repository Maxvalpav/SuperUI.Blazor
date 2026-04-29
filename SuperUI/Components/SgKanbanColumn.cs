using System;

namespace SuperUI.Components
{
    public class SgKanbanColumn
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string? Color { get; set; }
        
        /// <summary>
        /// Maximum number of tasks allowed in this column. 0 or null means no limit.
        /// </summary>
        public int? WipLimit { get; set; }
    }
}
