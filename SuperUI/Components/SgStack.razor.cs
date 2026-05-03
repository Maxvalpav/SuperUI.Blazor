using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public partial class SgStack : ComponentBase
{
    /// <summary>
    /// Gets or sets the child content to render inside the stack.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the stack orientation. Default is <see cref="SgOrientation.Horizontal"/>.
    /// </summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;

    /// <summary>
    /// Gets or sets the spacing between stack items. Default is "8px".
    /// </summary>
    [Parameter] public string Gap { get; set; } = "8px";

    /// <summary>
    /// Gets or sets the spacing between stack items (alias for Gap).
    /// </summary>
    [Parameter] public string Spacing { get => Gap; set => Gap = value; }

    /// <summary>
    /// Gets or sets the cross-axis alignment of items. Default is <see cref="SgAlignItems.Stretch"/>.
    /// </summary>
    [Parameter] public SgAlignItems Align { get; set; } = SgAlignItems.Stretch;

    /// <summary>
    /// Gets or sets the main-axis alignment of items. Default is <see cref="SgJustifyContent.Start"/>.
    /// </summary>
    [Parameter] public SgJustifyContent Justify { get; set; } = SgJustifyContent.Start;

    /// <summary>
    /// Gets or sets whether items should wrap to the next line when space is insufficient.
    /// </summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes to apply to the component.
    /// </summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>
    /// Gets or sets additional inline styles to apply to the component.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    private string AlignCss => Align switch
    {
        SgAlignItems.Start   => "flex-start",
        SgAlignItems.Center  => "center",
        SgAlignItems.End     => "flex-end",
        _                    => "stretch"
    };

    private string JustifyCss => Justify switch
    {
        SgJustifyContent.Center       => "center",
        SgJustifyContent.End          => "flex-end",
        SgJustifyContent.SpaceBetween => "space-between",
        SgJustifyContent.SpaceAround  => "space-around",
        _                             => "flex-start"
    };

    private string ComputedStyle =>
        $"display: flex; " +
        $"flex-direction: {(Orientation == SgOrientation.Vertical ? "column" : "row")}; " +
        $"gap: {Gap}; " +
        $"align-items: {AlignCss}; " +
        $"justify-content: {JustifyCss}; " +
        $"flex-wrap: {(Wrap ? "wrap" : "nowrap")}; " +
        $"{Style}";
}
