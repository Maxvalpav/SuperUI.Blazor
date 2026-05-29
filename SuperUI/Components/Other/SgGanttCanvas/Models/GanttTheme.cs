namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>Built-in Gantt chart theme presets.</summary>
public enum GanttThemeType
{
    Light,
    Dark,
    HighContrast,
    Corporate,
    Pastel
}

/// <summary>Color theme configuration for the Gantt chart canvas.</summary>
public class GanttTheme
{
    /// <summary>Theme preset type.</summary>
    public GanttThemeType Type { get; set; } = GanttThemeType.Light;
    /// <summary>Primary accent color.</summary>
    public string PrimaryColor { get; set; } = "#1890ff";
    /// <summary>Background color.</summary>
    public string BackgroundColor { get; set; } = "#ffffff";
    /// <summary>Text color.</summary>
    public string TextColor { get; set; } = "#000000";
    /// <summary>Grid line color.</summary>
    public string GridColor { get; set; } = "#eeeeee";
    /// <summary>Weekend highlight color.</summary>
    public string WeekendColor { get; set; } = "#fafafa";
    /// <summary>Critical path bar color.</summary>
    public string CriticalPathColor { get; set; } = "#ff4d4f";
    /// <summary>Baseline bar color.</summary>
    public string BaselineColor { get; set; } = "rgba(0,0,0,0.2)";
}
