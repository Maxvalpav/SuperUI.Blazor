// SuperUI/Base/Reactive/SgComputed.cs
// ИСПРАВЛЕНО v3:
// ✅ FIX: _compute() вызывается ВНЕ lock (_lock)
// ✅ FIX: Recompute использует двухфазный подход (dirty → compute → notify)
// ✅ FIX: _dependencies — HashSet с lock, не ConcurrentBag/ConcurrentDictionary
// ✅ PERF: избегаем двойного вхождения через _recomputeGuard
// ✅ AOT: нет рефлексии

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

public sealed class SgComputed<T> : IReadOnlySignal<T>, ISignalTrackingObserver, IDisposable, ISignalFlushable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T>? _comparer;
    private T _cachedValue = default!;
    private volatile bool _isDirty = true;
    private volatile int _disposed;
    private volatile int _recomputeInProgress; // guard против рекурсии

    // Подписчики computed'а
    private readonly object _subscribeLock = new();
    private ISignalObserver? _singleObserver;
    private List<ISignalObserver>? _observers;

    // Зависимости (сигналы, от которых зависит computed)
    private readonly object _depLock = new();
    private readonly HashSet<ISgSignal> _dependencies = new(ReferenceEqualityComparer.Instance);

    public string? DebugName { get; }

    public int SubscriberCount
    {
        get
        {
            lock (_subscribeLock)
                return (_singleObserver != null ? 1 : 0) + (_observers?.Count ?? 0);
        }
    }

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null, string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        _compute  = compute;
        _comparer = comparer;
        DebugName = debugName ?? $"Computed<{typeof(T).Name}>";
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SgReactiveComponentBase.TrackSignalImplicitly(this);

            if (_isDirty && Interlocked.CompareExchange(ref _recomputeInProgress, 1, 0) == 0)
            {
                try { Recompute(); }
                finally { Volatile.Write(ref _recomputeInProgress, 0); }
            }

            return _cachedValue;
        }
    }

    // ✅ FIX: compute вызывается ВНЕ lock
    private void Recompute()
    {
        if (!_isDirty) return;

        // Отписываемся от старых зависимостей
        ISgSignal[] oldDeps;
        lock (_depLock)
        {
            oldDeps = _dependencies.ToArray();
            _dependencies.Clear();
        }

        foreach (var dep in oldDeps)
            dep.Unsubscribe(this);

        // Вычисляем ВНЕ lock с отслеживанием зависимостей
        T newValue;
        using (SgReactiveComponentBase.EnterScope(this))
            newValue = _compute();

        var prevValue = _cachedValue;
        _cachedValue  = newValue;
        _isDirty      = false;

        // Уведомляем только если значение изменилось
        if (!AreEqual(prevValue, newValue))
            NotifyObservers();
    }

    // ISignalTrackingObserver
    public void OnSignalRead(ISgSignal signal)
    {
        lock (_depLock)
        {
            if (_dependencies.Add(signal))
                signal.Subscribe(this);
        }
    }

    public void OnSignalChanged(ISgSignal signal)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        // Атомарно помечаем как dirty
        if (!_isDirty)
        {
            _isDirty = true;

            if (SignalBatch.IsBatching)
                SignalBatch.MarkDirty(this);
            else
                NotifyObservers();
        }
    }

    void ISignalFlushable.FlushIfDirty()
    {
        if (_isDirty) NotifyObservers();
    }

    public void Subscribe(ISignalObserver observer)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        lock (_subscribeLock)
        {
            if (_singleObserver == null) { _singleObserver = observer; return; }
            if (ReferenceEquals(_singleObserver, observer)) return;

            _observers ??= new List<ISignalObserver>(4) { _singleObserver };
            _singleObserver = null;

            if (!_observers.Contains(observer)) _observers.Add(observer);
        }
    }

    public void Unsubscribe(ISignalObserver observer)
    {
        lock (_subscribeLock)
        {
            if (ReferenceEquals(_singleObserver, observer)) { _singleObserver = null; return; }
            _observers?.Remove(observer);
        }
    }

    private void NotifyObservers()
    {
        ISignalObserver? single;
        ISignalObserver[]? snapshot;

        lock (_subscribeLock)
        {
            single   = _singleObserver;
            snapshot = _observers?.Count > 0 ? _observers.ToArray() : null;
        }

        single?.OnSignalChanged(this);
        if (snapshot is not null)
            foreach (var obs in snapshot) obs.OnSignalChanged(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AreEqual(T a, T b) =>
        _comparer?.Equals(a, b) ?? EqualityComparer<T>.Default.Equals(a, b);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        lock (_depLock)
        {
            foreach (var dep in _dependencies) dep.Unsubscribe(this);
            _dependencies.Clear();
        }

        lock (_subscribeLock)
        {
            _singleObserver = null;
            _observers?.Clear();
            _observers = null;
        }
    }

    public static implicit operator T(SgComputed<T> c) => c.Value;

    public override string ToString() => $"{DebugName}: {_cachedValue}";
}

/// <summary>Расширение для внутреннего трекинга.</summary>
internal interface ISignalTrackingObserver : ISignalObserver
{
    void OnSignalRead(ISgSignal signal);
}
