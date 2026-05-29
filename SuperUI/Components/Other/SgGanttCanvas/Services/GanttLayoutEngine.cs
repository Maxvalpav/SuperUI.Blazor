using System;
using System.Collections.Generic;
using System.Linq;
using SuperUI.Components.SgGanttCanvas.Models;

namespace SuperUI.Components.SgGanttCanvas.Services;

/// <summary>Calculates pixel positions and dimensions for Gantt chart elements based on zoom level and time scale.</summary>
public class GanttLayoutEngine
{
    /// <summary>Returns the time unit and column width for a given zoom level.</summary>
    public (TimeUnit BottomUnit, int ColumnWidth) GetZoomSettings(int level)
    {
        return level switch
        {
            1 => (TimeUnit.Year, 100),    // 100px per year
            2 => (TimeUnit.Quarter, 100), // 100px per quarter
            3 => (TimeUnit.Month, 120),   // 120px per month
            4 => (TimeUnit.Week, 80),     // 80px per week
            5 => (TimeUnit.Day, 40),      // 40px per day (Default)
            6 => (TimeUnit.Hour, 60),     // 60px per hour
            7 => (TimeUnit.Minute15, 40), // 40px per 15 min
            _ => (TimeUnit.Day, 40)
        };
    }

    /// <summary>Converts a DateTime to a pixel X coordinate relative to the project start.</summary>
    public double GetX(DateTime date, DateTime projectStart, int level)
    {
        var settings = GetZoomSettings(level);
        var offset = date - projectStart;
        
        return settings.BottomUnit switch
        {
            TimeUnit.Year => (offset.TotalDays / 365.25) * settings.ColumnWidth,
            TimeUnit.Quarter => (offset.TotalDays / 91.3) * settings.ColumnWidth,
            TimeUnit.Month => (offset.TotalDays / 30.44) * settings.ColumnWidth,
            TimeUnit.Week => (offset.TotalDays / 7.0) * settings.ColumnWidth,
            TimeUnit.Day => offset.TotalDays * settings.ColumnWidth,
            TimeUnit.Hour => offset.TotalHours * settings.ColumnWidth,
            TimeUnit.Minute15 => (offset.TotalMinutes / 15.0) * settings.ColumnWidth,
            _ => offset.TotalDays * settings.ColumnWidth
        };
    }

    /// <summary>Converts a TimeSpan duration to a pixel width at the given zoom level.</summary>
    public double GetWidth(TimeSpan duration, int level)
    {
        var settings = GetZoomSettings(level);
        
        return settings.BottomUnit switch
        {
            TimeUnit.Year => (duration.TotalDays / 365.25) * settings.ColumnWidth,
            TimeUnit.Quarter => (duration.TotalDays / 91.3) * settings.ColumnWidth,
            TimeUnit.Month => (duration.TotalDays / 30.44) * settings.ColumnWidth,
            TimeUnit.Week => (duration.TotalDays / 7.0) * settings.ColumnWidth,
            TimeUnit.Day => duration.TotalDays * settings.ColumnWidth,
            TimeUnit.Hour => duration.TotalHours * settings.ColumnWidth,
            TimeUnit.Minute15 => (duration.TotalMinutes / 15.0) * settings.ColumnWidth,
            _ => duration.TotalDays * settings.ColumnWidth
        };
    }

    public double GetY(int rowIndex, int rowHeight)
    {
        return rowIndex * rowHeight;
    }
}
