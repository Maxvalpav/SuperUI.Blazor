using System;

namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>Stores the original planned start and end dates for a task (baseline comparison).</summary>
public class GanttBaseline
{
    /// <summary>ID of the task this baseline belongs to.</summary>
    public string TaskId { get; set; } = string.Empty;
    /// <summary>Original planned start date.</summary>
    public DateTime Start { get; set; }
    /// <summary>Original planned end date.</summary>
    public DateTime End { get; set; }
    /// <summary>Optional baseline bar color.</summary>
    public string? Color { get; set; }
}
