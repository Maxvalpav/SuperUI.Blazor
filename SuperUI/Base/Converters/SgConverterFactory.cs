// SuperUI/Base/Converters/SgConverterFactory.cs

using System.Collections.Concurrent;
using System.Globalization;

namespace SuperUI.Base.Converters;

/// <summary>
/// Фабрика конвертеров типов для компонентов форм SuperUI.
/// Thread-safe: ConcurrentDictionary.
/// Поддерживает регистрацию пользовательских конвертеров.
/// </summary>
public static class SgConverterFactory
{
    private static readonly ConcurrentDictionary<Type, object> _converters = new();

    static SgConverterFactory()
    {
        // Встроенные конвертеры
        Register<string>(new SgStringConverter());
        Register<int>(new SgIntConverter());
        Register<int?>(new SgNullableIntConverter());
        Register<double>(new SgDoubleConverter());
        Register<double?>(new SgNullableDoubleConverter());
        Register<decimal>(new SgDecimalConverter());
        Register<decimal?>(new SgNullableDecimalConverter());
        Register<bool>(new SgBoolConverter());
        Register<bool?>(new SgNullableBoolConverter());
        Register<DateTime>(new SgDateTimeConverter());
        Register<DateTime?>(new SgNullableDateTimeConverter());
        Register<DateTimeOffset>(new SgDateTimeOffsetConverter());
        Register<DateTimeOffset?>(new SgNullableDateTimeOffsetConverter());
        Register<DateOnly>(new SgDateOnlyConverter());
        Register<DateOnly?>(new SgNullableDateOnlyConverter());
        Register<TimeOnly>(new SgTimeOnlyConverter());
        Register<Guid>(new SgGuidConverter());
        Register<Guid?>(new SgNullableGuidConverter());
        Register<long>(new SgLongConverter());
        Register<long?>(new SgNullableLongConverter());
        Register<float>(new SgFloatConverter());
        Register<float?>(new SgNullableFloatConverter());
    }

    /// <summary>
    /// Зарегистрировать конвертер для типа T.
    /// Перезаписывает существующий.
    /// </summary>
    public static void Register<T>(ISgConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        _converters[typeof(T)] = converter;
    }

    /// <summary>
    /// Получить конвертер для типа T или null если не зарегистрирован.
    /// </summary>
    public static ISgConverter<T>? Get<T>()
    {
        if (_converters.TryGetValue(typeof(T), out var c))
            return (ISgConverter<T>)c;

        // Fallback для enum-типов
        var type = typeof(T);
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying?.IsEnum == true || type.IsEnum)
        {
            // Создаём enum-конвертер через reflection для любого enum
            var converterType = underlying is not null
                ? typeof(SgNullableEnumConverter<>).MakeGenericType(underlying)
                : typeof(SgEnumConverter<>).MakeGenericType(type);
            var converter = Activator.CreateInstance(converterType)!;
            _converters.TryAdd(type, converter);
            return (ISgConverter<T>)converter;
        }

        return null;
    }

    /// <summary>
    /// Получить конвертер для типа T или бросить исключение.
    /// </summary>
    public static ISgConverter<T> GetRequired<T>()
        => Get<T>() ?? throw new InvalidOperationException(
            $"Конвертер для типа '{typeof(T).FullName}' не зарегистрирован. " +
            $"Используйте SgConverterFactory.Register<{typeof(T).Name}>(...) для регистрации.");

    /// <summary>
    /// Проверить, зарегистрирован ли конвертер для типа T.
    /// </summary>
    public static bool Has<T>() => _converters.ContainsKey(typeof(T));
}

// ── String ────────────────────────────────────────────────────────────────────

/// <summary>Конвертер строк — возвращает значение без изменений.</summary>
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

// ── Int32 ─────────────────────────────────────────────────────────────────────

/// <summary>Конвертер int (32-bit integer).</summary>
public sealed class SgIntConverter : ISgConverter<int>
{
    public bool TryConvert(string? text, out int value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            error = null;
            return true;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = null;
            return true;
        }

