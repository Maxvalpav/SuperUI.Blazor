// SuperUI/Base/Reactive/SgSignal.cs
//
// ИСПРАВЛЕНИЯ:
//   CS0308: ISignalObserver<T> теперь корректно объявлен (два интерфейса)
//   Строки 24, 86, 91, 172 — все используют ISignalObserver<T> (generic)
//
// УЛУЧШЕНИЯ:
//   1. Reset(T) — сброс без уведомления (для тестов, инициализации)
//   2. ToString() — информативный вывод
//   3. AsObservable() — BehaviorSubject-семантика (emit current value on subscribe)
//   4. PurgeDeadSubscribers() — публичная очистка WeakRef
//   5. Корректный lock-порядок, нет deadlock
//   6. Volatile.Read для _disposedInt везде
//   7. NotifySubscribers() изолирует исключения observer'ов
//   8. ForceNotify() — принудительное уведомление без изменения значения

using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — автоматически перерисовывает компоненты при изменении Value.
/// </summary>
/// <typeparam name="T">Тип значения.</typeparam>
/// <remarks>
/// Thread safety:
///   WASM: однопоточный — lock не нужен логически, но используется для ARM-корректности.
///   Server: per-circuit, но сигналы могут обновляться из фоновых потоков → lock обязателен.
/// </remarks>
public sealed class SgSignal<T> : IDisposable
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;

    // ИСПРАВЛЕНИЕ CS0308: используем ISignalObserver<T> (generic)
    private readonly HashSet<ISignalObserver<T>> _observers = new();           // строка 24 была сломана
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    private readonly object _lock = new();
    private int _disposedInt;

    public SgSignal(T initial, IEqualityComparer<T>? comparer = null)
    {
        _value = initial;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>Реактивное чтение/запись. Чтение регистрирует подписку.</summary>
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
    /// Установить значение. Уведомляет подписчиков только при реальном изменении.
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

        if (changed) NotifySubscribers();
    }

    /// <summary>
    /// Атомарный read → compute → write. Предотвращает Lost Update при concurrent доступе.
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
    /// Сбросить значение БЕЗ уведомления подписчиков.
    /// Используется в тестах и при silent-инициализации.
    /// </summary>
    public void Reset(T value)
    {
        lock (_lock) { _value = value; }
    }

    /// <summary>
    /// Принудительно уведомить подписчиков без изменения значения.
    /// Полезно когда мутируется ссылочный тип (List, Dictionary).
    /// </summary>
    public void ForceNotify()
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        NotifySubscribers();
    }

    /// <summary>IObservable с BehaviorSubject-семантикой (emit current value on subscribe).</summary>
    public IObservable<T> AsObservable() => new SignalObservable(this);

    // ── Internal API ──────────────────────────────────────────────────────────

    internal void Subscribe(SgComponentBase component)
    {
        lock (_lock) _subscribers.Add(new WeakReference<SgComponentBase>(component));
    }

    // ИСПРАВЛЕНИЕ CS0308 строки 86, 91: используем ISignalObserver<T>
    internal void SubscribeObserver(ISignalObserver<T> observer)
    {
        lock (_lock) _observers.Add(observer);
    }

    internal void UnsubscribeObserver(ISignalObserver<T> observer)
    {
        lock (_lock) _observers.Remove(observer);
    }

    // ── Maintenance ───────────────────────────────────────────────────────────

    /// <summary>Принудительно удалить мёртвые WeakRef (GC-уборка).</summary>
    public void PurgeDeadSubscribers()
    {
        lock (_lock) _subscribers.RemoveWhere(w => !w.TryGetTarget(out _));
    }

    internal void Cleanup()
    {
        lock (_lock)
            _subscribers.RemoveWhere(wr => !wr.TryGetTarget(out var c) || c.IsDisposed);
    }

    // ── Notification ──────────────────────────────────────────────────────────

    private void NotifySubscribers()
    {
        List<WeakReference<SgComponentBase>>? snapshot = null;
        List<WeakReference<SgComponentBase>>? dead = null;
        ISignalObserver<T>[]? observerSnapshot = null;   // ИСПРАВЛЕНИЕ: typed array

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

        // Уведомляем компоненты через batch (вне lock!)
        if (snapshot is not null)
            foreach (var weakRef in snapshot)
                if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                    SignalBatch.NotifyComponent(comp);

        // Уведомляем typed observers (computed/effect) — изолируем исключения
        if (observerSnapshot is not null)
            foreach (var observer in observerSnapshot)
            {
                try { observer.OnSignalChanged(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SgSignal<{typeof(T).Name}>] Observer error: {ex.Message}");
                }
            }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        lock (_lock)
        {
            _subscribers.Clear();
            _observers.Clear();
        }
    }

    // ── Операторы ─────────────────────────────────────────────────────────────

    public static implicit operator T(SgSignal<T> signal) => signal.Value;

    public override string ToString() => $"SgSignal<{typeof(T).Name}>({_value})";

    // ── IObservable адаптер ───────────────────────────────────────────────────

    private sealed class SignalObservable : IObservable<T>
    {
        private readonly SgSignal<T> _signal;
        public SignalObservable(SgSignal<T> signal) => _signal = signal;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            var adapter = new ObserverAdapter(observer, _signal);
            _signal.SubscribeObserver(adapter);
            // BehaviorSubject-семантика: emit current value immediately
            try { observer.OnNext(_signal.Peek()); }
            catch { /* observer не должен бросать в OnNext */ }
            return adapter;
        }
    }

    // ИСПРАВЛЕНИЕ CS0308 строка 172: ISignalObserver<T> (generic)
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
            try { _observer.OnNext(_signal.Peek()); }
            catch { }
        }

        // ISignalObserver<T> — typed методы (не используются в этом адаптере)
        public void OnSignalRead(SgSignal<T> signal) { }
        public void OnComputedRead(SgComputed<T> computed) { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _signal.UnsubscribeObserver(this);
            try { _observer.OnCompleted(); }
            catch { }
        }
    }
}
