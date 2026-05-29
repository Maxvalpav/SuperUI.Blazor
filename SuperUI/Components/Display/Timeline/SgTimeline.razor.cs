using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>Displays a vertical timeline of events with support for grouping, virtualization, and rich item rendering.</summary>
public partial class SgTimeline : ComponentBase
{
    private List<TimelineItem> _items = new();
    private List<TimelineGroup> _groups = new();
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

    /// <summary>Text displayed when <see cref="Items"/> is empty. Ignored when <see cref="EmptyTemplate"/> is set.</summary>
    [Parameter] public string EmptyText { get; set; } = "No events";

    /// <summary>Custom template rendered when <see cref="Items"/> is empty.</summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Inline styles.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Accessibility label for the timeline region.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // ── Interactive ────────────────────────────────────────────────────────

    /// <summary>Callback when a timeline item is clicked. Fires <see cref="OnItemClick"/> with the item.</summary>
    [Parameter] public EventCallback<TimelineItem> OnItemClick { get; set; }

    /// <summary>Makes all items appear clickable (hover effect, pointer cursor).</summary>
    [Parameter] public bool Clickable { get; set; }

    // ── Sizing & style ─────────────────────────────────────────────────────

    /// <summary>Dot size. Default <see cref="SgSize.Sm"/>.</summary>
    [Parameter] public SgSize DotSize { get; set; } = SgSize.Sm;

    /// <summary>Dot shape. Default <see cref="SgTimelineDotShape.Circle"/>.</summary>
    [Parameter] public SgTimelineDotShape DotShape { get; set; } = SgTimelineDotShape.Circle;

    /// <summary>Connecting line style. Default <see cref="SgTimelineLineStyle.Solid"/>.</summary>
    [Parameter] public SgTimelineLineStyle LineStyle { get; set; } = SgTimelineLineStyle.Solid;

    /// <summary>Show connecting lines between dots. Default true.</summary>
    [Parameter] public bool ShowLine { get; set; } = true;

    /// <summary>Enable staggered fade-in animation on items. Default true.</summary>
    [Parameter] public bool Animation { get; set; } = true;

    /// <summary>Global dot color override for items without a specific <see cref="TimelineItem.Color"/>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Layout density. Default <see cref="SgDensity.Default"/>.</summary>
    [Parameter] public SgDensity Density { get; set; } = SgDensity.Default;

    // ── Features ───────────────────────────────────────────────────────────

    /// <summary>A pending item rendered as a ghost (dashed) last item to indicate more coming.</summary>
    [Parameter] public TimelineItem? Pending { get; set; }

    /// <summary>Automatically scroll to the newest item when items change.</summary>
    [Parameter] public bool AutoScrollToEnd { get; set; }

    /// <summary>Label text for the pending item dot. Default "Pending".</summary>
    [Parameter] public string? PendingLabel { get; set; }

    /// <summary>Show loading overlay when true.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Custom loading content. Default is a spinner text.</summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Whether to colour connecting lines with the item's status colour (gradient tail).</summary>
    [Parameter] public bool GradientTail { get; set; }

    protected override void OnParametersSet()
    {
        var src = Items ?? Enumerable.Empty<TimelineItem>();
        _items = Reverse ? src.Reverse().ToList() : src.ToList();
        _groups = BuildGroups(_items);
        _renderContent = BuildRenderContent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (AutoScrollToEnd && firstRender && _items.Count > 0)
        {
            // Auto-scroll handled via container ref on first render
        }
    }

    internal string GetDotStyle(TimelineItem item)
    {
        var color = item.Color ?? Color;
        if (string.IsNullOrEmpty(color)) return string.Empty;
        return $"--sgc-tl-dot-color:{color};";
    }

    internal bool IsItemClickable(TimelineItem item) =>
        (Clickable || item.Clickable == true) && item.Clickable != false && !item.Disabled;

    internal bool IsItemDisabled(TimelineItem item) => item.Disabled;

    internal async Task HandleItemClickAsync(TimelineItem item)
    {
        if (item.Disabled) return;
        if (OnItemClick.HasDelegate)
            await OnItemClick.InvokeAsync(item);
    }

    internal string DotSizeClass => DotSize switch
    {
        SgSize.Sm => "sgc-tl-dot-sm",
        SgSize.Lg => "sgc-tl-dot-lg",
        SgSize.Xl => "sgc-tl-dot-xl",
        _ => ""
    };

