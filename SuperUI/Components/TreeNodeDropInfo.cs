namespace SuperUI.Components;

/// <summary>
/// Describes a completed tree drag-and-drop operation.
/// </summary>
public sealed record TreeNodeDropInfo(
    TreeNode DraggedNode,
    TreeNode TargetNode,
    TreeNode? ParentNode,
    int InsertIndex,
    TreeNodeDropPosition Position);
