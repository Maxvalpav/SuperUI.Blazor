using System;
using System.Collections.Generic;

namespace SuperUI.Components.SgGantCanvas.Models;

public class GanttTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public TimeSpan Duration => End - Start;
    public double Progress { get; set; } // 0.0 .. 1.0
    public bool IsMilestone { get; set; }
    public bool IsSummary { get; set; }
    public bool IsCollapsed { get; set; }
    public List<string> ResourceIds { get; set; } = new();
    public string? Color { get; set; }
    public string? TextColor { get; set; }
    public DateTime? BaselineStart { get; set; }
    public DateTime? BaselineEnd { get; set; }
    public int RowIndex { get; set; }
    public int Level { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsCritical { get; set; }
    public string? Tooltip { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> CustomFields { get; set; } = new();
}
