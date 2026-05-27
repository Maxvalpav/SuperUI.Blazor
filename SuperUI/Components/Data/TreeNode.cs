using SuperUI.Enums;

namespace SuperUI.Components;

public sealed class TreeNode
{
    /// <summary>Unique key for this node.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional icon (SVG markup or emoji).</summary>
    public string? Icon { get; set; }

    /// <summary>Arbitrary data payload.</summary>
    public object? Tag { get; set; }

    /// <summary>Child nodes.</summary>
    public List<TreeNode> Children { get; set; } = new();

    /// <summary>Whether this node is initially expanded.</summary>
    public bool Expanded { get; set; }

    /// <summary>Whether this node is in loading state (e.g., lazy loading).</summary>
    public bool Loading { get; set; }

    /// <summary>Whether this node is checked (initial state in checkable mode).</summary>
    public bool Checked { get; set; }

    /// <summary>Additional CSS class for this row.</summary>
    public string? CssClass { get; set; }

    /// <summary>Whether this node is disabled.</summary>
    public bool Disabled { get; set; }

    /// <summary>Whether this node is a leaf (has no expandable children even if Children is empty).</summary>
    public bool IsLeaf { get; set; }

    /// <summary>Optional badge text shown next to the label.</summary>
    public string? BadgeText { get; set; }

    /// <summary>Badge variant for <see cref="BadgeText"/>.</summary>
    public SgBadgeVariant BadgeVariant { get; set; }
}
