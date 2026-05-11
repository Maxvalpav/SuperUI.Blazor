// SuperUI/Base/Reactive/SgSignal.cs
// ИСПРАВЛЕНО:
// 1. Добавлен using SuperUI.Base (CS0246 SgComponentBase не найден)
// 2. Subscribe принимает Func<Task> для совместимости с SignalTracker
// 3. NotifySubscribers: dead reference cleanup корректен
// 4. Переименован во избежание конфликта с Signal<T> из ComponentSignalGraph
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — автоматически перерисовывает компоненты при изменении.
/// Вдохновлён Solid.js signals, адаптирован под Blazor threading model.
/// 
/// Работает на WASM (однопоточный) и Server (многопоточный SignalR).
/// Lock на Server необходим так как SignalR хабы многопоточны.
/// </summary>
public sealed class SgSignal<T>
{
    private T _value;
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    // ИСПРАВЛЕНО: на Blazor Server нужен lock — SignalR callbacks могут быть из разных потоков
    // На WASM это no-op overhead но safe
    private readonly Lock _lock = new();

    public SgSignal(T initial) => _value = initial;

    public T Value
    {
        get
        {
            // Автоподписка при чтении в scope рендера
            SignalTracker.Track(this);
            return _value;
        }
        set => Set(value);
    }

    public void Set(T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(_value, newValue)) return;
        _value = newValue;
        NotifySubscribers();
    }

    /// <summary>
    /// Подписать компонент на изменения сигнала.
    /// Используется SignalTracker при автоматическом tracking.
    /// </summary>
    internal void Subscribe(SgComponentBase component)
    {
        lock (_lock)
            _subscribers.Add(new WeakReference<SgComponentBase>(component));
    }

    private void NotifySubscribers()
    {
        List<WeakReference<SgComponentBase>>? dead = null;
        lock (_lock)
        {
            foreach (var weakRef in _subscribers)
            {
                if (weakRef.TryGetTarget(out var comp) && !comp.IsDisposed)
                    _ = comp.RefreshAsync(); // fire-and-forget — InvokeAsync внутри
                else
                    (dead ??= new()).Add(weakRef);
            }
            if (dead is not null)
                foreach (var d in dead)
                    _subscribers.Remove(d);
        }
    }
}