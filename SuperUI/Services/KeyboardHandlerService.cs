// Файл: Services/KeyboardHandlerService.cs

using Microsoft.AspNetCore.Components.Web;
using SuperUI.Components.Base;

namespace SuperUI.Services;

/// <summary>
/// Централизованный сервис регистрации keyboard shortcuts.
/// Один JS event listener на документ вместо N на каждый компонент.
/// 
/// АРХИТЕКТУРА:
/// - Компонент регистрирует shortcut в сервисе
/// - Сервис имеет один JS listener (document.addEventListener)
/// - При нажатии — dispatch в нужный обработчик
/// - Поддержка приоритетов (последний зарегистрированный = высший приоритет)
/// </summary>
public interface IKeyboardHandlerService
{
    void Register(KeyboardShortcutRegistration registration);
    void Unregister(KeyboardShortcutRegistration registration);
}

public sealed class KeyboardHandlerService : IKeyboardHandlerService, IAsyncDisposable
{
    // Stack для приоритетов (LIFO — модальные диалоги перехватывают Escape первыми)
    private readonly Stack<KeyboardShortcutRegistration> _registrations = new();
    private readonly object _lock = new();

    public void Register(KeyboardShortcutRegistration registration)
    {
        lock (_lock)
            _registrations.Push(registration);
    }

    public void Unregister(KeyboardShortcutRegistration registration)
    {
        lock (_lock)
        {
            // Нельзя удалить из середины Stack — пересоздаём без данной регистрации
            var items = _registrations.Where(r => r != registration).ToArray();
            _registrations.Clear();
            // Добавляем в обратном порядке (Stack LIFO)
            foreach (var item in items.Reverse())
                _registrations.Push(item);
        }
    }

    /// <summary>
    /// Вызывается из JS при нажатии клавиши.
    /// [JSInvokable] метод регистрируется в SgComponentBase.
    /// </summary>
    public async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        KeyboardShortcutRegistration? handler = null;
        lock (_lock)
        {
            // Ищем первый подходящий handler (LIFO = высший приоритет сверху)
            handler = _registrations.FirstOrDefault(r =>
                string.Equals(r.Key, e.Key, StringComparison.OrdinalIgnoreCase) &&
                MatchesModifiers(r.Modifiers, e));
        }

        if (handler is not null)
            await handler.Handler(e);
    }

    private static bool MatchesModifiers(KeyboardModifiers modifiers, KeyboardEventArgs e)
    {
        if (modifiers == KeyboardModifiers.None)
            return !e.CtrlKey && !e.AltKey && !e.ShiftKey && !e.MetaKey;

        return
            ((modifiers & KeyboardModifiers.Ctrl) != 0) == e.CtrlKey &&
            ((modifiers & KeyboardModifiers.Shift) != 0) == e.ShiftKey &&
            ((modifiers & KeyboardModifiers.Alt) != 0) == e.AltKey &&
            ((modifiers & KeyboardModifiers.Meta) != 0) == e.MetaKey;
    }

    public ValueTask DisposeAsync()
    {
        lock (_lock) _registrations.Clear();
        return ValueTask.CompletedTask;
    }
}
