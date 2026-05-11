namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Диагностические метрики компонента (только DEBUG).
/// </summary>
public sealed class ComponentDiagnostics
{
    public string ComponentId { get; set; } = string.Empty;
    public int RenderCount { get; set; }
    public double LastRenderMs { get; set; }
    public double AverageRenderMs { get; set; }
    public int ParameterChangeCount { get; set; }
    public int JsCallCount { get; set; }
    public int JsErrorCount { get; set; }

    public override string ToString() =>
        $"[{ComponentId}] Renders={RenderCount}, " +
        $"AvgMs={AverageRenderMs:F2}, " +
        $"JS={JsCallCount} (err={JsErrorCount}), " +
        $"ParamChanges={ParameterChangeCount}";
}
