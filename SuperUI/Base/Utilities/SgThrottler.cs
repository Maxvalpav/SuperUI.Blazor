// SuperUI/Base/Utilities/SgThrottler.cs
// Leading+trailing throttler. Удобен для scroll/resize handlers, где нужно
// ограничить частоту вызова, но при этом гарантировать финальный вызов.

using System.Threading;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Throttler: гарантирует выполнение <b>не чаще</b> одного раза в окно,
/// а в trailing-режиме — ещё и финальный вызов после паузы.
/// </summary>
/// <remarks>
/// <para><b>Leading+trailing</b> (default): первый вызов в окне выполняется сразу;
/// последующие игнорируются, но после истечения окна выполняется последний
/// отложенный вызов. Это UX-паттерн "rate-limited live update".</para>
/// <para><b>Leading only</b>: только первый вызов, остальные игнорируются до конца окна.</para>
/// <para><b>Trailing only</b>: только последний вызов после паузы (аналог debouncer).</para>
/// </remarks>
public sealed class SgThrottler : IDisposable, IAsyncDisposable
{
    private readonly TimeProvider _time;
    private readonly Mode _mode;
    private long _windowStartTicks;
    private Func<CancellationToken, Task>? _pendingAction;
    private CancellationTokenSource? _pendingCts;
    private int _disposed;

    /// <summary>Throttle mode.</summary>
    public enum Mode { LeadingTrailing, LeadingOnly, TrailingOnly }

    /// <summary>
    /// Creates a new throttler.
    /// </summary>
    /// <param name="mode">Behavior. Default: <see cref="Mode.LeadingTrailing"/>.</param>
    /// <param name="time">Time provider. Defaults to <see cref="TimeProvider.System"/>.</param>
    public SgThrottler(Mode mode = Mode.LeadingTrailing, TimeProvider? time = null)
    {
        _mode = mode;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>True if a trailing call is scheduled.</summary>
    public bool IsPending => Volatile.Read(ref _pendingAction) is not null;

    /// <summary>
    /// Submits <paramref name="action"/>, throttled.
    /// </summary>
    public Task InvokeAsync(Func<CancellationToken, Task> action, TimeSpan window)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (Volatile.Read(ref _disposed) == 1) return Task.CompletedTask;

        var now = _time.GetTimestamp();
        var start = Interlocked.Read(ref _windowStartTicks);
        var elapsed = start == 0 ? window.Ticks + 1 : (now - start);
        var inWindow = elapsed < window.Ticks;

        if (!inWindow || (_mode == Mode.TrailingOnly && start != 0))
        {
            // Start a new window.
            Interlocked.Exchange(ref _windowStartTicks, now);
            if (_mode == Mode.TrailingOnly)
            {
                ScheduleTrailing(action, window);
                return Task.CompletedTask;
            }
            return SafeInvoke(action);
        }

        // In window: leading done, schedule trailing.
        if (_mode != Mode.LeadingOnly)
        {
            ScheduleTrailing(action, window);
        }
        return Task.CompletedTask;
    }

    /// <summary>Cancel any pending trailing call.</summary>
    public void Cancel()
    {
        Volatile.Write(ref _pendingAction, null);
        var cts = Interlocked.Exchange(ref _pendingCts, null);
        if (cts is null) return;
        try { cts.Cancel(); } catch (ObjectDisposedException) { }
        cts.Dispose();
    }

    private void ScheduleTrailing(Func<CancellationToken, Task> action, TimeSpan window)
    {
        Volatile.Write(ref _pendingAction, action);
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _pendingCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();
        _ = ExecuteTrailingAsync(window, newCts.Token);
    }

    private async Task ExecuteTrailingAsync(TimeSpan window, CancellationToken ct)
    {
        try
        {
            await Task.Delay(window, _time, ct).ConfigureAwait(false);
            var action = Interlocked.Exchange(ref _pendingAction, null);
            if (action is not null) await action(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    private static async Task SafeInvoke(Func<CancellationToken, Task> action)
    {
        try { await action(CancellationToken.None).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
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
