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
    [Parameter] public Type? ValueType { get; set; }

    /// <summary>
    /// When true, DateTime columns display both date and time (e.g. "27.08.2024 14:35").
    /// When false (default), only the date part is shown ("27.08.2024").
    /// Has no effect when <see cref="Format"/> is set explicitly.
    /// </summary>
    [Parameter] public bool ShowTime { get; set; }

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

    internal string GetDisplay(TItem item)
    {
        var v = GetValue(item);
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


