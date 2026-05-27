namespace SuperUI.Components;

public sealed class TreeNode
{
    public string Key { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public object? Tag { get; set; }
    public List<TreeNode> Children { get; set; } = new();
    public bool Expanded { get; set; }
    public bool Loading { get; set; }
    public bool Checked { get; set; }
    public string? CssClass { get; set; }
}
