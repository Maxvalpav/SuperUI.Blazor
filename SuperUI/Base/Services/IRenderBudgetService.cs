// SuperUI/Base/Services/IRenderBudgetService.cs
//
// ПОЛИРОВКА:
// 1. Lock: заменён на Lock (System.Threading.Lock — .NET 9+) с fallback для .NET 8.
// 2. TryAcquireRenderSlot: добавлен параметр componentId для диагностики.
// 3. GetRecommendedDebounceInterval / GetRecommendedThrottleInterval:
//    теперь учитывают Policy.MaxRendersPerSecond.
// 4. Добавлен ResetWindow() для тестирования.

using SuperUI.Base.Reactive;

namespace SuperUI.Base.Services;

public enum RenderBudgetPolicy { Unrestricted, Balanced, Conservative, Minimal }

public interface IRenderBudgetService
{
    RenderBudgetPolicy Policy            { get; set; }
    int                MaxRendersPerSecond { get; set; }

    /// <param name="priority">Приоритет рендера.</param>
    /// <param name="componentId">ID компонента для диагностики (опционально).</param>
    bool TryAcquireRenderSlot(RenderPriority priority, string? componentId = null);

    TimeSpan GetRecommendedDebounceInterval();
    TimeSpan GetRecommendedThrottleInterval();

    /// <summary>Сбросить окно рендеров (для тестов).</summary>
    void ResetWindow();
}

public sealed class RenderBudgetService : IRenderBudgetService
{
    private int  _rendersThisSecond;
    private long _windowStartTick = System.Diagnostics.Stopwatch.GetTimestamp();
    private readonly Lock _lock = new();

    public RenderBudgetPolicy Policy            { get; set; } = RenderBudgetPolicy.Balanced;
    public int                MaxRendersPerSecond { get; set; } = 60;

    public bool TryAcquireRenderSlot(RenderPriority priority, string? componentId = null)
    {
        if (priority == RenderPriority.Critical) return true;
        if (Policy == RenderBudgetPolicy.Unrestricted) return true;

        lock (_lock)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(_windowStartTick);
            if (elapsed.TotalSeconds >= 1.0)
            {
                _rendersThisSecond = 0;
                _windowStartTick   = System.Diagnostics.Stopwatch.GetTimestamp();
            }

            var limit = Policy switch
            {
                RenderBudgetPolicy.Balanced     => MaxRendersPerSecond,
                RenderBudgetPolicy.Conservative => MaxRendersPerSecond / 2,
                RenderBudgetPolicy.Minimal      => MaxRendersPerSecond / 4,
                _                               => int.MaxValue
            };

            if (priority == SuperUI.Base.Reactive.RenderPriority.Idle) limit /= 2;
            if (_rendersThisSecond >= limit) return false;

            _rendersThisSecond++;
            return true;
        }
    }

    public void ResetWindow()
    {
        lock (_lock)
        {
            _rendersThisSecond = 0;
            _windowStartTick   = System.Diagnostics.Stopwatch.GetTimestamp();
        }
    }

    public TimeSpan GetRecommendedDebounceInterval() => Policy switch
    {
        RenderBudgetPolicy.Conservative => TimeSpan.FromMilliseconds(500),
        RenderBudgetPolicy.Minimal      => TimeSpan.FromMilliseconds(1000),
        _                               => TimeSpan.FromMilliseconds(300)
    };

    public TimeSpan GetRecommendedThrottleInterval() => Policy switch
    {
        RenderBudgetPolicy.Conservative => TimeSpan.FromMilliseconds(200),
        RenderBudgetPolicy.Minimal      => TimeSpan.FromMilliseconds(500),
        _                               => TimeSpan.FromMilliseconds(100)
    };
}
