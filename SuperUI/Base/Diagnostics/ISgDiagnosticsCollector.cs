// SuperUI/Base/Diagnostics/ISgDiagnosticsCollector.cs
// Коллектор диагностических данных для DevTools панели
namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Коллектор диагностических данных компонентов.
/// Используется SgDiagnosticsPanel для отображения статистики рендера,
/// графа сигналов и ARIA-оценки.
/// </summary>
public interface ISgDiagnosticsCollector
{
    /// <summary>Все зарегистрированные компоненты с диагностикой.</summary>
    IReadOnlyList<ComponentDiagnostics> GetAll();

    /// <summary>Активные сигналы в графе зависимостей.</summary>
    IReadOnlyList<SignalDiagnostics> GetSignals();

    /// <summary>Количество активных сигналов.</summary>
    int SignalCount { get; }

    /// <summary>ARIA score (0-100) — оценка доступности.</summary>
    int AriaScore { get; }

    /// <summary>Проблемы ARIA найденные в компонентах.</summary>
    IReadOnlyList<AriaIssue> GetAriaIssues();
}

/// <summary>Диагностика сигнала.</summary>
public sealed record SignalDiagnostics(
    string Name,
    int SubscriberCount,
    string? ValuePreview = null);

/// <summary>Проблема ARIA.</summary>
public sealed record AriaIssue(
    string ComponentId,
    string Message,
    string Severity); // "error", "warning", "info"