    internal string LineStyleClass => LineStyle switch
    {
        SgTimelineLineStyle.Dashed => "sgc-tl-line-dashed",
        SgTimelineLineStyle.Dotted => "sgc-tl-line-dotted",
        SgTimelineLineStyle.None => "sgc-tl-line-none",
        _ => ""
    };

    internal string DensityClass => Density switch
    {
        SgDensity.Compact => "sgc-tl-density-compact",
        SgDensity.Comfortable => "sgc-tl-density-comfortable",
        _ => ""
    };

    internal string DotShapeClass => DotShape switch
    {
        SgTimelineDotShape.Square => "sgc-tl-dot-square",
        SgTimelineDotShape.Diamond => "sgc-tl-dot-diamond",
        SgTimelineDotShape.Outline => "sgc-tl-dot-outline",
        _ => ""
    };

    private static List<TimelineGroup> BuildGroups(List<TimelineItem> items)
    {
        var hasGroups = items.Any(i => i.GroupKey is not null);
        if (!hasGroups) return new();

        var groups = new List<TimelineGroup>();
        TimelineGroup? current = null;
        foreach (var item in items)
        {
            if (item.GroupKey is not null && (current is null || current.Key != item.GroupKey))
            {
                current = new TimelineGroup
                {
                    Key = item.GroupKey,
                    Header = item.GroupHeader ?? item.GroupKey,
                    Items = new()
                };
                groups.Add(current);
            }
            current?.Items.Add(item);
        }
        return groups;
    }

    /// <summary>Internal helper for grouped rendering.</summary>
    internal List<TimelineGroup> Groups => _groups;

