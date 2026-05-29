using Microsoft.AspNetCore.Components;

namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>Horizontal text alignment options for column cells.</summary>
public enum TextAlign
{
    Left,
    Center,
    Right
}

/// <summary>Defines a column in the Gantt chart's left-side task grid.</summary>
public class GanttColumn
{
    /// <summary>Data field name the column is bound to.</summary>
    public string Field { get; set; } = string.Empty;
    /// <summary>Column header text.</summary>
    public string Header { get; set; } = string.Empty;
    /// <summary>Column width in pixels.</summary>
    public int Width { get; set; } = 150;
    /// <summary>Minimum column width in pixels.</summary>
    public int MinWidth { get; set; } = 50;
    /// <summary>Maximum column width in pixels.</summary>
    public int MaxWidth { get; set; } = 500;
    /// <summary>Whether the column width is resizable.</summary>
    public bool Resizable { get; set; } = true;
    /// <summary>Whether the column supports sorting.</summary>
    public bool Sortable { get; set; } = true;
    /// <summary>Whether the column supports filtering.</summary>
    public bool Filterable { get; set; } = true;
    /// <summary>Whether cell values are editable.</summary>
    public bool Editable { get; set; } = true;
    /// <summary>Text alignment for cell content.</summary>
    public TextAlign Align { get; set; } = TextAlign.Left;
    /// <summary>Optional format string for cell values.</summary>
    public string? Format { get; set; }
    /// <summary>Custom cell template.</summary>
    public RenderFragment<GanttTask>? Template { get; set; }
    /// <summary>Whether the column is visible.</summary>
    public bool Visible { get; set; } = true;
}
