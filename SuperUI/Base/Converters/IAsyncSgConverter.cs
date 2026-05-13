// SuperUI/Base/Converters/IAsyncSgConverter.cs
// UX-2: async конвертация для remote-валидации и сложного форматирования

namespace SuperUI.Base.Converters;

/// <summary>
/// Асинхронный конвертер для полей форм.
/// Используется когда конвертация требует IO (API, локализация).
/// </summary>
public interface IAsyncSgConverter<TValue>
{
    /// <summary>Конвертировать строку в значение асинхронно.</summary>
    Task<(bool Success, TValue? Value, string? Error)> TryConvertAsync(
        string? text,
        CancellationToken ct = default);

    /// <summary>Конвертировать значение в строку асинхронно.</summary>
    Task<string?> ConvertBackAsync(TValue? value, CancellationToken ct = default);

    /// <summary>Синхронная версия (для быстрых путей).</summary>
    bool TryConvert(string? text, out TValue? value, out string? error);

    /// <summary>Синхронная обратная конвертация.</summary>
    string? ConvertBack(TValue? value);
}
