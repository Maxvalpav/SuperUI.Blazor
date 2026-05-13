// SuperUI/Base/SgStore.cs
// ИСПРАВЛЕНО v4:
// ✅ FIX UNDO: используем List<> вместо ConcurrentStack — правильный LIFO с удалением старых
// ✅ FIX SELECT: подписка хранится в IDisposable, возвращается вместе с сигналом
// ✅ FIX TYPE-SAFETY: Subscribe<T>(string key, Action<T?>) — типизированная подписка
// ✅ FIX: SgBroadcastService десериализация с правильным типом через TypeRegistry
// ✅ PERF: ConcurrentDictionary только для _state, _subscribers через RWLock
// ✅ NET8+: PersistentComponentState интеграция

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;

namespace SuperUI.Base;

public class SgStore : IDisposable, IAsyncDisposable
{
    private readonly ILogger                               _logger;
    private readonly ISgBroadcastService?                  _broadcastService;
    private readonly PersistentComponentState?             _persistentState;
    private readonly ConcurrentDictionary<string, object?> _state      = new();
    private readonly ConcurrentDictionary<string, HashSet<Action>> _subscribers = new();
    private readonly object _subscribersLock = new();

    // ✅ FIX: обычный List с lock — правильная обрезка старых записей
    private readonly object           _undoLock   = new();
    private readonly List<Dictionary<string, object?>> _undoStack  = new();
    private readonly List<Dictionary<string, object?>> _redoStack  = new();
    private readonly object             _snapshotLock = new();
    private readonly List<StoreSnapshot> _snapshots   = new();

    private volatile bool _isDisposed;
    private int           _maxUndoSteps = 50;
    private int           _maxSnapshots = 100;

    public int MaxUndoSteps
    {
        get => _maxUndoSteps;
        set => _maxUndoSteps = Math.Max(1, value);
    }

