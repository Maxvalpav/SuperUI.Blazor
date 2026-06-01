using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Represents a transient toast notification.
/// </summary>
public sealed class SgNotificationToastItem
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Toast title.</summary>
    public string? Title { get; set; }

    /// <summary>Toast body message.</summary>
    public string? Message { get; set; }

    /// <summary>Variant that controls the accent color. Default is <see cref="SgBadgeVariant.Default"/>.</summary>
    public SgBadgeVariant Variant { get; set; } = SgBadgeVariant.Default;

    /// <summary>Custom icon rendered in the leading slot.</summary>
    public RenderFragment? IconContent { get; set; }

    /// <summary>Whether the toast is closing (for exit animation).</summary>
    public bool IsClosing { get; set; }

    /// <summary>Auto-dismiss duration in milliseconds. 0 means sticky.</summary>
    public int DurationMs { get; set; } = 4000;
}
