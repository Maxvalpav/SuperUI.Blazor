using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

/// <summary>
/// Represents a single widget inside an <see cref="SgDashboard"/>.
/// </summary>
public sealed class SgDashboardWidgetItem
{
    /// <summary>
    /// Unique identifier for the widget.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Display title shown in the widget header.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Optional secondary line shown beneath the title.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Optional SVG icon markup displayed in the header.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// How many grid columns the widget spans. Default is 1.
    /// </summary>
    public int ColumnSpan { get; set; } = 1;

    /// <summary>
    /// How many grid rows the widget spans. Default is 1.
    /// </summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>
    /// When true, the widget cannot be dragged, resized or deleted regardless
    /// of the parent dashboard's permissions.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// When true, a skeleton shimmer is rendered in the widget body.
    /// </summary>
    public bool Loading { get; set; }

    /// <summary>
    /// Optional accent CSS color used for the icon and the top accent bar.
    /// Falls back to the global accent color.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Optional render fragment shown to the right of the title (badges, status, custom buttons).
    /// </summary>
    public RenderFragment? Action { get; set; }

    /// <summary>
    /// Arbitrary data object attached to the widget for use in templates.
    /// </summary>
    public object? Tag { get; set; }
}
