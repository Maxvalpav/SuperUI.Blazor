// SuperUI/Base/Reactive/SgComputed.cs
// ИСПРАВЛЕНО:
// ✅ OnSignalRead: добавлена проверка _disposed перед подпиской
// ✅ Recompute: двойная проверка _isDirty для предотвращения лишних вычислений
// ✅ Dispose: правильный порядок — сначала subscribers, потом deps
// ✅ Value getter: SgReactiveComponentBase.TrackSignalImplicitly вызывается ПЕРЕД lock

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Интерфейс для наблюдателей, которые хотят знать о факте чтения сигнала.
/// Объявлен ТОЛЬКО здесь. Убрать из SgReactiveComponentBase.cs.
/// </summary>
internal interface ISignalTrackingObserver : ISignalObserver
{
    void OnSignalRead(ISgSignal signal);
}

public sealed class SgComputed<T> : IReadOnlySignal<T>, ISignalTrackingObserver, IDisposable, ISignalFlushable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T>? _comparer;
    private T _cachedValue = default!;
    private volatile bool _isDirty = true;
    private volatile int _disposed;
    private volatile int _recomputeInProgress;
    private readonly object _subscribeLock = new();
    private ISignalObserver? _singleObserver;
    private List<ISignalObserver>? _observers;
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

    public SgComputed(
        Func<T> compute,
        IEqualityComparer<T>? comparer = null,
        string? debugName = null)
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
            // Трекаем зависимость ПЕРЕД вычислением (до lock)
            SgReactiveComponentBase.TrackSignalImplicitly(this);

            if (_isDirty && Interlocked.CompareExchange(ref _recomputeInProgress, 1, 0) == 0)
            {
                try { Recompute(); }
                finally { Volatile.Write(ref _recomputeInProgress, 0); }
            }

            return _cachedValue;
        }
    }

    private void Recompute()
    {
        // Двойная проверка: мог быть сброшен между CAS и вызовом
        if (!_isDirty) return;

        ISgSignal[] oldDeps;
        lock (_depLock)
        {
            oldDeps = _dependencies.ToArray();
            _dependencies.Clear();
        }

        foreach (var dep in oldDeps)
            dep.Unsubscribe(this);

        T newValue;
        using (SgReactiveComponentBase.EnterScope(this))
            newValue = _compute();

        var prevValue = _cachedValue;
        _cachedValue = newValue;
        _isDirty = false;

        if (!AreEqual(prevValue, newValue))
            NotifyObservers();
    }

    // ISignalTrackingObserver
    public void OnSignalRead(ISgSignal signal)
    {
        // ✅ FIX: проверяем _disposed перед подпиской
        if (Volatile.Read(ref _disposed) == 1) return;

        lock (_depLock)
        {
            if (Volatile.Read(ref _disposed) == 1) return; // double-check
            if (_dependencies.Add(signal))
                signal.Subscribe(this);
        }
    }

    public void OnSignalChanged(ISgSignal signal)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

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
            if (_singleObserver is null) { _singleObserver = observer; return; }
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
            single = _singleObserver;
            snapshot = _observers?.Count > 0 ? _observers.ToArray() : null;
        }

        single?.OnSignalChanged(this);
        if (snapshot is not null)
            foreach (var obs in snapshot) obs.OnSignalChanged(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AreEqual(T a, T b)
        => _comparer?.Equals(a, b) ?? EqualityComparer<T>.Default.Equals(a, b);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        // ✅ FIX: сначала subscriber'ы, потом deps
        lock (_subscribeLock)
        {
            _singleObserver = null;
            _observers?.Clear();
            _observers = null;
        }

        lock (_depLock)
        {
            foreach (var dep in _dependencies)
                dep.Unsubscribe(this);
            _dependencies.Clear();
        }
    }

    public static implicit operator T(SgComputed<T> c) => c.Value;

    public override string ToString() => $"{DebugName}: {_cachedValue}";
}
