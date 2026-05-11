// SuperUI/Base/Hooks/SgPerformanceInterceptor.cs
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Перехватчик для мониторинга производительности рендера компонентов.
/// Логирует предупреждение если рендер занимает больше 16мс (1 фрейм @ 60fps).
/// </summary>
public sealed class SgPerformanceInterceptor : SgComponentInterceptor
{
    private readonly ILogger<SgPerformanceInterceptor> _logger;
    private long _renderStart;

    public SgPerformanceInterceptor(ILogger<SgPerformanceInterceptor> logger)
        => _logger = logger;

    public override void OnAfterRender(SgComponentBase component, bool firstRender)
    {
        var elapsed = Stopwatch.GetElapsedTime(_renderStart);
        if (elapsed.TotalMilliseconds > 16)
        {
            _logger.LogWarning(
                "[Perf] {Id} slow render: {Ms:F2}ms (firstRender: {First})",
                component.ComponentId, elapsed.TotalMilliseconds, firstRender);
        }
    }
}
