namespace SuperUI.Components.SgGanttCanvas.Models;

public class GanttViewOptions
{
    public bool ShowDependencies { get; set; } = true;
    public bool ShowCriticalPath { get; set; } = false;
    public bool ShowBaselines { get; set; } = false;
    public bool ShowProgress { get; set; } = true;
    public bool ShowResourceNames { get; set; } = true;
    public bool ShowResourceAvatars { get; set; } = true;
    public bool ShowTooltips { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public bool ShowWeekends { get; set; } = true;
    public bool ShowTodayLine { get; set; } = true;
    public bool ShowRowNumbers { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
    public int RowHeight { get; set; } = 36;
    public int BarHeight { get; set; } = 24;
    public bool FlatMode { get; set; } = false;
    public string? GroupBy { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
