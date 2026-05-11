// SuperUI/Base/Reactive/SgComputed.cs
// НОВЫЙ: вычисляемые сигналы с мемоизацией
// Аналог computed() из Vue 3 / createMemo() из Solid.js
// Автоматически отслеживает зависимые сигналы и пересчитывает при их изменении

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Вычисляемый сигнал: мемоизирует результат функции и автоматически
/// инвалидируется когда изменяются любые SgSignal, прочитанные в теле функции.
/// </summary>
/// <example>
/// var count = new SgSignal<int>(5);
/// var doubled = new SgComputed<int>(() => count.Value * 2);
/// // doubled.Value == 10, автоматически обновляется при count.Set(...)
/// </example>
public sealed class SgComputed<T> : IDisposable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T> _comparer;
    private T _cachedValue;
    private bool _isDirty = true;
    private bool _disposed;

    // Внутренний фиктивный компонент-наблюдатель для отслеживания зависимостей
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
            if (_isDirty)
                Recompute();
            // Пробрасываем текущий scope-трекер выше (для вложенных computed)
            SignalTracker.TrackComputed(this);
            return _cachedValue;
        }
    }

    private void Recompute()
    {
        // Отслеживаем какие сигналы читаются внутри _compute
        using (SignalTracker.EnterScopeForObserver(_observer))
        {
            var newValue = _compute();
            if (_isDirty || !_comparer.Equals(_cachedValue, newValue))
            {
                _cachedValue = newValue;
                _isDirty = false;
                // Уведомляем тех, кто подписан на этот computed
                _observer.NotifyChanged();
            }
        }
    }

    private void Invalidate()
    {
        _isDirty = true;
        _observer.NotifyChanged();
    }

    internal void Subscribe(SgComponentBase component) => _observer.Subscribe(component);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _observer.Dispose();
    }

    public static implicit operator T(SgComputed<T> computed) => computed.Value;

    /// <summary>Внутренний наблюдатель — является "компонентом" для SignalTracker.</summary>
    private sealed class ComputedObserver : ISignalObserver, IDisposable
    {
        private readonly Action _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents = new();
        private readonly List<object> _dependencies = new();
        private readonly object _lock = new();
        private bool _disposed;

        public ComputedObserver(Action invalidate) => _invalidate = invalidate;

        internal void Subscribe(SgComponentBase component)
        {
            lock (_lock)
                _dependents.Add(new WeakReference<SgComponentBase>(component));
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
                if (dead is not null)
                    foreach (var d in dead) _dependents.Remove(d);
            }

            foreach (var r in snapshot)
                if (r.TryGetTarget(out var c) && !c.IsDisposed)
                    _ = c.RefreshAsync();
        }

        // SgSignal вызывает этот метод через SignalTracker
        public void OnSignalChanged() => _invalidate();

        public void OnSignalRead<TVal>(SgSignal<TVal> signal)
        {
            lock (_lock)
                _dependencies.Add(signal);
        }

        public void OnComputedRead<TVal>(SgComputed<TVal> computed)
        {
            lock (_lock)
                _dependencies.Add(computed);
        }

        public void Dispose() { _disposed = true; _dependents.Clear(); _dependencies.Clear(); }
    }
}