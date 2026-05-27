using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

/// <summary>
/// A single action item in a speed dial.
/// </summary>
public class SpeedDialAction
{
    /// <summary>SVG icon markup.</summary>
    public string Icon { get; set; } = "";
    /// <summary>Tooltip / label text.</summary>
    public string? Tooltip { get; set; }
    /// <summary>Click callback.</summary>
    public EventCallback OnClick { get; set; }
}
