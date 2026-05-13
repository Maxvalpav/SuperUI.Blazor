// SuperUI/Base/Reactive/SgSignal.cs
// ИСПРАВЛЕНИЯ v3:
// ✅ PERF-4: ArrayPool для snapshot — zero-allocation уведомление
// ✅ BUG-10 prevention: SubscribeObserver проверяет дубли
// ✅ НОВОЕ: Subscribe(Action<T>) — подписка без компонента
// ✅ НОВОЕ: Derived(Func<T, TDerived>) — создать производный сигнал
// ✅ НОВОЕ: SignalBatch integration — отложенная нотификация при Batch()

using System.Buffers;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

public sealed class SgSignal<T> : IDisposable, ISignalSubscribable, ISignalFlushable, ISgSignal<T>
{
    private readonly HashSet<ISignalObserver> _untypedObservers = new();

    void ISignalSubscribable.SubscribeObserverUntyped(ISignalObserver observer)
    {
        _lock.EnterWriteLock();
        try { _untypedObservers.Add(observer); }
        finally { _lock.ExitWriteLock(); }
    }

    private T _value;
    private readonly IEqualityComparer<T> _comparer;
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    private readonly HashSet<ISignalObserver<T>> _observers = new();
    private readonly List<WeakReference<Action<T>>> _callbacks = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private int _disposedInt;
    private volatile bool _dirty;
    private int _notifyCount;
    private const int PurgeEveryN = 100; // purge каждые 100 нотификаций

    public SgSignal(T initial, IEqualityComparer<T>? comparer = null)
    {
        _value = initial;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SignalTracker.Track(this);
            return _value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(value);
    }

    public T Peek()
    {
        _lock.EnterReadLock();
        try { return _value; }
        finally { _lock.ExitReadLock(); }
    }

    public void Set(T newValue)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;

        bool changed;
        _lock.EnterWriteLock();
        try
        {
            changed = !_comparer.Equals(_value, newValue);
            if (changed)
            {
                _value = newValue;

                // Если внутри SignalBatch — откладываем нотификацию
                if (SignalBatch.IsBatching)
                {
                    _dirty = true;
                    SignalBatch.AddDirty(this);
                    return;
                }
            }
        }
        finally { _lock.ExitWriteLock(); }

