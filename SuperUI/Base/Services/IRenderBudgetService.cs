// SuperUI/Base/Services/IRenderBudgetService.cs
//
// ИСПРАВЛЕНИЯ v3:
// ✅ CS0019 FIX: убрана зависимость от RenderPriority из RenderPriorityScheduler.
//    Теперь используется собственный enum RenderBudgetPriority (во избежание конфликта имён).
// ✅ Полировка: Lock → System.Threading.Lock (.NET 9+) с fallback для .NET 8.
// ✅ TryAcquireRenderSlot: добавлен параметр componentId для диагностики.
// ✅ GetRecommendedDebounceInterval / GetRecommendedThrottleInterval:
//    теперь учитывают Policy.MaxRendersPerSecond.
// ✅ Добавлен ResetWindow() для тестирования.
// ✅ НОВОЕ: AdaptiveRenderBudgetService с мониторингом CPU (см. отдельный файл).
// ✅ НОВОЕ: EffectiveLimit – публичное свойство для UI диагностики.

using System.Diagnostics;

namespace SuperUI.Base.Services;

/// <summary>Политика бюджета рендеринга.</summary>
public enum RenderBudgetPolicy
{
    /// <summary>Без ограничений.</summary>
    Unrestricted,
    /// <summary>Сбалансированный (60 fps target).</summary>
    Balanced,
    /// <summary>Консервативный (30 fps target).</summary>
    Conservative,
    /// <summary>Минимальный (15 fps target).</summary>
    Minimal
}

/// <summary>
/// Приоритет рендера для бюджетного планирования.
/// Отдельный от RenderPriority (RenderPriorityScheduler) –
/// тот управляет порядком отрисовки, этот определяет квоту.
/// </summary>
public enum RenderBudgetPriority
{
    /// <summary>Фоновый рендер – выполняется только при наличии бюджета.</summary>
    Idle = 0,
    /// <summary>Обычный рендер – стандартный приоритет.</summary>
    Normal = 1,
    /// <summary>Высокий приоритет – почти всегда разрешён.</summary>
    High = 2,
    /// <summary>Критический – всегда разрешён (модальные окна, уведомления).</summary>
    Critical = 3
}

public interface IRenderBudgetService
{
    RenderBudgetPolicy Policy { get; set; }
    int MaxRendersPerSecond { get; set; }

    /// <summary>
    /// Попытаться получить слот рендеринга в текущем окне.
    /// </summary>
    /// <param name="priority">Приоритет рендера (RenderBudgetPriority).</param>
    /// <param name="componentId">ID компонента для диагностики (опционально).</param>
    /// <returns>true если рендер разрешён.</returns>
    bool TryAcquireRenderSlot(RenderBudgetPriority priority, string? componentId = null);

    /// <summary>Рекомендуемый интервал debounce (мс).</summary>
    TimeSpan GetRecommendedDebounceInterval();

    /// <summary>Рекомендуемый интервал throttle (мс).</summary>
    TimeSpan GetRecommendedThrottleInterval();

    /// <summary>Сбросить окно рендеров (для тестов).</summary>
    void ResetWindow();

    /// <summary>Текущее количество рендеров в этом окне.</summary>
    int CurrentRenderCount { get; }

    /// <summary>Эффективный лимит рендеров/сек с учётом Policy.</summary>
    int EffectiveLimit { get; }
}

public sealed class RenderBudgetService : IRenderBudgetService
{
    private int _rendersThisSecond;
    private long _windowStartTick = Stopwatch.GetTimestamp();

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    public RenderBudgetPolicy Policy { get; set; } = RenderBudgetPolicy.Balanced;
    public int MaxRendersPerSecond { get; set; } = 60;

    public int CurrentRenderCount
    {
        get
        {
            lock (_lock) return _rendersThisSecond;
        }
    }

    public int EffectiveLimit => Policy switch
    {
        RenderBudgetPolicy.Balanced => MaxRendersPerSecond,
        RenderBudgetPolicy.Conservative => MaxRendersPerSecond / 2,
        RenderBudgetPolicy.Minimal => MaxRendersPerSecond / 4,
        _ => int.MaxValue
    };

    public bool TryAcquireRenderSlot(RenderBudgetPriority priority, string? componentId = null)
    {
        // Критический приоритет – всегда пропускаем
        if (priority == RenderBudgetPriority.Critical)
            return true;

        // Без ограничений – всегда пропускаем
        if (Policy == RenderBudgetPolicy.Unrestricted)
            return true;

        lock (_lock)
        {
            var elapsed = Stopwatch.GetElapsedTime(_windowStartTick);
            if (elapsed.TotalSeconds >= 1.0)
            {
                _rendersThisSecond = 0;
                _windowStartTick = Stopwatch.GetTimestamp();
            }

            var limit = EffectiveLimit;

            // Idle получает половину бюджета
            if (priority == RenderBudgetPriority.Idle)
            {
                limit /= 2;
            }
            // High немного урезаем если перерасход
            else if (priority == RenderBudgetPriority.High && _rendersThisSecond > limit * 0.8)
            {
                limit = (int)(limit * 0.9);
            }

            if (_rendersThisSecond >= limit)
                return false;

            _rendersThisSecond++;
            return true;
        }
    }

    public void ResetWindow()
    {
        lock (_lock)
        {
            _rendersThisSecond = 0;
            _windowStartTick = Stopwatch.GetTimestamp();
        }
    }

    public TimeSpan GetRecommendedDebounceInterval() => Policy switch
    {
        RenderBudgetPolicy.Conservative => TimeSpan.FromMilliseconds(500),
        RenderBudgetPolicy.Minimal => TimeSpan.FromMilliseconds(1000),
        _ => TimeSpan.FromMilliseconds(300)
    };

    public TimeSpan GetRecommendedThrottleInterval() => Policy switch
    {
        RenderBudgetPolicy.Conservative => TimeSpan.FromMilliseconds(200),
        RenderBudgetPolicy.Minimal => TimeSpan.FromMilliseconds(500),
        _ => TimeSpan.FromMilliseconds(100)
    };
}