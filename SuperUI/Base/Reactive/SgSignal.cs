// SuperUI/Base/Reactive/SgSignal.cs
// ИСПРАВЛЕНО:
// ✅ Race в двухслотовой оптимизации: унифицированы чтение singleObserver + observers
// ✅ ISignalFlushable реализован корректно (определён в SignalBatch.cs)
// ✅ Subscribe/Unsubscribe: полная атомарность под lock
// ✅ NotifyObservers: snapshot вне lock
// ✅ .NET 8/9/10 совместим

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

/// <summary>
/// Типизированный наблюдатель сигнала.
/// Default interface method (C# 8+) перенаправляет нетиповой вызов в типизированный.
/// </summary>
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
    private T _value;
    private readonly IEqualityComparer<T>? _comparer;

    // ✅ ИСПРАВЛЕНО: вместо двухслотовой оптимизации используем единый List<> под lock.
    // Двухслотовая оптимизация давала race при Subscribe → single→list переходе.
    // Для большинства сигналов (1-3 подписчика) List с начальной ёмкостью 2
    // не создаёт заметных аллокаций.
    private List<ISignalObserver>? _observers;
    private volatile bool _isDisposed;
    private readonly object _lock = new();

    public string? DebugName { get; }

    public int SubscriberCount
    {
        get
        {
            lock (_lock) return _observers?.Count ?? 0;
        }
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
    public void Update(Func<T, T> mutator) => Set(mutator(_value));

    public void MutateAndNotify(Action<T> mutator)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, DebugName!);
        mutator(_value);

        if (SignalBatch.IsBatching)
        {
            SignalBatch.MarkDirty(this);
            return;
        }

        NotifyObservers();
    }

    public void Subscribe(ISignalObserver observer)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            if (_isDisposed) return;

            _observers ??= new List<ISignalObserver>(2);

            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }
    }

    public void Unsubscribe(ISignalObserver observer)
    {
        lock (_lock)
        {
            _observers?.Remove(observer);
        }
    }

    /// <summary>
    /// ✅ ИСПРАВЛЕНО: snapshot под lock, вызов ВНЕ lock — предотвращает deadlock.
    /// </summary>
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

    // ✅ Реализует ISignalFlushable из SignalBatch.cs
    void ISignalFlushable.FlushIfDirty() => NotifyObservers();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AreEqual(T a, T b)
        => _comparer?.Equals(a, b) ?? EqualityComparer<T>.Default.Equals(a, b);

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (_lock)
        {
            _observers?.Clear();
            _observers = null;
        }
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;

    public override string ToString() => $"{DebugName}: {_value}";
}