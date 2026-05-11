// SuperUI/Base/Reactive/SgSignal.cs

/// <summary>
/// Реактивный сигнал — автоматически перерисовывает компоненты при изменении.
/// Вдохновлён Solid.js signals, адаптирован под Blazor threading model.
/// </summary>
public sealed class SgSignal<T>
{
    private T _value;
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = new();
    private readonly Lock _lock = new();

    public SgSignal(T initial) => _value = initial;

    public T Value
    {
        get => _value;
        set => Set(value);
    }

    public void Set(T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(_value, newValue)) return;
        _value = newValue;
        NotifySubscribers();
    }

    public void Subscribe(SgComponentBase component)
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
                    _ = comp.RefreshAsync();
                else
                    (dead ??= new()).Add(weakRef);
            }
            if (dead is not null)
                foreach (var d in dead) _subscribers.Remove(d);
        }
    }
}
