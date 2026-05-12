// SuperUI/Base/Reactive/SgComputed.cs
// Ключевые исправления:
// 1. _isDirty / _isRecomputing — Interlocked для thread-safety
// 2. _dependencies очищаются перед каждым Recompute

using System.Runtime.CompilerServices;
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
    
    // ИСПРАВЛЕНО: Interlocked-флаги вместо volatile bool
    private int _isDirtyInt = 1; // 1 = dirty, 0 = clean
    private int _isRecomputing; // 0 = free, 1 = computing
    
    private int _disposedInt;
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
            if (Volatile.Read(ref _isDirtyInt) == 1) Recompute();
            SignalTracker.TrackComputed(this);
            return _cachedValue;
        }
    }

    private void Recompute()
    {
        // ИСПРАВЛЕНО: только один поток перевычисляет (остальные получат кешированное значение)
        if (Interlocked.CompareExchange(ref _isRecomputing, 1, 0) == 1) return;
        try
        {
            // ИСПРАВЛЕНО: очищаем старые зависимости перед каждым вычислением
            _observer.BeginTracking();
            
            using (SignalTracker.EnterScopeForObserver(_observer))
            {
                var newValue = _compute();
                var isDirty = Interlocked.Exchange(ref _isDirtyInt, 0) == 1;
                if (isDirty || !_comparer.Equals(_cachedValue, newValue))
                {
                    _cachedValue = newValue;
                    _observer.NotifyChanged();
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isRecomputing, 0);
        }
    }

    private void Invalidate()
    {
        // Устанавливаем флаг dirty, но уведомляем только если transitioning clean→dirty
        var wasDirty = Interlocked.Exchange(ref _isDirtyInt, 1) == 1;
        if (!wasDirty) _observer.NotifyChanged();
    }

    internal void Subscribe(SgComponentBase component) => _observer.Subscribe(component);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
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
        private int _disposedInt;

        public ComputedObserver(Action invalidate) => _invalidate = invalidate;

        internal void Subscribe(SgComponentBase component)
        {
            lock (_lock) _dependents.Add(new WeakReference<SgComponentBase>(component));
        }

        /// <summary>Очистить список зависимостей перед новым вычислением.</summary>
        internal void BeginTracking() => lock (_lock) _dependencies.Clear();

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
            if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
            lock (_lock)
            {
                _dependents.Clear();
                _dependencies.Clear();
            }
        }
    }
}
