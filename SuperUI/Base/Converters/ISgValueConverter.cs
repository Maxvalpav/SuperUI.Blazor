// SuperUI/Base/Converters/ISgValueConverter.cs
// NEW: Типизированный конвертер значений для форм
// Аналог: Blazorise IFluentValueConverter, MudBlazor Converter<T>

namespace SuperUI.Base.Converters;

/// <summary>
/// Конвертер значений для form-компонентов.
/// Преобразует между <typeparamref name="T"/> (тип модели) и string (отображаемое значение).
/// </summary>
public interface ISgValueConverter<T>
{
    /// <summary>Конвертировать значение модели в строку для отображения.</summary>
    string? ConvertToString(T? value);

    /// <summary>Конвертировать строку из UI в значение модели.</summary>
    /// <param name="text">Введённый текст.</param>
    /// <param name="result">Результат конвертации.</param>
    /// <param name="error">Сообщение об ошибке или null.</param>
    bool TryConvertFromString(string? text, out T? result, out string? error);
}

/// <summary>
/// Базовая реализация конвертера — удобная точка расширения.
/// </summary>
public abstract class SgValueConverterBase<T> : ISgValueConverter<T>
{
    public abstract string? ConvertToString(T? value);
    public abstract bool TryConvertFromString(string? text, out T? result, out string? error);

    protected static string? ErrorMessage(string msg, out string? error)
    {
        error = msg;
        return null;
    }
}

// ── Стандартные конвертеры ──────────────────────────────────────────────────

/// <summary>int ↔ string.</summary>
public sealed class IntConverter : SgValueConverterBase<int>
{
    public static readonly IntConverter Default = new();
    public override string? ConvertToString(int value) => value.ToString();
    public override bool TryConvertFromString(string? text, out int result, out string? error)
    {
        error = null;
        if (int.TryParse(text, out result)) return true;
        error = $"'{text}' is not a valid integer.";
        return false;
    }
}

/// <summary>decimal ↔ string.</summary>
public sealed class DecimalConverter : SgValueConverterBase<decimal>
{
    public static readonly DecimalConverter Default = new();
    private readonly System.Globalization.CultureInfo _culture;

    public DecimalConverter(System.Globalization.CultureInfo? culture = null)
        => _culture = culture ?? System.Globalization.CultureInfo.CurrentCulture;

    public override string? ConvertToString(decimal value)
        => value.ToString(_culture);

    public override bool TryConvertFromString(string? text, out decimal result, out string? error)
    {
        error = null;
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any, _culture, out result))
            return true;
        error = $"'{text}' is not a valid decimal.";
        return false;
    }
}

/// <summary>DateTime ↔ string с форматом.</summary>
public sealed class DateTimeConverter : SgValueConverterBase<DateTime?>
{
    public static readonly DateTimeConverter Default = new();
    private readonly string _format;
    private readonly System.Globalization.CultureInfo _culture;

    public DateTimeConverter(
        string format = "yyyy-MM-dd",
        System.Globalization.CultureInfo? culture = null)
    {
        _format = format;
        _culture = culture ?? System.Globalization.CultureInfo.CurrentCulture;
    }

    public override string? ConvertToString(DateTime? value)
        => value?.ToString(_format, _culture);

    public override bool TryConvertFromString(string? text, out DateTime? result, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(text)) { result = null; return true; }
        if (DateTime.TryParseExact(text, _format, _culture,
            System.Globalization.DateTimeStyles.None, out var dt))
        {
            result = dt;
            return true;
        }
        error = $"'{text}' is not a valid date ({_format}).";
        result = null;
        return false;
    }
}