        error = $"'{text}' — не целое число.";
        return false;
    }

    public string? ConvertBack(int value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Конвертер int? (nullable int).</summary>
public sealed class SgNullableIntConverter : ISgConverter<int?>
{
    public bool TryConvert(string? text, out int? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            error = null;
            return true;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
        {
            value = v;
            error = null;
            return true;
        }

        value = null;
        error = $"'{text}' — не целое число.";
        return false;
    }

    public string? ConvertBack(int? value) => value?.ToString(CultureInfo.InvariantCulture);
}

// ── Long ──────────────────────────────────────────────────────────────────────

/// <summary>Конвертер long (64-bit integer).</summary>
public sealed class SgLongConverter : ISgConverter<long>
{
    public bool TryConvert(string? text, out long value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            error = null;
            return true;
        }

        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = null;
            return true;
        }

        error = $"'{text}' — не целое число.";
        return false;
    }

    public string? ConvertBack(long value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Конвертер long?.</summary>
public sealed class SgNullableLongConverter : ISgConverter<long?>
{
    public bool TryConvert(string? text, out long? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
        {
            value = v; error = null; return true;
        }
        value = null; error = $"'{text}' — не целое число."; return false;
    }

    public string? ConvertBack(long? value) => value?.ToString(CultureInfo.InvariantCulture);
}

// ── Float ─────────────────────────────────────────────────────────────────────

/// <summary>Конвертер float (32-bit floating point).</summary>
public sealed class SgFloatConverter : ISgConverter<float>
{
    public bool TryConvert(string? text, out float value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0f; error = null; return true; }
        if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            error = null; return true;
        }
        error = $"'{text}' — не число."; return false;
    }

    public string? ConvertBack(float value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Конвертер float?.</summary>
public sealed class SgNullableFloatConverter : ISgConverter<float?>
{
    public bool TryConvert(string? text, out float? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            value = v; error = null; return true;
        }
        value = null; error = $"'{text}' — не число."; return false;
    }

    public string? ConvertBack(float? value) => value?.ToString(CultureInfo.InvariantCulture);
}

// ── Double ────────────────────────────────────────────────────────────────────

/// <summary>Конвертер double.</summary>
public sealed class SgDoubleConverter : ISgConverter<double>
{
    public bool TryConvert(string? text, out double value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0; error = null; return true; }
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            error = null; return true;
        }
        error = $"'{text}' — не число."; return false;
    }

    public string? ConvertBack(double value)
        => double.IsNaN(value) ? null : value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Конвертер double?.</summary>
public sealed class SgNullableDoubleConverter : ISgConverter<double?>
{
    public bool TryConvert(string? text, out double? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            value = v; error = null; return true;
        }
        value = null; error = $"'{text}' — не число."; return false;
    }

    public string? ConvertBack(double? value)
        => value is null || double.IsNaN(value.Value)
            ? null
            : value.Value.ToString(CultureInfo.InvariantCulture);
}

// ── Decimal ───────────────────────────────────────────────────────────────────

/// <summary>Конвертер decimal (финансовые вычисления).</summary>
public sealed class SgDecimalConverter : ISgConverter<decimal>
{
    public bool TryConvert(string? text, out decimal value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0m; error = null; return true; }
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            error = null; return true;
        }
        error = $"'{text}' — не десятичное число."; return false;
    }

    public string? ConvertBack(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Конвертер decimal?.</summary>
public sealed class SgNullableDecimalConverter : ISgConverter<decimal?>
{
    public bool TryConvert(string? text, out decimal? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            value = v; error = null; return true;
        }
        value = null; error = $"'{text}' — не десятичное число."; return false;
    }

    public string? ConvertBack(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
}

// ── Bool ──────────────────────────────────────────────────────────────────────

/// <summary>Конвертер bool. Принимает: true/false/1/0/yes/no/да/нет.</summary>
public sealed class SgBoolConverter : ISgConverter<bool>
{
    private static readonly HashSet<string> TrueValues =
        new(StringComparer.OrdinalIgnoreCase) { "true", "1", "yes", "on", "да" };
    private static readonly HashSet<string> FalseValues =
        new(StringComparer.OrdinalIgnoreCase) { "false", "0", "no", "off", "нет" };

    public bool TryConvert(string? text, out bool value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = false; error = null; return true;
        }

        if (TrueValues.Contains(text)) { value = true; error = null; return true; }
        if (FalseValues.Contains(text)) { value = false; error = null; return true; }

        value = false;
        error = $"'{text}' — не булево значение. Допустимо: true/false, 1/0, yes/no.";
        return false;
    }

    public string? ConvertBack(bool value) => value ? "true" : "false";
}

