// SuperUI/Base/Reactive/SgObserver.cs
//
// Простой IObserver<T> адаптер для подписки на IObservable<T>.
// Используется в SgInteractiveBase.Subscribe<T>().
//
// Thread-safe: колбэки вызываются из произвольного потока → InvokeAsync обязателен.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Адаптер <see cref="IObserver{T}"/> для подписки на <see cref="IObservable{T}"/>.
/// </summary>
/// <typeparam name="T">Тип элементов последовательности.</typeparam>
public sealed class SgObserver<T> : IObserver<T>
{
    private readonly Action<T>    _onNext;
    private readonly Action<Exception>? _onError;
    private readonly Action?      _onCompleted;
    private int _disposed;

    public SgObserver(
        Action<T> onNext,
        Action<Exception>? onError     = null,
        Action?            onCompleted = null)
    {
        _onNext      = onNext     ?? throw new ArgumentNullException(nameof(onNext));
        _onError     = onError;
        _onCompleted = onCompleted;
    }

    public void OnNext(T value)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _onNext(value); }
        catch (Exception ex) { OnError(ex); }
    }

    public void OnError(Exception error)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try { _onError?.Invoke(error); }
        catch { /* observer не должен бросать в OnError */ }
    }

    public void OnCompleted()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try { _onCompleted?.Invoke(); }
        catch { }
    }
}
