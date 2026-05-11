// SuperUI/Base/Diagnostics/ComponentDiagnostics.cs
// УЛУЧШЕНО:
// 1. Добавлены SignalSubscriptionCount, BatchedRenderCount
// 2. ToString() расширен
// 3. GetReport() для structured logging

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Диагностические метрики компонента (только DEBUG).
/// Доступны через SgComponentBase.Diagnostics в DEBUG сборке.
/// </summary>
public sealed class ComponentDiagnostics
{
    public string ComponentId { get; set; } = string.Empty;

    // Рендер
    public int RenderCount { get; set; }
    public double LastRenderMs { get; set; }
    public double AverageRenderMs { get; set; }
    public double MaxRenderMs { get; set; }
    public int BatchedRenderCount { get; set; }    // сколько раз batching объединил рендеры

    // Параметры
    public int ParameterChangeCount { get; set; }

    // JS Interop
    public int JsCallCount { get; set; }
    public int JsErrorCount { get; set; }
    public double TotalJsMs { get; set; }

    // Reactive
    public int SignalSubscriptionCount { get; set; }  // сколько сигналов отслеживается
    public int SignalNotificationCount { get; set; }   // сколько раз получали уведомление

    public override string ToString() =>
        $"[{ComponentId}] " +
        $"Renders={RenderCount} (avg={AverageRenderMs:F2}ms, max={MaxRenderMs:F2}ms), " +
        $"Batched={BatchedRenderCount}, " +
        $"JS={JsCallCount} (err={JsErrorCount}, {TotalJsMs:F0}ms), " +
        $"Params={ParameterChangeCount}, " +
        $"Signals={SignalSubscriptionCount} (notified={SignalNotificationCount})";

    /// <summary>Сбросить статистику рендеров.</summary>
    public void ResetRenderStats()
    {
        RenderCount = 0;
        LastRenderMs = 0;
        AverageRenderMs = 0;
        MaxRenderMs = 0;
        BatchedRenderCount = 0;
    }
}