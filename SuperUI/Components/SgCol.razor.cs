using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public partial class SgCol : ComponentBase
{
    /// <summary>
    /// Gets or sets the gutter spacing inherited from the parent <see cref="SgRow"/>.
    /// </summary>
    [CascadingParameter] public string? Gutter { get; set; }
    
    /// <summary>
    /// Gets or sets the child content to render inside the column.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the column span width (1-12).
    /// Default is 12 (full width).
    /// </summary>
    [Parameter] public int Span { get; set; } = 12;

    /// <summary>
    /// Gets or sets the column span for extra small screens (below 576px).
    /// </summary>
    [Parameter] public int Xs { get; set; }
    
    /// <summary>
    /// Gets or sets the column span for small screens (576px and up).
    /// </summary>
    [Parameter] public int Sm { get; set; }

    /// <summary>
    /// Gets or sets the column span for medium screens (768px and up).
    /// </summary>
    [Parameter] public int Md { get; set; }

    /// <summary>
    /// Gets or sets the column span for large screens (992px and up).
    /// </summary>
    [Parameter] public int Lg { get; set; }

    /// <summary>
    /// Gets or sets the column span for extra large screens (1200px and up).
    /// </summary>
    [Parameter] public int Xl { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes to apply to the component.
    /// </summary>
    [Parameter] public string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets additional inline styles to apply to the component.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    private string ComputedStyle => Style ?? "";
    
    private string ComputedClass
    {
        get
        {
            var classes = new List<string> { "sg-col" };
            classes.Add($"sg-col-{Span}");
            if (Xs > 0) classes.Add($"sg-col-xs-{Xs}");
            if (Sm > 0) classes.Add($"sg-col-sm-{Sm}");
            if (Md > 0) classes.Add($"sg-col-md-{Md}");
            if (Lg > 0) classes.Add($"sg-col-lg-{Lg}");
            if (Xl > 0) classes.Add($"sg-col-xl-{Xl}");
            if (!string.IsNullOrEmpty(CssClass)) classes.Add(CssClass);
            return string.Join(" ", classes);
        }
    }
}
