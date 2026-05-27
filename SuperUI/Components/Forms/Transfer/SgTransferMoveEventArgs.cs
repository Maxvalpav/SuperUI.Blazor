namespace SuperUI.Components;

/// <summary>
/// Direction of a transfer item movement event.
/// </summary>
public enum SgTransferDirection
{
    /// <summary>Item moved from source to target.</summary>
    ToTarget,
    /// <summary>Item moved from target back to source.</summary>
    ToSource
}

/// <summary>
/// Mode controlling which transfer directions are allowed.
/// </summary>
public enum SgTransferMode
{
    /// <summary>Items can move in both directions (default).</summary>
    TwoWay,
    /// <summary>Items can only move from source to target.</summary>
    ToTargetOnly,
    /// <summary>Items can only move from target to source.</summary>
    ToSourceOnly,
    /// <summary>No items can move (display only).</summary>
    Disabled
}

/// <summary>
/// Event arguments for the <see cref="SgTransfer.OnItemMoved"/> callback.
/// </summary>
public sealed class SgTransferMoveEventArgs
{
    /// <summary>The key of the moved item.</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>The direction the item was moved.</summary>
    public SgTransferDirection Direction { get; init; }
}
