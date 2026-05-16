using System;

namespace SuperUI.Components.SgGantCanvas.Models;

public class GanttResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Color { get; set; } = "#1890ff";
    public double Capacity { get; set; } = 1.0;
    public DayOfWeek[] WorkingDays { get; set; } = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
}
