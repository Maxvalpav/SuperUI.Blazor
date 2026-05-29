using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>Defines a column in the <see cref="SgDataGrid{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of data items displayed in the grid.</typeparam>
public sealed class SgDataGridColumn<TItem> : ComponentBase, IDisposable where TItem : notnull
{
    /// <summary>Gets or sets the owning data grid instance, provided via cascading parameter.</summary>
    [CascadingParameter] public SgDataGrid<TItem>? Owner { get; set; }

    /// <summary>An optional unique key for this column. If not set, a GUID is generated automatically.</summary>
    [Parameter] public string? ColumnKey { get; set; }
    /// <summary>The display title shown in the column header.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;
    /// <summary>Optional function returning the cell value for this column from a data item. If not set, the column attempts to resolve the value from a property matching the <see cref="Title"/>.</summary>
    [Parameter] public Func<TItem, object?>? Value { get; set; }
    /// <summary>Optional custom render fragment for cell content. When set, replaces the default value display.</summary>
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    /// <summary>Whether the column is sortable by clicking the header.</summary>
    [Parameter] public bool Sortable { get; set; } = true;
    /// <summary>Whether the column supports filtering via the filter menu.</summary>
    [Parameter] public bool Filterable { get; set; } = true;
    /// <summary>Whether the column is pinned (frozen) to the left side of the grid.</summary>
    [Parameter] public bool Pinned { get; set; }
    /// <summary>Whether the column is hidden from view.</summary>
    [Parameter] public bool Hidden { get; set; }
    /// <summary>Whether the column width can be resized by the user.</summary>
    [Parameter] public bool Resizable { get; set; } = true;
    /// <summary>Whether the column can be reordered by drag-and-drop.</summary>
    [Parameter] public bool Reorderable { get; set; } = true;
    /// <summary>Whether the grid is initially grouped by this column.</summary>
    [Parameter] public bool GroupBy { get; set; }
    /// <summary>Whether cells in this column can be edited inline.</summary>
    [Parameter] public bool Editable { get; set; }
    /// <summary>Callback invoked when a cell value is changed through inline editing.</summary>
    [Parameter] public Action<TItem, object?>? OnValueChanged { get; set; }
    /// <summary>The column width. Accepts any CSS width value (e.g. "150px", "20%", "auto").</summary>
    [Parameter] public string? Width { get; set; }
    /// <summary>Optional format string for displaying cell values (e.g. "N2", "C", "{0:N2} ₽").</summary>
    [Parameter] public string? Format { get; set; }
    /// <summary>The aggregate function to compute for this column in the status bar.</summary>
    [Parameter] public Aggregate Aggregate { get; set; } = Aggregate.None;

    /// <summary>Sets aggregate without going through Blazor parameter pipeline (internal use).</summary>
    internal void SetAggregate(Aggregate value) => Aggregate = value;
    /// <summary>The underlying value type of the column, used for formatting and numeric detection.</summary>
    [Parameter] public Type? ValueType { get; set; }

    /// <summary>Optional function returning extra CSS class(es) for a cell based on the row item.</summary>
    [Parameter] public Func<TItem, string?>? CellClass { get; set; }

    /// <summary>Optional function returning inline CSS style for a cell based on the row item.</summary>
    [Parameter] public Func<TItem, string?>? CellStyle { get; set; }

    /// <summary>Optional custom header content. When set, replaces the default title text.</summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>Optional custom footer content.</summary>
    [Parameter] public RenderFragment<IReadOnlyList<TItem>>? FooterTemplate { get; set; }

    /// <summary>Horizontal alignment for cell and header content.</summary>
    [Parameter] public SgColumnAlign HAlign { get; set; }

    /// <summary>Horizontal alignment for the column header text.</summary>
    [Parameter] public SgColumnAlign HeaderAlign { get; set; }

    /// <summary>Vertical alignment for cell content.</summary>
    [Parameter] public SgColumnAlign VAlign { get; set; }

    /// <summary>When set, numeric values are rendered with tabular-nums and right-aligned by default.</summary>
    [Parameter] public bool ShowTime { get; set; }
    /// <summary>When true, forces numeric-style rendering (tabular-nums, right-aligned). When false, forces text-style. When null, auto-detects from data.</summary>
    [Parameter] public bool? NumericStyle { get; set; }

