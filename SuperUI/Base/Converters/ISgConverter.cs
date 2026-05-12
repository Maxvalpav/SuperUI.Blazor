// SuperUI/Base/Converters/ISgConverter.cs

namespace SuperUI.Base.Converters;

/// <summary>
/// Двунаправленный конвертер для компонентов форм.
/// Преобразует строку ввода ↔ типизированное значение.
/// </summary>
/// <typeparam name="TValue">Тип значения поля формы.</typeparam>
public interface ISgConverter<TValue>
{
    /// <summary>
    /// Преобразует строку пользовательского ввода в значение типа TValue.
    /// </summary>
    /// <param name="text">Строка из поля ввода (может быть null).</param>
    /// <param name="value">Результирующее значение при успехе.</param>
    /// <param name="error">Сообщение об ошибке при неудаче (null при успехе).</param>
    /// <returns>true если конвертация успешна.</returns>
    bool TryConvert(string? text, out TValue? value, out string? error);

    /// <summary>
    /// Преобразует значение обратно в строку для отображения в поле ввода.
    /// </summary>
    /// <param name="value">Значение (может быть null для nullable-типов).</param>
    /// <returns>Строковое представление или null.</returns>
    string? ConvertBack(TValue? value);
}
