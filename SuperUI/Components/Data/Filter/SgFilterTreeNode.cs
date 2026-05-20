namespace SuperUI.Components;

public class SgFilterTreeNode
{
    public string Label { get; set; } = "";
    public string? Value { get; set; } // The rawKey for leaf nodes (yyyy-MM-dd)
    public List<SgFilterTreeNode>? Children { get; set; }
    public bool IsExpanded { get; set; }
    public bool? IsSelected { get; set; } = true; // null = indeterminate

    // Metadata for dates
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
}
