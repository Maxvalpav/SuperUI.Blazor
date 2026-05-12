// SuperUI/Base/Reactive/Signal.cs
//
// Legacy-обёртка для обратной совместимости.
// Полностью делегирует SgSignal<T>.
// [Obsolete] — migration warning в IntelliSense.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Legacy реактивный сигнал. Используйте SgSignal&lt;T&gt; для нового кода.
/// </summary>
/// <remarks>
/// Signal&lt;T&gt; будет удалён в следующей мажорной версии.
/// Замените: var s = new Signal&lt;int&gt;(0) → var s = new SgSignal&lt;int&gt;(0)
/// </remarks>
[Obsolete("Use SgSignal<T> instead. Signal<T> will be removed in a future version.", error: false)]
public sealed class Signal<T> : IDisposable
{
    private readonly SgSignal<T> _inner;

    public Signal(T initial, IEqualityComparer<T>? comparer = null)
        => _inner = new SgSignal<T>(initial, comparer);

    public T Value
    {
        get => _inner.Value;
        set => _inner.Set(value);
    }

    public T Peek() => _inner.Peek();
    public void Set(T value) => _inner.Set(value);
    public void Update(Func<T, T> updater) => _inner.Update(updater);
    public void Reset(T value) => _inner.Reset(value);
    public void ForceNotify() => _inner.ForceNotify();
    public IObservable<T> AsObservable() => _inner.AsObservable();

    internal void Subscribe(SgComponentBase component)
        => _inner.Subscribe(component);

    public void Dispose() => _inner.Dispose();

    public static implicit operator T(Signal<T> s) => s.Value;
    public override string ToString() => _inner.ToString()!;
}