/// <summary>Конвертер bool?.</summary>
public sealed class SgNullableBoolConverter : ISgConverter<bool?>
{
    public bool TryConvert(string? text, out bool? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            text == "1" || text.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            value = true; error = null; return true;
        }
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            text == "0" || text.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            value = false; error = null; return true;
        }
        value = null; error = $"'{text}' — не булево значение."; return false;
    }

    public string? ConvertBack(bool? value) => value?.ToString().ToLowerInvariant();
}

// ── DateTime ──────────────────────────────────────────────────────────────────

/// <summary>Конвертер DateTime. ConvertBack: ISO 8601.</summary>
public sealed class SgDateTimeConverter : ISgConverter<DateTime>
{
    public bool TryConvert(string? text, out DateTime value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = default; error = null; return true; }
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out value))
        {
            error = null; return true;
        }
        error = $"'{text}' — не корректная дата."; return false;
    }

    public string? ConvertBack(DateTime value)
        => value == default ? null : value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}

/// <summary>Конвертер DateTime?.</summary>
public sealed class SgNullableDateTimeConverter : ISgConverter<DateTime?>
{
    public bool TryConvert(string? text, out DateTime? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var v))
        {
            value = v; error = null; return true;
        }
        value = null; error = $"'{text}' — не корректная дата."; return false;
    }

    public string? ConvertBack(DateTime? value)
        => value?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}

// ── DateTimeOffset ────────────────────────────────────────────────────────────

/// <summary>Конвертер DateTimeOffset.</summary>
public sealed class SgDateTimeOffsetConverter : ISgConverter<DateTimeOffset>
{
    public bool TryConvert(string? text, out DateTimeOffset value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = default; error = null; return true; }
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out value))
        {
            error = null; return true;
        }
        error = $"'{text}' — не корректная дата со смещением."; return false;
    }

    public string? ConvertBack(DateTimeOffset value)
        => value == default ? null : value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
}

/// <summary>Конвертер DateTimeOffset?.</summary>
public sealed class SgNullableDateTimeOffsetConverter : ISgConverter<DateTimeOffset?>
{
    public bool TryConvert(string? text, out DateTimeOffset? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var v))
        {
            value = v; error = null; return true;
        }
        value = null; error = $"'{text}' — не корректная дата со смещением."; return false;
    }

    public string? ConvertBack(DateTimeOffset? value)
        => value?.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
}

// ── DateOnly ──────────────────────────────────────────────────────────────────

/// <summary>Конвертер DateOnly (только дата, без времени).</summary>
public sealed class SgDateOnlyConverter : ISgConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public bool TryConvert(string? text, out DateOnly value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = default; error = null; return true; }
        if (DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out value) ||
            DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = null; return true;
        }
        error = $"'{text}' — не корректная дата. Ожидается формат: {Format}."; return false;
    }

    public string? ConvertBack(DateOnly value)
        => value == default ? null : value.ToString(Format, CultureInfo.InvariantCulture);
}

/// <summary>Конвертер DateOnly?.</summary>
public sealed class SgNullableDateOnlyConverter : ISgConverter<DateOnly?>
{
    private const string Format = "yyyy-MM-dd";

