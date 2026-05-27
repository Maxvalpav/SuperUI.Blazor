namespace SuperUI.Enums;

/// <summary>
/// Controls which selected values appear as tags in the SgTreeSelect trigger.
/// </summary>
public enum SgTreeSelectShowStrategy
{
    /// <summary>Show all selected values.</summary>
    All,

    /// <summary>Show only leaf-node selections.</summary>
    ShowLeaf,

    /// <summary>Show only parent-node selections (hide leaves).</summary>
    ShowParent
}