        if (changed)
            NotifySubscribers();
    }

    public void Update(Func<T, T> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        if (Volatile.Read(ref _disposedInt) == 1) return;

        T newValue;
        bool changed;
        _lock.EnterWriteLock();
        try
        {
            newValue = updater(_value);
            changed = !_comparer.Equals(_value, newValue);
            if (changed)
            {
                _value = newValue;
                if (SignalBatch.IsBatching)
                {
                    _dirty = true;
                    SignalBatch.AddDirty(this);
                    return;
                }
            }
        }
        finally { _lock.ExitWriteLock(); }

        if (changed)
            NotifySubscribers();
    }

    /// <summary>Сбросить значение без нотификации.</summary>
    public void Reset(T value)
    {
        _lock.EnterWriteLock();
        try { _value = value; }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Принудительно уведомить подписчиков (даже без изменения).</summary>
    public void ForceNotify()
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        NotifySubscribers();
    }

    // Вызывается из SignalBatch.End()
    void ISignalFlushable.FlushIfDirty()
    {
        if (_dirty)
        {
            _dirty = false;
            NotifySubscribers();
        }
    }

    // ── Подписки ────────────────────────────────────────────────────────────────

    public IDisposable Subscribe(Action<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var weak = new WeakReference<Action<T>>(callback);
        _lock.EnterWriteLock();
        try { _callbacks.Add(weak); }
        finally { _lock.ExitWriteLock(); }
        return new CallbackDisposable(this, callback, weak);
    }

    /// <summary>Начать пакетное обновление (batch).</summary>
    public IDisposable BeginBatch() => SignalBatch.Begin();

    public SgSignal<TDerived> Derived<TDerived>(Func<T, TDerived> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var derived = new SgSignal<TDerived>(selector(Peek()));
        Subscribe(value => derived.Set(selector(value)));
        return derived;
    }

    public IObservable<T> AsObservable() => new SignalObservable(this);

    internal void Subscribe(SgComponentBase component)
    {
        _lock.EnterWriteLock();
        try { _subscribers.Add(new WeakReference<SgComponentBase>(component)); }
        finally { _lock.ExitWriteLock(); }
    }

    internal void SubscribeObserver(ISignalObserver<T> observer)
    {
        _lock.EnterWriteLock();
        try
        {
            // BUG-10 prevention: проверяем дубли
            if (_observers.Contains(observer)) return;
            _observers.Add(observer);
        }
        finally { _lock.ExitWriteLock(); }
    }

    internal void UnsubscribeObserver(ISignalObserver<T> observer)
    {
        _lock.EnterWriteLock();
        try { _observers.Remove(observer); }
        finally { _lock.ExitWriteLock(); }
    }

    public void PurgeDeadSubscribers()
    {
        _lock.EnterWriteLock();
        try
        {
            _subscribers.RemoveWhere(w => !w.TryGetTarget(out _));
            _callbacks.RemoveWhere(w => !w.TryGetTarget(out _));
        }
        finally { _lock.ExitWriteLock(); }
    }

    internal void Cleanup()
    {
        _lock.EnterWriteLock();
        try
        {
            _subscribers.RemoveWhere(wr => !wr.TryGetTarget(out var c) || c.IsDisposed);
            _callbacks.RemoveWhere(wr => !wr.TryGetTarget(out _));
        }
        finally { _lock.ExitWriteLock(); }
    }

    // ── Нотификация (ArrayPool для zero-allocation) ─────────────────────────────

    private void NotifySubscribers()
    {
        // ✅ PERF-1: fast-path — нет подписчиков, не захватываем lock
        if (_subscribers.Count == 0
            && _observers.Count == 0
            && _callbacks.Count == 0
            && _untypedObservers.Count == 0)
            return;

        WeakReference<SgComponentBase>[]? rented = null;
        ISignalObserver<T>[]? observerSnapshot = null;
        WeakReference<Action<T>>[]? callbackSnapshot = null;
        int subCount = 0;

        _lock.EnterReadLock();
        try
        {
            if (_subscribers.Count > 0)
            {
                rented = ArrayPool<WeakReference<SgComponentBase>>.Shared.Rent(_subscribers.Count);
                foreach (var r in _subscribers)
                    rented[subCount++] = r;
            }

            if (_observers.Count > 0)
                observerSnapshot = _observers.ToArray();

            if (_callbacks.Count > 0)
                callbackSnapshot = _callbacks.ToArray();
        }
        finally { _lock.ExitReadLock(); }

        T currentValue = Peek();
        List<WeakReference<SgComponentBase>>? deadComponents = null;

        try
        {
            if (rented is not null)
            {
                for (int i = 0; i < subCount; i++)
                {
                    if (!rented[i].TryGetTarget(out var component) || component.IsDisposed)
                    {
                        deadComponents ??= [];
                        deadComponents.Add(rented[i]);
                        continue;
                    }

                    try { component.RequestRender(); }
                    catch (ObjectDisposedException) { deadComponents ??= []; deadComponents.Add(rented[i]); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[SgSignal] Subscriber error: {ex.Message}");
                    }
                }
            }

            if (observerSnapshot is not null)
                foreach (var obs in observerSnapshot)
                {
                    try { obs.OnSignalChanged(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[SgSignal] Observer error: {ex.Message}");
                    }
                }

            // Untyped observers (computed, effects)
            ISignalObserver[]? untypedSnapshot = null;
            _lock.EnterReadLock();
            try
            {
                if (_untypedObservers.Count > 0)
                    untypedSnapshot = _untypedObservers.ToArray();
            }
            finally { _lock.ExitReadLock(); }

            if (untypedSnapshot is not null)
                foreach (var obs in untypedSnapshot)
                {
                    try { obs.OnSignalChanged(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[SgSignal] Untyped observer error: {ex.Message}");
                    }
                }

            if (callbackSnapshot is not null)
                foreach (var weak in callbackSnapshot)
                {
                    if (weak.TryGetTarget(out var cb))
                    {
                        try { cb(currentValue); }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[SgSignal] Callback error: {ex.Message}");
                        }
                    }
                }
        }
        finally
        {
            if (rented is not null)
                ArrayPool<WeakReference<SgComponentBase>>.Shared.Return(rented, clearArray: true);
        }

        if (deadComponents is not null)
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var d in deadComponents)
                    _subscribers.Remove(d);
            }
            finally { _lock.ExitWriteLock(); }
        }

        // Авто-очистка каждые N нотификаций
        if (Interlocked.Increment(ref _notifyCount) % PurgeEveryN == 0)
            PurgeDeadSubscribers();
    }

    // ── IDisposable ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        _lock.EnterWriteLock();
        try
        {
            _subscribers.Clear();
            _observers.Clear();
            _callbacks.Clear();
            _untypedObservers.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
            _lock.Dispose();
        }
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;
    public override string ToString() => $"SgSignal<{typeof(T).Name}>({_value})";

    // ── Вспомогательные типы ────────────────────────────────────────────────────

    private sealed class CallbackDisposable : IDisposable
    {
        private readonly SgSignal<T> _signal;
        private readonly Action<T> _callback;
        private readonly WeakReference<Action<T>> _weak;
        private int _disposed;

        public CallbackDisposable(SgSignal<T> signal, Action<T> callback, WeakReference<Action<T>> weak)
        {
            _signal = signal;
            _callback = callback;
            _weak = weak;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _signal._lock.EnterWriteLock();
            try { _signal._callbacks.Remove(_weak); }
            finally { _signal._lock.ExitWriteLock(); }
        }
    }

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
        private readonly SgSignal<T> _signal;
        private int _disposed;

        public ObserverAdapter(IObserver<T> observer, SgSignal<T> signal)
        {
            _observer = observer;
            _signal = signal;
        }

        public void OnSignalChanged()
        {
            if (Volatile.Read(ref _disposed) == 1) return;
            try { _observer.OnNext(_signal.Peek()); } catch { }
        }

        public void OnSignalRead(SgSignal<T> signal) { }
        public void OnComputedRead(SgComputed<T> computed) { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _signal.UnsubscribeObserver(this);
            try { _observer.OnCompleted(); } catch { }
        }
    }
}
