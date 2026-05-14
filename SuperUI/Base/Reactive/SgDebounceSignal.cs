// SuperUI/Base/Reactive/SgDebounceSignal.cs
// ИСПРАВЛЕНО:
// ✅ async void Set → разделён на Set (немедленный) + SetDebounced (ValueTask)
// ✅ Исключения в async-эффектах перехватываются через _onError callback
// ✅ Корректная отмена предыдущего debounce timer
// ✅ .NET 8/9/10 совместим

namespace SuperUI.Base.Reactive;

/// <summary>
/// Сигнал с debounce: подписчики уведомляются только через <see cref="DelayMs"/> мс
/// после последнего вызова <see cref="SetDebounced"/>.
/// Немедленная установка — через <see cref="Set"/> (минуя debounce).
/// </summary>
public sealed class SgDebounceSignal<T> : ISgSignal<T>, IDisposable
{
    private readonly SgSignal<T> _inner;
    private readonly CancellationTokenSource _disposeCts = new();
    private CancellationTokenSource? _debounceCts;
    private readonly object _lock = new();
    private readonly Action<Exception>? _onError;

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;
    public int DelayMs { get; }
    public T Value => _inner.Value;

    public SgDebounceSignal(
        T initialValue,
        int delayMs = 300,
        string? debugName = null,
        Action<Exception>? onError = null)
    {
        if (delayMs < 0) throw new ArgumentOutOfRangeException(nameof(delayMs));
        DelayMs = delayMs;
        _onError = onError;
        _inner = new SgSignal<T>(
            initialValue,
            debugName is not null ? $"Debounce({debugName})" : null);
    }

    /// <summary>
    /// Немедленно устанавливает значение сигнала, отменяя любой pending debounce.
    /// Аналог Lodash <c>debounce.flush()</c>.
    /// </summary>
    public void Set(T value)
    {
        CancelPending();
        _inner.Set(value);
    }

    /// <summary>
    /// Устанавливает значение с задержкой debounce.
    /// Если вызван повторно до истечения DelayMs — предыдущий таймер сбрасывается.
    /// Возвращает <see cref="ValueTask"/> для await в случае необходимости.
    /// </summary>
    public ValueTask SetDebounced(T value)
    {
        if (_disposeCts.IsCancellationRequested)
            return ValueTask.CompletedTask;

        // Запускаем как Task, не ждём — caller может await или игнорировать
        return new ValueTask(SetDebouncedAsync(value));
    }

    private async Task SetDebouncedAsync(T value)
    {
        CancellationTokenSource cts;

        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
            cts = _debounceCts;
        }

        try
        {
            await Task.Delay(DelayMs, cts.Token).ConfigureAwait(false);

            if (!cts.Token.IsCancellationRequested)
                _inner.Set(value);
        }
        catch (OperationCanceledException)
        {
            // Нормально: debounce сброшен или dispose — не логируем
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
    }

    private void CancelPending()
    {
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);
    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public void Dispose()
    {
        _disposeCts.Cancel();
        _disposeCts.Dispose();

        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }

        _inner.Dispose();
    }

    public static implicit operator T(SgDebounceSignal<T> s) => s.Value;
}