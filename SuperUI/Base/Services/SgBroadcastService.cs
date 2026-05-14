// SuperUI/Base/Services/SgBroadcastService.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ N1/C5: System.Threading.Lock → #if NET9_0_OR_GREATER / object
// ✅ L8: Publish<T> fire-and-forget сохраняет SynchronizationContext
// ✅ Dispose идемпотентен через Interlocked
// ✅ IAsyncDisposable реализован корректно

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// In-process реализация ISgBroadcastService.
/// Type-based pub/sub через ConcurrentDictionary.
/// Thread-safe: Server (multi-circuit) и WASM (однопоточный).
/// </summary>
public sealed class SgBroadcastService : ISgBroadcastService, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers = new();

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private int _disposed;

    /// <inheritdoc/>
    public IDisposable Subscribe<T>(Action<T> handler) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        return SubscribeCore<T>(msg => { handler(msg); return Task.CompletedTask; });
    }

    /// <inheritdoc/>
    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        return SubscribeCore<T>(handler);
    }

    private IDisposable SubscribeCore<T>(Func<T, Task> handler) where T : notnull
    {
        ThrowIfDisposed();

        Func<object, Task> wrapper = msg => msg is T typed
            ? handler(typed)
            : Task.CompletedTask;

        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Func<object, Task>>());
        lock (_lock) list.Add(wrapper);

        return new Subscription(() =>
        {
            lock (_lock) list.Remove(wrapper);
        });
    }

    /// <inheritdoc/>
    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : notnull
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        ArgumentNullException.ThrowIfNull(message);

        if (!_handlers.TryGetValue(typeof(T), out var list)) return;

        Func<object, Task>[] snapshot;
        lock (_lock) snapshot = list.ToArray();

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

    /// <inheritdoc/>
    public void Publish<T>(T message) where T : notnull
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        ArgumentNullException.ThrowIfNull(message);

        if (!_handlers.TryGetValue(typeof(T), out var list)) return;

        Func<object, Task>[] snapshot;
        lock (_lock) snapshot = list.ToArray();

        // ✅ FIX L8: захватываем SynchronizationContext для Blazor Server circuit
        var capturedContext = SynchronizationContext.Current;

        foreach (var handler in snapshot)
        {
            var h = handler;
            if (capturedContext is not null)
            {
                // Выполняем в контексте circuit (для InvokeAsync/StateHasChanged)
                capturedContext.Post(_ =>
                {
                    _ = h(message).ContinueWith(
                        t => System.Diagnostics.Debug.WriteLine($"[SgBroadcastService] Publish error: {t.Exception}"),
                        TaskContinuationOptions.OnlyOnFaulted);
                }, null);
            }
            else
            {
                _ = Task.Run(() => h(message));
            }
        }
    }

    /// <inheritdoc/>
    public int GetSubscriberCount<T>() where T : notnull
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return 0;
        lock (_lock) return list.Count;
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return ValueTask.CompletedTask;
        _handlers.Clear();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, nameof(SgBroadcastService));

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