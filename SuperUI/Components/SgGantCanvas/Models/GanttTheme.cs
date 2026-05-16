namespace SuperUI.Components.SgGantCanvas.Models;

public enum GanttThemeType
{
    Light,
    Dark,
    HighContrast,
    Corporate,
    Pastel
}

public class GanttTheme
{
    public GanttThemeType Type { get; set; } = GanttThemeType.Light;
    public string PrimaryColor { get; set; } = "#1890ff";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#000000";
    public string GridColor { get; set; } = "#eeeeee";
    public string WeekendColor { get; set; } = "#fafafa";
    public string CriticalPathColor { get; set; } = "#ff4d4f";
    public string BaselineColor { get; set; } = "rgba(0,0,0,0.2)";
}
