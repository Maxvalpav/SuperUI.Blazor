// SuperUI/Base/Reactive/SgComputed.cs
// ИСПРАВЛЕНИЕ: Dependencies использует ConcurrentDictionary<K,byte> вместо ConcurrentBag
// → гарантирована дедупликация зависимостей, нет дублей

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Вычисляемое реактивное значение с дедупликацией зависимостей.
/// </summary>
public sealed class SgComputed<T> : IReadOnlySignal<T>, ISignalTrackingObserver, IDisposable, ISignalFlushable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T>? _comparer;
    private T _cachedValue = default!;
    private bool _isDirty = true;
    private int _disposed;
    private readonly object _lock = new();
    private ISignalObserver? _observer;
    private List<ISignalObserver>? _observers;
    private readonly HashSet<ISgSignal> _dependencies = new();

    public string? DebugName { get; }

    public int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                if (_observers != null) return _observers.Count;
                return _observer != null ? 1 : 0;
            }
        }
    }

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null, string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        _compute = compute;
        _comparer = comparer;
        DebugName = debugName ?? $"Computed<{typeof(T).Name}>";
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SgReactiveComponentBase.TrackSignalImplicitly(this);
            if (_isDirty)
            {
                Recompute();
            }
            return _cachedValue;
        }
    }

    private void Recompute()
    {
        lock (_lock)
        {
            if (!_isDirty) return;

            // Отписываемся от старых зависимостей
            foreach (var dep in _dependencies)
            {
                dep.Unsubscribe(this);
            }
            _dependencies.Clear();

            using (SgReactiveComponentBase.EnterScope(this))
            {
                var newValue = _compute();
                _cachedValue = newValue;
                _isDirty = false;
            }
        }
    }

    // ISignalTrackingObserver implementation
    public void OnSignalRead(ISgSignal signal)
    {
        if (_dependencies.Add(signal))
        {
            signal.Subscribe(this);
        }
    }

    public void OnSignalChanged(ISgSignal signal)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        bool changed = false;
        lock (_lock)
        {
            if (_isDirty) return;
            _isDirty = true;
            changed = true;
        }

        if (changed)
        {
            if (SignalBatch.IsBatching)
            {
                SignalBatch.MarkDirty(this);
            }
            else
            {
                NotifyObservers();
            }
        }
    }

    void ISignalFlushable.FlushIfDirty()
    {
        NotifyObservers();
    }

    public void Subscribe(ISignalObserver observer)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        lock (_lock)
        {
            if (_observer == null) _observer = observer;
            else if (_observer == observer) return;
            else
            {
                _observers ??= new List<ISignalObserver>(4) { _observer };
                if (!_observers.Contains(observer)) _observers.Add(observer);
            }
        }
    }

    public void Unsubscribe(ISignalObserver observer)
    {
        lock (_lock)
        {
            if (_observer == observer)
            {
                _observer = null;
                if (_observers is { Count: > 0 })
                {
                    _observer = _observers[0];
                    _observers.RemoveAt(0);
                    if (_observers.Count == 0) _observers = null;
                }
            }
            else if (_observers != null)
            {
                _observers.Remove(observer);
                if (_observers.Count == 0) _observers = null;
            }
        }
    }

    private void NotifyObservers()
    {
        lock (_lock)
        {
            if (_observer != null) _observer.OnSignalChanged(this);
            if (_observers != null)
            {
                var snapshot = _observers.ToArray();
                foreach (var obs in snapshot) obs.OnSignalChanged(this);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        lock (_lock)
        {
            foreach (var dep in _dependencies) dep.Unsubscribe(this);
            _dependencies.Clear();
            _observer = null;
            _observers?.Clear();
            _observers = null;
        }
    }

    public static implicit operator T(SgComputed<T> computed) => computed.Value;
    public override string ToString() => $"{DebugName}: {_cachedValue}";
}
