using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public partial class SgRow : ComponentBase
{
    /// <summary>
    /// Gets or sets the child content to render inside the row.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the gutter spacing between columns.
    /// Default is "16px".
    /// </summary>
    [Parameter] public string? Gutter { get; set; } = "16px";
    
    /// <summary>
    /// Gets or sets additional CSS classes to apply to the component.
    /// </summary>
    [Parameter] public string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets additional inline styles to apply to the component.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    private string ComputedStyle => $"display: flex; flex-wrap: wrap; gap: {Gutter}; {Style}";
}
