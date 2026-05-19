using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

public sealed class PropertyPanelItem
{
    public string Label { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Hint { get; set; }
    public string? Description { get; set; }
    public string? BadgeText { get; set; }
    public SgBadgeVariant BadgeVariant { get; set; } = SgBadgeVariant.Default;
    public int Span { get; set; } = 1;
    public RenderFragment? ValueTemplate { get; set; }
}
