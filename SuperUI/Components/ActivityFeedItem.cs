using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Represents a single item in an <see cref="SgActivityFeed"/>.
/// </summary>
public sealed class ActivityFeedItem
{
    /// <summary>Gets or sets the event title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the event description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the time label (e.g. "5 мин назад").</summary>
    public string? Time { get; set; }

    /// <summary>Gets or sets the user display name or initials shown in the avatar.</summary>
    public string? User { get; set; }

    /// <summary>Gets or sets the URL of the user avatar image. When set, takes precedence over <see cref="User"/> initials.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Gets or sets the badge label shown in the top-right of the card.</summary>
    public string? BadgeText { get; set; }

    /// <summary>Gets or sets the badge variant. Default is <see cref="SgBadgeVariant.Default"/>.</summary>
    public SgBadgeVariant BadgeVariant { get; set; } = SgBadgeVariant.Default;

    /// <summary>Gets or sets the accent color used for the avatar background and timeline line.</summary>
    public string AccentColor { get; set; } = "var(--sui-accent, #2563eb)";

    /// <summary>Gets or sets a custom icon rendered inside the dot instead of the avatar.</summary>
    public RenderFragment? IconContent { get; set; }

    /// <summary>Gets or sets extra content rendered below the description inside the card.</summary>
    public RenderFragment? ExtraContent { get; set; }
}
