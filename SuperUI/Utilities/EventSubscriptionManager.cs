// Файл: Utilities/EventSubscriptionManager.cs
// Зависимости: NONE
// Решает проблему забытых event subscriptions = memory leaks

namespace SuperUI.Utilities;

/// <summary>
/// Менеджер подписок на события с автоматической отпиской при Dispose.
/// 
/// ПРОБЛЕМА: разработчик подписывается на событие в OnInitialized,
/// но забывает отписаться в Dispose → memory leak + double-events.
/// 
/// РЕШЕНИЕ: регистрируем пары (subscribe, unsubscribe) и вызываем
/// unsubscribe автоматически.
/// </summary>
public sealed class EventSubscriptionManager : IDisposable
{
    // Stack чтобы отписываться в обратном порядке подписки
    private readonly Stack<Action> _unsubscriptions = new();
    private bool _disposed;

    /// <summary>
    /// Зарегистрировать подписку с автоматической отпиской.
    /// </summary>
    public void Register(Action subscribe, Action unsubscribe)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(EventSubscriptionManager));
        subscribe();
        _unsubscriptions.Push(unsubscribe);
    }

    /// <summary>
    /// Shorthand: Register(source += handler, source -= handler).
    /// Использование: Register(() => SomeEvent += OnEvent, () => SomeEvent -= OnEvent)
    /// </summary>
    public void Register<TArgs>(
        ref EventHandler<TArgs>? eventField,
        EventHandler<TArgs> handler)
    {
        eventField += handler;
        _unsubscriptions.Push(() => eventField -= handler);
    }

    /// <summary>Подписка на Action-событие.</summary>
    public void Register(ref Action? eventField, Action handler)
    {
        eventField += handler;
        _unsubscriptions.Push(() => eventField -= handler);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        while (_unsubscriptions.Count > 0)
        {
            try { _unsubscriptions.Pop()(); }
            catch { /* best effort */ }
        }
    }
}
