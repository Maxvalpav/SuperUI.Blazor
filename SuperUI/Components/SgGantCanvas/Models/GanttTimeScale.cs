using System;

namespace SuperUI.Components.SgGantCanvas.Models;

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

public class GanttTimeScale
{
    public int ZoomLevel { get; set; } = 4;
    public TimeUnit TopUnit { get; set; } = TimeUnit.Month;
    public TimeUnit BottomUnit { get; set; } = TimeUnit.Day;
    public int ColumnWidth { get; set; } = 60;
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public (int Start, int End) WorkingHours { get; set; } = (9, 18);
    public DayOfWeek[] WorkingDays { get; set; } = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
    public string NonWorkingColor { get; set; } = "#f5f5f5";
}