    // Cached result of numeric detection from sampled value (null = not yet detected)
    private bool? _isNumericDetected;

    internal bool IsNumericColumn
    {
        get
        {
            if (NumericStyle.HasValue) return NumericStyle.Value;
            if (ValueType is not null)
                return IsNumericType(Nullable.GetUnderlyingType(ValueType) ?? ValueType);
            return _isNumericDetected ?? false;
        }
    }

    internal void TryDetectNumericType(object? sample)
    {
        if (_isNumericDetected.HasValue) return;
        if (NumericStyle.HasValue) return;
        if (ValueType is not null) return;
        if (sample is null) return;
        var t = Nullable.GetUnderlyingType(sample.GetType()) ?? sample.GetType();
        _isNumericDetected = IsNumericType(t);
    }

    private static bool IsNumericType(Type t) =>
        t == typeof(byte)    || t == typeof(sbyte)  ||
        t == typeof(short)   || t == typeof(ushort) ||
        t == typeof(int)     || t == typeof(uint)   ||
        t == typeof(long)    || t == typeof(ulong)  ||
        t == typeof(float)   || t == typeof(double) ||
        t == typeof(decimal);

    internal bool IsNumericResolved => NumericStyle.HasValue || ValueType is not null || _isNumericDetected.HasValue;

    internal static bool IsNumericTypeStatic(Type t) => IsNumericType(t);

    internal string Key { get; private set; } = Guid.NewGuid().ToString("N");
    internal bool IsSynthetic { get; private set; }

    internal void InvalidateOwnerCache()
    {
        Owner?.InvalidateComputedRowsCache();
    }

    internal static SgDataGridColumn<TItem> CreateSynthetic(
        string key,
        string title,
        Func<TItem, object?> value,
        Type? valueType,
        string? format)
    {
        var column = new SgDataGridColumn<TItem>
        {
            IsSynthetic = true,
            Key = key
        };
        column.Title = title;
        column.Value = value;
        column.ValueType = valueType;
        column.Format = format;
        return column;
    }

    protected override void OnInitialized()
    {
        if (IsSynthetic) return;
        if (Owner is null)
            throw new InvalidOperationException("SuperUIColumn must be inside a SuperUI.");
        if (!string.IsNullOrEmpty(ColumnKey)) Key = ColumnKey;
        Owner.RegisterColumn(this);
        if (GroupBy) Owner.InitGroupBy(Key);
    }

    internal object? GetValue(TItem item) => Value is null ? null : Value(item);

    internal string GetDisplay(TItem item) => GetDisplayFromValue(GetValue(item));

    /// <summary>
    /// Returns a stable group key for the item — uses raw value without display formatting
    /// to avoid expensive format calls (N0, C0, dates) during grouping.
    /// </summary>
    internal string GetGroupKey(TItem item)
    {
        var v = GetValue(item);
        if (v is null) return string.Empty;
        if (v is bool b) return b ? "true" : "false";
        if (v is Enum) return GetDisplayFromValue(v);
        if (v is DateTime dt) return dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (v is DateTimeOffset dto) return dto.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (v is DateOnly d) return d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return v.ToString() ?? string.Empty;
    }

    internal string GetDisplayFromValue(object? v)
    {
        if (v is null) return string.Empty;
        if (v is bool b) return b ? "✓" : "✗";

        if (!string.IsNullOrEmpty(Format))
        {
            if (Format.Contains('{') && Format.Contains('}'))
            {
                try { return string.Format(System.Globalization.CultureInfo.CurrentCulture, Format, v); }
                catch { }
            }

            if (v is IFormattable f)
            {
                try { return f.ToString(Format, System.Globalization.CultureInfo.CurrentCulture); }
                catch { }
            }
        }

        if (v is DateTime dt)
            return ShowTime ? dt.ToString("g", System.Globalization.CultureInfo.CurrentCulture) : dt.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        if (v is DateTimeOffset dto)
            return ShowTime ? dto.ToString("g", System.Globalization.CultureInfo.CurrentCulture) : dto.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        if (v is DateOnly d) return d.ToString("d", System.Globalization.CultureInfo.CurrentCulture);

        return v.ToString() ?? string.Empty;
    }

    /// <summary>Unregisters this column from the owning data grid.</summary>
    public void Dispose() => Owner?.UnregisterColumn(this);
}
