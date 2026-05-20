using System;

namespace SuperUI.Components.SgGanttCanvas.Models;

public class GanttBaseline
{
    public string TaskId { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Color { get; set; }
}
