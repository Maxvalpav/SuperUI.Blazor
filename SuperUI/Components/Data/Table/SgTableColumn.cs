using Microsoft.AspNetCore.Components;
using SuperUI.Enums;
using System.Reflection;

namespace SuperUI.Components;

/// <summary>Defines a column in the <see cref="SgTable{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of data items displayed in the table.</typeparam>
public sealed class SgTableColumn<TItem> : ComponentBase, IDisposable
{
    /// <summary>Gets or sets the owning table instance, provided via cascading parameter.</summary>
    [CascadingParameter] public SgTable<TItem>? Owner { get; set; }

    /// <summary>An optional unique key for this column. If not set, a GUID is generated automatically.</summary>
    [Parameter] public string? ColumnKey { get; set; }
    /// <summary>The display title shown in the column header.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;
    /// <summary>Optional function returning the cell value for this column from a data item.</summary>
    [Parameter] public Func<TItem, object?>? Value { get; set; }
    /// <summary>Optional custom render fragment for cell content. When set, replaces the default value display.</summary>
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    /// <summary>Whether the column is sortable by clicking the header.</summary>
    [Parameter] public bool Sortable { get; set; } = true;
    /// <summary>The column width. Accepts any CSS width value (e.g. "150px", "20%").</summary>
    [Parameter] public string? Width { get; set; }
    /// <summary>Optional format string for displaying cell values.</summary>
    [Parameter] public string? Format { get; set; }
    /// <summary>The underlying value type of the column, used for formatting and numeric detection.</summary>
    [Parameter] public Type? ValueType { get; set; }
    /// <summary>Horizontal alignment for cell content.</summary>
    [Parameter] public SgHAlign HAlign { get; set; } = SgHAlign.Default;
    /// <summary>Horizontal alignment for the column header text.</summary>
    [Parameter] public SgHAlign HeaderAlign { get; set; } = SgHAlign.Default;
    /// <summary>Vertical alignment for cell content.</summary>
    [Parameter] public SgVAlign VAlign { get; set; } = SgVAlign.Default;
    /// <summary>When true, forces numeric-style rendering. When false, forces text-style. When null, auto-detects from data.</summary>
    [Parameter] public bool? NumericStyle { get; set; }
    /// <summary>Optional function returning additional CSS class(es) for a cell based on the row item.</summary>
    [Parameter] public Func<TItem, string?>? CellClass { get; set; }
    /// <summary>Optional function returning inline CSS styles for a cell based on the row item.</summary>
    [Parameter] public Func<TItem, string?>? CellStyle { get; set; }

    private bool? _isNumericDetected;

    internal string Key { get; private set; } = Guid.NewGuid().ToString("N");
    internal bool IsSynthetic { get; private set; }

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
        t == typeof(byte) || t == typeof(sbyte) ||
        t == typeof(short) || t == typeof(ushort) ||
        t == typeof(int) || t == typeof(uint) ||
        t == typeof(long) || t == typeof(ulong) ||
        t == typeof(float) || t == typeof(double) ||
        t == typeof(decimal);

    internal static SgTableColumn<TItem> CreateSynthetic(
        string key,
        string title,
        Func<TItem, object?> value,
        bool sortable = true,
        Type? valueType = null)
    {
        var column = new SgTableColumn<TItem>
        {
            IsSynthetic = true,
            Key = key
        };
        column.Title = title;
        column.Value = value;
        column.Sortable = sortable;
        column.ValueType = valueType;
        return column;
    }

    protected override void OnInitialized()
    {
        if (IsSynthetic) return;
        if (Owner is null)
            throw new InvalidOperationException("SgTableColumn must be inside a SgTable.");
        if (!string.IsNullOrEmpty(ColumnKey)) Key = ColumnKey;
        Owner.RegisterColumn(this);
    }

    internal object? GetValue(TItem item) => Value is null ? null : Value(item);

    internal string GetDisplay(TItem item) => GetDisplayFromValue(GetValue(item));

    internal string GetDisplayFromValue(object? v)
    {
        if (v is null) return string.Empty;
        if (v is bool b) return b ? "✓" : "✗";

        if (!string.IsNullOrEmpty(Format))
        {
            if (Format.Contains('{') && Format.Contains('}'))
            {
                try
                {
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture, Format, v);
                }
                catch
                {
                }
            }

            if (v is IFormattable f)
            {
                try
                {
                    return f.ToString(Format, System.Globalization.CultureInfo.CurrentCulture);
                }
                catch
                {
                }
            }
        }

        if (v is DateTime dt)
        {
            return dt.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        }
        if (v is DateTimeOffset dto)
        {
            return dto.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        }
        if (v is DateOnly d)
        {
            return d.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        }

        return v.ToString() ?? string.Empty;
    }

    /// <summary>Unregisters this column from the owning table.</summary>
    public void Dispose()
    {
        if (!IsSynthetic)
            Owner?.UnregisterColumn(this);
    }
}
