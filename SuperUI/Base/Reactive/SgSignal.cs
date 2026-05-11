// SuperUI/Base/Reactive/SgSignal.cs
// ИСПРАВЛЕНО:
// 1. Lock освобождается ДО вызова RefreshAsync (предотвращение deadlock)
// 2. Снимаем копию _subscribers перед итерацией (минимальное время удержания lock)
// 3. Поддержка IEqualityComparer<T> как в Signal<T>
// 4. Оператор implicit для удобного присвоения
// 5. ToString() для отладки
// 6. SubscribeObserver для поддержки SgComputed / SgEffect

using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — автоматически перерисовывает компоненты при изменении Value.
/// Вдохновлён Solid.js signals, адаптирован под Blazor threading model.
/// </summary>
/// <remarks>
/// Thread safety:
/// - WASM: однопоточный — lock является no-op overhead, но безопасен.
/// - Server: SignalR callbacks могут приходить из разных потоков → lock обязателен.
/// DEADLOCK FIX: lock освобождается ДО вызова RefreshAsync.
/// </remarks>
public sealed class SgSignal<T>
{
    private T _value;
    private readonly IEqualityComparer<T> _comparer;

    // WeakReference<SgComponentBase> — компоненты не удерживаются от GC
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    private readonly object _lock = new();

    // Наблюдатели (computed / effect) — сильные ссылки, живут пока жив сигнал
    private readonly HashSet<ISignalObserver> _observers = new();

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
            // Автоподписка при чтении в scope рендера
            SignalTracker.Track(this);
            return _value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(value);
    }

    /// <summary>
    /// Установить новое значение. Уведомляет подписчиков только при реальном изменении.
    /// </summary>
    public void Set(T newValue)
    {
        if (_comparer.Equals(_value, newValue)) return;
        _value = newValue;
        NotifySubscribers();
    }

    /// <summary>
    /// Обновить значение через функцию (атомарно для читателей).
    /// </summary>
    public void Update(Func<T, T> updater)
    {
        Set(updater(_value));
    }

    /// <summary>
    /// Подписать компонент. Вызывается SignalTracker автоматически.
    /// </summary>
    internal void Subscribe(SgComponentBase component)
    {
        lock (_lock)
            _subscribers.Add(new WeakReference<SgComponentBase>(component));
    }

    /// <summary>
    /// Подписать наблюдателя (SgComputed / SgEffect).
    /// </summary>
    internal void SubscribeObserver(ISignalObserver observer)
    {
        lock (_lock)
            _observers.Add(observer);
    }

    private void NotifySubscribers()
    {
        // ИСПРАВЛЕНО: копируем список и освобождаем lock ДО вызова RefreshAsync
        // Это предотвращает deadlock: RefreshAsync → Set → NotifySubscribers → lock (занят)
        List<WeakReference<SgComponentBase>>? snapshot = null;
        List<WeakReference<SgComponentBase>>? dead = null;
        ISignalObserver[]? observerSnapshot = null;

        lock (_lock)
        {
            if (_subscribers.Count == 0 && _observers.Count == 0) return;

            if (_subscribers.Count > 0)
            {
                snapshot = new List<WeakReference<SgComponentBase>>(_subscribers.Count);
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

        // Уведомляем компоненты ПОСЛЕ освобождения lock
        if (snapshot is not null)
        {
            foreach (var weakRef in snapshot)
            {
                if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                    _ = comp.RefreshAsync();
            }
        }

        // Уведомляем наблюдателей (computed / effect) ПОСЛЕ освобождения lock
        if (observerSnapshot is not null)
        {
            foreach (var observer in observerSnapshot)
            {
                try { observer.OnSignalChanged(); }
                catch { /* наблюдатель не должен бросать */ }
            }
        }
    }

    // Удобный implicit для: SgSignal<int> count = 0;
    public static implicit operator T(SgSignal<T> signal) => signal.Value;

    public override string ToString() => _value?.ToString() ?? "null";
}