using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

/// <summary>
/// An item in a <see cref="SgSplitButton"/> dropdown menu.
/// </summary>
public class SplitButtonItem
{
    /// <summary>Display text.</summary>
    public string Text { get; set; } = "";
    /// <summary>SVG icon markup.</summary>
    public string? Icon { get; set; }
    /// <summary>Keyboard shortcut hint.</summary>
    public string? Shortcut { get; set; }
    /// <summary>When true, renders as a separator line instead of a button.</summary>
    public bool IsSeparator { get; set; }
    /// <summary>When true, the item appears in danger/red style.</summary>
    public bool Danger { get; set; }
    /// <summary>When true, the item is disabled.</summary>
    public bool Disabled { get; set; }
    /// <summary>Click callback for this item.</summary>
    public EventCallback OnClick { get; set; }
}
