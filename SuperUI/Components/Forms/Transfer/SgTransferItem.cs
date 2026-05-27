using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Single item for <see cref="SgTransfer"/>.
/// </summary>
public sealed class SgTransferItem
{
    /// <summary>Unique key of the item.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable title displayed in the list.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description shown below the title.</summary>
    public string? Description { get; set; }

    /// <summary>When true the item cannot be moved.</summary>
    public bool Disabled { get; set; }

    /// <summary>Optional SVG icon markup shown before the title.</summary>
    public string? Icon { get; set; }

    /// <summary>Optional badge text shown on the item.</summary>
    public string? Badge { get; set; }

    /// <summary>Badge variant when <see cref="Badge"/> is set.</summary>
    public SgBadgeVariant BadgeVariant { get; set; } = SgBadgeVariant.Default;

    /// <summary>Optional group/category name for visual grouping.</summary>
    public string? Group { get; set; }

    /// <summary>Optional avatar initials (2-3 characters) shown as a colored circle.</summary>
    public string? Avatar { get; set; }

    /// <summary>Optional avatar background color (CSS color). Used when <see cref="Avatar"/> is set.</summary>
    public string AvatarColor { get; set; } = "var(--sg-primary)";

    /// <summary>
    /// Optional child items for tree/hierarchy mode.
    /// When set, the item renders as a parent node with expand/collapse.
    /// </summary>
    public List<SgTransferItem>? Children { get; set; }

    /// <summary>Whether the children are expanded in tree mode. Default true.</summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>Optional metadata for programmatic use (not rendered).</summary>
    public object? Tag { get; set; }
}
