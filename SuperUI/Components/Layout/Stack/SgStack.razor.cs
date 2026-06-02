using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Linear flex layout container that arranges its children either horizontally or vertically
/// with full control over gap, alignment, wrapping, sizing, padding and flex-grow/shrink.
/// </summary>
public partial class SgStack : SgComponentBase
{
    /// <summary>Child content to render inside the stack.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Stack orientation. Default is <see cref="SgOrientation.Horizontal"/>.</summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;

    /// <summary>Shortcut: renders the stack horizontally. Equivalent to <c>Orientation="SgOrientation.Horizontal"</c>.</summary>
    [Parameter] public bool Horizontal { get; set; }

    /// <summary>Shortcut: renders the stack vertically. Equivalent to <c>Orientation="SgOrientation.Vertical"</c>.</summary>
    [Parameter] public bool Vertical { get; set; }

    /// <summary>If <c>true</c>, items are laid out in reverse order along the main axis.</summary>
    [Parameter] public bool Reverse { get; set; }

    /// <summary>Gap between items (any CSS length). Default <c>"8px"</c>.</summary>
    [Parameter] public string Gap { get; set; } = "8px";

    /// <summary>Optional row-gap override (used when wrap is enabled or with grid-like layouts).</summary>
    [Parameter] public string? RowGap { get; set; }

    /// <summary>Optional column-gap override.</summary>
    [Parameter] public string? ColumnGap { get; set; }

    /// <summary>Alias for <see cref="Gap"/>.</summary>
    [Parameter] public string? Spacing { get => Gap; set => Gap = value; }

    /// <summary>Spacing size from theme. If set, overrides <see cref="Gap"/>.</summary>
    [Parameter] public SgSize? Space { get; set; }

    /// <summary>Visual density of the stack. Scales internal gap and padding via CSS tokens.</summary>
    [Parameter] public SgDensity Density { get; set; } = SgDensity.Default;

    /// <summary>If <c>true</c>, adds a divider between items.</summary>
    [Parameter] public bool ShowDividers { get; set; }

    /// <summary>Cross-axis alignment of items. Default <see cref="SgAlignItems.Stretch"/>.</summary>
    [Parameter] public SgAlignItems Align { get; set; } = SgAlignItems.Stretch;

    /// <summary>If <c>true</c>, forces all children to stretch across the cross axis (overrides <see cref="Align"/>).</summary>
    [Parameter] public bool Stretch { get; set; }

    /// <summary>Main-axis distribution. Default <see cref="SgJustifyContent.Start"/>.</summary>
    [Parameter] public SgJustifyContent Justify { get; set; } = SgJustifyContent.Start;

    /// <summary>If <c>true</c>, items wrap to next line/column when space is insufficient.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Render as inline-flex instead of flex.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>If <c>true</c>, the stack stretches to fill its parent's main axis (flex: 1).</summary>
    [Parameter] public bool Grow { get; set; }

    /// <summary>If set, controls flex-grow factor explicitly. Overrides <see cref="Grow"/>.</summary>
    [Parameter] public int? FlexGrow { get; set; }

    /// <summary>If set, controls flex-shrink factor explicitly.</summary>
    [Parameter] public int? FlexShrink { get; set; }

    /// <summary>Default <c>flex-basis</c> for direct children (any CSS length, e.g. <c>"120px"</c>, <c>"20%"</c>).</summary>
    [Parameter] public string? ItemBasis { get; set; }

    /// <summary>Optional CSS padding.</summary>
    [Parameter] public string? Padding { get; set; }

    /// <summary>Optional CSS margin.</summary>
    [Parameter] public string? Margin { get; set; }

    /// <summary>Optional CSS width.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Optional CSS height.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Optional CSS min-width.</summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>Optional CSS min-height.</summary>
    [Parameter] public string? MinHeight { get; set; }

    /// <summary>Optional CSS max-width.</summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>Optional CSS max-height.</summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>Optional background color.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>If <c>true</c>, the stack fills the full width of its parent.</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>If <c>true</c>, the stack fills the full height of its parent.</summary>
    [Parameter] public bool FullHeight { get; set; }

    /// <summary>HTML tag to render. Default <c>SgRowTag.Div</c>.</summary>
    [Parameter] public SgRowTag Tag { get; set; } = SgRowTag.Div;

