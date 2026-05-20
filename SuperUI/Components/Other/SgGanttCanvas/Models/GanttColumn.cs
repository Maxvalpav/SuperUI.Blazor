using Microsoft.AspNetCore.Components;

namespace SuperUI.Components.SgGanttCanvas.Models;

public enum TextAlign
{
    Left,
    Center,
    Right
}

public class GanttColumn
{
    public string Field { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public int Width { get; set; } = 150;
    public int MinWidth { get; set; } = 50;
    public int MaxWidth { get; set; } = 500;
    public bool Resizable { get; set; } = true;
    public bool Sortable { get; set; } = true;
    public bool Filterable { get; set; } = true;
    public bool Editable { get; set; } = true;
    public TextAlign Align { get; set; } = TextAlign.Left;
    public string? Format { get; set; }
    public RenderFragment<GanttTask>? Template { get; set; }
    public bool Visible { get; set; } = true;
}
