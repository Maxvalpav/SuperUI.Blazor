namespace SuperUI.Core;

/// <summary>
/// Async debouncer. Each call to <see cref="DebounceAsync"/> cancels the previous pending
/// invocation and schedules the supplied delegate to run after <see cref="Delay"/>. Typical
/// uses: search-as-you-type, autosave, resize handlers.
/// </summary>
/// <remarks>
/// Hold one debouncer per logical operation in a component field and dispose it in
/// <c>Dispose</c> / <c>DisposeAsyncCore</c>. The debouncer is thread-safe; pending work
/// is cancelled (not awaited) when a new call arrives or when <see cref="Dispose"/> runs.
/// </remarks>
public sealed class SgDebouncer : IDisposable
{
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>Delay between the last call and the action firing.</summary>
    public TimeSpan Delay { get; set; }

    /// <summary>Creates a debouncer with the given delay in milliseconds.</summary>
    public SgDebouncer(int delayMs) : this(TimeSpan.FromMilliseconds(delayMs)) { }

    /// <summary>Creates a debouncer with the given delay.</summary>
    public SgDebouncer(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));
        Delay = delay;
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run after <see cref="Delay"/>. If called again
    /// before the delay elapses, the previously pending call is cancelled. The returned task
    /// completes when the scheduled action finishes — or is cancelled silently if superseded.
    /// </summary>
    public async Task DebounceAsync(Func<CancellationToken, Task> action, CancellationToken external = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_disposed) return;

        CancellationTokenSource cts;
        lock (_gate)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            cts = external.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(external)
                : new CancellationTokenSource();
            _cts = cts;
        }

        try
        {
            await Task.Delay(Delay, cts.Token).ConfigureAwait(false);
            if (cts.IsCancellationRequested) return;
            await action(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* superseded or disposed */ }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_cts, cts))
                {
                    _cts = null;
                    cts.Dispose();
                }
            }
        }
    }

    /// <summary>Convenience overload for synchronous actions.</summary>
    public Task DebounceAsync(Action action, CancellationToken external = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return DebounceAsync(_ => { action(); return Task.CompletedTask; }, external);
    }

    /// <summary>Cancels any pending invocation without running it.</summary>
    public void Cancel()
    {
        lock (_gate)
        {
            _cts?.Cancel();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_gate)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
