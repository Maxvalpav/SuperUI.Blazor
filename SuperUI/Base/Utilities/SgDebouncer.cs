// SuperUI/Base/Utilities/SgDebouncer.cs
// Trailing-edge debouncer with leading support and TimeProvider injection.
// Replaces the previous lock-heavy version with lock-free atomic swap.

using System.Threading;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Trailing-edge debouncer that coalesces rapid calls into a single delayed invocation.
/// </summary>
/// <remarks>
/// <para>Trailing mode (default): the action runs <paramref name="delay"/> ms after
/// the <b>last</b> call. Useful for "search as you type" or "fiters applied on user idle".</para>
/// <para>Leading mode: the action runs on the <b>first</b> call, subsequent calls
/// within the window are ignored. Useful for "double-click suppression" or button debouncing.</para>
/// <para>Thread-safe: the cancellation token is replaced atomically; in-flight
/// continuations are cancelled via <see cref="CancellationTokenSource.Cancel()"/>.</para>
/// <para>Testable: supply a <see cref="TimeProvider"/> for deterministic time control.</para>
/// </remarks>
public sealed class SgDebouncer : IDisposable, IAsyncDisposable
{
    private readonly TimeProvider _time;
    private readonly bool _leading;
    private CancellationTokenSource? _cts;
    private long _lastTriggerTicks; // for leading-edge suppression
    private int _disposed;

    /// <summary>
    /// Creates a new debouncer.
    /// </summary>
    /// <param name="leading">If <c>true</c>, fire on the first call; otherwise wait for the delay (trailing).</param>
    /// <param name="time">Time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
    public SgDebouncer(bool leading = false, TimeProvider? time = null)
    {
        _leading = leading;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>True if a debounce call is pending or currently executing.</summary>
    public bool IsPending => Volatile.Read(ref _cts) is { IsCancellationRequested: false };

    /// <summary>
    /// Schedules <paramref name="action"/> to run after <paramref name="delay"/>,
    /// replacing any pending call.
    /// </summary>
    public Task RunAsync(Func<CancellationToken, Task> action, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (Volatile.Read(ref _disposed) == 1) return Task.CompletedTask;

        if (_leading)
        {
            var now = _time.GetTimestamp();
            var last = Interlocked.Read(ref _lastTriggerTicks);
            // Simple leading: skip if we already triggered within `delay`.
            if (last != 0 && (now - last) < delay.Ticks) return Task.CompletedTask;
            Interlocked.Exchange(ref _lastTriggerTicks, now);
            return SafeInvokeAsync(action);
        }

        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _cts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        return ExecuteAsync(action, delay, newCts.Token);
    }

    /// <summary>Schedules a synchronous action.</summary>
    public Task RunAsync(Action action, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(action);
        return RunAsync(_ => { action(); return Task.CompletedTask; }, delay);
    }

    /// <summary>Schedules a function returning a value.</summary>
    public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> func, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(func);
        if (Volatile.Read(ref _disposed) == 1) return Task.FromResult(default(T)!);

        if (_leading)
        {
            var now = _time.GetTimestamp();
            var last = Interlocked.Read(ref _lastTriggerTicks);
            if (last != 0 && (now - last) < delay.Ticks) return Task.FromResult(default(T)!);
            Interlocked.Exchange(ref _lastTriggerTicks, now);
            return SafeInvokeAsync(func);
        }

        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _cts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        return ExecuteAsync(func, delay, newCts.Token);
    }

    /// <summary>Cancels the pending call (if any).</summary>
    public void Cancel()
    {
        var cts = Interlocked.Exchange(ref _cts, null);
        if (cts is null) return;
        try { cts.Cancel(); } catch (ObjectDisposedException) { }
        cts.Dispose();
    }

    private async Task ExecuteAsync(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, _time, ct).ConfigureAwait(false);
            if (!ct.IsCancellationRequested) await action(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, _time, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) return default(T)!;
            return await action(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return default(T)!;
        }
    }

    private static async Task SafeInvokeAsync(Func<CancellationToken, Task> action)
    {
        try { await action(CancellationToken.None).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
    }

    private static async Task<T> SafeInvokeAsync<T>(Func<CancellationToken, Task<T>> action)
    {
        try { return await action(CancellationToken.None).ConfigureAwait(false); }
        catch (OperationCanceledException) { return default(T)!; }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        Cancel();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
