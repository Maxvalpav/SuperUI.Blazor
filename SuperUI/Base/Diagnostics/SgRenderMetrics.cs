// SuperUI/Base/Diagnostics/SgRenderMetrics.cs
// НОВЫЙ КЛАСС:
// ✅ Интеграция с System.Diagnostics.Metrics (.NET 8)
// ✅ Счётчики: рендеры, время, ошибки
// ✅ OpenTelemetry совместимость

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Метрики рендера компонентов SuperUI через System.Diagnostics.Metrics (.NET 8+).
/// Совместимо с OpenTelemetry и Prometheus.
/// </summary>
public sealed class SgRenderMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _renderCount;
    private readonly Histogram<double> _renderDuration;
    private readonly Counter<long> _errorCount;
    private readonly Counter<long> _parameterChangeCount;
    private readonly ObservableGauge<int> _activeComponents;
    private int _activeComponentCount;

    public static readonly SgRenderMetrics Instance = new();

    public SgRenderMetrics(string meterName = "SuperUI.Components")
    {
        _meter = new Meter(meterName, "1.0.0");

        _renderCount = _meter.CreateCounter<long>("superui.component.renders",
            unit: "{renders}",
            description: "Total number of component renders");

        _renderDuration = _meter.CreateHistogram<double>("superui.component.render_duration",
            unit: "ms",
            description: "Component render duration in milliseconds");

        _errorCount = _meter.CreateCounter<long>("superui.component.errors",
            unit: "{errors}",
            description: "Total number of component errors");

        _parameterChangeCount = _meter.CreateCounter<long>("superui.component.parameter_changes",
            unit: "{changes}",
            description: "Total number of parameter changes");

        _activeComponents = _meter.CreateObservableGauge("superui.component.active_count",
            () => _activeComponentCount,
            unit: "{components}",
            description: "Number of active SuperUI components");
    }

    public void RecordRender(string componentId, double durationMs)
    {
        _renderCount.Add(1, new TagList { { "component_id", componentId } });
        _renderDuration.Record(durationMs, new TagList { { "component_id", componentId } });
    }

    public void RecordError(string componentId, string errorType)
    {
        _errorCount.Add(1, new TagList
        {
            { "component_id", componentId },
            { "error_type", errorType }
        });
    }

    public void RecordParameterChange(string componentId, string parameterName)
    {
        _parameterChangeCount.Add(1, new TagList
        {
            { "component_id", componentId },
            { "parameter", parameterName }
        });
    }

    public void IncrementActiveComponents() =>
        System.Threading.Interlocked.Increment(ref _activeComponentCount);

    public void DecrementActiveComponents() =>
        System.Threading.Interlocked.Decrement(ref _activeComponentCount);

    public void Dispose() => _meter.Dispose();
}