    /// <summary>Legacy: HTML tag to render as a string (div/section/header/footer/nav/main). Use <see cref="Tag"/> instead.</summary>
    [Obsolete("Use the strongly-typed Tag parameter (SgRowTag) instead.")]
    [Parameter] public string? TagName
    {
        get => null;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (Enum.TryParse<SgRowTag>(value, ignoreCase: true, out var parsed))
            {
                Tag = parsed;
            }
        }
    }

    /// <summary>If <c>true</c>, the stack lifts on hover with a subtle shadow transition.</summary>
    [Parameter] public bool Hoverable { get; set; }

    /// <summary>Click event on the stack root.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private SgRowTag ResolvedTag => Tag;

    private bool hasOnClick => OnClick.HasDelegate;

    private SgOrientation ResolvedOrientation =>
        Vertical ? SgOrientation.Vertical :
        Horizontal ? SgOrientation.Horizontal :
        Orientation;

    private string AlignCss
    {
        get
        {
            if (Stretch) return "stretch";
            return Align switch
            {
                SgAlignItems.Start    => "flex-start",
                SgAlignItems.Center   => "center",
                SgAlignItems.End      => "flex-end",
                SgAlignItems.Baseline => "baseline",
                _                     => "stretch"
            };
        }
    }

    private string JustifyCss => Justify switch
    {
        SgJustifyContent.Center       => "center",
        SgJustifyContent.End          => "flex-end",
        SgJustifyContent.SpaceBetween => "space-between",
        SgJustifyContent.SpaceAround  => "space-around",
        SgJustifyContent.SpaceEvenly  => "space-evenly",
        _                             => "flex-start"
    };

    private string FlexDirectionCss
    {
        get
        {
            var dir = ResolvedOrientation == SgOrientation.Vertical ? "column" : "row";
            return Reverse ? dir + "-reverse" : dir;
        }
    }

    private string ComputedClass => Css("sg-stack")
        .AddClass("sg-stack-inline",    Inline)
        .AddClass("sg-stack-wrap",      Wrap)
        .AddClass("sg-stack-vertical",  ResolvedOrientation == SgOrientation.Vertical)
        .AddClass("sg-stack-horizontal",ResolvedOrientation != SgOrientation.Vertical)
        .AddClass("sg-stack-dividers",  ShowDividers)
        .AddClass("sg-stack-stretch",   Stretch)
        .AddClass("sg-row-hoverable",   Hoverable)
        .Build();

    private string? FixUnit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            return value + "px";
        }
        return value;
    }

    private double DensityGapScale => Density switch
    {
        SgDensity.Compact      => 0.5,
        SgDensity.Comfortable  => 1.5,
        _                      => 1.0
    };

    private string? ScaledGap(string? baseGap)
    {
        var g = FixUnit(baseGap);
        if (g is null) return null;
        if (Density == SgDensity.Default) return g;
        if (double.TryParse(g.TrimEnd('p','x','%','e','m','r','t'), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) && g.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            return (n * DensityGapScale).ToString("0.##", CultureInfo.InvariantCulture) + "px";
        }
        return g;
    }

    private string? ResolvedGap
    {
        get
        {
            var g = Space switch
            {
                SgSize.Sm     => "var(--sg-space-4)",
                SgSize.Md     => "var(--sg-space-8)",
                SgSize.Lg     => "var(--sg-space-16)",
                SgSize.Xl     => "var(--sg-space-24)",
                SgSize.FibMd  => "var(--sg-space-fib-4)",
                SgSize.FibLg  => "var(--sg-space-fib-5)",
                SgSize.FibXl  => "var(--sg-space-fib-6)",
                SgSize.FibXxl => "var(--sg-space-fib-7)",
                _ => !string.IsNullOrWhiteSpace(Gap) ? Gap : null
            };
            return g is null ? null : ScaledGap(g);
        }
    }

    private string ComputedStyle => Styles()
        .AddStyle("display",          Inline ? "inline-flex" : "flex")
        .AddStyle("flex-direction",   FlexDirectionCss)
        .AddStyle("row-gap",          ScaledGap(RowGap) ?? ResolvedGap, ResolvedGap != null || RowGap != null)
        .AddStyle("column-gap",       ScaledGap(ColumnGap) ?? ResolvedGap, ResolvedGap != null || ColumnGap != null)
        .AddStyle("align-items",      AlignCss)
        .AddStyle("justify-content",  JustifyCss)
        .AddStyle("flex-wrap",        Wrap ? "wrap" : "nowrap")
        .AddStyle("flex-grow",        FlexGrow?.ToString(), FlexGrow.HasValue)
        .AddStyle("flex",             "1 1 auto", Grow && !FlexGrow.HasValue)
        .AddStyle("flex-shrink",      FlexShrink?.ToString(), FlexShrink.HasValue)
        .AddStyle("--sg-stack-item-basis", FixUnit(ItemBasis), !string.IsNullOrWhiteSpace(ItemBasis))
        .AddStyle("width",            "100%",  FullWidth)
        .AddStyle("width",            Width,   !FullWidth && !string.IsNullOrWhiteSpace(Width))
        .AddStyle("height",           "100%",  FullHeight)
        .AddStyle("height",           Height,  !FullHeight && !string.IsNullOrWhiteSpace(Height))
        .AddStyle("min-width",        MinWidth,  !string.IsNullOrWhiteSpace(MinWidth))
        .AddStyle("min-height",       MinHeight, !string.IsNullOrWhiteSpace(MinHeight))
        .AddStyle("max-width",        MaxWidth,  !string.IsNullOrWhiteSpace(MaxWidth))
        .AddStyle("max-height",       MaxHeight, !string.IsNullOrWhiteSpace(MaxHeight))
        .AddStyle("padding",          Padding,   !string.IsNullOrWhiteSpace(Padding))
        .AddStyle("margin",           Margin,    !string.IsNullOrWhiteSpace(Margin))
        .AddStyle("background",       Background,!string.IsNullOrWhiteSpace(Background))
        .Build();

    private Task HandleClick(MouseEventArgs args) =>
        OnClick.HasDelegate ? OnClick.InvokeAsync(args) : Task.CompletedTask;
}
