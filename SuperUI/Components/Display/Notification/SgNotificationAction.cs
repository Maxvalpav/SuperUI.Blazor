using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Represents an action button rendered inside a notification item.
/// </summary>
public sealed class SgNotificationAction
{
    /// <summary>Button label text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional icon SVG markup.</summary>
    public string? Icon { get; set; }

    /// <summary>Button variant.</summary>
    public SgButtonVariant Variant { get; set; } = SgButtonVariant.Default;

    /// <summary>Callback invoked when the action is clicked.</summary>
    public Func<Task>? OnClick { get; set; }

    /// <summary>Navigation URL (optional).</summary>
    public string? Href { get; set; }
}
