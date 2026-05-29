using System;
using System.Collections.Generic;

namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>Represents a single task in the Gantt chart with timing, progress, resources, and hierarchy.</summary>
public class GanttTask
{
    /// <summary>Unique task identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>ID of the parent summary task (null for root tasks).</summary>
    public string? ParentId { get; set; }
    /// <summary>Display name of the task.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Task start date/time.</summary>
    public DateTime Start { get; set; }
    /// <summary>Task end date/time.</summary>
    public DateTime End { get; set; }
    /// <summary>Computed duration (End - Start).</summary>
    public TimeSpan Duration => End - Start;
    /// <summary>Completion progress from 0.0 to 1.0.</summary>
    public double Progress { get; set; }
    /// <summary>Whether this is a milestone (zero-duration marker).</summary>
    public bool IsMilestone { get; set; }
    /// <summary>Whether this task is a summary (parent of subtasks).</summary>
    public bool IsSummary { get; set; }
    /// <summary>Whether the summary task is collapsed.</summary>
    public bool IsCollapsed { get; set; }
    /// <summary>Resource IDs assigned to this task.</summary>
    public List<string> ResourceIds { get; set; } = new();
    /// <summary>Bar color for the task.</summary>
    public string? Color { get; set; }
    /// <summary>Text color for the task label.</summary>
    public string? TextColor { get; set; }
    /// <summary>Original baseline start date (variance tracking).</summary>
    public DateTime? BaselineStart { get; set; }
    /// <summary>Original baseline end date (variance tracking).</summary>
    public DateTime? BaselineEnd { get; set; }
    /// <summary>Row index used for vertical positioning.</summary>
    public int RowIndex { get; set; }
    /// <summary>Hierarchy level (0 = root).</summary>
    public int Level { get; set; }
    /// <summary>Prevents user interaction on this task.</summary>
    public bool IsReadOnly { get; set; }
    /// <summary>Whether this task is on the critical path.</summary>
    public bool IsCritical { get; set; }
    /// <summary>Custom tooltip text for the task bar.</summary>
    public string? Tooltip { get; set; }
    /// <summary>Tags for categorization and filtering.</summary>
    public List<string> Tags { get; set; } = new();
    /// <summary>Custom field data attached to the task.</summary>
    public Dictionary<string, object> CustomFields { get; set; } = new();
}
