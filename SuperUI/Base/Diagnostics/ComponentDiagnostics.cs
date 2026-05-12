// SuperUI/Base/Diagnostics/ComponentDiagnostics.cs
// ✅ УЛУЧШЕНИЯ:
//   - MemorySnapshot (DEBUG)
//   - FirstRenderAt / LastRenderAt для timeline
//   - ToString() расширенный

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

    /// <summary>Минимальное время рендера (мс). -1 = нет данных.</summary>
    public double MinRenderMs { get; set; } = -1;

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

    /// <summary>Количество пропущенных рендеров (ShouldRender = false).</summary>
    public int SkippedRenderCount { get; set; }

    /// <summary>Время создания компонента.</summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>Время первого рендера.</summary>
    public DateTimeOffset? FirstRenderAt { get; set; }

    /// <summary>Время последнего рендера.</summary>
    public DateTimeOffset? LastRenderAt { get; set; }

#if DEBUG
    /// <summary>Снимок managed-памяти на момент последнего рендера (байт).</summary>
    public long? ManagedMemoryBytes { get; set; }

    /// <summary>Обновить снимок памяти.</summary>
    public void SnapshotMemory() =>
        ManagedMemoryBytes = GC.GetTotalMemory(forceFullCollection: false);
#endif

    /// <summary>Средний размер JS вызова (мс).</summary>
    public double AverageJsMs => JsCallCount > 0
        ? TotalJsMs / JsCallCount
        : 0;

    /// <summary>Uptime компонента.</summary>
    public TimeSpan Uptime => DateTimeOffset.UtcNow - CreatedAt;

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"[{ComponentId}] ");
        sb.Append($"Renders={RenderCount}");
        if (SkippedRenderCount > 0) sb.Append($"(+{SkippedRenderCount} skipped)");
        sb.Append($", AvgRenderMs={AverageRenderMs:F2}");
        sb.Append($", MaxRenderMs={MaxRenderMs:F2}");
        if (JsCallCount > 0)
        {
            sb.Append($", JsCalls={JsCallCount}");
            if (JsErrorCount > 0) sb.Append($"/{JsErrorCount}err");
            sb.Append($", TotalJsMs={TotalJsMs:F2}");
        }
        sb.Append($", Uptime={Uptime:mm\\:ss}");
#if DEBUG
        if (ManagedMemoryBytes.HasValue)
            sb.Append($", Mem={ManagedMemoryBytes / 1024}KB");
#endif
        return sb.ToString();
    }
}
