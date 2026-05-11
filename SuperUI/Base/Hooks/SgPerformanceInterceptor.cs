// SuperUI/Base/Hooks/SgPerformanceInterceptor.cs
// ИСПРАВЛЕНО:
// 1. _renderStart захватывается в ShouldRender (начало замера)
// 2. Interlocked.Exchange для _renderStart (thread-safe на Server)
// 3. Threshold вынесен в параметр конструктора
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Перехватчик для мониторинга производительности рендера.
/// Логирует Warning если рендер превышает порог (по умолчанию 16 мс = 1 кадр @ 60fps).
/// </summary>
public sealed class SgPerformanceInterceptor : SgComponentInterceptor
{
    private readonly ILogger _logger;
    private readonly double _thresholdMs;
    private long _renderStart;

    /// <param name="logger">Логгер.</param>
    /// <param name="thresholdMs">Порог в мс. По умолчанию 16 мс.</param>
    public SgPerformanceInterceptor(ILogger logger, double thresholdMs = 16.0)
    {
        _logger = logger;
        _thresholdMs = thresholdMs;
    }

    // ИСПРАВЛЕНО: начало замера в ShouldRender (вызывается ДО рендера)
    public override bool ShouldRender(SgComponentBase component)
    {
        Interlocked.Exchange(ref _renderStart, Stopwatch.GetTimestamp());
        return true;
    }

    public override void OnAfterRender(SgComponentBase component, bool firstRender)
    {
        var start = Interlocked.Read(ref _renderStart);
        if (start == 0) return;

        var elapsed = Stopwatch.GetElapsedTime(start);
        if (elapsed.TotalMilliseconds > _thresholdMs)
        {
            _logger.LogWarning(
                "[Perf] {Id} slow render: {Ms:F2}ms (firstRender: {First})",
                component.ComponentId, elapsed.TotalMilliseconds, firstRender);
        }
    }
}