using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public class TimelineItem
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Time { get; set; }
    public string? Color { get; set; } // hex or css variable
    public RenderFragment? Icon { get; set; }
    public RenderFragment? DotContent { get; set; }
}
