using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public partial class SgTimeline : ComponentBase
{
    [Parameter] public IEnumerable<TimelineItem>? Items { get; set; }
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    /// <summary>
    /// Left, Right, Alternate, or Horizontal
    /// </summary>
    [Parameter] public string Mode { get; set; } = "Left";
    
    /// <summary>
    /// Gets or sets whether the items should be virtualized.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>
    /// Gets or sets the height for virtualization.
    /// </summary>
    [Parameter] public string Height { get; set; } = "400px";

    [Parameter] public bool Reverse { get; set; }
    
    [Parameter] public string? CssClass { get; set; }
    
    [Parameter] public string? Style { get; set; }

    private string GetDotStyle(TimelineItem item)
    {
        if (string.IsNullOrEmpty(item.Color)) return "";
        
        if (item.Color.StartsWith("var("))
            return $"border-color: {item.Color}; color: {item.Color};";
        
        return $"border-color: {item.Color}; color: {item.Color};";
    }
}
