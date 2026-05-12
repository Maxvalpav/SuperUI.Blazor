// SuperUI/Base/Converters/SgConverterFactory.cs

using System.Collections.Concurrent;

namespace SuperUI.Base.Converters;

/// <summary>
/// Фабрика конвертеров для SgFormBase.
/// Регистрирует конвертеры типов для автоматического преобразования.
/// </summary>
public static class SgConverterFactory
{
    private static readonly ConcurrentDictionary<Type, object> _converters = new();

    static SgConverterFactory()
    {
        // Регистрируем встроенные конвертеры
        Register(new SgStringConverter());
        Register(new SgIntConverter());
        Register(new SgDoubleConverter());
        Register(new SgDecimalConverter());
        Register(new SgBoolConverter());
        Register(new SgDateTimeConverter());
        Register(new SgDateTimeOffsetConverter());
        Register(new SgNullableIntConverter());
        Register(new SgNullableDoubleConverter());
        Register(new SgNullableDecimalConverter());
        Register(new SgNullableDateTimeConverter());
    }

    /// <summary>Зарегистрировать конвертер для типа T.</summary>
    public static void Register<T>(ISgConverter<T> converter)
        => _converters[typeof(T)] = converter;

    /// <summary>Получить конвертер для типа T или null.</summary>
    public static ISgConverter<T>? Get<T>()
        => _converters.TryGetValue(typeof(T), out var c) ? (ISgConverter<T>)c : null;
}

/// <summary>Контракт конвертера значений поля формы.</summary>
public interface ISgConverter<T>
{
    /// <summary>Преобразовать строку ввода в значение.</summary>
    bool TryConvert(string? text, out T? value, out string? error);

    /// <summary>Преобразовать значение в строку ввода.</summary>
    string? ConvertBack(T? value);
}

// ── Встроенные конвертеры ──────────────────────────────────────────────────────

public sealed class SgStringConverter : ISgConverter<string>
{
    public bool TryConvert(string? text, out string? value, out string? error)
    {
        value = text;
        error = null;
        return true;
    }

    public string? ConvertBack(string? value) => value;
}

public sealed class SgIntConverter : ISgConverter<int>
{
    public bool TryConvert(string? text, out int value, out string? error)
    {
        if (int.TryParse(text, out value)) { error = null; return true; }
        error = $"Введите целое число";
        return false;
    }

    public string? ConvertBack(int value) => value.ToString();
}

public sealed class SgDoubleConverter : ISgConverter<double>
{
    public bool TryConvert(string? text, out double value, out string? error)
    {
        if (double.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value))
        { error = null; return true; }
        error = "Введите число";
        return false;
    }

    public string? ConvertBack(double value)
        => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class SgDecimalConverter : ISgConverter<decimal>
{
    public bool TryConvert(string? text, out decimal value, out string? error)
    {
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value))
        { error = null; return true; }
        error = "Введите число";
        return false;
    }

    public string? ConvertBack(decimal value)
        => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class SgBoolConverter : ISgConverter<bool>
{
    public bool TryConvert(string? text, out bool value, out string? error)
    {
        value = text?.ToLowerInvariant() is "true" or "1" or "yes" or "да";
        error = null;
        return true;
    }

    public string? ConvertBack(bool value) => value.ToString().ToLowerInvariant();
}

public sealed class SgDateTimeConverter : ISgConverter<DateTime>
{
    public bool TryConvert(string? text, out DateTime value, out string? error)
    {
        if (DateTime.TryParse(text, out value)) { error = null; return true; }
        error = "Введите дату";
        return false;
    }

    public string? ConvertBack(DateTime value)
        => value == default ? null : value.ToString("yyyy-MM-dd");
}

public sealed class SgDateTimeOffsetConverter : ISgConverter<DateTimeOffset>
{
    public bool TryConvert(string? text, out DateTimeOffset value, out string? error)
    {
        if (DateTimeOffset.TryParse(text, out value)) { error = null; return true; }
        error = "Введите дату";
        return false;
    }

    public string? ConvertBack(DateTimeOffset value)
        => value == default ? null : value.ToString("yyyy-MM-ddTHH:mm:sszzz");
}

// Nullable конвертеры

public sealed class SgNullableIntConverter : ISgConverter<int?>
{
    public bool TryConvert(string? text, out int? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (int.TryParse(text, out var v)) { value = v; error = null; return true; }
        value = null; error = "Введите целое число"; return false;
    }

    public string? ConvertBack(int? value) => value?.ToString();
}

public sealed class SgNullableDoubleConverter : ISgConverter<double?>
{
    public bool TryConvert(string? text, out double? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (double.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v))
        { value = v; error = null; return true; }
        value = null; error = "Введите число"; return false;
    }

    public string? ConvertBack(double? value)
        => value?.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class SgNullableDecimalConverter : ISgConverter<decimal?>
{
    public bool TryConvert(string? text, out decimal? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v))
        { value = v; error = null; return true; }
        value = null; error = "Введите число"; return false;
    }

    public string? ConvertBack(decimal? value)
        => value?.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class SgNullableDateTimeConverter : ISgConverter<DateTime?>
{
    public bool TryConvert(string? text, out DateTime? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (DateTime.TryParse(text, out var v)) { value = v; error = null; return true; }
        value = null; error = "Введите дату"; return false;
    }

    public string? ConvertBack(DateTime? value)
        => value.HasValue ? value.Value.ToString("yyyy-MM-dd") : null;
}
