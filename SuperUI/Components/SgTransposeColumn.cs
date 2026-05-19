namespace SuperUI.Components;

using SuperUI.Enums;

/// <summary>
/// Defines a field (row) in an <see cref="SgTransposeGrid{TItem}"/>.
/// </summary>
public sealed class SgTransposeColumn<TItem>
{
    /// <summary>Field label shown in the left column.</summary>
    public string Title { get; set; } = "";

    /// <summary>Selector that extracts the value from an item.</summary>
    public Func<TItem, object?>? ValueSelector { get; set; }

    /// <summary>Optional format string (e.g. "C0", "dd.MM.yyyy", "{0:N2} кг").</summary>
    public string? Format { get; set; }

    /// <summary>Optional section name for grouping rows.</summary>
    public string? Section { get; set; }

    /// <summary>Optional SVG icon shown before the field label.</summary>
    public string? Icon { get; set; }

    /// <summary>Optional tooltip on the field label.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Optional hint icon with text shown next to the label.</summary>
    public string? Hint { get; set; }

    /// <summary>When true, the row is highlighted with an accent left border.</summary>
    public bool Highlighted { get; set; }

    /// <summary>When true, the row is shown in a muted/secondary style.</summary>
    public bool Muted { get; set; }

    /// <summary>When true, cells in this row can be edited by double-clicking.</summary>
    public bool Editable { get; set; }

    /// <summary>When true, the row is hidden.</summary>
    public bool Hidden { get; set; }

    /// <summary>Aggregate function shown in the footer for this row.</summary>
    public SgTransposeAggregate Aggregate { get; set; } = SgTransposeAggregate.None;

    /// <summary>Callback invoked when a cell value is edited. Receives (item, newStringValue).</summary>
    public Action<TItem, string?>? OnValueChanged { get; set; }
}

/// <summary>Context passed to the <see cref="SgTransposeGrid{TItem}.CellTemplate"/>.</summary>
public sealed class SgTransposeCellContext<TItem>
{
    public SgTransposeCellContext(TItem item, SgTransposeColumn<TItem> column, object? rawValue, string display)
    {
        Item = item;
        Column = column;
        RawValue = rawValue;
        Display = display;
    }

    /// <summary>The data item for this column.</summary>
    public TItem Item { get; }

    /// <summary>The column (field) definition for this row.</summary>
    public SgTransposeColumn<TItem> Column { get; }

    /// <summary>The raw value returned by <see cref="SgTransposeColumn{TItem}.ValueSelector"/>.</summary>
    public object? RawValue { get; }

    /// <summary>The formatted display string.</summary>
    public string Display { get; }
}

/// <summary>Arguments passed to <see cref="SgTransposeGrid{TItem}.OnCellEdited"/>.</summary>
public sealed class SgTransposeEditEventArgs<TItem>
{
    public required TItem Item { get; init; }
    public required SgTransposeColumn<TItem> Column { get; init; }
    public string? NewValue { get; init; }
}
