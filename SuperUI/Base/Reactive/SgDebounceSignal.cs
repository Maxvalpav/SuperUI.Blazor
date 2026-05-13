// SuperUI/Base/Reactive/SgDebounceSignal.cs — НОВЫЙ
//
// Что это: сигнал, который откладывает уведомление подписчиков
// на заданный интервал. Полезно для поисковых полей, автосохранения и т.д.
//
// Аналог: Rx Throttle, Lodash debounce, но сигнал-ориентированный.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Сигнал с debounce: уведомления откладываются на заданный интервал.
/// Если значение меняется снова до истечения интервала — таймер сбрасывается.
/// 
/// Пример: поисковое поле, которое отправляет запрос через 300ms после последнего ввода.
/// </summary>
public sealed class SgDebounceSignal<T> : ISgSignal<T>, IDisposable
{
    private readonly SgSignal<T> _inner;
    private readonly int _delayMs;
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;

    public T Value
    {
        get => _inner.Value;
    }

    public SgDebounceSignal(T initialValue, int delayMs = 300, string? debugName = null)
    {
        if (delayMs < 0) throw new ArgumentOutOfRangeException(nameof(delayMs));

        _delayMs = delayMs;
        _inner = new SgSignal<T>(initialValue, debugName is not null ? $"Debounce({debugName})" : null);
    }

    /// <summary>
    /// Установить значение с debounce.
    /// </summary>
    public async void Set(T value)
    {
        CancelPending();

        lock (_lock)
        {
            _cts = new CancellationTokenSource();
        }

        var token = _cts.Token;

        try
        {
            await Task.Delay(_delayMs, token);

            if (!token.IsCancellationRequested)
                _inner.Set(value);
        }
        catch (TaskCanceledException) { }
    }

    /// <summary>
    /// Форсированно установить значение без задержки.
    /// </summary>
    public void SetImmediate(T value)
    {
        CancelPending();
        _inner.Set(value);
    }

    private void CancelPending()
    {
        lock (_lock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);

    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public void Dispose()
    {
        CancelPending();
        _inner.Dispose();
    }
}
