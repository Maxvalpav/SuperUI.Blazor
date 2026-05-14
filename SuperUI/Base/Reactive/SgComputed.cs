// SuperUI/Base/Reactive/SgComputed.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ C2: Recompute защищён от concurrent execution через полный re-check цикл
// ✅ Dispose: правильный порядок + Interlocked
// ✅ _singleObserver → унифицирован в List (нет race при переходе single→list)
// ✅ TrackSignalImplicitly: вызывается ДО lock (корректно)

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Интерфейс для наблюдателей, отслеживающих факт чтения сигнала.
/// Определён ТОЛЬКО здесь — не дублировать в других файлах.
/// </summary>
internal interface ISignalTrackingObserver : ISignalObserver
{
    void OnSignalRead(ISgSignal signal);
}

public sealed class SgComputed<T> : IReadOnlySignal<T>, ISignalTrackingObserver,
    IDisposable, ISignalFlushable
{
    private readonly Func<T> _compute;
    private readonly IEqualityComparer<T>? _comparer;
    private T _cachedValue = default!;

    // ✅ FIX: _isDirty + _recomputeInProgress как int для Interlocked
    private volatile bool _isDirty = true;
    private int _disposed; // Interlocked
    private int _recomputeInProgress; // Interlocked CAS guard

    // ✅ FIX: единый List под _subscribeLock (нет race single→list)
    private readonly object _subscribeLock = new();
    private List<ISignalObserver>? _subscribers;

    private readonly object _depLock = new();
    private readonly HashSet<ISgSignal> _dependencies = new(ReferenceEqualityComparer.Instance);

    public string? DebugName { get; }

    public int SubscriberCount
    {
        get { lock (_subscribeLock) return _subscribers?.Count ?? 0; }
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
            // Трекаем зависимость ДО вычисления
            SgReactiveComponentBase.TrackSignalImplicitly(this);

            if (_isDirty)
            {
                // ✅ FIX: используем do-while для повторного вычисления
                // если _isDirty стал true снова во время вычисления
                if (Interlocked.CompareExchange(ref _recomputeInProgress, 1, 0) == 0)
                {
                    try
                    {
                        do
                        {
                            Recompute();
                        }
                        // Если во время вычисления пришло новое изменение — пересчитываем
                        while (_isDirty && Volatile.Read(ref _disposed) == 0);
                    }
                    finally
                    {
                        Volatile.Write(ref _recomputeInProgress, 0);
                    }
                }
                // else: другой поток вычисляет — возвращаем кешированное значение
            }
            return _cachedValue;
        }
    }

    private void Recompute()
    {
        if (!_isDirty) return;

        // Очищаем старые зависимости
        ISgSignal[] oldDeps;
        lock (_depLock)
        {
            oldDeps = _dependencies.ToArray();
            _dependencies.Clear();
        }
        foreach (var dep in oldDeps)
            dep.Unsubscribe(this);

        T newValue;
        // Сбрасываем _isDirty ДО вычисления, чтобы изменения во время вычисления
        // снова поставили _isDirty = true
        _isDirty = false;

        using (SgReactiveComponentBase.EnterScope(this))
            newValue = _compute();

        var prevValue = _cachedValue;
        _cachedValue = newValue;

        if (!AreEqual(prevValue, newValue))
            NotifySubscribers();
    }

    // ISignalTrackingObserver
    public void OnSignalRead(ISgSignal signal)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        lock (_depLock)
        {
            if (Volatile.Read(ref _disposed) == 1) return;
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
                NotifySubscribers();
        }
    }

    void ISignalFlushable.FlushIfDirty()
    {
        if (_isDirty) NotifySubscribers();
    }

    public void Subscribe(ISignalObserver observer)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        lock (_subscribeLock)
        {
            if (Volatile.Read(ref _disposed) == 1) return;
            _subscribers ??= new List<ISignalObserver>(2);
            if (!_subscribers.Contains(observer))
                _subscribers.Add(observer);
        }
    }

    public void Unsubscribe(ISignalObserver observer)
    {
        lock (_subscribeLock)
            _subscribers?.Remove(observer);
    }

    private void NotifySubscribers()
    {
        ISignalObserver[]? snapshot;
        lock (_subscribeLock)
        {
            if (_subscribers is null || _subscribers.Count == 0) return;
            snapshot = _subscribers.ToArray();
        }
        foreach (var obs in snapshot)
        {
            try { obs.OnSignalChanged(this); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SgComputed] Subscriber error in {DebugName}: {ex}");
            }
        }
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
            _subscribers?.Clear();
            _subscribers = null;
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