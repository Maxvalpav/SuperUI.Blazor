namespace SuperUI.Enums;

/// <summary>Drag-and-drop insertion position relative to a tree node.</summary>
public enum TreeNodeDropPosition
{
    /// <summary>Position not determined yet.</summary>
    None = 0,
    /// <summary>Insert before the target node.</summary>
    Before = 1,
    /// <summary>Insert inside (as a child of) the target node.</summary>
    Inside = 2,
    /// <summary>Insert after the target node.</summary>
    After = 3
}
