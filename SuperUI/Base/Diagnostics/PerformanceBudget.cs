// SuperUI/Base/Diagnostics/PerformanceBudget.cs
// НОВОЕ: Performance Budget для компонентов.
// В DEV режиме выводит предупреждения если компонент:
// - рендерится чаще заданного лимита в секунду
// - рендер занимает больше заданного времени
// - делает больше N JS вызовов за один рендер
//
// Аналогов нет ни в одной Blazor библиотеке.
// Похоже на React DevTools Profiler, но встроено в компонент.
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Бюджет производительности компонента.
/// В DEBUG режиме логирует нарушения через ILogger.
/// В RELEASE режиме — zero-cost (весь код исключается компилятором).
/// </summary>
public sealed class PerformanceBudget
{
#if DEBUG
    private readonly string _componentId;
    private readonly ILogger _logger;
    
    // Лимиты по умолчанию
    private int _maxRendersPerSecond = 30;
    private double _maxRenderMs = 16.0; // один кадр при 60fps
    private int _maxJsCallsPerRender = 5;
    
    // Статистика
    private int _rendersThisSecond;
    private long _secondStartTick;
    private bool _budgetExceeded;

    public PerformanceBudget(string componentId, ILogger logger)
    {
        _componentId = componentId;
        _logger = logger;
        _secondStartTick = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    /// <summary>Настроить лимиты бюджета.</summary>
    public PerformanceBudget WithLimits(
        int maxRendersPerSecond = 30,
        double maxRenderMs = 16.0,
        int maxJsCallsPerRender = 5)
    {
        _maxRendersPerSecond = maxRendersPerSecond;
        _maxRenderMs = maxRenderMs;
        _maxJsCallsPerRender = maxJsCallsPerRender;
        return this;
    }

    /// <summary>Проверить бюджет после рендера.</summary>
    public void CheckAfterRender(double renderMs, int jsCallsThisRender)
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var elapsedSec = System.Diagnostics.Stopwatch.GetElapsedTime(_secondStartTick).TotalSeconds;
        
        if (elapsedSec >= 1.0)
        {
            _rendersThisSecond = 0;
            _secondStartTick = now;
            _budgetExceeded = false;
        }

        _rendersThisSecond++;

        // Проверяем нарушения бюджета
        if (renderMs > _maxRenderMs)
        {
            _logger.LogWarning(
                "⚠️ [PerformanceBudget] [{Id}] Render took {Ms:F1}ms > budget {Budget}ms",
                _componentId, renderMs, _maxRenderMs);
        }

        if (_rendersThisSecond > _maxRendersPerSecond && !_budgetExceeded)
        {
            _budgetExceeded = true;
            _logger.LogWarning(
                "⚠️ [PerformanceBudget] [{Id}] Renders/sec={Count} > budget {Budget}",
                _componentId, _rendersThisSecond, _maxRendersPerSecond);
        }

        if (jsCallsThisRender > _maxJsCallsPerRender)
        {
            _logger.LogWarning(
                "⚠️ [PerformanceBudget] [{Id}] JS calls={Count} > budget {Budget} in one render",
                _componentId, jsCallsThisRender, _maxJsCallsPerRender);
        }
    }
#else
    // RELEASE: all methods are no-ops compiled away
    public PerformanceBudget(string componentId, ILogger logger) { }
    public PerformanceBudget WithLimits(int a = 0, double b = 0, int c = 0) => this;
    public void CheckAfterRender(double renderMs, int jsCallsThisRender) { }
#endif
}