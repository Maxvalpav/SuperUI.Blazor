using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

/// <summary>
/// Represents a single tab item inside an <see cref="SgTabsWithBadge"/> component.
/// </summary>
public sealed class SgTabItem
{
    /// <summary>
    /// Unique identifier for the tab. Used for active state tracking and closures.
    /// Auto-generated if not explicitly set.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// DOM ID for the tab button element (for ARIA). Auto-generated.
    /// </summary>
    public string TabId { get; set; } = "sg-twb-tab-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// DOM ID for the tab panel element (for ARIA). Auto-generated.
    /// </summary>
    public string PanelId { get; set; } = "sg-twb-panel-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// The title text displayed on the tab.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional icon rendered before the title.
    /// </summary>
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Optional badge content (e.g., count) displayed after the title.
    /// </summary>
    public object? Badge { get; set; }

    /// <summary>
    /// The visual variant for the badge. Default is <see cref="SgBadgeVariant.Default"/>.
    /// </summary>
    public SgBadgeVariant BadgeVariant { get; set; } = SgBadgeVariant.Default;

    /// <summary>
    /// The content rendered when this tab is active.
    /// </summary>
    public RenderFragment? Content { get; set; }

    /// <summary>
    /// When true, a close button is shown on the tab.
    /// </summary>
    public bool IsClosable { get; set; }

    /// <summary>
    /// When true, the tab cannot be selected or closed.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Additional CSS class applied to the tab element.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Arbitrary data associated with the tab for use in templates or event handlers.
    /// </summary>
    public object? Tag { get; set; }
}
