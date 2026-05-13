// SuperUI/Base/SgCancellableOperation.cs
// NEW: Паттерн отменяемой операции для компонентов
// Решает: race conditions, double-fetch, zombie задачи

namespace SuperUI.Base;

/// <summary>
/// Представляет отменяемую асинхронную операцию с состоянием.
/// Используется в компонентах для безопасной отмены предыдущих запросов.
/// </summary>
/// <typeparam name="T">Тип результата.</typeparam>
/// <example>
/// private readonly SgCancellableOperation<UserDto[]> _loadOp = new();
///
/// async Task LoadAsync(string query)
/// {
///     await _loadOp.RunAsync(async ct =>
///     {
///         Users = await ApiService.SearchAsync(query, ct);
///         StateHasChanged();
///     });
/// }
/// </example>
public sealed class SgCancellableOperation<T> : IDisposable
{
    private CancellationTokenSource? _currentCts;
    private readonly object _lock = new();
    private int _disposed;

    /// <summary>Текущий результат.</summary>
    public T? Result { get; private set; }

    /// <summary>Ошибка последней операции.</summary>
    public Exception? Error { get; private set; }

    /// <summary>Операция выполняется.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Операция завершена успешно.</summary>
    public bool IsSuccess => Result is not null && Error is null && !IsRunning;

    /// <summary>
    /// Запустить операцию, отменив предыдущую.
    /// </summary>
    public async Task RunAsync(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken externalToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        ArgumentNullException.ThrowIfNull(operation);

        CancellationTokenSource newCts;
        lock (_lock)
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            newCts = externalToken == default
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _currentCts = newCts;
        }

        IsRunning = true;
        Error = null;

        try
        {
            Result = await operation(newCts.Token);
        }
        catch (OperationCanceledException) when (newCts.IsCancellationRequested)
        {
            // Операция была отменена — не ошибка
        }
        catch (Exception ex)
        {
            Error = ex;
            Result = default;
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>Overload без возвращаемого значения.</summary>
    public Task RunAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken externalToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return RunAsync(async ct => { await operation(ct); return default!; }, externalToken);
    }

    /// <summary>Отменить текущую операцию.</summary>
    public void Cancel()
    {
        lock (_lock) _currentCts?.Cancel();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        lock (_lock)
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }
}
