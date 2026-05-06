using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public sealed class SgDataGridColumn<TItem> : ComponentBase, IDisposable
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

    /// <summary>
    /// Optional function returning extra CSS class(es) for a cell based on the row item.
    /// Example: <c>CellClass="@(e => e.Salary > 100000 ? "highlight" : null)"</c>
    /// </summary>
    [Parameter] public Func<TItem, string?>? CellClass { get; set; }

    /// <summary>
    /// Optional function returning inline CSS style for a cell based on the row item.
    /// Example: <c>CellStyle="@(e => e.IsActive ? "font-weight:600" : null)"</c>
    /// </summary>
    [Parameter] public Func<TItem, string?>? CellStyle { get; set; }

    /// <summary>
    /// Optional custom header content. When set, replaces the default title text in the column header.
    /// The column title is still used for sort/filter labels.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Optional custom footer content. When set, replaces the aggregate value in the status bar footer.
    /// Receives the list of all filtered items so you can compute custom summaries.
    /// </summary>
    [Parameter] public RenderFragment<IReadOnlyList<TItem>>? FooterTemplate { get; set; }

    /// <summary>
    /// When true, DateTime columns display both date and time (e.g. "27.08.2024 14:35").
    /// When false (default), only the date part is shown ("27.08.2024").
    /// Has no effect when <see cref="Format"/> is set explicitly.
    /// </summary>
    [Parameter] public bool ShowTime { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of the cell content.
    /// Defaults to <see cref="SgHAlign.Default"/> which left-aligns content
    /// for all column types (including numeric).
    /// </summary>
    [Parameter] public SgHAlign HAlign { get; set; } = SgHAlign.Default;

    /// <summary>
    /// Gets or sets the horizontal alignment of the column header text.
    /// Defaults to <see cref="SgHAlign.Default"/> which keeps the header left-aligned
    /// regardless of the column type (including numeric columns).
    /// </summary>
    [Parameter] public SgHAlign HeaderAlign { get; set; } = SgHAlign.Default;

    /// <summary>
    /// Gets or sets the vertical alignment of the cell content.
    /// Defaults to <see cref="SgVAlign.Default"/> (middle).
    /// </summary>
    [Parameter] public SgVAlign VAlign { get; set; } = SgVAlign.Default;

    /// <summary>
    /// When <c>true</c>, numeric values are rendered with tabular-nums and right-aligned by default.
    /// Automatically detected from <see cref="ValueType"/> or the first sampled value.
    /// Override with <c>true</c>/<c>false</c> to force the behaviour.
    /// </summary>
    [Parameter] public bool? NumericStyle { get; set; }

    // Cached result of numeric detection from sampled value (null = not yet detected)
    private bool? _isNumericDetected;

    /// <summary>
    /// Returns <c>true</c> when the column should use numeric cell rendering.
    /// </summary>
    internal bool IsNumericColumn
    {
        get
        {
            // 1. Explicit parameter override
            if (NumericStyle.HasValue) return NumericStyle.Value;
            // 2. ValueType parameter
            if (ValueType is not null)
                return IsNumericType(Nullable.GetUnderlyingType(ValueType) ?? ValueType);
            // 3. Cached detection from sampled value
            return _isNumericDetected ?? false;
        }
    }

    /// <summary>
    /// Returns true if numeric type has been detected or is known from ValueType/NumericStyle.
    /// Used by the grid to skip re-sampling when already resolved.
    /// </summary>
    internal bool IsNumericResolved =>
        NumericStyle.HasValue || ValueType is not null || _isNumericDetected.HasValue;

    /// <summary>
    /// Called by the grid with the first non-null value to detect numeric type
    /// when <see cref="ValueType"/> is not explicitly set.
    /// </summary>
    internal void TryDetectNumericType(object? sample)
    {
        if (_isNumericDetected.HasValue) return;
        if (NumericStyle.HasValue) return;
        if (ValueType is not null) return;
        if (sample is null) return; // don't cache null — data may not be loaded yet
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
        // For enums use the display name if available, otherwise ToString
        if (v is Enum) return GetDisplayFromValue(v);
        // For dates use invariant short format for stable grouping
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
            // Check if format is a composite format string like "{0:N2} ₽"
            if (Format.Contains('{') && Format.Contains('}'))
            {
                try
                {
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture, Format, v);
                }
                catch
                {
                    // Fallback to standard formatting
                }
            }
            
            // Standard format specifier like "N2", "C2", etc.
            if (v is IFormattable f)
            {
                try
                {
                    return f.ToString(Format, System.Globalization.CultureInfo.CurrentCulture);
                }
                catch
                {
                    // Fallback to ToString
                }
            }
        }

        // Auto-format dates: use short date or date+time based on ShowTime flag
        // This avoids "27.08.2024 0:00:00" when no Format is specified
        if (v is DateTime dt)
        {
            return ShowTime
                ? dt.ToString("g", System.Globalization.CultureInfo.CurrentCulture)
                : dt.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        }
        if (v is DateTimeOffset dto)
        {
            return ShowTime
                ? dto.ToString("g", System.Globalization.CultureInfo.CurrentCulture)
                : dto.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        }
        if (v is DateOnly d)
        {
            return d.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        }
        
        return v.ToString() ?? string.Empty;
    }

    public void Dispose() => Owner?.UnregisterColumn(this);
}

public enum Aggregate
{
    None,
    Sum,
    Average,
    Min,
    Max,
    Count
}

public enum FilterCondition
{
    Contains,
    NotContains,
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    IsEmpty,
    IsNotEmpty
}

public sealed record FilterRule
{
    public FilterCondition Condition { get; init; } = FilterCondition.Contains;
    public string? Value { get; init; }
}

public enum DetailPlacement { Inline, Drawer, Window }

public sealed class ColumnFilter
{
    public ColumnFilter(IEnumerable<FilterRule> rules, bool and = true)
    {
        Rules = rules.ToList();
        And = and;
    }
    public List<FilterRule> Rules { get; }
    public bool And { get; }
}

public sealed class RowHighlightRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public List<QueryRule> Rules { get; set; } = new();
    public bool RulesAnd { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// When set, only the cell in this column key is colored instead of the whole row.
    /// </summary>
    public string? TargetColumnKey { get; set; }
}

public sealed class PersistedRowHighlightRule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<QueryRule> Rules { get; set; } = new();
    public bool RulesAnd { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? TargetColumnKey { get; set; }
}


