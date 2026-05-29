using System;

namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>A milestone marker displayed at a specific date on the Gantt timeline.</summary>
public class GanttMilestone
{
    /// <summary>Unique milestone identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Display name of the milestone.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Date of the milestone.</summary>
    public DateTime Date { get; set; }
    /// <summary>Marker color.</summary>
    public string Color { get; set; } = "#ff4d4f";
}
