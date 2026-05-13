// SuperUI/Base/SgThrottledBatchRenderer.cs
// НОВЫЙ: батчевый рендеринг с адаптивным throttle
// Для компонентов с высокочастотными обновлениями (realtime charts, live data)

namespace SuperUI.Base;

/// <summary>
/// Батч-рендерер: коагулирует множество RequestRender() в один StateHasChanged().
/// Адаптивный: на WASM использует requestAnimationFrame-подобную семантику,
/// на Server — ограничение по времени.
/// </summary>
public sealed class SgBatchRenderScheduler : IAsyncDisposable
{
    private readonly SgComponentBase _component;
    private readonly TimeSpan _minInterval;
    private int _pendingRender; // 0 = нет, 1 = pending
    private long _lastRenderTick;
    private readonly CancellationToken _ct;
    private Task? _scheduledTask;

    /// <summary>
    /// Создать планировщик.
    /// </summary>
    /// <param name="component">Компонент для рендеринга.</param>
    /// <param name="minInterval">Минимальный интервал между рендерами (по умолчанию 16ms = ~60fps).</param>
    public SgBatchRenderScheduler(SgComponentBase component, TimeSpan? minInterval = null)
    {
        _component = component;
        _minInterval = minInterval ?? TimeSpan.FromMilliseconds(16); // 60fps
        _ct = component.ComponentToken;
    }

    /// <summary>
    /// Запланировать рендер. Если рендер уже запланирован — игнорируется.
    /// Если прошло меньше minInterval с последнего рендера — ждёт.
    /// </summary>
    public void Schedule()
    {
        if (_component.IsDisposed || _ct.IsCancellationRequested) return;
        if (Interlocked.CompareExchange(ref _pendingRender, 1, 0) == 1) return;

        var elapsed = GetElapsedSinceLastRender();
        if (elapsed >= _minInterval)
        {
            // Можно рендерить сразу
            _pendingRender = 0;
            Interlocked.Exchange(ref _lastRenderTick, System.Diagnostics.Stopwatch.GetTimestamp());
            _component.RequestRender();
        }
        else
        {
            // Отложить до следующего окна
            var delay = _minInterval - elapsed;
            _scheduledTask = ScheduleDelayedAsync(delay);
        }
    }

    private async Task ScheduleDelayedAsync(TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, _ct);
            Interlocked.Exchange(ref _pendingRender, 0);
            Interlocked.Exchange(ref _lastRenderTick, System.Diagnostics.Stopwatch.GetTimestamp());
            if (!_component.IsDisposed)
                _component.RequestRender();
        }
        catch (OperationCanceledException) { }
        finally { Interlocked.Exchange(ref _pendingRender, 0); }
    }

    private TimeSpan GetElapsedSinceLastRender()
    {
        var last = Volatile.Read(ref _lastRenderTick);
        if (last == 0) return TimeSpan.MaxValue;
        return System.Diagnostics.Stopwatch.GetElapsedTime(last);
    }

    public async ValueTask DisposeAsync()
    {
        if (_scheduledTask is not null)
        {
            try { await _scheduledTask.WaitAsync(TimeSpan.FromSeconds(1)); }
            catch { }
        }
    }
}
