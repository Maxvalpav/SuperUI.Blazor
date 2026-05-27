using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Single option for <see cref="SgCascader"/>.
/// </summary>
public sealed class SgCascaderOption
{
    /// <summary>Unique value of this option.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Display label for this option.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Child options for the next cascade level.</summary>
    public List<SgCascaderOption> Children { get; set; } = new();

    /// <summary>When true the option cannot be selected.</summary>
    public bool Disabled { get; set; }

    /// <summary>Optional icon (SVG markup) shown before the label.</summary>
    public string? Icon { get; set; }

    /// <summary>Optional badge text shown after the label.</summary>
    public string? BadgeText { get; set; }

    /// <summary>Optional badge variant.</summary>
    public SgBadgeVariant BadgeVariant { get; set; } = SgBadgeVariant.Default;

    /// <summary>
    /// Explicitly marks this option as a leaf (no children).
    /// When true, no expand arrow is shown even if <see cref="Children"/> is non-empty.
    /// </summary>
    public bool IsLeaf { get; set; }

    /// <summary>
    /// When true, this option acts as a non-selectable group header.
    /// Useful for visually grouping options within a column.
    /// </summary>
    public bool IsGroup { get; set; }

    /// <summary>
    /// Optional data payload attached to this option.
    /// Can hold any application-specific data.
    /// </summary>
    public object? Data { get; set; }
}
