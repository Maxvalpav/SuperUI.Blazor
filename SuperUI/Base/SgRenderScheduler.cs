// SuperUI/Base/SgRenderScheduler.cs
// НОВЫЙ: глобальный планировщик рендеров с приоритетами (Critical > Normal > Low)
// Аналог React Scheduler / Concurrent Mode для Blazor

namespace SuperUI.Base;

/// <summary>Приоритет рендеринга компонента.</summary>
public enum RenderPriority
{
    /// Немедленно (пользовательский ввод, анимации)
    Critical = 0,
    /// В следующем batch-окне (~16ms)
    Normal = 1,
    /// При простое (~100ms)
    Low = 2
}

/// <summary>
/// Глобальный планировщик рендеров.
/// Регистрируется как Singleton (WASM) / Scoped (Server).
/// </summary>
public sealed class SgRenderScheduler : IDisposable
{
    // Очереди по приоритетам
    private readonly Queue<WeakReference<SgComponentBase>> _critical = new();
    private readonly Queue<WeakReference<SgComponentBase>> _normal = new();
    private readonly Queue<WeakReference<SgComponentBase>> _low = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Task? _flushTask;
    private readonly CancellationTokenSource _cts = new();

    // Интервалы для каждого приоритета
    private static readonly TimeSpan CriticalInterval = TimeSpan.FromMilliseconds(0);
    private static readonly TimeSpan NormalInterval = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan LowInterval = TimeSpan.FromMilliseconds(100);

    public void Schedule(SgComponentBase component, RenderPriority priority = RenderPriority.Normal)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (component.IsDisposed) return;

        var weakRef = new WeakReference<SgComponentBase>(component);
        lock (_critical)
        {
            switch (priority)
            {
                case RenderPriority.Critical: _critical.Enqueue(weakRef); break;
                case RenderPriority.Normal: _normal.Enqueue(weakRef); break;
                case RenderPriority.Low: _low.Enqueue(weakRef); break;
            }
        }

        EnsureFlushRunning(priority);
    }

    private void EnsureFlushRunning(RenderPriority priority)
    {
        if (_flushTask is { IsCompleted: false }) return;
        _flushTask = FlushAsync(priority);
    }

    private async Task FlushAsync(RenderPriority priority)
    {
        var delay = priority switch
        {
            RenderPriority.Critical => CriticalInterval,
            RenderPriority.Normal => NormalInterval,
            _ => LowInterval
        };

        if (delay > TimeSpan.Zero)
        {
            try { await Task.Delay(delay, _cts.Token); }
            catch (OperationCanceledException) { return; }
        }

        await _lock.WaitAsync(_cts.Token);
        try
        {
            // Флашим в порядке приоритета
            FlushQueue(_critical);
            FlushQueue(_normal);
            FlushQueue(_low);
        }
        finally { _lock.Release(); }
    }

    private static void FlushQueue(Queue<WeakReference<SgComponentBase>> queue)
    {
        while (queue.TryDequeue(out var wr))
        {
            if (wr.TryGetTarget(out var comp) && !comp.IsDisposed)
                comp.RequestRender();
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _lock.Dispose();
    }
}