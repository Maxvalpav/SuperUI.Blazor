namespace SuperUI.Components;

/// <summary>
/// Context passed to the <see cref="SgDataMatrix{TItem}.CellTemplate"/> render fragment.
/// </summary>
/// <typeparam name="TItem">Type of the data item.</typeparam>
public sealed class SgDataMatrixCellContext<TItem>
{
    /// <summary>Row key for this cell.</summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>Column key for this cell.</summary>
    public string ColumnKey { get; set; } = string.Empty;

    /// <summary>Items that belong to this row/column intersection.</summary>
    public List<TItem> Items { get; set; } = new();

    /// <summary>Pre-computed numeric aggregate when <see cref="SgDataMatrix{TItem}.ValueField"/> is set.</summary>
    public decimal? AggregateValue { get; set; }

    /// <summary>True when this cell is currently in edit mode (only for <see cref="SgDataMatrix{TItem}.EditTemplate"/>).</summary>
    public bool IsEditing { get; set; }

    /// <summary>Begin editing this cell (no-op when <see cref="SgDataMatrix{TItem}.EditTemplate"/> is null).</summary>
    public Func<Task> BeginEdit { get; set; } = () => Task.CompletedTask;

    /// <summary>Commit pending edits and exit edit mode. Triggers <see cref="SgDataMatrix{TItem}.OnCellCommit"/>.</summary>
    public Func<Task> Commit { get; set; } = () => Task.CompletedTask;

    /// <summary>Cancel edit and exit edit mode without firing OnCellCommit.</summary>
    public Func<Task> Cancel { get; set; } = () => Task.CompletedTask;
}
