// SuperUI/Base/SgStore.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ C4: Update<T> использует правильный lock-объект _updateLock (не _state)
// ✅ L5: PushUndo хранит только изменённый ключ, не полный снимок (opt-in full snapshot)
// ✅ W4: Select<T> возвращает SgSelectHandle<T> с IDisposable для signal + sub
// ✅ Dispose идемпотентен через Interlocked

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Глобальное key-value хранилище состояния с поддержкой
/// undo/redo, снимков, broadcast-синхронизации и реактивного select.
/// </summary>
public class SgStore : IDisposable, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly ISgBroadcastService? _broadcastService;
    private readonly PersistentComponentState? _persistentState;
    private readonly ConcurrentDictionary<string, object?> _state = new();
    private readonly ConcurrentDictionary<string, HashSet<Action>> _subscribers = new();

    private readonly object _subscribersLock = new();
    // ✅ FIX C4: отдельный lock для Update операций
    private readonly object _updateLock = new();

    // ✅ FIX L5: хранит (key, oldValue, newValue) — не полный снимок
    private readonly object _undoLock = new();
    private readonly List<UndoEntry> _undoStack = new();
    private readonly List<UndoEntry> _redoStack = new();
    private readonly object _snapshotLock = new();
    private readonly List<StoreSnapshot> _snapshots = new();

    private int _disposed;
    private int _maxUndoSteps = 50;
    private int _maxSnapshots = 100;

    public int MaxUndoSteps
    {
        get => _maxUndoSteps;
        set => _maxUndoSteps = Math.Max(1, value);
    }

    public SgStore(
        ILogger logger,
        ISgBroadcastService? broadcastService = null,
        PersistentComponentState? persistentState = null)
    {
        _logger = logger;
        _broadcastService = broadcastService;
        _persistentState = persistentState;

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

    // ── Get / Set ────────────────────────────────────────────────────────────
    public T? Get<T>(string key)
    {
        if (_state.TryGetValue(key, out var value) && value is T typed)
            return typed;
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

    /// <summary>
    /// ✅ FIX C4: атомарное read-modify-write через отдельный lock.
    /// ConcurrentDictionary не является lock-объектом!
    /// </summary>
    public void Update<T>(string key, Func<T?, T> updater) where T : class
    {
        lock (_updateLock)
        {
            var oldValue = Get<T>(key);
            var newValue = updater(oldValue);
            Set(key, newValue);
        }
    }

    // ── Subscribe ────────────────────────────────────────────────────────────
    public IDisposable Subscribe(string key, Action callback)
    {
        var subscribers = _subscribers.GetOrAdd(key, _ => new HashSet<Action>());
        lock (_subscribersLock)
            subscribers.Add(callback);

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
        lock (_subscribersLock)
            snapshot = subscribers.ToArray();

        foreach (var cb in snapshot)
        {
            try { cb(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Store subscriber error for key {Key}", key);
            }
        }
    }

    // ── Undo / Redo ──────────────────────────────────────────────────────────
    // ✅ FIX L5: UndoEntry хранит только изменённый ключ + значения
    private readonly record struct UndoEntry(string Key, object? OldValue, object? NewValue);

    private void PushUndo<T>(string key, T? oldValue, T newValue)
    {
        lock (_undoLock)
        {
            _undoStack.Add(new UndoEntry(key, oldValue, newValue));
            while (_undoStack.Count > _maxUndoSteps)
                _undoStack.RemoveAt(0);
            _redoStack.Clear();
        }
    }

    public bool CanUndo { get { lock (_undoLock) return _undoStack.Count > 0; } }
    public bool CanRedo { get { lock (_undoLock) return _redoStack.Count > 0; } }

    public bool Undo()
    {
        UndoEntry entry;
        lock (_undoLock)
        {
            if (_undoStack.Count == 0) return false;
            entry = _undoStack[^1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _redoStack.Add(entry);
        }
        // Восстанавливаем старое значение напрямую (без PushUndo — нет рекурсии)
        _state[entry.Key] = entry.OldValue;
        NotifySubscribers(entry.Key);
        _logger.LogDebug("Store: Undo key={Key}", entry.Key);
        return true;
    }

    public bool Redo()
    {
        UndoEntry entry;
        lock (_undoLock)
        {
            if (_redoStack.Count == 0) return false;
            entry = _redoStack[^1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _undoStack.Add(entry);
        }
        _state[entry.Key] = entry.NewValue;
        NotifySubscribers(entry.Key);
        _logger.LogDebug("Store: Redo key={Key}", entry.Key);
        return true;
    }

    // ── Snapshots ────────────────────────────────────────────────────────────
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
        lock (_snapshotLock) return _snapshots.ToArray();
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
        foreach (var (key, value) in snapshot)
        {
            _state[key] = value;
            NotifySubscribers(key);
        }
    }

    // ── Select (Derived State) ───────────────────────────────────────────────
    // ✅ FIX W4: возвращаем SgSelectHandle<T> который является IDisposable
    // и содержит как Signal, так и Subscription.
    // Caller получает IDisposable и может корректно освободить ресурсы.
    public SgSelectHandle<T> Select<T>(
        string[] keys,
        Func<IReadOnlyDictionary<string, object?>, T> selector)
    {
        var signal = new SgSignal<T>(selector(GetSnapshotInternal()));
        var sub = Subscribe(keys, () => signal.Set(selector(GetSnapshotInternal())));
        return new SgSelectHandle<T>(signal, sub);
    }

    private Dictionary<string, object?> GetSnapshotInternal()
    {
        var dict = new Dictionary<string, object?>();
        foreach (var (key, value) in _state)
            dict[key] = value;
        return dict;
    }

    // ── Clear / Dispose ──────────────────────────────────────────────────────
    public void Clear()
    {
        lock (_undoLock)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
        _state.Clear();
        lock (_snapshotLock) _snapshots.Clear();
        foreach (var key in _subscribers.Keys.ToArray())
            NotifySubscribers(key);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        lock (_subscribersLock) _subscribers.Clear();
        _state.Clear();
        lock (_undoLock) { _undoStack.Clear(); _redoStack.Clear(); }
        lock (_snapshotLock) _snapshots.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    // ── Nested types ─────────────────────────────────────────────────────────
    public record StoreSnapshot(
        DateTimeOffset Timestamp,
        string Label,
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
        public CompositeDisposable(IDisposable[] d) => _disposables = d;
        public void Dispose() { foreach (var d in _disposables) d.Dispose(); }
    }
}

/// <summary>
/// ✅ FIX W4: Handle для Select — caller должен Dispose для освобождения Signal + Subscription.
/// </summary>
public sealed class SgSelectHandle<T> : IDisposable
{
    private readonly SgSignal<T> _signal;
    private readonly IDisposable _subscription;
    private int _disposed;

    public IReadOnlySignal<T> Signal => _signal;

    internal SgSelectHandle(SgSignal<T> signal, IDisposable subscription)
    {
        _signal = signal;
        _subscription = subscription;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _subscription.Dispose();
        _signal.Dispose();
    }
}

public sealed record SgStoreChangedMessage(string Key, string SerializedValue);