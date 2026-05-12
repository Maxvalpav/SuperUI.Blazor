// SuperUI/Base/Reactive/Signal.cs
// LEGACY: оригинальный Signal<T> вынесен в отдельный файл
// Используйте SgSignal<T> для нового кода.
// Этот файл обеспечивает обратную совместимость.
// ИСПРАВЛЕНО CS0101: удалён из ComponentSignalGraph.cs, живёт только здесь.

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Legacy реактивный сигнал. Используйте <see cref="SgSignal{T}"/> для нового кода.
/// Оставлен для обратной совместимости.
/// </summary>
[Obsolete("Используйте SgSignal<T>. Signal<T> будет удалён в следующей major версии.")]
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
        // Копируем перед итерацией для thread safety
        var snapshot = _subscribers.ToList();
        foreach (var weakRef in snapshot)
        {
            if (weakRef.TryGetTarget(out var component) && !component.IsDisposed)
                SignalBatch.NotifyComponent(component);
            else
                toRemove.Add(weakRef);
        }
        foreach (var dead in toRemove)
            _subscribers.Remove(dead);
    }
}