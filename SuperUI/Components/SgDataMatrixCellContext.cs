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
}
