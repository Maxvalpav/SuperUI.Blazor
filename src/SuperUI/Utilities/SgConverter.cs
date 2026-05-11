namespace SuperUI.Utilities;

using System.Globalization;
using System.Numerics;

/// <summary>
/// Двунаправленный конвертер между T и string.
/// Используется в форм-компонентах для связи типизированного Value и текстового представления.
/// </summary>
public interface ISgConverter<T>
{
    bool TryConvert(string? text, out T? value, out string? error);
    string? ConvertBack(T? value);
}

/// <summary>
/// Базовая реализация конвертера с культурой.
/// </summary>
public abstract class SgConverter<T> : ISgConverter<T>
{
    protected IFormatProvider? Culture { get; set; }

    public SgConverter<T> WithCulture(IFormatProvider culture)
    {
        Culture = culture;
        return this;
    }

    public abstract bool TryConvert(string? text, out T? value, out string? error);
    public abstract string? ConvertBack(T? value);
}
