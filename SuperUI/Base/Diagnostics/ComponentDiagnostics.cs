namespace SuperUI.Diagnostics;

/// <summary>
/// Встроенный профайлер компонента.
/// В режиме разработки показывает overlay с метриками.
/// Нет ни у одной библиотеки!
/// </summary>
public sealed class ComponentDiagnostics
{
    public string ComponentId { get; set; } = "";
    public int RenderCount { get; set; }
    public double AverageRenderMs { get; set; }
    public double LastRenderMs { get; set; }
    public int ParameterChangeCount { get; set; }
    public int JsCallCount { get; set; }
    public int JsErrorCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Uptime => DateTime.UtcNow - CreatedAt;
}