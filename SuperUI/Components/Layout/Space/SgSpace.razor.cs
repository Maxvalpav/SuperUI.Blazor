using Microsoft.AspNetCore.Components;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Set components spacing.
/// </summary>
public partial class SgSpace : SgComponentBase
{
    /// <summary>Items to be spaced.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Space orientation. Default is <see cref="SgOrientation.Horizontal"/>.</summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;

    /// <summary>
    /// Space size. Can be "small", "middle", "large" or a custom pixel value (e.g. "16px").
    /// Default is "small" (8px).
    /// </summary>
    [Parameter] public string Size { get; set; } = "small";

    /// <summary>Alignment of items. Default is <see cref="SgAlignItems.Center"/>.</summary>
    [Parameter] public SgAlignItems Align { get; set; } = SgAlignItems.Center;

    /// <summary>Whether to wrap lines. Only works in horizontal orientation.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Optional separator between items.</summary>
    [Parameter] public RenderFragment? Split { get; set; }

    /// <summary>If true, the space fills the full width of its parent.</summary>
    [Parameter] public bool FullWidth { get; set; }

    private string GapValue => Size.ToLower() switch
    {
        "small"  => "8px",
        "middle" => "16px",
        "large"  => "24px",
        _        => Size.Contains("px") ? Size : $"{Size}px"
    };

    private string AlignCss => Align switch
    {
        SgAlignItems.Start    => "flex-start",
        SgAlignItems.Center   => "center",
        SgAlignItems.End      => "flex-end",
        SgAlignItems.Baseline => "baseline",
        _                     => "stretch"
    };

    private string ComputedStyle => 
        $"display: {(FullWidth ? "flex" : "inline-flex")};" +
        $"flex-direction: {(Orientation == SgOrientation.Horizontal ? "row" : "column")};" +
        $"gap: {GapValue};" +
        $"align-items: {AlignCss};" +
        $"flex-wrap: {(Wrap ? "wrap" : "nowrap")};" +
        (FullWidth ? "width: 100%;" : "") +
        Style;

    private string ComputedClass => $"sgc-space {CssClass}";
}
