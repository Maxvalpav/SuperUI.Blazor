namespace SuperUI.Components;

/// <summary>
/// Arguments passed to <see cref="SgDataGrid{TItem}.OnBulkSave"/> when the user
/// confirms a bulk edit operation.
/// </summary>
public sealed class SgBulkEditEventArgs<TItem>
{
    /// <summary>The rows that were selected for bulk editing.</summary>
    public IReadOnlyList<TItem> Items { get; init; } = Array.Empty<TItem>();

    /// <summary>
    /// Dictionary of column key → new string value entered by the user.
    /// A <c>null</c> value means the field was left blank (skip update for that column).
    /// </summary>
    public IReadOnlyDictionary<string, string?> Changes { get; init; } =
        new Dictionary<string, string?>(StringComparer.Ordinal);
}
