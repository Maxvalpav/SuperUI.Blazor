using SuperUI.Base;
using SuperUI.Base.Reactive;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный Signal для умного трекинга зависимостей.
/// Компоненты автоматически подписываются при чтении Value и получают уведомление при изменении.
/// </summary>
public sealed class Signal<T>
{
    private T _value;
    private readonly HashSet<WeakReference<SgComponentBase>> _subscribers = [];
    private readonly IEqualityComparer<T> _comparer;

    public Signal(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        _value = initialValue;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public T Value
    {
        get
        {
            SignalTracker.Track(this);
            return _value;
        }
    }

    public void Set(T newValue)
    {
        if (_comparer.Equals(_value, newValue)) return;
        _value = newValue;
        NotifySubscribers();
    }

    internal void Subscribe(SgComponentBase component)
        => _subscribers.Add(new WeakReference<SgComponentBase>(component));

    private void NotifySubscribers()
    {
        var toRemove = new List<WeakReference<SgComponentBase>>();
        foreach (var weakRef in _subscribers)
        {
            if (weakRef.TryGetTarget(out var component) && !component.IsDisposed)
                _ = component.RefreshAsync(); // fire-and-forget
            else
                toRemove.Add(weakRef);
        }
        foreach (var dead in toRemove) _subscribers.Remove(dead);
    }
}