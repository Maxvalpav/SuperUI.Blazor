using System;

namespace SuperUI.Components.SgGantCanvas.Models;

public class GanttMilestone
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Color { get; set; } = "#ff4d4f";
}
