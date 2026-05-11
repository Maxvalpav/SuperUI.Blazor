// SuperUI/Base/Reactive/SgObserver.cs
namespace SuperUI.Base.Reactive;

/// <summary>
/// Минимальная реализация IObserver<T> для Subscribe паттерна.
/// Избегает зависимости от System.Reactive.
/// </summary>
internal sealed class SgObserver<T> : IObserver<T>
{
    private readonly Action<T> _onNext;
    private readonly Action<Exception>? _onError;
    private readonly Action? _onCompleted;

    public SgObserver(Action<T> onNext, Action<Exception>? onError = null, Action? onCompleted = null)
    {
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        _onError = onError;
        _onCompleted = onCompleted;
    }

    public void OnNext(T value) => _onNext(value);
    public void OnError(Exception error) => _onError?.Invoke(error);
    public void OnCompleted() => _onCompleted?.Invoke();
}
