// SuperUI/Base/Utilities/SgDebouncer.cs
// Утилиты debounce и throttle для async-операций.
// Заменяет ручной CTS-debounce в SgButton, SgTooltip, SgDataGrid, SgDropdown.

namespace SuperUI.Base.Utilities;

/// <summary>
/// Debouncer — откладывает выполнение действия до истечения паузы.
/// </summary>
/// <remarks>
/// <para>Использование:</para>
/// <code>
/// private readonly SgDebouncer _debouncer = new();
///
/// private async Task OnSearchChanged(string value)
/// {
///     await _debouncer.RunAsync(
///         ct => FetchResultsAsync(value, ct),
///         TimeSpan.FromMilliseconds(300));
/// }
///
/// public void Dispose() => _debouncer.Dispose();
/// </code>
/// </remarks>
public sealed class SgDebouncer : IDisposable
{
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Возвращает <c>true</c>, если есть ожидающее (отложенное) действие.
    /// </summary>
    public bool IsPending
    {
        get
        {
            lock (_lock) return _cts is not null && !_cts.IsCancellationRequested;
        }
    }

    /// <summary>
    /// Запускает или перезапускает debounce-таймер.
    /// Предыдущий вызов отменяется.
    /// </summary>
    /// <param name="action">Действие с поддержкой отмены.</param>
    /// <param name="delay">Задержка перед выполнением.</param>
    public Task RunAsync(Func<CancellationToken, Task> action, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(action);

        CancellationTokenSource newCts;
        lock (_lock)
        {
            if (_disposed) return Task.CompletedTask;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = newCts = new CancellationTokenSource();
        }

        return ExecuteAsync(action, delay, newCts.Token);
    }

    /// <summary>
    /// Запускает или перезапускает debounce-таймер (синхронное действие).
    /// </summary>
    /// <param name="action">Синхронное действие.</param>
    /// <param name="delay">Задержка.</param>
    public Task RunAsync(Action action, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(action);
        return RunAsync(_ => { action(); return Task.CompletedTask; }, delay);
    }

    /// <summary>
    /// Отменяет ожидающее действие.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _cts?.Cancel();
        }
    }

    private static async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        TimeSpan delay,
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            if (!ct.IsCancellationRequested)
            {
                await action(ct);
            }
        }
        catch (OperationCanceledException) { /* debounce — норма */ }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}

/// <summary>
/// Throttler — ограничивает частоту выполнения действия.
/// </summary>
/// <remarks>
/// <para>В отличие от <see cref="SgDebouncer"/>, throttler выполняет действие
/// сразу и блокирует повторные вызовы до истечения интервала.</para>
/// <code>
/// private readonly AsyncThrottler _throttler = new();
///
/// private async Task OnScrollAsync()
/// {
///     await _throttler.TryRunAsync(
///         () => UpdateVisibleItemsAsync(),
///         TimeSpan.FromMilliseconds(16)); // ~60fps
/// }
/// </code>
/// </remarks>
public sealed class AsyncThrottler : IDisposable
{
    private readonly TimeProvider _time;
    private long _lastRunTicks = long.MinValue;
    private int _running;
    private bool _disposed;

    /// <summary>
    /// Создаёт новый <see cref="AsyncThrottler"/>.
    /// </summary>
    /// <param name="time">
    /// Источник времени. По умолчанию — <see cref="TimeProvider.System"/>.
    /// Inject <see cref="TimeProvider"/> в тестах для детерминированного времени.
    /// </param>
    public AsyncThrottler(TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
    }

    /// <summary>
    /// Выполняет <paramref name="action"/>, если прошло достаточно времени
    /// с предыдущего вызова.
    /// </summary>
    /// <param name="action">Асинхронное действие.</param>
    /// <param name="interval">Минимальный интервал между выполнениями.</param>
    /// <returns>
    /// <c>true</c>, если действие было выполнено; <c>false</c>, если пропущено.
    /// </returns>
    public async Task<bool> TryRunAsync(Func<Task> action, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_disposed) return false;
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return false;

        try
        {
            var now = _time.GetTimestamp();
            var elapsed = TimeSpan.FromTicks(now - _lastRunTicks);
            if (elapsed < interval) return false;

            _lastRunTicks = now;
            await action();
            return true;
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _disposed = true;
}