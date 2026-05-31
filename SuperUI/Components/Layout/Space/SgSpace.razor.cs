using Microsoft.AspNetCore.Components;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Set components spacing. Arranges children with consistent gaps, optional separators,
/// and supports both horizontal and vertical orientations.
/// </summary>
public partial class SgSpace : SgComponentBase
{
    /// <summary>Items to be spaced.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Space orientation. Default is <see cref="SgOrientation.Horizontal"/>.</summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;

    /// <summary>
    /// Space size. Can be "small", "middle", "large" or a custom CSS length (e.g. "16px").
    /// Default is "small" (8px).
    /// </summary>
    [Parameter] public string Size { get; set; } = "small";

    /// <summary>
    /// Space size from design system. If set, overrides <see cref="Size"/>.
    /// </summary>
    [Parameter] public SgSize? Space { get; set; }

    /// <summary>Alignment of items. Default is <see cref="SgAlignItems.Center"/>.</summary>
    [Parameter] public SgAlignItems Align { get; set; } = SgAlignItems.Center;

    /// <summary>Whether to wrap lines. Only works in horizontal orientation.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Optional separator rendered between each child item.</summary>
    [Parameter] public RenderFragment? Split { get; set; }

    /// <summary>If true, the space fills the full width of its parent.</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>If true, renders as inline-flex instead of flex.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>If <c>true</c>, the container lifts on hover.</summary>
    [Parameter] public bool Hoverable { get; set; }

    private string ResolvedGap => Space switch
    {
        SgSize.Sm     => "var(--sg-space-4)",       // 8px
        SgSize.Md     => "var(--sg-space-8)",       // 16px
        SgSize.Lg     => "var(--sg-space-16)",      // 32px
        SgSize.Xl     => "var(--sg-space-24)",      // 48px
        SgSize.FibMd  => "var(--sg-space-fib-4)",   // 13px
        SgSize.FibLg  => "var(--sg-space-fib-5)",   // 21px
        SgSize.FibXl  => "var(--sg-space-fib-6)",   // 34px
        SgSize.FibXxl => "var(--sg-space-fib-7)",   // 55px
        _ => Size.ToLower() switch
        {
            "small"  => "8px",
            "middle" => "16px",
            "large"  => "24px",
            _ => Size.Contains("px") || Size.Contains("rem") || Size.Contains("var") ? Size : $"{Size}px"
        }
    };

    private string AlignCss => Align switch
    {
        SgAlignItems.Start    => "flex-start",
        SgAlignItems.Center   => "center",
        SgAlignItems.End      => "flex-end",
        SgAlignItems.Baseline => "baseline",
        _                     => "stretch"
    };

    private bool IsVertical => Orientation == SgOrientation.Vertical;

    private string ComputedStyle
    {
        get
        {
            var display = (FullWidth && !Inline) ? "flex" : "inline-flex";
            var direction = IsVertical ? "column" : "row";
            var wrapVal = Wrap ? "wrap" : "nowrap";
            var style = $"display:{display};flex-direction:{direction};gap:{ResolvedGap};align-items:{AlignCss};flex-wrap:{wrapVal};";
            if (FullWidth) style += "width:100%;";
            if (!string.IsNullOrWhiteSpace(Style)) style += Style;
            return style;
        }
    }

    private string ComputedClass =>
        $"sgc-space{(Hoverable ? " sg-row-hoverable" : "")}{(!string.IsNullOrWhiteSpace(CssClass) ? " " + CssClass : "")}";
}
