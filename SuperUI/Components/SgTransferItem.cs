namespace SuperUI.Components;

/// <summary>
/// Single item for <see cref="SgTransfer"/>.
/// </summary>
public sealed class SgTransferItem
{
    /// <summary>Unique key of the item.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable title displayed in the list.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description shown below the title.</summary>
    public string? Description { get; set; }

    /// <summary>When true the item cannot be moved.</summary>
    public bool Disabled { get; set; }
}
