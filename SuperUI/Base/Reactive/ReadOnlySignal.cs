// SuperUI/Base/Reactive/ReadOnlySignal.cs
// NEW: Read-only обёртка над SgSignal<T>
// Аналог: MobX ReadOnlyObservable, Vue computed readonly
// Позволяет публично предоставлять сигнал без права его изменять

namespace SuperUI.Base.Reactive;

/// <summary>
/// Read-only обёртка над <see cref="SgSignal{T}"/>.
/// Компонент может читать и подписываться, но не изменять значение.
/// </summary>
/// <typeparam name="T">Тип значения.</typeparam>
public sealed class ReadOnlySignal<T> : IDisposable
{
    private readonly SgSignal<T> _source;

    public ReadOnlySignal(SgSignal<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>Текущее значение (с отслеживанием зависимостей).</summary>
    public T Value => _source.Value;

    /// <summary>Текущее значение без отслеживания зависимостей.</summary>
    public T Peek() => _source.Peek();

    /// <summary>Подписаться на изменения.</summary>
    public IDisposable Subscribe(Action<T> callback) => _source.Subscribe(callback);

    /// <summary>Как IObservable&lt;T&gt;.</summary>
    public IObservable<T> AsObservable() => _source.AsObservable();

    /// <summary>Создать производный read-only сигнал.</summary>
    public ReadOnlySignal<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var derived = new SgSignal<TResult>(selector(_source.Peek()));
        _source.Subscribe(v => derived.Set(selector(v)));
        return new ReadOnlySignal<TResult>(derived);
    }

    public static implicit operator T(ReadOnlySignal<T> signal) => signal.Value;
    public override string ToString() => $"ReadOnlySignal<{typeof(T).Name}>({_source.Peek()})";

    public void Dispose() { /* source управляется снаружу */ }
}

// Extension для SgSignal<T>
public static class SgSignalReadOnlyExtensions
{
    /// <summary>Получить read-only обёртку над сигналом.</summary>
    public static ReadOnlySignal<T> AsReadOnly<T>(this SgSignal<T> signal)
        => new(signal);
}
