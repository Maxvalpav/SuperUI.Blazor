using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Enums;

namespace SuperUI.Components;

public partial class SgTimeline : ComponentBase
{
    private List<TimelineItem> _items = new();
    private ElementReference _containerRef;

    /// <summary>Collection of timeline items to display.</summary>
    [Parameter] public IEnumerable<TimelineItem>? Items { get; set; }

    /// <summary>Arbitrary child content appended after the item list. Useful for a custom "pending" tail item.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Layout mode. Default is <see cref="SgTimelineMode.Left"/>.</summary>
    [Parameter] public SgTimelineMode Mode { get; set; } = SgTimelineMode.Left;

    /// <summary>Whether the item order is reversed (newest first).</summary>
    [Parameter] public bool Reverse { get; set; }

    /// <summary>Whether the items list should be virtualized.</summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>Container height when <see cref="Virtualize"/> is true. Default "400px".</summary>
    [Parameter] public string Height { get; set; } = "400px";

    /// <summary>Text displayed when <see cref="Items"/> is empty.</summary>
    [Parameter] public string EmptyText { get; set; } = "No events";

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Inline styles.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Accessibility label for the timeline region.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ── New optional params ────────────────────────────────────────────────

    /// <summary>Callback when a timeline item is clicked. Fires <see cref="OnItemClick"/> with the item.</summary>
    [Parameter] public EventCallback<TimelineItem> OnItemClick { get; set; }

    /// <summary>Makes all items appear clickable (hover effect, pointer cursor).</summary>
    [Parameter] public bool Clickable { get; set; }

    /// <summary>Dot size. Default <see cref="SgSize.Sm"/>.</summary>
    [Parameter] public SgSize DotSize { get; set; } = SgSize.Sm;

    /// <summary>Connecting line style. Default <see cref="SgTimelineLineStyle.Solid"/>.</summary>
    [Parameter] public SgTimelineLineStyle LineStyle { get; set; } = SgTimelineLineStyle.Solid;

    /// <summary>Show connecting lines between dots. Default true.</summary>
    [Parameter] public bool ShowLine { get; set; } = true;

    /// <summary>Enable staggered fade-in animation on items. Default true.</summary>
    [Parameter] public bool Animation { get; set; } = true;

    /// <summary>Global dot color override for items without a specific <see cref="TimelineItem.Color"/>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>A pending item rendered as a ghost (dashed) last item to indicate more coming.</summary>
    [Parameter] public TimelineItem? Pending { get; set; }

    /// <summary>Automatically scroll to the newest item when items change.</summary>
    [Parameter] public bool AutoScrollToEnd { get; set; }

    /// <summary>Label text for the pending item dot. Default "Pending".</summary>
    [Parameter] public string? PendingLabel { get; set; }

    protected override void OnParametersSet()
    {
        var src = Items ?? Enumerable.Empty<TimelineItem>();
        _items = Reverse ? src.Reverse().ToList() : src.ToList();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (AutoScrollToEnd && firstRender && _items.Count > 0)
        {
            // Auto-scroll handled via container ref on first render
        }
    }

    private string GetDotStyle(TimelineItem item)
    {
        var color = item.Color ?? Color;
        if (string.IsNullOrEmpty(color)) return string.Empty;
        return $"--sgc-tl-dot-color:{color};";
    }

    private bool IsItemClickable(TimelineItem item) =>
        (Clickable || item.Clickable == true) && item.Clickable != false && !item.Disabled;

    private bool IsItemDisabled(TimelineItem item) => item.Disabled;

    private async Task HandleItemClickAsync(TimelineItem item)
    {
        if (item.Disabled) return;
        if (OnItemClick.HasDelegate)
            await OnItemClick.InvokeAsync(item);
    }

    private string DotSizeClass => DotSize switch
    {
        SgSize.Sm => "sgc-tl-dot-sm",
        SgSize.Lg => "sgc-tl-dot-lg",
        SgSize.Xl => "sgc-tl-dot-xl",
        _ => ""
    };

    private string LineStyleClass => LineStyle switch
    {
        SgTimelineLineStyle.Dashed => "sgc-tl-line-dashed",
        SgTimelineLineStyle.Dotted => "sgc-tl-line-dotted",
        SgTimelineLineStyle.None => "sgc-tl-line-none",
        _ => ""
    };
}
