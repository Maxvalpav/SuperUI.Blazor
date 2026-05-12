// SuperUI/Base/Services/SgBroadcastService.cs

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// In-process реализация ISgBroadcastService.
/// Type-based pub/sub через ConcurrentDictionary.
///
/// Thread safety:
///   WASM: однопоточный — lock не нужен, но безопасен.
///   Server: per-circuit (Scoped) или Singleton — ConcurrentDictionary + lock для списков.
///
/// Для cross-server (multi-instance): переопределите с SignalR Hub или Redis Pub/Sub.
/// </summary>
public sealed class SgBroadcastService : ISgBroadcastService
{
    // Type → список type-erased async-обработчиков
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly Lock _lock = new();
    private volatile bool _disposed;

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Action<T> handler) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        return SubscribeCore<T>(msg =>
        {
            handler(msg);
            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        return SubscribeCore<T>(handler);
    }

    private IDisposable SubscribeCore<T>(Func<T, Task> handler) where T : notnull
    {
        ThrowIfDisposed();

        // Оборачиваем в type-erased Func<object, Task>
        Func<object, Task> wrapper = msg =>
            msg is T typed ? handler(typed) : Task.CompletedTask;

        var list = _handlers.GetOrAdd(typeof(T), _ => []);
        lock (_lock) list.Add(wrapper);

        return new Subscription(() =>
        {
            lock (_lock) list.Remove(wrapper);
        });
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : notnull
    {
        if (_disposed) return;
        ArgumentNullException.ThrowIfNull(message);

        if (!_handlers.TryGetValue(typeof(T), out var list)) return;

        Func<object, Task>[] snapshot;
        lock (_lock) snapshot = [.. list];

        foreach (var handler in snapshot)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await handler(message).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Ошибка одного подписчика не прерывает остальных
            }
        }
    }

    /// <inheritdoc />
    public void Publish<T>(T message) where T : notnull
    {
        if (_disposed) return;
        ArgumentNullException.ThrowIfNull(message);

        if (!_handlers.TryGetValue(typeof(T), out var list)) return;

        Func<object, Task>[] snapshot;
        lock (_lock) snapshot = [.. list];

        // Fire-and-forget: async-обработчики запускаем через Task.Run
        foreach (var handler in snapshot)
        {
            var h = handler;
            Task.Run(() => h(message));
        }
    }

    /// <inheritdoc />
    public int GetSubscriberCount<T>() where T : notnull
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return 0;
        lock (_lock) return list.Count;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        _handlers.Clear();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, nameof(SgBroadcastService));

    // ── Вспомогательный класс для отписки ────────────────────────────────────

    private sealed class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private int _disposed;

        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _unsubscribe();
        }
    }
}
