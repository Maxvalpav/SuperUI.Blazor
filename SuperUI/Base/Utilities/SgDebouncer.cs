// SuperUI/Base/Utilities/SgDebouncer.cs
namespace SuperUI.Base.Utilities;

public sealed class SgDebouncer : IDisposable, IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();
    private bool _disposed;

    public bool IsPending
    {
        get { lock (_lock) return _cts is not null && !_cts.IsCancellationRequested; }
    }

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

    public Task RunAsync(Action action, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(action);
        return RunAsync(_ => { action(); return Task.CompletedTask; }, delay);
    }

    public void Cancel()
    {
        lock (_lock) { _cts?.Cancel(); }
    }

    private static async Task ExecuteAsync(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            if (!ct.IsCancellationRequested) await action(ct);
        }
        catch (OperationCanceledException) { }
    }

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

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
