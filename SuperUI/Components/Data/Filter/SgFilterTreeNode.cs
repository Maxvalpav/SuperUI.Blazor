namespace SuperUI.Components;

/// <summary>Represents a node in a filter value tree, supporting hierarchical date/time grouping and selection state.</summary>
public class SgFilterTreeNode
{
    /// <summary>The display label for this node.</summary>
    public string Label { get; set; } = "";
    /// <summary>The raw value key for leaf nodes (e.g. "yyyy-MM-dd" for dates).</summary>
    public string? Value { get; set; }
    /// <summary>Child nodes for hierarchical filtering.</summary>
    public List<SgFilterTreeNode>? Children { get; set; }
    /// <summary>Whether this node is expanded in the tree view.</summary>
    public bool IsExpanded { get; set; }
    /// <summary>Selection state: true = selected, false = not selected, null = indeterminate (some children selected).</summary>
    public bool? IsSelected { get; set; } = true;
    /// <summary>The year component for date-based filter nodes.</summary>
    public int? Year { get; set; }
    /// <summary>The month component for date-based filter nodes.</summary>
    public int? Month { get; set; }
    /// <summary>The day component for date-based filter nodes.</summary>
    public int? Day { get; set; }
}
