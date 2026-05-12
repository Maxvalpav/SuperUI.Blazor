// SuperUI/Base/Reactive/SgComputed.cs
//
// ИСПРАВЛЕНИЯ:
//   CS0308: ComputedObserver реализует ISignalObserver<T> (generic) — строка 102
//
// УЛУЧШЕНИЯ:
//   1. ForceInvalidate() — инвалидация без пересчёта
//   2. IsStale — публичный флаг устаревания
//   3. Recompute защита от reentrance (_isRecomputing)
//   4. _dependencies отслеживает все сигналы для DevTools
//   5. Thread-safe Dispose (idempotent)
//   6. ToString() информативный
//   7. НОВОЕ: TryGetCached() — безопасное чтение без пересчёта

using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Вычисляемый сигнал: мемоизирует результат и инвалидируется при изменении зависимостей.
/// Аналог Vue computed / MobX computed / Angular signal computed.
/// </summary>
/// <typeparam name="T">Тип вычисляемого значения.</typeparam>
/// <remarks>
/// WASM: однопоточный — _isRecomputing предотвращает рекурсию.
/// Server: per-circuit — Recompute может вызываться из разных потоков → защита через Interlocked.
/// </remarks>
public sealed class SgComputed<T> : IDisposable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T> _comparer;
    private T _cachedValue;
    private int _isDirtyInt = 1;      // 1 = dirty (требует пересчёта), 0 = clean
    private int _isRecomputing;       // 0 = свободен, 1 = вычисляется (anti-reentrance)
    private int _disposedInt;
    private readonly ComputedObserver _observer;

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _cachedValue = default!;
        _observer = new ComputedObserver(Invalidate);
    }

    /// <summary>
    /// Текущее вычисленное значение. Пересчитывается лениво при изменении зависимостей.
    /// Регистрирует подписку в SignalTracker (реактивное чтение).
    /// </summary>
    public T Value
    {
        get
        {
            if (Volatile.Read(ref _isDirtyInt) == 1)
                Recompute();

            // Регистрируем зависимость в родительском computed/effect
            SignalTracker.TrackComputed(this);
            return _cachedValue;
        }
    }

    /// <summary>true — данные устарели и будут пересчитаны при следующем обращении.</summary>
    public bool IsStale => Volatile.Read(ref _isDirtyInt) == 1;

    /// <summary>
    /// Прочитать кэшированное значение БЕЗ пересчёта и БЕЗ регистрации подписки.
    /// Возвращает (false, default) если данные устарели.
    /// </summary>
    public (bool HasValue, T Value) TryGetCached()
        => Volatile.Read(ref _isDirtyInt) == 0
            ? (true, _cachedValue)
            : (false, default!);

    private void Recompute()
    {
        // Anti-reentrance: предотвращаем рекурсивный пересчёт
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
                // ✅ PERF-5 FIX: Volatile.Write для visibility между потоками (Blazor Server)
                // На WASM это no-op, но безопасно.
                Volatile.Write(ref _cachedValue!, newValue);
                _observer.NotifyChanged();  // уведомить зависимые computed/components
            }
            else
            {
                // Обновляем даже если равно (ссылки могут меняться)
                Volatile.Write(ref _cachedValue!, newValue);
            }
        }
        catch (Exception ex)
        {
            // Сбрасываем dirty чтобы избежать бесконечного retry
            Interlocked.Exchange(ref _isDirtyInt, 0);
            System.Diagnostics.Debug.WriteLine(
                $"[SgComputed<{typeof(T).Name}>] Compute error: {ex.Message}");
            throw;  // propagate — пусть компонент получит исключение
        }
        finally
        {
            Interlocked.Exchange(ref _isRecomputing, 0);
        }
    }

    /// <summary>Принудительно инвалидировать кэш и уведомить подписчиков (без пересчёта).</summary>
    public void ForceInvalidate()
    {
        Interlocked.Exchange(ref _isDirtyInt, 1);
        _observer.NotifyChanged();
    }

    private void Invalidate()
    {
        // Инвалидируем только если были clean (избегаем лишних уведомлений)
        var wasDirty = Interlocked.Exchange(ref _isDirtyInt, 1) == 1;
        if (!wasDirty) _observer.NotifyChanged();
    }

    // ── Internal API ──────────────────────────────────────────────────────────

    internal void Subscribe(SgComponentBase component)
        => _observer.Subscribe(component);

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        _observer.Dispose();
    }

    // ── Операторы ─────────────────────────────────────────────────────────────

    public static implicit operator T(SgComputed<T> computed) => computed.Value;

    public override string ToString() => $"SgComputed<{typeof(T).Name}>({_cachedValue})";

    // ── Вложенный наблюдатель ─────────────────────────────────────────────────

    // ИСПРАВЛЕНИЕ CS0308 строка 102: ISignalObserver<T> (generic)
    private sealed class ComputedObserver : ISignalObserver<T>, IDisposable
    {
        private readonly Action _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents = new();
        private readonly HashSet<object> _dependencies = new();    // для DevTools
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

        // ISignalObserver (non-generic) — изменение зависимости
        public void OnSignalChanged() => _invalidate();

        // ISignalObserver<T> — typed tracking (для DevTools графа зависимостей)
        public void OnSignalRead(SgSignal<T> signal)
        {
            lock (_lock) _dependencies.Add(signal);
        }

        public void OnComputedRead(SgComputed<T> computed)
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
