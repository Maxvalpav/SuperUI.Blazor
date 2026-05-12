// SuperUI/Base/Reactive/SgSignal.cs
// ИСПРАВЛЕНО:
// 1. Добавлен IDisposable — Dispose() очищает всех подписчиков
// 2. Peek() — чтение без реактивной подписки
// 3. PurgeDeadSubscribers() — принудительная очистка мёртвых WeakRef
// 4. Update() — не атомарен на Server, добавлен комментарий
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
    private readonly HashSet<ISignalObserver> _observers = new();
    private volatile bool _disposed;

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
    public T Peek() => _value;

    /// <summary>
    /// Установить новое значение. Уведомляет подписчиков только при реальном изменении.
    /// </summary>
    public void Set(T newValue)
    {
        if (_disposed) return;
        if (_comparer.Equals(_value, newValue)) return;
        _value = newValue;
        NotifySubscribers();
    }

    /// <summary>
    /// Обновить значение через функцию.
    /// ⚠️ Не атомарен относительно concurrent Set() на Blazor Server.
    /// Для атомарных операций используйте SgStore.Dispatch().
    /// </summary>
    public void Update(Func<T, T> updater) => Set(updater(_value));

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
        {
            _subscribers.RemoveWhere(w => !w.TryGetTarget(out _));
        }
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

        if (snapshot is not null)
        {
            foreach (var weakRef in snapshot)
                if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                    _ = comp.RefreshAsync();
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
        _disposed = true;
        lock (_lock)
        {
            _subscribers.Clear();
            _observers.Clear();
        }
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;
    public override string ToString() => _value?.ToString() ?? "null";
}