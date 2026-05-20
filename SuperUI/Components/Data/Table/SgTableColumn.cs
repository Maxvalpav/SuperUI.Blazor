using Microsoft.AspNetCore.Components;
using SuperUI.Enums;
using System.Reflection;

namespace SuperUI.Components;

public sealed class SgTableColumn<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] public SgTable<TItem>? Owner { get; set; }

    [Parameter] public string? ColumnKey { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; } = default!;
    [Parameter] public Func<TItem, object?>? Value { get; set; }
    [Parameter] public RenderFragment<TItem>? Template { get; set; }
    [Parameter] public bool Sortable { get; set; } = true;
    [Parameter] public string? Width { get; set; }
    [Parameter] public string? Format { get; set; }
    [Parameter] public Type? ValueType { get; set; }
    [Parameter] public SgHAlign HAlign { get; set; } = SgHAlign.Default;
    [Parameter] public SgHAlign HeaderAlign { get; set; } = SgHAlign.Default;
    [Parameter] public SgVAlign VAlign { get; set; } = SgVAlign.Default;
    [Parameter] public bool? NumericStyle { get; set; }
    [Parameter] public Func<TItem, string?>? CellClass { get; set; }
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

    public void Dispose()
    {
        if (!IsSynthetic)
            Owner?.UnregisterColumn(this);
    }
}
