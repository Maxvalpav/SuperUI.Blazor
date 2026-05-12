using System.Buffers;
using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — автоматически перерисовывает компоненты при изменении Value.
/// </summary>
/// <typeparam name="T">Тип значения.</typeparam>
/// <remarks>
/// Thread safety:
/// WASM: однопоточный — lock не нужен логически, но используется для ARM-корректности.
/// Server: per-circuit, но сигналы могут обновляться из фоновых потоков → lock обязателен.
/// </remarks>
public sealed class SgSignal<T> : IDisposable
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;

    // ИСПРАВЛЕНИЕ C7: кэш EffectSignalBridge чтобы не создавать новый на каждый Track
    private readonly ConditionalWeakTable<ISignalObserver, EffectSignalBridge<T>> _bridgeCache = new();

    private readonly HashSet<WeakReference<SgComponentBase>> _observers = new();
    private readonly HashSet<ISignalObserver<T>> _subscribers = new();
    private readonly HashSet<ISignalObserver> _genericObservers = new();
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

    /// <summary>Установить значение. Уведомляет подписчиков только при реальном изменении.</summary>
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

    /// <summary>Атомарный read → compute → write.</summary>
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

    /// <summary>Сбросить значение БЕЗ уведомления подписчиков.</summary>
    public void Reset(T value)
    {
        lock (_lock) { _value = value; }
    }

    /// <summary>Принудительно уведомить подписчиков без изменения значения.</summary>
    public void ForceNotify()
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        NotifySubscribers();
    }

    /// <summary>IObservable с BehaviorSubject-семантикой.</summary>
    public IObservable<T> AsObservable() => new SignalObservable<T>(this);

    // ── Internal API ──────────────────────────────────────────────────────

    internal void Subscribe(SgComponentBase component)
    {
        lock (_lock) _observers.Add(new WeakReference<SgComponentBase>(component));
    }

    internal void SubscribeObserver(ISignalObserver<T> observer)
    {
        lock (_lock) _subscribers.Add(observer);
    }

    internal void UnsubscribeObserver(ISignalObserver<T> observer)
    {
        lock (_lock) _subscribers.Remove(observer);
    }

    /// <summary>Для EffectObserver (non-generic) через мост.</summary>
    internal void SubscribeGenericObserver(ISignalObserver observer)
    {
        lock (_lock) _genericObservers.Add(observer);
    }

    internal void UnsubscribeGenericObserver(ISignalObserver observer)
    {
        lock (_lock) _genericObservers.Remove(observer);
    }

    // ── C7 FIX: кэшированный мост ────────────────────────────────────────

    /// <summary>
    /// Получить или создать EffectSignalBridge для observer.
    /// ИСПРАВЛЕНИЕ: кэширует мост через ConditionalWeakTable.
    /// </summary>
    internal EffectSignalBridge<T> GetOrCreateBridge(ISignalObserver observer)
    {
        // ConditionalWeakTable потокобезопасен
        if (!_bridgeCache.TryGetValue(observer, out var bridge))
        {
            bridge = new EffectSignalBridge<T>(this, observer);
            _bridgeCache.AddOrUpdate(observer, bridge);
        }
        return bridge;
    }

    // ── Maintenance ───────────────────────────────────────────────────────

    /// <summary>Принудительно удалить мёртвые WeakRef.</summary>
    public void PurgeDeadSubscribers()
    {
        lock (_lock) _observers.RemoveWhere(w => !w.TryGetTarget(out _));
    }

    internal void Cleanup()
    {
        lock (_lock)
            _observers.RemoveWhere(wr => !wr.TryGetTarget(out var c) || c.IsDisposed);
    }

    // ── C2 FIX: NotifySubscribers без deadlock ───────────────────────────

    private void NotifySubscribers()
    {
        // П1: Используем ArrayPool для уменьшения аллокаций
        WeakReference<SgComponentBase>[]? observerSnapshot = null;
        ISignalObserver<T>[]? subscriberSnapshot = null;
        ISignalObserver[]? genericSnapshot = null;
        int observerCount = 0, subscriberCount = 0, genericCount = 0;
        List<WeakReference<SgComponentBase>>? dead = null;

        lock (_lock)
        {
            if (_observers.Count == 0 && _subscribers.Count == 0 && _genericObservers.Count == 0)
                return;

            // Снимаем снепшоты ПОД lock для консистентности
            if (_observers.Count > 0)
            {
                observerSnapshot = ArrayPool<WeakReference<SgComponentBase>>.Shared.Rent(_observers.Count);
                foreach (var weakRef in _observers)
                {
                    if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                        observerSnapshot[observerCount++] = weakRef;
                    else
                        (dead ??= new()).Add(weakRef);
                }
                if (dead is not null)
                    foreach (var d in dead) _observers.Remove(d);
            }

            if (_subscribers.Count > 0)
            {
                subscriberSnapshot = ArrayPool<ISignalObserver<T>>.Shared.Rent(_subscribers.Count);
                foreach (var sub in _subscribers)
                    subscriberSnapshot[subscriberCount++] = sub;
            }

            if (_genericObservers.Count > 0)
            {
                genericSnapshot = ArrayPool<ISignalObserver>.Shared.Rent(_genericObservers.Count);
                foreach (var obs in _genericObservers)
                    genericSnapshot[genericCount++] = obs;
            }
        }

        // 🔑 ВАЖНО: уведомления ВНЕ lock — предотвращает deadlock (C2)

        // Уведомляем компоненты через batch
        if (observerSnapshot is not null)
        {
            for (int i = 0; i < observerCount; i++)
            {
                if (observerSnapshot[i].TryGetTarget(out var comp) && !comp.IsDisposed)
                    SignalBatch.NotifyComponent(comp);
            }
            ArrayPool<WeakReference<SgComponentBase>>.Shared.Return(observerSnapshot);
        }

        // Уведомляем typed observers (computed/effect) — изолируем исключения
        if (subscriberSnapshot is not null)
        {
            for (int i = 0; i < subscriberCount; i++)
            {
                try { subscriberSnapshot[i].OnSignalChanged(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SgSignal<{typeof(T).Name}>] Observer error: {ex.Message}"); }
            }
            ArrayPool<ISignalObserver<T>>.Shared.Return(subscriberSnapshot);
        }

        // Уведомляем generic observers (EffectObserver через мост)
        if (genericSnapshot is not null)
        {
            for (int i = 0; i < genericCount; i++)
            {
                try { genericSnapshot[i].OnSignalChanged(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SgSignal<{typeof(T).Name}>] GenericObserver error: {ex.Message}"); }
            }
            ArrayPool<ISignalObserver>.Shared.Return(genericSnapshot);
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;

        lock (_lock)
        {
            _observers.Clear();
            _subscribers.Clear();
            _genericObservers.Clear();
        }
    }

    // ── Операторы ─────────────────────────────────────────────────────────

    public static implicit operator T(SgSignal<T> signal) => signal.Value;

    public override string ToString() => $"SgSignal<{typeof(T).Name}>({_value})";

    // ── IObservable адаптер ───────────────────────────────────────────────

    private sealed class SignalObservable<TObs> : IObservable<TObs>
    {
        private readonly SgSignal<TObs> _signal;

        public SignalObservable(SgSignal<TObs> signal) => _signal = signal;

        public IDisposable Subscribe(IObserver<TObs> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            var adapter = new ObserverAdapter<TObs>(observer, _signal);
            _signal.SubscribeObserver(adapter);
            try { observer.OnNext(_signal.Peek()); } catch { }
            return adapter;
        }
    }

    private sealed class ObserverAdapter<TObs> : ISignalObserver<TObs>, IDisposable
    {
        private readonly IObserver<TObs> _observer;
        private readonly SgSignal<TObs> _signal;
        private int _disposed;

        public ObserverAdapter(IObserver<TObs> observer, SgSignal<TObs> signal)
        {
            _observer = observer;
            _signal = signal;
        }

        public void OnSignalChanged()
        {
            if (Volatile.Read(ref _disposed) == 1) return;
            try { _observer.OnNext(_signal.Peek()); } catch { }
        }

        public void OnSignalRead(SgSignal<TObs> signal) { }

        public void OnComputedRead(SgComputed<TObs> computed) { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _signal.UnsubscribeObserver(this);
            try { _observer.OnCompleted(); } catch { }
        }
    }
}

// ── EffectSignalBridge (вынесен из SignalTracker для переиспользования) ──

/// <summary>
/// Мост для подписки EffectObserver (non-generic) на typed SgSignal&lt;T&gt;.
/// ИСПРАВЛЕНИЕ C7: кэшируется в SgSignal для предотвращения утечек подписок.
/// </summary>
internal sealed class EffectSignalBridge<T> : ISignalObserver<T>, IDisposable
{
    private readonly SgSignal<T> _signal;
    private readonly ISignalObserver _effect;
    private int _disposed;

    public EffectSignalBridge(SgSignal<T> signal, ISignalObserver effect)
    {
        _signal = signal;
        _effect = effect;
    }

    public void OnSignalChanged()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _effect.OnSignalChanged();
    }

    public void OnSignalRead(SgSignal<T> signal) { }

    public void OnComputedRead(SgComputed<T> computed) { }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _signal.UnsubscribeObserver(this);
        _signal.UnsubscribeGenericObserver(_effect);
    }
}
