// Файл: Services/IKeyboardHandlerService.cs
// Зависимости: NONE (интерфейс)

using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Services;

/// <summary>
/// Сервис глобальной обработки клавиатурных событий.
/// Позволяет компонентам регистрировать keyboard shortcuts.
/// </summary>
public interface IKeyboardHandlerService
{
    void Register(KeyboardShortcutRegistration registration);
    void Unregister(KeyboardShortcutRegistration registration);
}

/// <summary>
/// Сервис глобальной обработки мышиных событий.
/// </summary>
public interface IMouseHandlerService
{
    void Register(MouseEventRegistration registration);
    void Unregister(MouseEventRegistration registration);
}

/// <summary>
/// Сервис управления Focus Trap (удержание фокуса внутри контейнера).
/// Критично для модальных окон, диалогов, dropdown.
/// </summary>
public interface IFocusTrapService
{
    /// <summary>Активировать focus trap для элемента. Возвращает ID трапа.</summary>
    ValueTask<string> ActivateAsync(ElementReference element, string componentId);
    
    /// <summary>Деактивировать focus trap по ID.</summary>
    ValueTask DeactivateAsync(string trapId);
}
