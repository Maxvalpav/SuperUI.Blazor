using System;

namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>A resource (person, team, equipment) that can be assigned to Gantt tasks.</summary>
public class GanttResource
{
    /// <summary>Unique resource identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>Display name of the resource.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Avatar image URL.</summary>
    public string? Avatar { get; set; }
    /// <summary>Resource accent color.</summary>
    public string Color { get; set; } = "#1890ff";
    /// <summary>Capacity allocation (1.0 = full time).</summary>
    public double Capacity { get; set; } = 1.0;
    /// <summary>Days the resource is available for work.</summary>
    public DayOfWeek[] WorkingDays { get; set; } = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
}
