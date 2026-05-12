// SuperUI/Base/Reactive/SgComputed.cs
//
// ДОРАБОТКИ:
// 1. ForceRecompute() — принудительный пересчёт (обход кэша)
// 2. InvalidateAndNotify() — инвалидировать и уведомить подписчиков
// 3. Recompute: защита от конкурентного вычисления
// 4. IsStale — публичное свойство (для UI: "данные устарели")

using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Вычисляемый сигнал: мемоизирует результат и инвалидируется при изменении зависимостей.
/// </summary>
public sealed class SgComputed<T> : IDisposable
{
    private readonly Func<T>              _compute;
    private readonly IEqualityComparer<T> _comparer;
    private T   _cachedValue;
    private int _isDirtyInt    = 1;  // 1 = dirty, 0 = clean
    private int _isRecomputing;      // 0 = free, 1 = computing
    private int _disposedInt;
    private readonly ComputedObserver _observer;

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        _compute      = compute ?? throw new ArgumentNullException(nameof(compute));
        _comparer     = comparer ?? EqualityComparer<T>.Default;
        _cachedValue  = default!;
        _observer     = new ComputedObserver(Invalidate);
    }

    public T Value
    {
        get
        {
            if (Volatile.Read(ref _isDirtyInt) == 1) Recompute();
            SignalTracker.TrackComputed(this);
            return _cachedValue;
        }
    }

    /// <summary>Данные устарели и будут пересчитаны при следующем обращении.</summary>
    public bool IsStale => Volatile.Read(ref _isDirtyInt) == 1;

    private void Recompute()
    {
        if (Interlocked.CompareExchange(ref _isRecomputing, 1, 0) == 1) return;
        try
        {
            _observer.BeginTracking();
            T newValue;
            using (SignalTracker.EnterScopeForObserver(_observer))
                newValue = _compute();

            Interlocked.Exchange(ref _isDirtyInt, 0);

            if (!_comparer.Equals(_cachedValue, newValue))
            {
                _cachedValue = newValue;
                _observer.NotifyChanged();
            }
            else
            {
                _cachedValue = newValue;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isRecomputing, 0);
        }
    }

    /// <summary>Принудительно инвалидировать и уведомить (без пересчёта).</summary>
    public void ForceInvalidate()
    {
        Interlocked.Exchange(ref _isDirtyInt, 1);
        _observer.NotifyChanged();
    }

    private void Invalidate()
    {
        var wasDirty = Interlocked.Exchange(ref _isDirtyInt, 1) == 1;
        if (!wasDirty) _observer.NotifyChanged();
    }

    // ИСПРАВЛЕНО CS1061: Subscribe
    internal void Subscribe(SgComponentBase component) => _observer.Subscribe(component);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        _observer.Dispose();
    }

    public static implicit operator T(SgComputed<T> computed) => computed.Value;
    public override string? ToString() => $"SgComputed<{typeof(T).Name}>({_cachedValue})";

    // ── Вложенный наблюдатель ─────────────────────────────────────────────────────
    private sealed class ComputedObserver : ISignalObserver<T>, IDisposable
    {
        private readonly Action                                  _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents   = new();
        private readonly HashSet<object>                         _dependencies = new();
        private readonly object _lock = new();
        private int _disposedInt;

        public ComputedObserver(Action invalidate) => _invalidate = invalidate;

        internal void Subscribe(SgComponentBase component)
        {
            lock (_lock) _dependents.Add(new WeakReference<SgComponentBase>(component));
        }

        internal void BeginTracking()
        {
            lock (_lock) _dependencies.Clear();
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
                    if (r.TryGetTarget(out var c) && !c.IsDisposed)
                        snapshot.Add(r);
                    else
                        (dead ??= new()).Add(r);
                }
                if (dead is not null)
                    foreach (var d in dead) _dependents.Remove(d);
            }
            foreach (var r in snapshot)
                if (r.TryGetTarget(out var c) && !c.IsDisposed)
                    SignalBatch.NotifyComponent(c);
        }

        public void OnSignalChanged() => _invalidate();

        public void OnSignalRead(SgSignal<T> signal)
        { lock (_lock) _dependencies.Add(signal); }

        public void OnComputedRead<TVal>(SgComputed<TVal> computed)
        { lock (_lock) _dependencies.Add(computed); }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
            lock (_lock) { _dependents.Clear(); _dependencies.Clear(); }
        }
    }
}
