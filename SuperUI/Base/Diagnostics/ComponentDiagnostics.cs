// SuperUI/Base/Diagnostics/ComponentDiagnostics.cs

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Диагностические данные компонента SuperUI.
/// Доступны только в DEBUG-сборке через SgComponentBase.Diagnostics.
/// </summary>
public sealed class ComponentDiagnostics
{
    /// <summary>ID компонента.</summary>
    public string ComponentId { get; init; } = string.Empty;

    /// <summary>Количество выполненных рендеров.</summary>
    public int RenderCount { get; set; }

    /// <summary>Количество изменений параметров.</summary>
    public int ParameterChangeCount { get; set; }

    /// <summary>Время последнего рендера (мс).</summary>
    public double LastRenderMs { get; set; }

    /// <summary>Максимальное время рендера (мс).</summary>
    public double MaxRenderMs { get; set; }

    /// <summary>Среднее время рендера (мс).</summary>
    public double AverageRenderMs { get; set; }

    /// <summary>Количество JS вызовов.</summary>
    public int JsCallCount { get; set; }

    /// <summary>Количество ошибок JS.</summary>
    public int JsErrorCount { get; set; }

    /// <summary>Суммарное время JS вызовов (мс).</summary>
    public double TotalJsMs { get; set; }

    /// <summary>Компонент был в prerendering состоянии.</summary>
    public bool WasPrerendered { get; set; }

    /// <summary>Время создания компонента.</summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>Форматированный отчёт для логирования.</summary>
    public override string ToString() =>
        $"[{ComponentId}] Renders={RenderCount}, " +
        $"AvgRenderMs={AverageRenderMs:F2}, MaxRenderMs={MaxRenderMs:F2}, " +
        $"JsCalls={JsCallCount}, JsErrors={JsErrorCount}, TotalJsMs={TotalJsMs:F2}";
}
