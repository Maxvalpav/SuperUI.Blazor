namespace SuperUI.Enums;

/// <summary>Strategy for displaying checked indicators in selection components.</summary>
public enum SgCheckedStrategy
{
    /// <summary>Show checked indicators for all selected items.</summary>
    All,
    /// <summary>Show checked indicators only for leaf items (items within groups).</summary>
    ShowLeaf,
    /// <summary>Show checked indicators only for parent/group-level items.</summary>
    ShowParent
}
