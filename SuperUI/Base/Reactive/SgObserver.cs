namespace SuperUI.Base.Reactive;

/// <summary>
/// Минимальный IObserver<T> без зависимости от System.Reactive.
/// Позволяет передавать лямбду туда, где нужен IObserver<T>.
/// </summary>
/// <remarks>
/// Используется в SgInteractiveBase.Subscribe() вместо source.Subscribe(lambda),
/// которое требует пакет System.Reactive и его перегрузки.
/// </remarks>
internal sealed class SgObserver<T> : IObserver<T>
{
    private readonly Action<T> _onNext;
    private readonly Action<Exception>? _onError;
    private readonly Action? _onCompleted;

    public SgObserver(
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
    {
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        _onError = onError;
        _onCompleted = onCompleted;
    }

    public void OnNext(T value) => _onNext(value);
    public void OnError(Exception error) => _onError?.Invoke(error);
    public void OnCompleted() => _onCompleted?.Invoke();
}
