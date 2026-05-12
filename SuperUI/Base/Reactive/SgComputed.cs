// SuperUI/Base/Reactive/SgComputed.cs
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

    // ── FIX CS1002/CS1525/CS1519/CS8124 ──────────────────────────────────────
    // Объявляем поля ОТДЕЛЬНЫМИ строками — никаких кортежей и никаких
    // выражений с lock в инициализаторах полей.
    private int _isDirtyInt = 1;    // 1 = dirty, 0 = clean
    private int _isRecomputing;     // 0 = free, 1 = computing
    private int _disposedInt;       // 0 = alive, 1 = disposed
    // ─────────────────────────────────────────────────────────────────────────

    private readonly ComputedObserver _observer;

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        _compute  = compute  ?? throw new ArgumentNullException(nameof(compute));
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _cachedValue = default!;
        _observer = new ComputedObserver(Invalidate);
    }

    public T Value
    {
        get
        {
            if (Volatile.Read(ref _isDirtyInt) == 1)
                Recompute();
            SignalTracker.TrackComputed(this);
            return _cachedValue;
        }
    }

    private void Recompute()
    {
        // Только один поток пересчитывает; остальные получают кэшированное значение
        if (Interlocked.CompareExchange(ref _isRecomputing, 1, 0) == 1)
            return;
        try
        {
            _observer.BeginTracking();
            T newValue;
            using (SignalTracker.EnterScopeForObserver(_observer))
            {
                newValue = _compute();
            }
            // Сбрасываем dirty флаг после вычисления, но внутри отслеживания зависимостей
            Interlocked.Exchange(ref _isDirtyInt, 0);
            // Уведомляем ТОЛЬКО при реальном изменении значения
            if (!_comparer.Equals(_cachedValue, newValue))
            {
                _cachedValue = newValue;
                _observer.NotifyChanged();
            }
            else
            {
                // Обновляем кэш на случай reference equality (например, для строк)
                _cachedValue = newValue;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isRecomputing, 0);
        }
    }

    private void Invalidate()
    {
        var wasDirty = Interlocked.Exchange(ref _isDirtyInt, 1) == 1;
        if (!wasDirty)
            _observer.NotifyChanged();
    }

    /// <summary>FIX CS1061: метод Subscribe, который ищет SgComponentBase.cs стр.117/128</summary>
    internal void Subscribe(SgComponentBase component)
        => _observer.Subscribe(component);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        _observer.Dispose();
    }

    public static implicit operator T(SgComputed<T> computed) => computed.Value;

    // ── Вложенный наблюдатель ─────────────────────────────────────────────────
    private sealed class ComputedObserver : ISignalObserver, IDisposable
    {
        private readonly Action _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents = new();
        private readonly HashSet<object> _dependencies = new();  // SgSignal<T> | SgComputed<T>
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

        public void OnSignalChanged()   => _invalidate();
        public void OnSignalRead<T2>(SgSignal<T2> signal)
        {
            lock (_lock) _dependencies.Add(signal);
        }
        public void OnComputedRead<T2>(SgComputed<T2> computed)
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