// SuperUI/Base/Diagnostics/ComponentDiagnostics.cs
//
// Диагностические метрики компонента.
// Доступны только в DEBUG-сборках через SgComponentBase.Diagnostics.
//
// Не используем lock — чтения/записи из одного потока (per-circuit на Server).
// Interlocked не нужен: диагностика не критична к атомарности.

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Диагностические метрики компонента SuperUI.
/// Доступны только в DEBUG: <c>#if DEBUG</c>.
/// </summary>
public sealed class ComponentDiagnostics
{
    /// <summary>ID компонента.</summary>
    public string ComponentId { get; set; } = string.Empty;

    // ── Render метрики ───────────────────────────────────────────────────────

    /// <summary>Общее количество рендеров.</summary>
    public int RenderCount { get; set; }

    /// <summary>Время последнего рендера в мс.</summary>
    public double LastRenderMs { get; set; }

    /// <summary>Максимальное время рендера в мс.</summary>
    public double MaxRenderMs { get; set; }

    /// <summary>Среднее время рендера в мс.</summary>
    public double AverageRenderMs { get; set; }

    // ── Parameter метрики ────────────────────────────────────────────────────

    /// <summary>Количество вызовов SetParameters.</summary>
    public int ParameterChangeCount { get; set; }

    // ── JS Interop метрики ───────────────────────────────────────────────────

    /// <summary>Количество JS-вызовов.</summary>
    public int JsCallCount { get; set; }

    /// <summary>Количество ошибок JS-вызовов.</summary>
    public int JsErrorCount { get; set; }

    /// <summary>Суммарное время JS-вызовов в мс.</summary>
    public double TotalJsMs { get; set; }

    /// <summary>Среднее время JS-вызова в мс.</summary>
    public double AverageJsMs
        => JsCallCount > 0 ? TotalJsMs / JsCallCount : 0;

    // ── Signal метрики ───────────────────────────────────────────────────────

    /// <summary>Количество рендеров инициированных сигналами.</summary>
    public int SignalRenderCount { get; set; }

    // ── Форматирование ───────────────────────────────────────────────────────

    /// <summary>Краткая сводка метрик в одну строку.</summary>
    public override string ToString()
        => $"[{ComponentId}] renders={RenderCount}, " +
           $"lastMs={LastRenderMs:F1}, maxMs={MaxRenderMs:F1}, avgMs={AverageRenderMs:F1}, " +
           $"params={ParameterChangeCount}, js={JsCallCount}(err={JsErrorCount}, avgMs={AverageJsMs:F1})";

    /// <summary>Сбросить все метрики.</summary>
    public void Reset()
    {
        RenderCount = 0;
        LastRenderMs = 0;
        MaxRenderMs = 0;
        AverageRenderMs = 0;
        ParameterChangeCount = 0;
        JsCallCount = 0;
        JsErrorCount = 0;
        TotalJsMs = 0;
        SignalRenderCount = 0;
    }
}
