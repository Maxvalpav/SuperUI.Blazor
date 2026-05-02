using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public partial class SgTimeline : ComponentBase
{
    private List<TimelineItem> _items = new();

    /// <summary>
    /// Gets or sets the collection of timeline items to display.
    /// </summary>
    [Parameter] public IEnumerable<TimelineItem>? Items { get; set; }

    /// <summary>
    /// Gets or sets arbitrary child content appended after the item list.
    /// Useful for adding a custom "pending" tail item.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the layout mode. Supported values: "left" (default), "right", "alternate".
    /// </summary>
    [Parameter] public string Mode { get; set; } = "left";

    /// <summary>
    /// Gets or sets whether the item order is reversed (newest first).
    /// </summary>
    [Parameter] public bool Reverse { get; set; }

    /// <summary>
    /// Gets or sets whether the items list should be virtualized.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// Gets or sets the container height used when <see cref="Virtualize"/> is true.
    /// Default is "400px".
    /// </summary>
    [Parameter] public string Height { get; set; } = "400px";

    /// <summary>
    /// Gets or sets the text displayed when <see cref="Items"/> is empty.
    /// </summary>
    [Parameter] public string EmptyText { get; set; } = "Нет событий";

    /// <summary>
    /// Gets or sets additional CSS class names for the root element.
    /// </summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>
    /// Gets or sets inline styles for the root element.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    protected override void OnParametersSet()
    {
        var src = Items ?? Enumerable.Empty<TimelineItem>();
        _items = Reverse ? src.Reverse().ToList() : src.ToList();
    }

    private static string GetDotStyle(TimelineItem item)
    {
        if (string.IsNullOrEmpty(item.Color)) return string.Empty;
        return $"border-color:{item.Color};color:{item.Color};";
    }
}
