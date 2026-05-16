namespace SuperUI.Components.SgGantCanvas.Models;

public enum ExportFormat
{
    Png,
    Pdf,
    Excel,
    Json
}

public class GanttExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Png;
    public string FileName { get; set; } = "gantt-export";
    public bool IncludeTimeline { get; set; } = true;
    public bool IncludeResources { get; set; } = true;
    public bool IncludeDependencies { get; set; } = true;
}
