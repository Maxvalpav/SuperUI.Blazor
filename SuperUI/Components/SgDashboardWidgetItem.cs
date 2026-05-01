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
    /// Arbitrary data object attached to the widget for use in templates.
    /// </summary>
    public object? Tag { get; set; }
}
