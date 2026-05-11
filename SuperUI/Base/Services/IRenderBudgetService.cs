// SuperUI/Base/Services/IRenderBudgetService.cs
// НОВОЕ: Сервис управления глобальным бюджетом рендеров.
// Позволяет настроить максимальное количество одновременных рендеров
// и автоматически defer'ить низкоприоритетные компоненты.
//
// Полезно для:
// - Мобильных устройств (слабый CPU)
// - Медленных соединений (Blazor Server)
// - Real-time дашбордов с сотнями обновлений в секунду
using SuperUI.Base.Reactive;

namespace SuperUI.Base.Services;

/// <summary>
/// Политика рендеров для текущего окружения.
/// </summary>
public enum RenderBudgetPolicy
{
    /// <summary>Максимальная производительность (все рендеры немедленно).</summary>
    Unrestricted,
    
    /// <summary>Балансированный режим (рекомендован для Server).</summary>
    Balanced,
    
    /// <summary>Экономия ресурсов (рекомендован для мобильных/слабых устройств).</summary>
    Conservative,
    
    /// <summary>Минимальное потребление (только критические обновления).</summary>
    Minimal
}

/// <summary>
/// Сервис управления бюджетом рендеров.
/// Регистрируется как Scoped (per-circuit).
/// </summary>
public interface IRenderBudgetService
{
    /// <summary>Текущая политика.</summary>
    RenderBudgetPolicy Policy { get; set; }
    
    /// <summary>Максимальное количество рендеров в секунду (0 = без ограничений).</summary>
    int MaxRendersPerSecond { get; set; }
    
    /// <summary>Запросить слот рендера. Возвращает true если рендер разрешён сейчас.</summary>
    bool TryAcquireRenderSlot(RenderPriority priority);
    
    /// <summary>Получить рекомендованный интервал debounce для UI событий.</summary>
    TimeSpan GetRecommendedDebounceInterval();
    
    /// <summary>Получить рекомендованный интервал throttle для data updates.</summary>
    TimeSpan GetRecommendedThrottleInterval();
}

/// <summary>
/// Реализация RenderBudgetService.
/// </summary>
public sealed class RenderBudgetService : IRenderBudgetService
{
    private int _rendersThisSecond;
    private long _windowStartTick = System.Diagnostics.Stopwatch.GetTimestamp();
    private readonly Lock _lock = new();

    public RenderBudgetPolicy Policy { get; set; } = RenderBudgetPolicy.Balanced;
    
    public int MaxRendersPerSecond { get; set; } = 60;

    public bool TryAcquireRenderSlot(RenderPriority priority)
    {
        // Critical renders are always allowed
        if (priority == RenderPriority.Critical) return true;
        if (Policy == RenderBudgetPolicy.Unrestricted) return true;

        lock (_lock)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(_windowStartTick);
            if (elapsed.TotalSeconds >= 1.0)
            {
                _rendersThisSecond = 0;
                _windowStartTick = System.Diagnostics.Stopwatch.GetTimestamp();
            }

            var limit = Policy switch
            {
                RenderBudgetPolicy.Balanced     => MaxRendersPerSecond,
                RenderBudgetPolicy.Conservative => MaxRendersPerSecond / 2,
                RenderBudgetPolicy.Minimal      => MaxRendersPerSecond / 4,
                _                               => int.MaxValue
            };

            // Idle renders have tighter limits
            if (priority == RenderPriority.Idle) limit /= 2;

            if (_rendersThisSecond >= limit) return false;
            _rendersThisSecond++;
            return true;
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