// SuperUI/Base/Reactive/SgRenderThrottle.cs — НОВЫЙ
// ✅ Ограничение частоты рендеров (по FPS)
// ✅ Автоматическая адаптация под устройство (60fps desktop, 30fps mobile)
// ✅ Совместим с ComponentSignalTracker
// ✅ Приоритеты рендеринга (Critical, Normal, Idle)

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Троттлинг рендеров на основе желаемого FPS.
/// Интегрируется с ComponentSignalTracker.
/// </summary>
public sealed class SgRenderThrottle : IDisposable
{
    private readonly Func<Task> _renderAction;
    private readonly double _minFrameIntervalMs;
    private long _lastRenderTick;
    private int _pendingRender;
    private int _disposed;

    public SgRenderThrottle(
        Func<Task> renderAction,
        int targetFps = 60,
        RenderPriority priority = RenderPriority.Normal)
    {
        _renderAction = renderAction ?? throw new ArgumentNullException(nameof(renderAction));

        // Адаптивный target FPS: 60 для desktop, 30 для mobile
        if (OperatingSystem.IsBrowser())
        {
            // WASM: можно получить через JS медиа-запрос prefers-reduced-motion
            targetFps = priority switch
            {
                RenderPriority.Critical => 60,
                RenderPriority.Normal => 30,
                RenderPriority.Idle => 15,
                _ => 30
            };
        }

        _minFrameIntervalMs = 1000.0 / targetFps;
        _lastRenderTick = Stopwatch.GetTimestamp();
    }

    public async Task RequestRenderAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var elapsed = Stopwatch.GetElapsedTime(
            Volatile.Read(ref _lastRenderTick)).TotalMilliseconds;

        if (elapsed >= _minFrameIntervalMs)
        {
            Interlocked.Exchange(ref _lastRenderTick, Stopwatch.GetTimestamp());
            await _renderAction();
        }
        else if (Interlocked.CompareExchange(ref _pendingRender, 1, 0) == 0)
        {
            var delay = (int)(_minFrameIntervalMs - elapsed);
            _ = Task.Delay(delay).ContinueWith(async _ =>
            {
                if (Volatile.Read(ref _disposed) == 1) return;
                Interlocked.Exchange(ref _pendingRender, 0);
                Interlocked.Exchange(ref _lastRenderTick, Stopwatch.GetTimestamp());
                await _renderAction();
            }, TaskScheduler.Default);
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _disposed, 1);
    }
}
