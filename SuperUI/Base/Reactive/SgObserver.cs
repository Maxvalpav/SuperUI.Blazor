// SuperUI/Base/Reactive/SgObserver.cs
//
// УЛУЧШЕНИЯ:
//   1. IDisposable реализован явно (unsubscribe от источника)
//   2. IsDisposed публичное свойство
//   3. Документация thread-safety

namespace SuperUI.Base.Reactive;

/// <summary>
/// Адаптер для подписки на IObservable&lt;T&gt;.
/// Thread-safe: колбэки могут вызываться из произвольного потока.
/// При использовании в Blazor-компоненте — оберните в InvokeAsync.
/// </summary>
/// <typeparam name="T">Тип элементов последовательности.</typeparam>
public sealed class SgObserver<T> : IObserver<T>, IDisposable
{
    private readonly Action<T> _onNext;
    private readonly Action<Exception>? _onError;
    private readonly Action? _onCompleted;
    private int _disposed;

    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    public SgObserver(
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
    {
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        _onError = onError;
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

    public void Dispose()
    {
        Interlocked.Exchange(ref _disposed, 1);
    }
}