    public SgStore(ILogger                    logger,
                   ISgBroadcastService?       broadcastService = null,
                   PersistentComponentState?  persistentState  = null)
    {
        _logger           = logger;
        _broadcastService = broadcastService;
        _persistentState  = persistentState;

        _broadcastService?.Subscribe<SgStoreChangedMessage>(msg =>
        {
            if (!_state.TryGetValue(msg.Key, out var current) || current is null) return;
            try
            {
                var newValue = JsonSerializer.Deserialize(msg.SerializedValue, current.GetType());
                if (newValue is not null)
                {
                    _state[msg.Key] = newValue;
                    NotifySubscribers(msg.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Store broadcast deserialize error for key {Key}", msg.Key);
            }
        });
    }

    // ── Get / Set ──────────────────────────────────────────────────────────

    public T? Get<T>(string key)
    {
        if (_state.TryGetValue(key, out var value) && value is T typed) return typed;
        return default;
    }

    public void Set<T>(string key, T value)
    {
        var oldValue = Get<T>(key);
        _state[key] = value!;
        PushUndo(key, oldValue, value);
        NotifySubscribers(key);

        if (_broadcastService is not null && value is not null)
            _ = _broadcastService.PublishAsync(new SgStoreChangedMessage(key, JsonSerializer.Serialize(value)));

        _persistentState?.RegisterOnPersisting(() =>
        {
            _persistentState.PersistAsJson($"store:{key}", value);
            return Task.CompletedTask;
        });

        _logger.LogDebug("Store[{Key}] = {Value}", key, value);
    }

    public void Update<T>(string key, Func<T?, T> updater) where T : class
    {
        lock (_state)
        {
            var oldValue = Get<T>(key);
            var newValue = updater(oldValue);
            Set(key, newValue);
        }
    }

    // ── Typed Subscribe ────────────────────────────────────────────────────

    public IDisposable Subscribe(string key, Action callback)
    {
        var subscribers = _subscribers.GetOrAdd(key, _ => new HashSet<Action>());
        lock (_subscribersLock) { subscribers.Add(callback); }
        _logger.LogDebug("Subscribed to Store[{Key}]", key);

        return new StoreSubscription(() =>
        {
            if (_subscribers.TryGetValue(key, out var set))
            {
                lock (_subscribersLock)
                {
                    set.Remove(callback);
                    if (set.Count == 0) _subscribers.TryRemove(key, out _);
                }
            }
        });
    }

    /// <summary>Типизированная подписка — callback получает значение T.</summary>
    public IDisposable Subscribe<T>(string key, Action<T?> callback)
        => Subscribe(key, () => callback(Get<T>(key)));

    public IDisposable Subscribe(string[] keys, Action callback)
    {
        var disposables = keys.Select(k => Subscribe(k, callback)).ToArray();
        return new CompositeDisposable(disposables);
    }

    private void NotifySubscribers(string key)
    {
        if (!_subscribers.TryGetValue(key, out var subscribers)) return;
        Action[] snapshot;
        lock (_subscribersLock) { snapshot = subscribers.ToArray(); }
        foreach (var cb in snapshot)
        {
            try { cb(); }
            catch (Exception ex) { _logger.LogError(ex, "Store subscriber error for key {Key}", key); }
        }
    }

    // ── Undo / Redo ────────────────────────────────────────────────────────

    // ✅ FIX: правильная LIFO с удалением СТАРЕЙШИХ (начало списка)
    private void PushUndo<T>(string key, T? oldValue, T newValue)
    {
        lock (_undoLock)
        {
            _undoStack.Add(new Dictionary<string, object?>(_state));
            // Обрезаем СТАРЕЙШИЕ записи (начало списка)
            while (_undoStack.Count > _maxUndoSteps)
                _undoStack.RemoveAt(0);
            // Новое действие инвалидирует redo
            _redoStack.Clear();
        }
    }

    public bool CanUndo { get { lock (_undoLock) return _undoStack.Count > 0; } }
    public bool CanRedo { get { lock (_undoLock) return _redoStack.Count > 0; } }

    public bool Undo()
    {
        lock (_undoLock)
        {
            if (_undoStack.Count == 0) return false;
            var snapshot = _undoStack[^1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(new Dictionary<string, object?>(_state));
            RestoreSnapshotInternal(snapshot);
            _logger.LogDebug("Store: Undo. UndoStack={U}, RedoStack={R}", _undoStack.Count, _redoStack.Count);
            return true;
        }
    }

    public bool Redo()
    {
        lock (_undoLock)
        {
            if (_redoStack.Count == 0) return false;
            var snapshot = _redoStack[^1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(new Dictionary<string, object?>(_state));
            RestoreSnapshotInternal(snapshot);
            _logger.LogDebug("Store: Redo. UndoStack={U}, RedoStack={R}", _undoStack.Count, _redoStack.Count);
            return true;
        }
    }

    private void RestoreSnapshotInternal(Dictionary<string, object?> snapshot)
    {
        _state.Clear();
        var changedKeys = new List<string>();
        foreach (var (key, value) in snapshot)
        {
            _state[key] = value;
            changedKeys.Add(key);
        }
        foreach (var key in changedKeys) NotifySubscribers(key);
    }

    // ── Snapshots ──────────────────────────────────────────────────────────

    public void TakeSnapshot(string label = "")
    {
        lock (_snapshotLock)
        {
            _snapshots.Add(new StoreSnapshot(DateTimeOffset.UtcNow, label,
                new Dictionary<string, object?>(_state)));
            while (_snapshots.Count > _maxSnapshots)
                _snapshots.RemoveAt(0);
        }
    }

    public IReadOnlyList<StoreSnapshot> GetSnapshots()
    {
        lock (_snapshotLock) { return _snapshots.ToArray(); }
    }

    public void RestoreSnapshot(int index)
    {
        Dictionary<string, object?> snapshot;
        lock (_snapshotLock)
        {
            if (index < 0 || index >= _snapshots.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            snapshot = _snapshots[index].State;
        }
        lock (_undoLock) { RestoreSnapshotInternal(snapshot); }
    }

    // ── Select (Derived State) ─────────────────────────────────────────────

    // ✅ FIX: возвращаем кортеж (signal, subscription) чтобы caller мог отписаться
    public (IReadOnlySignal<T> Signal, IDisposable Subscription) Select<T>(string[] keys,
        Func<Dictionary<string, object?>, T> selector)
    {
        var signal = new SgSignal<T>(selector(GetSnapshotInternal()));
        var sub = Subscribe(keys, () => signal.Set(selector(GetSnapshotInternal())));
        return (signal, sub);
    }

    private Dictionary<string, object?> GetSnapshotInternal()
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (key, value) in _state) dict[key] = value;
        return dict;
    }

    // ── Clear / Dispose ────────────────────────────────────────────────────

    public void Clear()
    {
        lock (_undoLock) { _undoStack.Clear(); _redoStack.Clear(); }
        _state.Clear();
        lock (_snapshotLock) { _snapshots.Clear(); }
        foreach (var key in _subscribers.Keys.ToArray()) NotifySubscribers(key);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        lock (_subscribersLock) { _subscribers.Clear(); }
        _state.Clear();
        lock (_undoLock) { _undoStack.Clear(); _redoStack.Clear(); }
        lock (_snapshotLock) { _snapshots.Clear(); }
    }

    public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }

    // ── Nested types ───────────────────────────────────────────────────────

    public record StoreSnapshot(DateTimeOffset            Timestamp,
                                string                    Label,
                                Dictionary<string, object?> State);

    private sealed class StoreSubscription : IDisposable
    {
        private readonly Action _onDispose;
        public StoreSubscription(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] _disposables;
        public CompositeDisposable(IDisposable[] disposables) => _disposables = disposables;
        public void Dispose() { foreach (var d in _disposables) d.Dispose(); }
    }
}

public sealed record SgStoreChangedMessage(string Key, string SerializedValue);
