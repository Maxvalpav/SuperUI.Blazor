// SuperUI/Base/Reactive/SgSignal.cs
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

/// <summary>Наблюдатель изменений сигнала.</summary>
public interface ISignalObserver
{
    void OnSignalChanged(ISgSignal signal);
}

/// <summary>Маркер для batch-flush.</summary>
internal interface ISignalFlushable
{
    void FlushIfDirty();
}

// ══════════════════════════════════════════════
// SgSignal<T>
// ══════════════════════════════════════════════

[DebuggerDisplay("{DebugName,nq} = {_value} ({SubscriberCount} subscribers)")]
public sealed class SgSignal<T> : ISgSignal<T>, IDisposable, ISignalFlushable
{
    private T _value;
    private readonly IEqualityComparer<T>? _comparer;

    // Двухслотовая оптимизация: один подписчик без аллокации списка
    private ISignalObserver? _singleObserver;
    private List<ISignalObserver>? _observers;
    private volatile bool _isDisposed;
    private readonly object _subscribeLock = new();

    public string? DebugName { get; }

    public int SubscriberCount
    {
        get
        {
            // lock-free read через snapshot
            var single = Volatile.Read(ref _singleObserver);
            var list   = Volatile.Read(ref _observers);
            return (single != null ? 1 : 0) + (list?.Count ?? 0);
        }
    }

    public SgSignal(T initialValue, string? debugName = null)
        : this(initialValue, null, debugName) { }

    public SgSignal(T initialValue, IEqualityComparer<T>? comparer, string? debugName = null)
    {
        _value    = initialValue;
        _comparer = comparer;
        DebugName = debugName ?? $"Signal<{typeof(T).Name}>";
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SgReactiveComponentBase.TrackSignalImplicitly(this);
            return _value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T newValue)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, DebugName!);
        if (AreEqual(_value, newValue)) return;

        _value = newValue;

        if (SignalBatch.IsBatching)
        {
            SignalBatch.MarkDirty(this);
            return;
        }

        NotifyObservers();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(Func<T, T> mutator)
    {
        var newValue = mutator(_value);
        Set(newValue);
    }

    public void MutateAndNotify(Action<T> mutator)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, DebugName!);
        mutator(_value);

        if (SignalBatch.IsBatching) { SignalBatch.MarkDirty(this); return; }

        NotifyObservers();
    }

    public void Subscribe(ISignalObserver observer)
    {
        if (_isDisposed) return;

        lock (_subscribeLock)
        {
            if (_isDisposed) return;

            if (_singleObserver == null)
            {
                _singleObserver = observer;
                return;
            }

            if (ReferenceEquals(_singleObserver, observer)) return;

            _observers ??= new List<ISignalObserver>(4) { _singleObserver };
            _singleObserver = null; // переходим в список

            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }
    }

    public void Unsubscribe(ISignalObserver observer)
    {
        lock (_subscribeLock)
        {
            if (ReferenceEquals(_singleObserver, observer))
            {
                _singleObserver = null;
                return;
            }

            _observers?.Remove(observer);
        }
    }

    // ✅ FIX DEADLOCK: снимаем snapshot ВНЕ lock, затем вызываем подписчиков
    internal void NotifyObservers()
    {
        ISignalObserver? single;
        ISignalObserver[]? snapshot;

        lock (_subscribeLock)
        {
            single   = _singleObserver;
            snapshot = _observers?.Count > 0 ? _observers.ToArray() : null;
        }

        // Вызов ВНЕ lock!
        single?.OnSignalChanged(this);
        if (snapshot is not null)
        {
            foreach (var obs in snapshot)
                obs.OnSignalChanged(this);
        }
    }

    void ISignalFlushable.FlushIfDirty() => NotifyObservers();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AreEqual(T a, T b) =>
        _comparer?.Equals(a, b) ?? EqualityComparer<T>.Default.Equals(a, b);

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (_subscribeLock)
        {
            _singleObserver = null;
            _observers?.Clear();
            _observers = null;
        }
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;

    public override string ToString() => $"{DebugName}: {_value}";
}
