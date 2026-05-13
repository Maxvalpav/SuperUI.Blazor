// SuperUI/Base/Reactive/SgComputed.cs
// ИСПРАВЛЕНИЕ: Dependencies использует ConcurrentDictionary<K,byte> вместо ConcurrentBag
// → гарантирована дедупликация зависимостей, нет дублей

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Вычисляемое реактивное значение с дедупликацией зависимостей.
/// Dependencies — ConcurrentDictionary вместо ConcurrentBag, поэтому
/// каждая зависимость (сигнал/компьют) добавляется только один раз.
/// </summary>
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

    // Исправлено: ConcurrentDictionary<object, byte> дедуплицирует ключи автоматически
    public ConcurrentDictionary<object, byte> Dependencies { get; } = new();

    private static readonly AsyncLocal<int> _computeDepth = new();
    private const int MaxComputeDepth = 50;

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(compute);
        _compute = compute;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _cachedValue = ComputeWithGuard();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ComputeInternal()
    {
        using var scope = SignalTracker.EnterScopeForObserver(this);
        return _compute();
    }

    private T ComputeWithGuard()
    {
        _computeDepth.Value++;
        try
        {
            if (_computeDepth.Value > MaxComputeDepth)
                throw new InvalidOperationException(
                    $"SgComputed<{typeof(T).Name}>: cyclic dependency detected. " +
                    $"Compute depth exceeded {MaxComputeDepth}. Check your signal graph.");

            return ComputeInternal();
        }
        finally
        {
            _computeDepth.Value--;
        }
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SignalTracker.TrackComputed(this);

            if (Interlocked.CompareExchange(ref _subscribed, 1, 0) == 0)
            {
                _computeDepth.Value++;
                try
                {
                    if (_computeDepth.Value > MaxComputeDepth)
                        throw new InvalidOperationException(
                            $"SgComputed<{typeof(T).Name}>: cyclic dependency detected " +
                            $"in lazy subscription.");

                    using var scope = SignalTracker.EnterScopeForObserver(this);
                    _cachedValue = _compute();
                    SignalTracker.SubscribeToTracked(this);
                }
                finally
                {
                    _computeDepth.Value--;
                }
            }

            return _cachedValue;
        }
    }

    public T Peek() => _cachedValue;

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

        // Guard при перевычислении из нотификации
        _computeDepth.Value++;
        try
        {
            if (_computeDepth.Value > MaxComputeDepth)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SgComputed<{typeof(T).Name}>] Cyclic dependency suspected, keeping stale value");
                return;
            }

            var newValue = ComputeInternal();
            bool changed;
            lock (_lock)
            {
                if (_disposed == 1) return;
                changed = !_comparer.Equals(_cachedValue, newValue);
                if (changed)
                {
                    _cachedValue = newValue;
                }
            }

            if (changed) NotifySubscribers();
        }
        finally
        {
            _computeDepth.Value--;
        }
    }

    public void OnSignalRead(SgSignal<T> signal)
    {
        // Дедупликация через ConcurrentDictionary: TryAdd возвращает false если ключ уже есть
        Dependencies.TryAdd(signal, 0);
    }

    public void OnComputedRead(SgComputed<T> computed)
    {
        Dependencies.TryAdd(computed, 0);
    }

    private void NotifySubscribers()
    {
        WeakReference<SgComponentBase>[]? compSnapshot;
        ISignalObserver[]? untypedSnapshot;

        lock (_lock)
        {
            compSnapshot = _componentSubscribers.Count > 0
                ? _componentSubscribers.ToArray() : null;
            untypedSnapshot = _untypedObservers.Count > 0
                ? _untypedObservers.ToArray() : null;
        }

        if (compSnapshot is not null)
        {
            List<WeakReference<SgComponentBase>>? dead = null;
            foreach (var wr in compSnapshot)
            {
                if (wr.TryGetTarget(out var comp) && !comp.IsDisposed)
                    SignalBatch.NotifyComponent(comp);
                else
                    (dead ??= []).Add(wr);
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
                {
                    System.Diagnostics.Debug.WriteLine($"[SgComputed] Observer error: {ex.Message}");
                }
            }
    }

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
