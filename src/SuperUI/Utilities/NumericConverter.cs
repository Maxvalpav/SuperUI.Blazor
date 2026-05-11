namespace SuperUI.Utilities;

using System.Globalization;
using System.Numerics;

/// <summary>
/// Конвертер для числовых типов.
/// </summary>
public sealed class NumericConverter<T> : SgConverter<T> where T : struct, INumber<T>
{
    public override bool TryConvert(string? text, out T? value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(text)) { value = null; return true; }

        if (T.TryParse(text, NumberStyles.Any, Culture ?? CultureInfo.CurrentCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        error = $"Cannot convert '{text}' to {typeof(T).Name}";
        return false;
    }

    public override string? ConvertBack(T? value)
        => value?.ToString(null, Culture as CultureInfo ?? CultureInfo.CurrentCulture);
}