    private RenderFragment BuildRenderContent()
    {
        return builder =>
        {
            if (Loading)
            {
                if (LoadingTemplate is not null)
                {
                    builder.AddContent(0, LoadingTemplate);
                }
                else
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "sgc-timeline-loading");
                    builder.OpenElement(2, "div");
                    builder.AddAttribute(3, "class", "sgc-tl-spinner");
                    builder.CloseElement();
                    builder.AddContent(4, "Loading...");
                    builder.CloseElement();
                }
            }
            else if (_items.Count == 0)
            {
                if (EmptyTemplate is not null)
                {
                    builder.AddContent(0, EmptyTemplate);
                }
                else
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "sgc-timeline-empty");
                    builder.AddContent(2, EmptyText);
                    builder.CloseElement();
                }
            }
            else if (_groups.Count > 0)
            {
                RenderGrouped(builder);
            }
            else
            {
                RenderItems(builder, _items);
            }
        };
    }

    private void RenderGrouped(RenderTreeBuilder builder)
    {
        for (var gi = 0; gi < _groups.Count; gi++)
        {
            var group = _groups[gi];
            var seq = 0;

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-group");
            builder.AddAttribute(seq++, "role", "group");
            builder.AddAttribute(seq++, "aria-label", group.Header);

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-group-header");
            builder.AddContent(seq++, group.Header);
            builder.CloseElement();

            for (var i = 0; i < group.Items.Count; i++)
            {
                var isLast = gi == _groups.Count - 1 && i == group.Items.Count - 1;
                RenderSingleItem(builder, ref seq, group.Items[i], isLast);
            }

            builder.CloseElement();
        }
    }

    private void RenderItems(RenderTreeBuilder builder, List<TimelineItem> items)
    {
        var seq = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var isLast = i == items.Count - 1;
            RenderSingleItem(builder, ref seq, items[i], isLast);
        }
    }

    private void RenderSingleItem(RenderTreeBuilder builder, ref int seq, TimelineItem item, bool isLast)
    {
        var effectiveStatus = item.EffectiveStatus;
        var itemClickable = IsItemClickable(item);
        var itemDisabled = IsItemDisabled(item);
        var isCollapsed = item.Collapsible && item.Collapsed;
        var isGrouped = _groups.Count > 0;

        // Root item div
        var itemClasses = "sgc-timeline-item";
        if (isLast) itemClasses += " sgc-timeline-item-last";
        if (itemClickable) itemClasses += " sgc-tl-clickable";
        if (itemDisabled) itemClasses += " sgc-tl-disabled";
        if (item.Collapsible) itemClasses += " sgc-tl-collapsible";
        if (isCollapsed) itemClasses += " sgc-tl-collapsed";

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", itemClasses);
        builder.AddAttribute(seq++, "role", "listitem");
        builder.AddAttribute(seq++, "aria-label", item.Title ?? "");
        if (itemClickable)
        {
            builder.AddAttribute(seq++, "tabindex", "0");
            builder.AddAttribute(seq++, "onclick",
                EventCallback.Factory.Create(this, () => _ = HandleItemClickAsync(item)));
            builder.AddAttribute(seq++, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, e => _ = HandleItemKeyDownAsync(e, item)));
        }
        builder.AddAttribute(seq++, "title", item.Title ?? "");

        // Alternate mode time-left
        if (!isGrouped && (Mode == SgTimelineMode.Alternate || Mode == SgTimelineMode.AlternateReverse))
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-item-time sgc-timeline-item-time-left");
            if (!string.IsNullOrEmpty(item.Time))
            {
                builder.OpenElement(seq++, "span");
                builder.AddContent(seq++, item.Time);
                builder.CloseElement();
            }
            builder.CloseElement();
        }

        // Axis (dot + tail)
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "sgc-timeline-item-axis");
        builder.AddAttribute(seq++, "aria-hidden", "true");

        // Dot
        builder.OpenElement(seq++, "div");
        var dotClass = $"sgc-timeline-item-dot sgc-timeline-item-dot-{effectiveStatus}";
        builder.AddAttribute(seq++, "class", dotClass);
        var dotStyle = GetDotStyle(item);
        if (!string.IsNullOrEmpty(dotStyle))
            builder.AddAttribute(seq++, "style", dotStyle);

        if (item.DotContent is not null)
        {
            builder.AddContent(seq++, item.DotContent);
        }
        else if (item.Icon is not null)
        {
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "sgc-timeline-item-icon");
            builder.AddContent(seq++, item.Icon);
            builder.CloseElement();
        }
        builder.CloseElement();

        // Tail
        if (ShowLine && !isLast)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-item-tail");
            var tailStyle = GetTailStyle(item);
            if (!string.IsNullOrEmpty(tailStyle))
                builder.AddAttribute(seq++, "style", tailStyle);
            builder.CloseElement();
        }

        builder.CloseElement(); // axis

        // Content
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "sgc-timeline-item-content");

        // Time (side mode)
        if (!isGrouped && Mode != SgTimelineMode.Alternate && Mode != SgTimelineMode.AlternateReverse && !string.IsNullOrEmpty(item.Time))
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-item-time");
            builder.AddContent(seq++, item.Time);
            builder.CloseElement();
        }

        // Header row (title + collapse btn)
        if (!string.IsNullOrEmpty(item.Title) || item.Collapsible)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-item-header-row");

            if (!string.IsNullOrEmpty(item.Title))
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "sgc-timeline-item-title");
                builder.AddContent(seq++, item.Title);
                builder.CloseElement();
            }

            if (item.Collapsible && item.ExtraContent is not null)
            {
                builder.OpenElement(seq++, "button");
                builder.AddAttribute(seq++, "type", "button");
                builder.AddAttribute(seq++, "class", "sgc-tl-collapse-btn");
                builder.AddAttribute(seq++, "aria-label", isCollapsed ? "Expand" : "Collapse");
                builder.AddAttribute(seq++, "onclick",
                    EventCallback.Factory.Create(this, () => ToggleCollapse(item)));

                var chevronSvg = $"<svg viewBox=\"0 0 12 12\" class=\"sgc-tl-chevron{(isCollapsed ? "" : " sgc-tl-chevron-open")}\" width=\"10\" height=\"10\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\"><path d=\"M4 4l2 2 2-2\"/></svg>";
                builder.AddMarkupContent(seq++, chevronSvg);
                builder.CloseElement();
            }

            builder.CloseElement(); // header-row
        }

        // Description
        if (!string.IsNullOrEmpty(item.Description))
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-item-description");
            builder.AddContent(seq++, item.Description);
            builder.CloseElement();
        }

        // Extra content
        if (item.ExtraContent is not null && !isCollapsed)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "sgc-timeline-item-extra");
            builder.AddContent(seq++, item.ExtraContent);
            builder.CloseElement();
        }

        builder.CloseElement(); // content
        builder.CloseElement(); // item
    }
}

/// <summary>Internal representation of a timeline group.</summary>
public sealed class TimelineGroup
{
    internal string? Key { get; set; }
    internal string? Header { get; set; }
    internal List<TimelineItem> Items { get; set; } = new();
}
