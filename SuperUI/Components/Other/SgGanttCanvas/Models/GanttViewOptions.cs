namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>View options controlling visibility of Gantt chart elements and layout.</summary>
public class GanttViewOptions
{
    /// <summary>Whether dependency arrows are visible.</summary>
    public bool ShowDependencies { get; set; } = true;
    /// <summary>Whether the critical path is highlighted.</summary>
    public bool ShowCriticalPath { get; set; } = false;
    /// <summary>Whether baseline bars are visible.</summary>
    public bool ShowBaselines { get; set; } = false;
    /// <summary>Whether progress indicators are shown.</summary>
    public bool ShowProgress { get; set; } = true;
    /// <summary>Whether resource names appear on task bars.</summary>
    public bool ShowResourceNames { get; set; } = true;
    /// <summary>Whether resource avatars are shown.</summary>
    public bool ShowResourceAvatars { get; set; } = true;
    /// <summary>Whether tooltips are shown on hover.</summary>
    public bool ShowTooltips { get; set; } = true;
    /// <summary>Whether the background grid is visible.</summary>
    public bool ShowGrid { get; set; } = true;
    /// <summary>Whether weekends are visually distinguished.</summary>
    public bool ShowWeekends { get; set; } = true;
    /// <summary>Whether the current-day marker line is shown.</summary>
    public bool ShowTodayLine { get; set; } = true;
    /// <summary>Whether row numbers are shown in the left panel.</summary>
    public bool ShowRowNumbers { get; set; } = true;
    /// <summary>Whether the status bar is visible.</summary>
    public bool ShowStatusBar { get; set; } = true;
    /// <summary>Height of each task row in pixels.</summary>
    public int RowHeight { get; set; } = 36;
    /// <summary>Height of each task bar in pixels.</summary>
    public int BarHeight { get; set; } = 24;
    /// <summary>Flat mode (no hierarchy indentation).</summary>
    public bool FlatMode { get; set; } = false;
    /// <summary>Field name to group tasks by.</summary>
    public string? GroupBy { get; set; }
    /// <summary>Field name to sort tasks by.</summary>
    public string? SortBy { get; set; }
    /// <summary>Sort direction (true = descending).</summary>
    public bool SortDescending { get; set; } = false;
}
