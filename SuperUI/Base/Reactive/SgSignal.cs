// SuperUI/Base/Reactive/SgSignal.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ PERF-4: ArrayPool<WeakReference> для snapshot — zero-allocation уведомление
// ✅ BUG-10 prevention: SubscribeObserver проверяет дубли (HashSet уже это делает)
// ✅ НОВОЕ: Subscribe(Action<T>) — подписка без компонента (для внешних наблюдателей)
// ✅ НОВОЕ: Derived<TResult>(Func<T,TResult>) — создать производный сигнал

using System.Buffers;
using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

public sealed class SgSignal<T> : IDisposable, ISignalSubscribable
{
    private readonly HashSet<ISignalObserver> _untypedObservers = new();

    void ISignalSubscribable.SubscribeObserverUntyped(ISignalObserver observer)
    {
        lock (_lock) _untypedObservers.Add(observer);
    }
    private T _value;
    private readonly IEqualityComparer<T> _comparer;
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    private readonly HashSet<ISignalObserver<T>> _observers = new();
    private readonly List<Action<T>> _callbacks = new();
    private readonly object _lock = new();
    private int _disposedInt;

    public SgSignal(T initial, IEqualityComparer<T>? comparer = null)
    {
        _value = initial;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { SignalTracker.Track(this); return _value; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(value);
    }

    public T Peek() { lock (_lock) return _value; }

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

    public void Update(Func<T, T> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);
        if (Volatile.Read(ref _disposedInt) == 1) return;
        T newValue;
        bool changed;
        lock (_lock)
        {
            newValue = updater(_value);
            changed = !_comparer.Equals(_value, newValue);
            if (changed) _value = newValue;
        }
        if (changed) NotifySubscribers();
    }

    public void Reset(T value) { lock (_lock) { _value = value; } }

    public void ForceNotify()
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        NotifySubscribers();
    }

    // НОВОЕ: Subscribe с callback (без компонента)
    public IDisposable Subscribe(Action<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        lock (_lock) _callbacks.Add(callback);
        return new CallbackDisposable(this, callback);
    }

    // НОВОЕ: Derived signal
    public SgSignal<TResult> Derived<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var derived = new SgSignal<TResult>(selector(Peek()));
        Subscribe(value => derived.Set(selector(value)));
        return derived;
    }

    public IObservable<T> AsObservable() => new SignalObservable(this);

    // ── Internal API ────────────────────────────────────────────────────────────
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

    public void PurgeDeadSubscribers()
    {
        lock (_lock) _subscribers.RemoveWhere(w => !w.TryGetTarget(out _));
    }

    internal void Cleanup()
    {
        lock (_lock)
            _subscribers.RemoveWhere(wr => !wr.TryGetTarget(out var c) || c.IsDisposed);
    }

    // ── PERF-4: ArrayPool snapshot ──────────────────────────────────────────────
    private void NotifySubscribers()
    {
        WeakReference<SgComponentBase>[]? rented = null;
        ISignalObserver<T>[]? observerSnapshot = null;
        Action<T>[]? callbackSnapshot = null;
        int subCount = 0;

        lock (_lock)
        {
            if (_subscribers.Count == 0 && _observers.Count == 0 && _callbacks.Count == 0)
                return;

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

        T currentValue = Peek();
        List<WeakReference<SgComponentBase>>? dead = null;

        try
        {
            if (rented is not null)
            {
                for (int i = 0; i < subCount; i++)
                {
                    var wr = rented[i];
                    if (wr.TryGetTarget(out var comp) && !comp.IsDisposed)
                        SignalBatch.NotifyComponent(comp);
                    else
                        (dead ??= new()).Add(wr);
                }
            }

            if (observerSnapshot is not null)
                foreach (var obs in observerSnapshot)
                {
                    try { obs.OnSignalChanged(); }
                    catch (Exception ex)
                    { System.Diagnostics.Debug.WriteLine($"[SgSignal<{typeof(T).Name}>] Observer error: {ex.Message}"); }
                }

            // Untyped observers (computed, effects)
            ISignalObserver[]? untypedSnapshot = null;
            lock (_lock)
            {
                if (_untypedObservers.Count > 0)
                    untypedSnapshot = _untypedObservers.ToArray();
            }
            if (untypedSnapshot is not null)
                foreach (var obs in untypedSnapshot)
                {
                    try { obs.OnSignalChanged(); }
                    catch (Exception ex)
                    { System.Diagnostics.Debug.WriteLine($"[SgSignal] Untyped observer error: {ex.Message}"); }
                }

            if (callbackSnapshot is not null)
                foreach (var cb in callbackSnapshot)
                {
                    try { cb(currentValue); }
                    catch (Exception ex)
                    { System.Diagnostics.Debug.WriteLine($"[SgSignal] Callback error: {ex.Message}"); }
                }
        }
        finally
        {
            if (rented is not null)
                ArrayPool<WeakReference<SgComponentBase>>.Shared.Return(rented, clearArray: true);
        }

        if (dead is not null)
        {
            lock (_lock)
                foreach (var d in dead)
                    _subscribers.Remove(d);
        }
    }

    // ── IDisposable ─────────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        lock (_lock)
        {
            _subscribers.Clear();
            _observers.Clear();
            _callbacks.Clear();
        }
    }

    // ── Операторы ───────────────────────────────────────────────────────────────
    public static implicit operator T(SgSignal<T> signal) => signal.Value;
    public override string ToString() => $"SgSignal<{typeof(T).Name}>({_value})";

    // ── Вспомогательные типы ────────────────────────────────────────────────────
    private sealed class CallbackDisposable : IDisposable
    {
        private readonly SgSignal<T> _signal;
        private readonly Action<T> _callback;
        private int _disposed;

        public CallbackDisposable(SgSignal<T> signal, Action<T> callback)
        { _signal = signal; _callback = callback; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            lock (_signal._lock) _signal._callbacks.Remove(_callback);
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
            try { observer.OnNext(_signal.Peek()); }
            catch { }
            return adapter;
        }
    }

    private sealed class ObserverAdapter : ISignalObserver<T>, IDisposable
    {
        private readonly IObserver<T> _observer;
        private readonly SgSignal<T> _signal;
        private int _disposed;

        public ObserverAdapter(IObserver<T> observer, SgSignal<T> signal)
        { _observer = observer; _signal = signal; }

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
