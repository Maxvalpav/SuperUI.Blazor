using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Represents a single item in an <see cref="SgTimeline"/>.
/// </summary>
public sealed class TimelineItem
{
    /// <summary>Gets or sets the item title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the item description text.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the time or date label displayed next to the dot.</summary>
    public string? Time { get; set; }

    /// <summary>
    /// Gets or sets the dot color as a hex value or CSS variable (e.g. "#52c41a" or "var(--sg-color-success)").
    /// When null the default accent color is used.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the item status as a string. Supported values: "default", "active", "done", "pending", "error".
    /// Controls the dot appearance when <see cref="Color"/> is not set.
    /// </summary>
    public string Status { get; set; } = "default";

    /// <summary>
    /// Gets or sets the item status using the <see cref="SgTimelineStatus"/> enum.
    /// When set, overrides <see cref="Status"/>.
    /// </summary>
    public SgTimelineStatus? StatusEnum { get; set; }

    /// <summary>Gets or sets a custom icon rendered inside the dot.</summary>
    public RenderFragment? Icon { get; set; }

    /// <summary>Gets or sets fully custom dot content, replacing the default dot entirely.</summary>
    public RenderFragment? DotContent { get; set; }

    /// <summary>Gets or sets custom content rendered below the description.</summary>
    public RenderFragment? ExtraContent { get; set; }

    /// <summary>Whether this item can be clicked. Default is true when <see cref="SgTimeline.Clickable"/> is enabled.</summary>
    public bool? Clickable { get; set; }

    /// <summary>Whether this item is disabled (no click, muted style).</summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets the effective status string, preferring <see cref="StatusEnum"/> over <see cref="Status"/>.
    /// </summary>
    internal string EffectiveStatus => StatusEnum switch
    {
        SgTimelineStatus.Default => "default",
        SgTimelineStatus.InProgress => "active",
        SgTimelineStatus.Done => "done",
        SgTimelineStatus.Error => "error",
        SgTimelineStatus.Pending => "pending",
        null => Status,
        _ => Status
    };

    // ── New fields ──────────────────────────────────────────────────────────

    /// <summary>Key for grouping items under a shared header. Items with the same key are grouped.</summary>
    public string? GroupKey { get; set; }

    /// <summary>Optional header text rendered above the group. When null, <see cref="GroupKey"/> is shown.</summary>
    public string? GroupHeader { get; set; }

    /// <summary>Whether this item can be collapsed to hide extra content.</summary>
    public bool Collapsible { get; set; }

    /// <summary>Whether the extra content is collapsed by default.</summary>
    public bool Collapsed { get; set; }
}
