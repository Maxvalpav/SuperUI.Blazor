using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public partial class SgStack : ComponentBase
{
    /// <summary>
    /// Gets or sets the child content to render inside the stack.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the stack orientation.
    /// Supported values: "horizontal", "vertical". Default is "horizontal".
    /// </summary>
    [Parameter] public string Orientation { get; set; } = "horizontal";
    
    /// <summary>
    /// Gets or sets the spacing between stack items.
    /// Default is "8px".
    /// </summary>
    [Parameter] public string Gap { get; set; } = "8px";

    /// <summary>
    /// Gets or sets the spacing between stack items (alias for Gap).
    /// </summary>
    [Parameter] public string Spacing { get => Gap; set => Gap = value; }
    
    /// <summary>
    /// Gets or sets the cross-axis alignment of items.
    /// Supported values: "flex-start", "center", "flex-end", "stretch". Default is "stretch".
    /// </summary>
    [Parameter] public string Align { get; set; } = "stretch";
    
    /// <summary>
    /// Gets or sets the main-axis alignment of items.
    /// Supported values: "flex-start", "center", "flex-end", "space-between". Default is "flex-start".
    /// </summary>
    [Parameter] public string Justify { get; set; } = "flex-start";
    
    /// <summary>
    /// Gets or sets whether items should wrap to the next line when space is insufficient.
    /// Default is false.
    /// </summary>
    [Parameter] public bool Wrap { get; set; } = false;

    /// <summary>
    /// Gets or sets additional CSS classes to apply to the component.
    /// </summary>
    [Parameter] public string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets additional inline styles to apply to the component.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    private string ComputedStyle => 
        $"display: flex; " +
        $"flex-direction: {(Orientation == "vertical" ? "column" : "row")}; " +
        $"gap: {Gap}; " +
        $"align-items: {Align}; " +
        $"justify-content: {Justify}; " +
        $"flex-wrap: {(Wrap ? "wrap" : "nowrap")}; " +
        $"{Style}";
}
