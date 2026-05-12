using System.Runtime.CompilerServices;
using System.Reactive.Linq;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — автоматически перерисовывает компоненты при изменении Value.
///
/// Thread safety:
/// - WASM: однопоточный, lock — минимальный overhead.
/// - Server: каждый circuit — свой поток. lock(_lock) защищает _value, _subscribers, _observers.
///
/// ИСПРАВЛЕНО:
/// 1. Update() — атомарный read+compute+write под lock (устранён Lost Update).
/// 2. NotifySubscribers — уведомление вне lock (предотвращение deadlock).
/// 3. AsObservable() — интеграция с IObservable[T].
/// </summary>
public sealed class SgSignal<T> : IDisposable
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    private readonly object _lock = new();
    private readonly HashSet<ISignalObserver> _observers = new();
    private int _disposedInt;

    public SgSignal(T initial, IEqualityComparer<T>? comparer = null)
    {
        _value = initial;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Читает значение. При чтении внутри render-scope — автоматически подписывает компонент.
    /// </summary>
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

    /// <summary>Прочитать значение БЕЗ реактивной подписки.</summary>
    public T Peek()
    {
        lock (_lock) return _value;
    }

    /// <summary>
    /// Установить новое значение. Уведомляет подписчиков только при реальном изменении.
    /// </summary>
    public void Set(T newValue)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;

        bool changed;
        lock (_lock)
        {
            changed = !_comparer.Equals(_value, newValue);
            if (changed) _value = newValue;
        }

        // Уведомление — вне lock (предотвращаем deadlock если subscriber вызовет Set)
        if (changed) NotifySubscribers();
    }

    /// <summary>
    /// Обновить значение через функцию (атомарный read+compute+write).
    ///
    /// ИСПРАВЛЕНО: весь цикл read+compute+write под lock → нет Lost Update.
    /// Ограничение: updater не должен выбрасывать исключения — иначе lock будет
    /// держаться до завершения исключения (это корректно, но документируйте).
    /// </summary>
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

    /// <summary>
    /// Представить сигнал как <see cref="IObservable{T}"/> для интеграции с Rx.NET.
    /// Подписка активна пока IDisposable не будет Dispose'd.
    ///
    /// НОВОЕ: позволяет использовать LINQ/Rx операторы (Throttle, DistinctUntilChanged и др.).
    /// Не требует Rx.NET — работает с любым IObserver[T].
    /// </summary>
    public IObservable<T> AsObservable() => new SignalObservable(this);

    internal void Subscribe(SgComponentBase component)
    {
        lock (_lock) _subscribers.Add(new WeakReference<SgComponentBase>(component));
    }

    internal void SubscribeObserver(ISignalObserver observer)
    {
        lock (_lock) _observers.Add(observer);
    }

    internal void UnsubscribeObserver(ISignalObserver observer)
    {
        lock (_lock) _observers.Remove(observer);
    }

    /// <summary>Принудительно удалить мёртвые WeakRef (вызывайте при GC давлении).</summary>
    public void PurgeDeadSubscribers()
    {
        lock (_lock)
            _subscribers.RemoveWhere(w => !w.TryGetTarget(out _));
    }

    internal void Cleanup()
    {
        lock (_lock)
            _subscribers.RemoveWhere(wr => !wr.TryGetTarget(out var c) || c.IsDisposed);
    }

    private void NotifySubscribers()
    {
        List<WeakReference<SgComponentBase>>? snapshot = null;
        List<WeakReference<SgComponentBase>>? dead = null;
        ISignalObserver[]? observerSnapshot = null;

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
        {
            foreach (var weakRef in snapshot)
                if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                    SignalBatch.NotifyComponent(comp);
        }

        if (observerSnapshot is not null)
        {
            foreach (var observer in observerSnapshot)
            {
                try { observer.OnSignalChanged(); }
                catch { /* наблюдатель не должен бросать */ }
            }
        }
    }

    /// <summary>Освободить всех подписчиков и наблюдателей.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        lock (_lock)
        {
            _subscribers.Clear();
            _observers.Clear();
        }
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;
    public override string ToString() => _value?.ToString() ?? "null";

    // ── IObservable адаптер ─────────────────────────────────────────────────

    private sealed class SignalObservable : IObservable<T>
    {
        private readonly SgSignal<T> _signal;
        public SignalObservable(SgSignal<T> signal) => _signal = signal;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            var adapter = new ObserverAdapter(observer, _signal);
            _signal.SubscribeObserver(adapter);
            // Отправляем текущее значение сразу (BehaviorSubject-семантика)
            try { observer.OnNext(_signal.Peek()); } catch { }
            return adapter;
        }
    }

    private sealed class ObserverAdapter : ISignalObserver, IDisposable
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
            try { _observer.OnNext(_signal.Peek()); }
            catch { /* observer не должен бросать */ }
        }

        // Не используется в контексте сигнала-наблюдателя
        public void OnSignalRead<TRead>(SgSignal<TRead> signal) { }
        public void OnComputedRead<TRead>(SgComputed<TRead> computed) { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _signal.UnsubscribeObserver(this);
            try { _observer.OnCompleted(); } catch { }
        }
    }
}
