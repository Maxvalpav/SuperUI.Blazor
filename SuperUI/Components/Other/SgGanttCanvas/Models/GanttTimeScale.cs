using System;

namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>Time unit used for Gantt timeline columns.</summary>
public enum TimeUnit
{
    Year,
    Quarter,
    Month,
    Week,
    Day,
    Hour,
    Minute15
}

/// <summary>Configuration for the Gantt chart timeline scale and zoom settings.</summary>
public class GanttTimeScale
{
    /// <summary>Zoom level (1–7, higher = more detail).</summary>
    public int ZoomLevel { get; set; } = 4;
    /// <summary>Top-level time unit for the timeline header.</summary>
    public TimeUnit TopUnit { get; set; } = TimeUnit.Month;
    /// <summary>Bottom-level time unit for column divisions.</summary>
    public TimeUnit BottomUnit { get; set; } = TimeUnit.Day;
    /// <summary>Pixel width of each time column.</summary>
    public int ColumnWidth { get; set; } = 60;
    /// <summary>Date format string for timeline labels.</summary>
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    /// <summary>Working hours range (start, end).</summary>
    public (int Start, int End) WorkingHours { get; set; } = (9, 18);
    /// <summary>Days considered as working days.</summary>
    public DayOfWeek[] WorkingDays { get; set; } = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
    /// <summary>Background color for non-working hours.</summary>
    public string NonWorkingColor { get; set; } = "#f5f5f5";
}
