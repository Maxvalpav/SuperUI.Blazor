namespace SuperUI.Components;

public sealed class StatusPanelItem
{
    public string Title { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Subtitle { get; set; }
    public string? Hint { get; set; }
    public string? BadgeText { get; set; }
    public string BadgeVariant { get; set; } = "default";
    public double? TrendValue { get; set; }
}
