// SuperUI/Base/Services/SgMediatorService.cs
// ✅ NEW: типизированный EventAggregator (аналог MudBlazor MediatorService)
// ✅ Слабые ссылки — не удерживает компоненты от GC
// ✅ NET8+: потокобезопасен для Server и WASM

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Services;

public sealed class SgMediatorService : ISgMediatorService
{
    private readonly ConcurrentDictionary<Type, List<WeakHandlerWrapper>> _handlers = new();
    private readonly ILogger<SgMediatorService>? _logger;
    private readonly object _lock = new();

    public SgMediatorService(ILogger<SgMediatorService>? logger = null)
    {
        _logger = logger;
    }

    public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : ISgMessage
    {
        var wrapper = new SyncWeakHandler<TMessage>(handler);
        RegisterWrapper(typeof(TMessage), wrapper);
        return new HandlerDisposable(() => UnregisterWrapper(typeof(TMessage), wrapper));
    }

    public IDisposable Subscribe<TMessage>(Func<TMessage, Task> handler) where TMessage : ISgMessage
    {
        var wrapper = new AsyncWeakHandler<TMessage>(handler);
        RegisterWrapper(typeof(TMessage), wrapper);
        return new HandlerDisposable(() => UnregisterWrapper(typeof(TMessage), wrapper));
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : ISgMessage
    {
        var handlers = GetLiveHandlers(typeof(TMessage));
        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await handler.InvokeAsync(message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Mediator handler error for {MessageType}", typeof(TMessage).Name);
            }
        }
    }

    public void Publish<TMessage>(TMessage message) where TMessage : ISgMessage
    {
        var handlers = GetLiveHandlers(typeof(TMessage));
        foreach (var handler in handlers)
        {
            try { handler.InvokeSync(message); }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Mediator sync handler error for {MessageType}", typeof(TMessage).Name);
            }
        }
    }

    private void RegisterWrapper(Type messageType, WeakHandlerWrapper wrapper)
    {
        var list = _handlers.GetOrAdd(messageType, _ => new List<WeakHandlerWrapper>());
        lock (_lock) { list.Add(wrapper); }
    }

    private void UnregisterWrapper(Type messageType, WeakHandlerWrapper wrapper)
    {
        if (!_handlers.TryGetValue(messageType, out var list)) return;
        lock (_lock) { list.Remove(wrapper); }
    }

    private WeakHandlerWrapper[] GetLiveHandlers(Type messageType)
    {
        if (!_handlers.TryGetValue(messageType, out var list)) return Array.Empty<WeakHandlerWrapper>();
        lock (_lock)
        {
            // Удаляем мёртвые слабые ссылки
            list.RemoveAll(h => !h.IsAlive);
            return list.ToArray();
        }
    }

    // ── Вложенные типы ─────────────────────────────────────────────────────

    private abstract class WeakHandlerWrapper
    {
        public abstract bool IsAlive { get; }
        public abstract Task InvokeAsync(object message);
        public abstract void InvokeSync(object message);
    }

    private sealed class SyncWeakHandler<TMessage> : WeakHandlerWrapper
    {
        private readonly WeakReference<Action<TMessage>> _ref;

        public SyncWeakHandler(Action<TMessage> handler) =>
            _ref = new WeakReference<Action<TMessage>>(handler);

        public override bool IsAlive => _ref.TryGetTarget(out _);

        public override Task InvokeAsync(object message)
        {
            if (_ref.TryGetTarget(out var h) && message is TMessage m) h(m);
            return Task.CompletedTask;
        }

        public override void InvokeSync(object message)
        {
            if (_ref.TryGetTarget(out var h) && message is TMessage m) h(m);
        }
    }

    private sealed class AsyncWeakHandler<TMessage> : WeakHandlerWrapper
    {
        private readonly WeakReference<Func<TMessage, Task>> _ref;

        public AsyncWeakHandler(Func<TMessage, Task> handler) =>
            _ref = new WeakReference<Func<TMessage, Task>>(handler);

        public override bool IsAlive => _ref.TryGetTarget(out _);

        public override Task InvokeAsync(object message)
        {
            if (_ref.TryGetTarget(out var h) && message is TMessage m) return h(m);
            return Task.CompletedTask;
        }

        public override void InvokeSync(object message)
        {
            if (_ref.TryGetTarget(out var h) && message is TMessage m)
                _ = h(m); // fire-and-forget для sync context
        }
    }

    private sealed class HandlerDisposable : IDisposable
    {
        private readonly Action _onDispose;
        private volatile bool _disposed;

        public HandlerDisposable(Action onDispose) => _onDispose = onDispose;

        public void Dispose() { if (!_disposed) { _disposed = true; _onDispose(); } }
    }
}
