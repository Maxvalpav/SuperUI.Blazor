using Microsoft.AspNetCore.Components;

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
    /// Gets or sets the dot color as a hex value or CSS variable (e.g. "#52c41a" or "var(--sui-success)").
    /// When null the default accent color is used.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the item status. Supported values: "default", "active", "done", "pending", "error".
    /// Controls the dot appearance when <see cref="Color"/> is not set.
    /// </summary>
    public string Status { get; set; } = "default";

    /// <summary>Gets or sets a custom icon rendered inside the dot.</summary>
    public RenderFragment? Icon { get; set; }

    /// <summary>Gets or sets fully custom dot content, replacing the default dot entirely.</summary>
    public RenderFragment? DotContent { get; set; }

    /// <summary>Gets or sets custom content rendered below the description.</summary>
    public RenderFragment? ExtraContent { get; set; }
}
