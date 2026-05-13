// SuperUI/Base/Reactive/SgComputed.cs
// ✅ Lazy subscription — подписка только при первом чтении Value
// ✅ Dispose — отмена подписки с сигналов, очистка графа

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

public sealed class SgComputed<T> : ISignalObserver<T>, ISignalSubscribable, IDisposable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T> _comparer;
    private T _cachedValue;
    private int _disposed;
    private int _subscribed;
    private readonly object _lock = new();

    private readonly HashSet<WeakReference<SgComponentBase>> _componentSubscribers = new();
    private readonly HashSet<ISignalObserver> _untypedObservers = new();

    /// <summary>Граф зависимостей (для отладки/анализа).</summary>
    public ConcurrentBag<object> Dependencies { get; } = new();

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        _compute = compute;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _cachedValue = ComputeInternal();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ComputeInternal()
    {
        using var scope = SignalTracker.EnterScopeForObserver(this);
        return _compute();
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // Регистрируемся как зависимость текущего scope (если он есть)
            SignalTracker.TrackComputed(this);

            // Ленивая подписка — только при первом чтении
            if (Interlocked.CompareExchange(ref _subscribed, 1, 0) == 0)
            {
                // Пересобираем зависимости + подписываемся
                using var scope = SignalTracker.EnterScopeForObserver(this);
                _cachedValue = _compute();
                SignalTracker.SubscribeToTracked(this);
            }
            return _cachedValue;
        }
    }

    public T Peek() => _cachedValue;

    /// <summary>Подписать компонент: при изменении computed — RequestRender.</summary>
    public void Subscribe(SgComponentBase component)
    {
        lock (_lock) _componentSubscribers.Add(new WeakReference<SgComponentBase>(component));
    }

    void ISignalSubscribable.SubscribeObserverUntyped(ISignalObserver observer)
    {
        lock (_lock) _untypedObservers.Add(observer);
    }

    // ── ISignalObserver<T> ────────────────────────────────────────────────────
    public void OnSignalChanged()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        var newValue = ComputeInternal();
        bool changed;
        lock (_lock)
        {
            if (_disposed == 1) return;
            changed = !_comparer.Equals(_cachedValue, newValue);
            if (changed) _cachedValue = newValue;
        }
        if (changed) NotifySubscribers();
    }

    public void OnSignalRead(SgSignal<T> signal) { }
    public void OnComputedRead(SgComputed<T> computed) { }

    private void NotifySubscribers()
    {
        WeakReference<SgComponentBase>[]? compSnapshot;
        ISignalObserver[]? untypedSnapshot;
        lock (_lock)
        {
            compSnapshot = _componentSubscribers.Count > 0 ? _componentSubscribers.ToArray() : null;
            untypedSnapshot = _untypedObservers.Count > 0 ? _untypedObservers.ToArray() : null;
        }
        if (compSnapshot is not null)
        {
            List<WeakReference<SgComponentBase>>? dead = null;
            foreach (var wr in compSnapshot)
            {
                if (wr.TryGetTarget(out var comp) && !comp.IsDisposed)
                    SignalBatch.NotifyComponent(comp);
                else
                    (dead ??= new()).Add(wr);
            }
            if (dead is not null)
                lock (_lock)
                    foreach (var d in dead) _componentSubscribers.Remove(d);
        }
        if (untypedSnapshot is not null)
            foreach (var obs in untypedSnapshot)
            {
                try { obs.OnSignalChanged(); }
                catch (Exception ex)
                { System.Diagnostics.Debug.WriteLine($"[SgComputed] Observer error: {ex.Message}"); }
            }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        Dependencies.Clear();
        lock (_lock)
        {
            _componentSubscribers.Clear();
            _untypedObservers.Clear();
        }
    }

    public static implicit operator T(SgComputed<T> computed) => computed.Value;
    public override string ToString() => $"SgComputed<{typeof(T).Name}>({_cachedValue})";
}
