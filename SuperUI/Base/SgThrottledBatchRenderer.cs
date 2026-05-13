// SuperUI/Base/SgThrottledBatchRenderer.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base;

/// <summary>
/// Throttles StateHasChanged calls to batch multiple
/// render requests into fewer actual renders.
/// </summary>
public class SgThrottledBatchRenderer : IDisposable
{
    private readonly ComponentBase _component;
    private readonly int _throttleMs;
    private readonly int _maxBatchSize;
    private Timer? _timer;
    private int _pendingCount;
    private bool _disposed;

    public int PendingCount => _pendingCount;

    /// <summary>
    /// Creates a throttled batch renderer for a component.
    /// Uses reflection-safe approach: InvokeAsync(StateHasChanged) via Action.
    /// </summary>
    public SgThrottledBatchRenderer(ComponentBase component, int throttleMs = 16, int maxBatchSize = 10)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
        _throttleMs = Math.Max(1, throttleMs);
        _maxBatchSize = maxBatchSize;
    }

    /// <summary>Request a render. Multiple rapid requests are batched.</summary>
    public void RequestRender()
    {
        if (_disposed) return;

        var count = Interlocked.Increment(ref _pendingCount);

        // If we exceed batch size, render immediately
        if (count >= _maxBatchSize)
        {
            Flush();
            return;
        }

        // Otherwise, schedule a throttled render
        if (_timer == null)
        {
            _timer = new Timer(_ =>
            {
                Flush();
            }, null, _throttleMs, Timeout.Infinite);
        }
    }

    private void Flush()
    {
        if (_disposed) return;

        _timer?.Dispose();
        _timer = null;
        Interlocked.Exchange(ref _pendingCount, 0);

        // Исправление CS0122: Используем InvokeAsync через публичный метод
        // ComponentBase.InvokeAsync(Action) — protected, поэтому
        // используем InvokeAsync(Func<Task>) с пустой лямбдой.
        _ = _component.InvokeAsync(() =>
        {
            _component.StateHasChanged();
            return Task.CompletedTask;
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
        GC.SuppressFinalize(this);
    }
}
