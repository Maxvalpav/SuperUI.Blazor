// SuperUI/Base/Reactive/Signal.cs
//
// Legacy-обёртка для обратной совместимости.
// Используйте SgSignal<T> для нового кода.
// Signal<T> делегирует все операции SgSignal<T>.
//
// [Obsolete] помечен для migration warning в IntelliSense.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Legacy реактивный сигнал. Используйте <see cref="SgSignal{T}"/> для нового кода.
/// </summary>
[Obsolete("Use SgSignal<T> instead. Signal<T> will be removed in a future version.")]
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

    public T Peek()  => _inner.Peek();
    public void Set(T value) => _inner.Set(value);
    public void Update(Func<T, T> updater) => _inner.Update(updater);
    public IObservable<T> AsObservable() => _inner.AsObservable();

    internal void Subscribe(SgComponentBase component) => _inner.Subscribe(component);

    public void Dispose() => _inner.Dispose();

    public static implicit operator T(Signal<T> s) => s.Value;
    public override string? ToString() => _inner.ToString();
}
