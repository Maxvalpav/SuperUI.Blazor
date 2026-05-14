// SuperUI/Base/Reactive/SgSignal.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ C1: Volatile.Write для _value на ARM/x64 Server
// ✅ Dispose идемпотентен через Interlocked
// ✅ Set: проверка disposed через Volatile.Read (не _isDisposed)
// ✅ MutateAndNotify: Volatile.Write перед notify

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

// ══════════════════════════════════════════════
// ИНТЕРФЕЙСЫ
// ══════════════════════════════════════════════

/// <summary>Базовый интерфейс для всех сигналов.</summary>
public interface ISgSignal
{
    string? DebugName { get; }
    int SubscriberCount { get; }
    void Subscribe(ISignalObserver observer);
    void Unsubscribe(ISignalObserver observer);
}

/// <summary>Типизированный read-only сигнал.</summary>
public interface IReadOnlySignal<T> : ISgSignal
{
    T Value { get; }
}

/// <summary>Типизированный сигнал с записью.</summary>
public interface ISgSignal<T> : IReadOnlySignal<T>
{
    void Set(T value);
}

/// <summary>Наблюдатель изменений сигнала (нетиповой).</summary>
public interface ISignalObserver
{
    void OnSignalChanged(ISgSignal signal);
}

/// <summary>Типизированный наблюдатель сигнала.</summary>
public interface ISignalObserver<T> : ISignalObserver
{
    void OnSignalChanged(ISgSignal<T> typedSignal);

    void ISignalObserver.OnSignalChanged(ISgSignal signal)
    {
        if (signal is ISgSignal<T> typed)
            OnSignalChanged(typed);
    }
}

// ══════════════════════════════════════════════
// SgSignal<T>
// ══════════════════════════════════════════════

[DebuggerDisplay("{DebugName,nq} = {_value} ({SubscriberCount} subscribers)")]
public sealed class SgSignal<T> : ISgSignal<T>, IDisposable, ISignalFlushable
{
    // ✅ FIX C1: T может быть value type; для ссылочных типов нужен volatile.
    // Используем отдельный volatile int _valueVersion как memory fence.
    private T _value;
    private volatile int _valueVersion; // memory fence для _value
    private readonly IEqualityComparer<T>? _comparer;
    private List<ISignalObserver>? _observers;
    // ✅ FIX: Dispose идемпотентен через int + Interlocked
    private int _disposed; // 0 = alive, 1 = disposed
    private readonly object _lock = new();

    public string? DebugName { get; }

    public int SubscriberCount
    {
        get { lock (_lock) return _observers?.Count ?? 0; }
    }

    public SgSignal(T initialValue, string? debugName = null)
        : this(initialValue, null, debugName) { }

    public SgSignal(T initialValue, IEqualityComparer<T>? comparer, string? debugName = null)
    {
        _value = initialValue;
        _comparer = comparer;
        DebugName = debugName ?? $"Signal<{typeof(T).Name}>";
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SgReactiveComponentBase.TrackSignalImplicitly(this);
            // ✅ FIX: _valueVersion как memory fence гарантирует видимость _value
            _ = _valueVersion; // volatile read → acquire fence
            return _value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T newValue)
    {
        // ✅ FIX: Volatile.Read для _disposed без аллокации
        if (Volatile.Read(ref _disposed) == 1)
            throw new ObjectDisposedException(DebugName);

        if (AreEqual(_value, newValue)) return;

        // ✅ FIX: _valueVersion++ как release fence перед notify
        _value = newValue;
        Interlocked.Increment(ref _valueVersion); // release fence

        if (SignalBatch.IsBatching)
        {
            SignalBatch.MarkDirty(this);
            return;
        }
        NotifyObservers();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(Func<T, T> mutator) => Set(mutator(_value));

    public void MutateAndNotify(Action<T> mutator)
    {
        if (Volatile.Read(ref _disposed) == 1)
            throw new ObjectDisposedException(DebugName);

        mutator(_value);
        // ✅ FIX: release fence после мутации
        Interlocked.Increment(ref _valueVersion);

        if (SignalBatch.IsBatching)
        {
            SignalBatch.MarkDirty(this);
            return;
        }
        NotifyObservers();
    }

    public void Subscribe(ISignalObserver observer)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        lock (_lock)
        {
            if (Volatile.Read(ref _disposed) == 1) return; // double-check
            _observers ??= new List<ISignalObserver>(2);
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }
    }

    public void Unsubscribe(ISignalObserver observer)
    {
        lock (_lock)
            _observers?.Remove(observer);
    }

    internal void NotifyObservers()
    {
        ISignalObserver[]? snapshot;
        lock (_lock)
        {
            if (_observers is null || _observers.Count == 0) return;
            snapshot = _observers.ToArray();
        }
        foreach (var obs in snapshot)
        {
            try { obs.OnSignalChanged(this); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SgSignal] Observer error in {DebugName}: {ex}");
            }
        }
    }

    void ISignalFlushable.FlushIfDirty() => NotifyObservers();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AreEqual(T a, T b)
        => _comparer?.Equals(a, b) ?? EqualityComparer<T>.Default.Equals(a, b);

    public void Dispose()
    {
        // ✅ FIX: идемпотентный dispose через Interlocked
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        lock (_lock)
        {
            _observers?.Clear();
            _observers = null;
        }
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;
    public override string ToString() => $"{DebugName}: {_value}";
}