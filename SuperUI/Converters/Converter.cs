// Файл: Converters/Converter.cs
// Зависимости: NONE

namespace SuperUI.Converters;

/// <summary>
/// Двусторонний конвертер TValue ↔ string для форм.
/// Аналог MudBlazor Converter, но с улучшенной обработкой культуры
/// и поддержкой custom format string.
/// </summary>
/// <typeparam name="TValue">Тип значения поля формы.</typeparam>
public class Converter<TValue>
{
    private Func<TValue?, string?>? _toStringFunc;
    private Func<string?, TValue?>? _fromStringFunc;

    public string? ConversionError { get; protected set; }
    public bool HasError => ConversionError is not null;

    /// <summary>Настроить функцию TValue → string.</summary>
    public Converter<TValue> SetToString(Func<TValue?, string?> func)
    {
        _toStringFunc = func;
        return this;
    }

    /// <summary>Настроить функцию string → TValue.</summary>
    public Converter<TValue> SetFromString(Func<string?, TValue?> func)
    {
        _fromStringFunc = func;
        return this;
    }

    /// <summary>Конвертировать значение в строку.</summary>
    public string? Convert(TValue? value)
    {
        ConversionError = null;
        try
        {
            if (_toStringFunc is not null)
                return _toStringFunc(value);
            return DefaultToString(value);
        }
        catch (Exception ex)
        {
            ConversionError = ex.Message;
            return null;
        }
    }

    /// <summary>Конвертировать строку в значение.</summary>
    public TValue? ConvertBack(string? value)
    {
        ConversionError = null;
        try
        {
            if (_fromStringFunc is not null)
                return _fromStringFunc(value);
            return DefaultFromString(value);
        }
        catch (Exception ex)
        {
            ConversionError = ex.Message;
            return default;
        }
    }

    protected virtual string? DefaultToString(TValue? value)
        => value?.ToString();

    protected virtual TValue? DefaultFromString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return default;
        try { return (TValue?)System.ComponentModel.TypeDescriptor.GetConverter(typeof(TValue)).ConvertFromString(value); }
        catch { throw new FormatException($"Не удалось преобразовать '{value}' в {typeof(TValue).Name}"); }
    }
}

/// <summary>Конвертер с поддержкой IFormatProvider (культура).</summary>
public class CultureAwareConverter<TValue> : Converter<TValue>
{
    private readonly IFormatProvider? _formatProvider;

    public CultureAwareConverter(IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider;
    }

    protected override string? DefaultToString(TValue? value)
    {
        if (value is IFormattable formattable)
            return formattable.ToString(null, _formatProvider);
        return value?.ToString();
    }

    protected override TValue? DefaultFromString(string? value)
    {
        if (string.IsNullOrEmpty(value)) return default;
        if (typeof(TValue) == typeof(double))
            return (TValue?)(object?)double.Parse(value, _formatProvider);
        if (typeof(TValue) == typeof(decimal))
            return (TValue?)(object?)decimal.Parse(value, _formatProvider);
        if (typeof(TValue) == typeof(DateTime))
            return (TValue?)(object?)DateTime.Parse(value, _formatProvider);
        return base.DefaultFromString(value);
    }
}
