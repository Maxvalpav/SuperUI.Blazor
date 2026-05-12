// SuperUI/Base/Reactive/SgComputed.cs
// ИСПРАВЛЕНО:
// 1. _isDirty — volatile для Server thread-safety
// 2. Recompute — защита от двойного входа через lock
// 3. ComputedObserver._dependencies — HashSet для дедупликации
// 4. Invalidate — проверка _isDirty перед NotifyChanged
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Вычисляемый сигнал: мемоизирует результат функции и автоматически
/// инвалидируется когда изменяются любые SgSignal, прочитанные в теле функции.
/// </summary>
public sealed class SgComputed<T> : IDisposable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T> _comparer;
    private T _cachedValue;
    // ИСПРАВЛЕНО: volatile для Server
    private volatile bool _isDirty = true;
    private int _disposed;
    private readonly object _recomputeLock = new();
    private readonly ComputedObserver _observer;

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        _compute = compute;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _cachedValue = default!;
        _observer = new ComputedObserver(Invalidate);
    }

    public T Value
    {
        get
        {
            if (_isDirty) Recompute();
            SignalTracker.TrackComputed(this);
            return _cachedValue;
        }
    }

    private void Recompute()
    {
        // ИСПРАВЛЕНО: lock защищает от двойного вычисления на Server
        lock (_recomputeLock)
        {
            if (!_isDirty) return; // double-check
            using (SignalTracker.EnterScopeForObserver(_observer))
            {
                var newValue = _compute();
                if (!_comparer.Equals(_cachedValue, newValue))
                {
                    _cachedValue = newValue;
                    _observer.NotifyChanged();
                }
                _isDirty = false;
            }
        }
    }

    private void Invalidate()
    {
        // ИСПРАВЛЕНО: избегаем лишних уведомлений если уже dirty
        if (_isDirty) return;
        _isDirty = true;
        _observer.NotifyChanged();
    }

    internal void Subscribe(SgComponentBase component) => _observer.Subscribe(component);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _observer.Dispose();
    }

    public static implicit operator T(SgComputed<T> computed) => computed.Value;

    private sealed class ComputedObserver : ISignalObserver, IDisposable
    {
        private readonly Action _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents = new();
        // ИСПРАВЛЕНО: HashSet для дедупликации зависимостей
        private readonly HashSet<object> _dependencies = new();
        private readonly object _lock = new();
        private int _disposed;

        public ComputedObserver(Action invalidate) => _invalidate = invalidate;

        internal void Subscribe(SgComponentBase component)
        {
            lock (_lock) _dependents.Add(new WeakReference<SgComponentBase>(component));
        }

        internal void NotifyChanged()
        {
            List<WeakReference<SgComponentBase>>? snapshot;
            List<WeakReference<SgComponentBase>>? dead = null;
            lock (_lock)
            {
                if (_dependents.Count == 0) return;
                snapshot = new(_dependents.Count);
                foreach (var r in _dependents)
                {
                    if (r.TryGetTarget(out var c) && !c.IsDisposed) snapshot.Add(r);
                    else (dead ??= new()).Add(r);
                }
                if (dead is not null) foreach (var d in dead) _dependents.Remove(d);
            }
            foreach (var r in snapshot)
                if (r.TryGetTarget(out var c) && !c.IsDisposed)
                    _ = c.RefreshAsync();
        }

        public void OnSignalChanged() => _invalidate();

        public void OnSignalRead<TVal>(SgSignal<TVal> signal)
        {
            lock (_lock) _dependencies.Add(signal);
        }

        public void OnComputedRead<TVal>(SgComputed<TVal> computed)
        {
            lock (_lock) _dependencies.Add(computed);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            lock (_lock)
            {
                _dependents.Clear();
                _dependencies.Clear();
            }
        }
    }
}