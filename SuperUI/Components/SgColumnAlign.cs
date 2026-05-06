namespace SuperUI.Components;

/// <summary>
/// Horizontal text alignment for a <see cref="SgDataGridColumn{TItem}"/> cell and header.
/// </summary>
public enum SgHAlign
{
    /// <summary>Inherits the default alignment (left for text, right for numbers).</summary>
    Default,
    /// <summary>Aligns content to the left.</summary>
    Left,
    /// <summary>Centers content horizontally.</summary>
    Center,
    /// <summary>Aligns content to the right.</summary>
    Right
}

/// <summary>
/// Vertical alignment for a <see cref="SgDataGridColumn{TItem}"/> cell.
/// </summary>
public enum SgVAlign
{
    /// <summary>Inherits the default vertical alignment (middle).</summary>
    Default,
    /// <summary>Aligns content to the top of the cell.</summary>
    Top,
    /// <summary>Centers content vertically.</summary>
    Middle,
    /// <summary>Aligns content to the bottom of the cell.</summary>
    Bottom
}