    public bool TryConvert(string? text, out DateOnly? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var v) ||
            DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out v))
        {
            value = v; error = null; return true;
        }
        value = null; error = $"'{text}' — не корректная дата. Ожидается: {Format}."; return false;
    }

    public string? ConvertBack(DateOnly? value)
        => value?.ToString(Format, CultureInfo.InvariantCulture);
}

// ── TimeOnly ──────────────────────────────────────────────────────────────────

/// <summary>Конвертер TimeOnly (только время, без даты).</summary>
public sealed class SgTimeOnlyConverter : ISgConverter<TimeOnly>
{
    private const string Format = "HH:mm:ss";

    public bool TryConvert(string? text, out TimeOnly value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = default; error = null; return true; }
        if (TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            error = null; return true;
        }
        error = $"'{text}' — не корректное время. Ожидается: HH:mm или HH:mm:ss."; return false;
    }

    public string? ConvertBack(TimeOnly value)
        => value == default ? null : value.ToString(Format, CultureInfo.InvariantCulture);
}

// ── Guid ──────────────────────────────────────────────────────────────────────

/// <summary>Конвертер Guid.</summary>
public sealed class SgGuidConverter : ISgConverter<Guid>
{
    public bool TryConvert(string? text, out Guid value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = Guid.Empty; error = null; return true; }
        if (Guid.TryParse(text, out value)) { error = null; return true; }
        error = $"'{text}' — не корректный GUID."; return false;
    }

    public string? ConvertBack(Guid value)
        => value == Guid.Empty ? null : value.ToString("D");
}

/// <summary>Конвертер Guid?.</summary>
public sealed class SgNullableGuidConverter : ISgConverter<Guid?>
{
    public bool TryConvert(string? text, out Guid? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (Guid.TryParse(text, out var v)) { value = v; error = null; return true; }
        value = null; error = $"'{text}' — не корректный GUID."; return false;
    }

    public string? ConvertBack(Guid? value) => value?.ToString("D");
}

// ── Enum ──────────────────────────────────────────────────────────────────────

/// <summary>Универсальный конвертер для enum-типов.</summary>
public sealed class SgEnumConverter<TEnum> : ISgConverter<TEnum>
    where TEnum : struct, Enum
{
    public bool TryConvert(string? text, out TEnum value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = default; error = null; return true; }
        if (Enum.TryParse<TEnum>(text, ignoreCase: true, out value)) { error = null; return true; }
        error = $"'{text}' — не допустимое значение для {typeof(TEnum).Name}. " +
                $"Допустимо: {string.Join(", ", Enum.GetNames<TEnum>())}.";
        return false;
    }

    public string? ConvertBack(TEnum value) => value.ToString();
}

/// <summary>Универсальный конвертер для nullable enum-типов.</summary>
public sealed class SgNullableEnumConverter<TEnum> : ISgConverter<TEnum?>
    where TEnum : struct, Enum
{
    public bool TryConvert(string? text, out TEnum? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = null; error = null; return true; }
        if (Enum.TryParse<TEnum>(text, ignoreCase: true, out var v))
        {
            value = v; error = null; return true;
        }
        value = null;
        error = $"'{text}' — не допустимое значение для {typeof(TEnum).Name}.";
        return false;
    }

    public string? ConvertBack(TEnum? value) => value?.ToString();
}

// ── ToString Fallback ─────────────────────────────────────────────────────────

/// <summary>
/// Универсальный fallback-конвертер через Convert.ChangeType.
/// Используйте для типов без специализированного конвертера.
/// </summary>
public sealed class SgToStringConverter<TValue> : ISgConverter<TValue>
{
    public bool TryConvert(string? text, out TValue? value, out string? error)
    {
        if (text is null)
        {
            value = default;
            error = null;
            return true;
        }

        try
        {
            value = (TValue?)Convert.ChangeType(text, typeof(TValue), CultureInfo.InvariantCulture);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            value = default;
            error = $"Не удалось преобразовать '{text}' в {typeof(TValue).Name}: {ex.Message}";
            return false;
        }
    }

    public string? ConvertBack(TValue? value) => value?.ToString();
}
