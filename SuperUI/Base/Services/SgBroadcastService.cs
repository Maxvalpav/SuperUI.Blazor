using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SuperUI.Base.Services;

/// <summary>
/// In-process реализация ISgBroadcastService через System.Threading.Channels.
/// Thread-safe: ConcurrentDictionary + поддержка async-обработчиков.
///
/// WASM: работает корректно (однопоточный, но Channel поддерживает async).
/// Server: работает per-server-process (не cross-server).
/// Для cross-server: переопределите на SignalR Hub или Redis pub/sub.
/// </summary>
public sealed class SgBroadcastService : ISgBroadcastService
{
    // Ключ: channel name → список обработчиков (type-erased)
    private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _handlers = new();
    private readonly Lock _handlersLock = new();
    private volatile bool _disposed;

    /// <inheritdoc/>
    public async Task PublishAsync<T>(string channel, T message) where T : notnull
    {
        if (_disposed) return;
        ArgumentNullException.ThrowIfNull(channel);

        if (!_handlers.TryGetValue(channel, out var handlers)) return;

        Func<object, Task>[] snapshot;
        lock (_handlersLock)
            snapshot = [.. handlers];

        foreach (var handler in snapshot)
        {
            try { await handler(message); }
            catch { /* не прерываем остальных подписчиков */ }
        }
    }

    /// <inheritdoc/>
    public IAsyncDisposable Subscribe<T>(string channel, Func<T, Task> handler) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(handler);

        Func<object, Task> wrapper = msg =>
            msg is T typed ? handler(typed) : Task.CompletedTask;

        var list = _handlers.GetOrAdd(channel, _ => []);
        lock (_handlersLock)
            list.Add(wrapper);

        return new AsyncSubscription(() =>
        {
            lock (_handlersLock)
                list.Remove(wrapper);
            return ValueTask.CompletedTask;
        });
    }

    /// <inheritdoc/>
    public IAsyncDisposable Subscribe<T>(string channel, Action<T> handler) where T : notnull
        => Subscribe<T>(channel, msg => { handler(msg); return Task.CompletedTask; });

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _handlers.Clear();
        return ValueTask.CompletedTask;
    }

    private sealed class AsyncSubscription : IAsyncDisposable
    {
        private readonly Func<ValueTask> _dispose;
        public AsyncSubscription(Func<ValueTask> dispose) => _dispose = dispose;
        public ValueTask DisposeAsync() => _dispose();
    }
}
