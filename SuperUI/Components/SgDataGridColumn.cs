using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

public sealed class SgDataGridColumn<TItem> : ComponentBase, IDisposable where TItem : notnull
{
    [CascadingParameter] public SgDataGrid<TItem>? Owner { get; set; }

    [Parameter] public string? ColumnKey { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; } = default!;
    [Parameter] public Func<TItem, object?>? Value { get; set; }
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    [Parameter] public bool Sortable { get; set; } = true;
    [Parameter] public bool Filterable { get; set; } = true;
    [Parameter] public bool Pinned { get; set; }
    [Parameter] public bool Hidden { get; set; }
    [Parameter] public bool Resizable { get; set; } = true;
    [Parameter] public bool Reorderable { get; set; } = true;
    [Parameter] public bool GroupBy { get; set; }
    [Parameter] public bool Editable { get; set; }
    [Parameter] public Action<TItem, object?>? OnValueChanged { get; set; }
    [Parameter] public string? Width { get; set; }
    [Parameter] public string? Format { get; set; }
    [Parameter] public Aggregate Aggregate { get; set; } = Aggregate.None;

    /// <summary>Sets aggregate without going through Blazor parameter pipeline (internal use).</summary>
    internal void SetAggregate(Aggregate value) => Aggregate = value;
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

    public void Dispose() => Owner?.UnregisterColumn(this);
}
