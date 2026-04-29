using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public sealed class ActivityFeedItem
{
    public string? Time { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? User { get; set; }
    public string? BadgeText { get; set; }
    public string BadgeVariant { get; set; } = "default";
    public string AccentColor { get; set; } = "var(--sui-accent, #2563eb)";
    public RenderFragment? IconContent { get; set; }
    public RenderFragment? ExtraContent { get; set; }
}
