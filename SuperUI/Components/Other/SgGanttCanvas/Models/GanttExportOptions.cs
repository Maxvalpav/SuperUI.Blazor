namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>Supported export formats for the Gantt chart.</summary>
public enum ExportFormat
{
    Png,
    Pdf,
    Excel,
    Json
}

/// <summary>Configuration options for exporting the Gantt chart.</summary>
public class GanttExportOptions
{
    /// <summary>Export file format.</summary>
    public ExportFormat Format { get; set; } = ExportFormat.Png;
    /// <summary>Output file name (without extension).</summary>
    public string FileName { get; set; } = "gantt-export";
    /// <summary>Whether to include the timeline.</summary>
    public bool IncludeTimeline { get; set; } = true;
    /// <summary>Whether to include resource information.</summary>
    public bool IncludeResources { get; set; } = true;
    /// <summary>Whether to include dependency lines.</summary>
    public bool IncludeDependencies { get; set; } = true;
}
