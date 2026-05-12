// SuperUI/Base/Reactive/SgSignal.cs
//
// ДОРАБОТКИ:
// 1. Reset(T) — сброс значения без уведомления подписчиков
// 2. ToString() — более информативный вывод
// 3. AsObservable() гарантированно BehaviorSubject-семантика
// 4. PurgeDeadSubscribers() — публичный
// 5. Корректный порядок полей (без выражений в инициализаторах)

using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — автоматически перерисовывает компоненты при изменении Value.
/// </summary>
public sealed class SgSignal<T> : IDisposable
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    private readonly object _lock = new();
    private readonly HashSet<ISignalObserver<T>> _observers = new();
    private int _disposedInt;

    public SgSignal(T initial, IEqualityComparer<T>? comparer = null)
    {
        _value    = initial;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { SignalTracker.Track(this); return _value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(value);
    }

    /// <summary>Прочитать значение БЕЗ реактивной подписки.</summary>
    public T Peek() { lock (_lock) return _value; }

    /// <summary>Установить значение. Уведомляет только при реальном изменении.</summary>
    public void Set(T newValue)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        bool changed;
        lock (_lock)
        {
            changed = !_comparer.Equals(_value, newValue);
            if (changed) _value = newValue;
        }
        if (changed) NotifySubscribers();
    }

    /// <summary>Атомарный read+compute+write (нет Lost Update).</summary>
    public void Update(Func<T, T> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        if (Volatile.Read(ref _disposedInt) == 1) return;
        T newValue;
        bool changed;
        lock (_lock)
        {
            newValue = updater(_value);
            changed  = !_comparer.Equals(_value, newValue);
            if (changed) _value = newValue;
        }
        if (changed) NotifySubscribers();
    }

    /// <summary>Сбросить значение БЕЗ уведомления подписчиков (тесты, инициализация).</summary>
    public void Reset(T value)
    {
        lock (_lock) { _value = value; }
    }

    public IObservable<T> AsObservable() => new SignalObservable(this);

    internal void Subscribe(SgComponentBase component)
    {
        lock (_lock) _subscribers.Add(new WeakReference<SgComponentBase>(component));
    }

    internal void SubscribeObserver(ISignalObserver<T> observer)
    {
        lock (_lock) _observers.Add(observer);
    }

    internal void UnsubscribeObserver(ISignalObserver<T> observer)
    {
        lock (_lock) _observers.Remove(observer);
    }

    /// <summary>Принудительно удалить мёртвые WeakRef.</summary>
    public void PurgeDeadSubscribers()
    {
        lock (_lock) _subscribers.RemoveWhere(w => !w.TryGetTarget(out _));
    }

    internal void Cleanup()
    {
        lock (_lock)
            _subscribers.RemoveWhere(wr => !wr.TryGetTarget(out var c) || c.IsDisposed);
    }

    private void NotifySubscribers()
    {
        List<WeakReference<SgComponentBase>>? snapshot = null;
        List<WeakReference<SgComponentBase>>? dead     = null;
        ISignalObserver<T>[]? observerSnapshot = null;

        lock (_lock)
        {
            if (_subscribers.Count == 0 && _observers.Count == 0) return;
            if (_subscribers.Count > 0)
            {
                snapshot = new(_subscribers.Count);
                foreach (var weakRef in _subscribers)
                {
                    if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                        snapshot.Add(weakRef);
                    else
                        (dead ??= new()).Add(weakRef);
                }
                if (dead is not null)
                    foreach (var d in dead) _subscribers.Remove(d);
            }
            if (_observers.Count > 0)
                observerSnapshot = _observers.ToArray();
        }

        if (snapshot is not null)
            foreach (var weakRef in snapshot)
                if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                    SignalBatch.NotifyComponent(comp);

        if (observerSnapshot is not null)
            foreach (var observer in observerSnapshot)
            {
                try { observer.OnSignalChanged(); }
                catch { /* наблюдатель не должен бросать */ }
            }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        lock (_lock) { _subscribers.Clear(); _observers.Clear(); }
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;
    public override string? ToString() => $"SgSignal<{typeof(T).Name}>({_value})";

    // ── IObservable адаптер ───────────────────────────────────────────────────────
    private sealed class SignalObservable : IObservable<T>
    {
        private readonly SgSignal<T> _signal;
        public SignalObservable(SgSignal<T> signal) => _signal = signal;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            var adapter = new ObserverAdapter(observer, _signal);
            _signal.SubscribeObserver(adapter);
            try { observer.OnNext(_signal.Peek()); } catch { }
            return adapter;
        }
    }

    private sealed class ObserverAdapter : ISignalObserver<T>, IDisposable
    {
        private readonly IObserver<T> _observer;
        private readonly SgSignal<T>  _signal;
        private int _disposed;

        public ObserverAdapter(IObserver<T> observer, SgSignal<T> signal)
        {
            _observer = observer;
            _signal   = signal;
        }

        public void OnSignalChanged()
        {
            if (Volatile.Read(ref _disposed) == 1) return;
            try { _observer.OnNext(_signal.Peek()); } catch { }
        }

        public void OnSignalRead(SgSignal<T> signal)  { }
        public void OnComputedRead<TVal>(SgComputed<TVal> c) { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _signal.UnsubscribeObserver(this);
            try { _observer.OnCompleted(); } catch { }
        }
    }
}
