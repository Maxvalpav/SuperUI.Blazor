// SuperUI/Base/Converters/ISgConverter.cs
namespace SuperUI.Base.Converters;

/// <summary>
/// Двунаправленный конвертер для форм.
/// </summary>
public interface ISgConverter<TValue>
{
    bool TryConvert(string? text, out TValue? value, out string? error);
    string? ConvertBack(TValue? value);
}

/// <summary>
/// Фабрика конвертеров — кэшируется per-type.
/// </summary>
public static class SgConverterFactory
{
    private static readonly Dictionary<Type, object> _cache = new();

    public static ISgConverter<TValue> Get<TValue>()
    {
        var type = typeof(TValue);
        if (_cache.TryGetValue(type, out var cached))
            return (ISgConverter<TValue>)cached;

        ISgConverter<TValue> converter = type switch
        {
            _ when type == typeof(string)   => (ISgConverter<TValue>)(object)new StringConverter(),
            _ when type == typeof(int)      => (ISgConverter<TValue>)(object)new Int32Converter(),
            _ when type == typeof(int?)     => (ISgConverter<TValue>)(object)new NullableInt32Converter(),
            _ when type == typeof(double)   => (ISgConverter<TValue>)(object)new DoubleConverter(),
            _ when type == typeof(double?)  => (ISgConverter<TValue>)(object)new NullableDoubleConverter(),
            _ when type == typeof(decimal)  => (ISgConverter<TValue>)(object)new DecimalConverter(),
            _ when type == typeof(decimal?) => (ISgConverter<TValue>)(object)new NullableDecimalConverter(),
            _ when type == typeof(bool)     => (ISgConverter<TValue>)(object)new BoolConverter(),
            _ when type == typeof(bool?)    => (ISgConverter<TValue>)(object)new NullableBoolConverter(),
            _ when type == typeof(DateTime) => (ISgConverter<TValue>)(object)new DateTimeConverter(),
            _ when type == typeof(DateTime?)=> (ISgConverter<TValue>)(object)new NullableDateTimeConverter(),
            _ when type == typeof(DateOnly) => (ISgConverter<TValue>)(object)new DateOnlyConverter(),
            _ when type == typeof(DateOnly?)=> (ISgConverter<TValue>)(object)new NullableDateOnlyConverter(),
            _ when type == typeof(Guid)     => (ISgConverter<TValue>)(object)new GuidConverter(),
            _                               => new ToStringConverter<TValue>()
        };

        _cache[type] = converter;
        return converter;
    }
}

internal sealed class StringConverter : ISgConverter<string>
{
    public bool TryConvert(string? text, out string? value, out string? error)
    { value = text; error = null; return true; }
    public string? ConvertBack(string? value) => value;
}

internal sealed class Int32Converter : ISgConverter<int>
{
    public bool TryConvert(string? text, out int value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0; error = null; return true; }
        if (int.TryParse(text, out value))   { error = null; return true; }
        error = $"'{text}' is not a valid integer."; return false;
    }
    public string? ConvertBack(int value) => value.ToString();
}

internal sealed class NullableInt32Converter : ISgConverter<int?>
{
    public bool TryConvert(string? text, out int? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (int.TryParse(text, out var v))   { value = v; error = null; return true; }
        value = null; error = $"'{text}' is not a valid integer."; return false;
    }
    public string? ConvertBack(int? value) => value?.ToString();
}

internal sealed class DoubleConverter : ISgConverter<double>
{
    public bool TryConvert(string? text, out double value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0; error = null; return true; }
        if (double.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value))
        { error = null; return true; }
        error = $"'{text}' is not a valid number."; return false;
    }
    public string? ConvertBack(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

internal sealed class NullableDoubleConverter : ISgConverter<double?>
{
    public bool TryConvert(string? text, out double? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (double.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v))
        { value = v; error = null; return true; }
        value = null; error = $"'{text}' is not a valid number."; return false;
    }
    public string? ConvertBack(double? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

internal sealed class DecimalConverter : ISgConverter<decimal>
{
    public bool TryConvert(string? text, out decimal value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0; error = null; return true; }
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value))
        { error = null; return true; }
        error = $"'{text}' is not a valid decimal."; return false;
    }
    public string? ConvertBack(decimal value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

internal sealed class NullableDecimalConverter : ISgConverter<decimal?>
{
    public bool TryConvert(string? text, out decimal? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v))
        { value = v; error = null; return true; }
        value = null; error = $"'{text}' is not a valid decimal."; return false;
    }
    public string? ConvertBack(decimal? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

internal sealed class BoolConverter : ISgConverter<bool>
{
    public bool TryConvert(string? text, out bool value, out string? error)
    {
        value = text is "true" or "1" or "on"; error = null; return true;
    }
    public string? ConvertBack(bool value) => value ? "true" : "false";
}

internal sealed class NullableBoolConverter : ISgConverter<bool?>
{
    public bool TryConvert(string? text, out bool? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        value = text is "true" or "1" or "on"; error = null; return true;
    }
    public string? ConvertBack(bool? value) => value?.ToString().ToLower();
}

internal sealed class DateTimeConverter : ISgConverter<DateTime>
{
    public bool TryConvert(string? text, out DateTime value, out string? error)
    {
        if (DateTime.TryParse(text, out value)) { error = null; return true; }
        error = $"'{text}' is not a valid date."; return false;
    }
    public string? ConvertBack(DateTime value) => value.ToString("O");
}

internal sealed class NullableDateTimeConverter : ISgConverter<DateTime?>
{
    public bool TryConvert(string? text, out DateTime? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (DateTime.TryParse(text, out var v)) { value = v; error = null; return true; }
        value = null; error = $"'{text}' is not a valid date."; return false;
    }
    public string? ConvertBack(DateTime? value) => value?.ToString("O");
}

internal sealed class DateOnlyConverter : ISgConverter<DateOnly>
{
    public bool TryConvert(string? text, out DateOnly value, out string? error)
    {
        if (DateOnly.TryParse(text, out value)) { error = null; return true; }
        error = $"'{text}' is not a valid date."; return false;
    }
    public string? ConvertBack(DateOnly value) => value.ToString("yyyy-MM-dd");
}

internal sealed class NullableDateOnlyConverter : ISgConverter<DateOnly?>
{
    public bool TryConvert(string? text, out DateOnly? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (DateOnly.TryParse(text, out var v)) { value = v; error = null; return true; }
        value = null; error = $"'{text}' is not a valid date."; return false;
    }
    public string? ConvertBack(DateOnly? value) => value?.ToString("yyyy-MM-dd");
}

internal sealed class GuidConverter : ISgConverter<Guid>
{
    public bool TryConvert(string? text, out Guid value, out string? error)
    {
        if (Guid.TryParse(text, out value)) { error = null; return true; }
        error = $"'{text}' is not a valid GUID."; return false;
    }
    public string? ConvertBack(Guid value) => value.ToString();
}

internal sealed class ToStringConverter<TValue> : ISgConverter<TValue>
{
    public bool TryConvert(string? text, out TValue? value, out string? error)
    {
        try
        {
            value = (TValue?)Convert.ChangeType(text, typeof(TValue));
            error = null; return true;
        }
        catch { value = default; error = $"Cannot convert '{text}' to {typeof(TValue).Name}."; return false; }
    }
    public string? ConvertBack(TValue? value) => value?.ToString();
}
