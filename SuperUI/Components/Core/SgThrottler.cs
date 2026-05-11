namespace SuperUI.Core;

/// <summary>
/// Leading-edge throttler. The first call within a window fires immediately;
/// subsequent calls until the window elapses are coalesced into a single trailing
/// invocation (configurable). Typical uses: scroll handlers, resize observers,
/// pointer-move broadcasting.
/// </summary>
public sealed class SgThrottler : IDisposable
{
    private readonly object _gate = new();
    private DateTimeOffset _lastInvoke = DateTimeOffset.MinValue;
    private CancellationTokenSource? _trailingCts;
    private bool _disposed;

    /// <summary>Minimum interval between successive invocations.</summary>
    public TimeSpan Interval { get; set; }

    /// <summary>When true, a final call is scheduled at the end of the window so the latest input is not lost.</summary>
    public bool Trailing { get; set; } = true;

    /// <summary>Creates a throttler with the given interval in milliseconds.</summary>
    public SgThrottler(int intervalMs, bool trailing = true) : this(TimeSpan.FromMilliseconds(intervalMs), trailing) { }

    /// <summary>Creates a throttler with the given interval.</summary>
    public SgThrottler(TimeSpan interval, bool trailing = true)
    {
        if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        Interval = interval;
        Trailing = trailing;
    }

    /// <summary>
    /// Invokes <paramref name="action"/> at most once per <see cref="Interval"/>.
    /// If <see cref="Trailing"/> is true, calls that arrive during the window schedule
    /// one trailing run with the latest delegate.
    /// </summary>
    public async Task ThrottleAsync(Func<CancellationToken, Task> action, CancellationToken external = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_disposed) return;

        bool fireNow;
        TimeSpan remaining;
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            remaining = Interval - (now - _lastInvoke);
            if (remaining <= TimeSpan.Zero)
            {
                _lastInvoke = now;
                fireNow = true;
            }
            else
            {
                fireNow = false;
            }
        }

        if (fireNow)
        {
            try { await action(external).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            return;
        }

        if (!Trailing) return;

        CancellationTokenSource cts;
        lock (_gate)
        {
            _trailingCts?.Cancel();
            _trailingCts?.Dispose();
            cts = external.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(external)
                : new CancellationTokenSource();
            _trailingCts = cts;
        }

        try
        {
            await Task.Delay(remaining, cts.Token).ConfigureAwait(false);
            if (cts.IsCancellationRequested) return;

            lock (_gate) { _lastInvoke = DateTimeOffset.UtcNow; }
            await action(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_trailingCts, cts))
                {
                    _trailingCts = null;
                    cts.Dispose();
                }
            }
        }
    }

    /// <summary>Convenience overload for synchronous actions.</summary>
    public Task ThrottleAsync(Action action, CancellationToken external = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ThrottleAsync(_ => { action(); return Task.CompletedTask; }, external);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_gate)
        {
            _trailingCts?.Cancel();
            _trailingCts?.Dispose();
            _trailingCts = null;
        }
    }
}
