// SuperUI/Services/SgEventAggregator.cs
// Лёгкий in-memory pub/sub для слабой связи между сервисами и компонентами.

using System.Collections.Concurrent;

namespace SuperUI.Services;

/// <summary>
/// In-memory шина событий для слабой связи между сервисами/компонентами.
/// </summary>
/// <remarks>
/// <para>Поддерживает <see cref="Publish{TEvent}(TEvent)"/> и
/// <see cref="Subscribe{TEvent}(Func{TEvent, ValueTask})"/> с IDisposable-токеном.</para>
/// <para>Регистрируется как Singleton (глобальное состояние на circuit / session).</para>
/// </remarks>
public sealed class SgEventAggregator
{
    // Key: typeof(TEvent), Value: list of subscriptions.
    // Подписчики могут быть lambda/методом — захватывают внешние переменные.
    // Dispose: при отписке удаляем из списка (потокобезопасно под lock).
    private readonly ConcurrentDictionary<Type, List<Subscription>> _subscribers = new();
    private readonly object _lock = new();

    /// <summary>Публикует событие всем подписчикам.</summary>
    public async ValueTask PublishAsync<TEvent>(TEvent payload, CancellationToken ct = default)
        where TEvent : notnull
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var list)) return;

        // Snapshot чтобы не удерживать lock во время await
        Func<object, ValueTask>[] snapshot;
        lock (_lock)
        {
            if (list.Count == 0) return;
            snapshot = new Func<object, ValueTask>[list.Count];
            for (int i = 0; i < list.Count; i++) snapshot[i] = list[i].Handler;
        }

        foreach (var handler in snapshot)
        {
            try
            {
                if (ct.IsCancellationRequested) break;
                await handler(payload!).ConfigureAwait(false);
            }
            catch
            {
                // отдельный подписчик упал — продолжаем
            }
        }
    }

    /// <summary>Публикует событие (синхронный вариант, оборачивает в ValueTask.CompletedTask).</summary>
    public void Publish<TEvent>(TEvent payload) where TEvent : notnull
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var list)) return;

        Func<object, ValueTask>[] snapshot;
        lock (_lock)
        {
            if (list.Count == 0) return;
            snapshot = new Func<object, ValueTask>[list.Count];
            for (int i = 0; i < list.Count; i++) snapshot[i] = list[i].Handler;
        }

        foreach (var handler in snapshot)
        {
            try { _ = handler(payload!); }
            catch { }
        }
    }

    /// <summary>Подписывается на событие типа <typeparamref name="TEvent"/>.</summary>
    public IDisposable Subscribe<TEvent>(Func<TEvent, ValueTask> handler) where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        var sub = new Subscription(this, typeof(TEvent), e => handler((TEvent)e));
        lock (_lock)
        {
            var list = _subscribers.GetOrAdd(typeof(TEvent), _ => new List<Subscription>());
            list.Add(sub);
        }
        return sub;
    }

    /// <summary>Подписывается на событие типа <typeparamref name="TEvent"/> (синхронный handler).</summary>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Subscribe<TEvent>(e => { handler(e); return ValueTask.CompletedTask; });
    }

    private void Unsubscribe(Subscription sub)
    {
        if (!_subscribers.TryGetValue(sub.EventType, out var list)) return;
        lock (_lock)
        {
            list.Remove(sub);
            if (list.Count == 0) _subscribers.TryRemove(sub.EventType, out _);
        }
    }

    /// <summary>Очищает всех подписчиков.</summary>
    public void Clear()
    {
        lock (_lock) { _subscribers.Clear(); }
    }

    /// <summary>Подписчик (token, returned from Subscribe).</summary>
    private sealed class Subscription : IDisposable
    {
        private readonly SgEventAggregator _owner;
        public Type EventType { get; }
        public Func<object, ValueTask> Handler { get; }
        private int _disposed;

        public Subscription(SgEventAggregator owner, Type type, Func<object, ValueTask> handler)
        {
            _owner = owner;
            EventType = type;
            Handler = handler;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _owner.Unsubscribe(this);
        }
    }
}
