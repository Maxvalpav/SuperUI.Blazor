// Файл: Diagnostics/ComponentDiagnostics.cs
// ИННОВАЦИЯ: OpenTelemetry-совместимое логирование lifecycle

using System.Diagnostics;

namespace SuperUI.Diagnostics;

/// <summary>
/// Диагностика компонентов через Activity (OpenTelemetry совместимо).
/// 
/// ИННОВАЦИЯ: Ни одна Blazor библиотека не предоставляет встроенный
/// OpenTelemetry tracing для component lifecycle.
/// 
/// ПРИМЕНЕНИЕ:
/// - Performance profiling (какой компонент тормозит?)
/// - Debug traces в distributed systems
/// - Custom metrics (render count, init time, etc.)
/// </summary>
public static class ComponentDiagnostics
{
    public static readonly ActivitySource Source = new("SuperUI.Components", "1.0.0");

    public static Activity? StartComponentActivity(string componentName, string operation)
        => Source.StartActivity($"Blazor.{componentName}.{operation}",
            ActivityKind.Internal,
            parentContext: default);

    public static void RecordRender(string componentName, bool firstRender)
    {
        using var activity = Source.StartActivity($"Blazor.{componentName}.Render");
        activity?.SetTag("blazor.component.name", componentName);
        activity?.SetTag("blazor.component.first_render", firstRender);
    }

    public static void RecordParametersSet(string componentName, int changedCount)
    {
        using var activity = Source.StartActivity($"Blazor.{componentName}.ParametersSet");
        activity?.SetTag("blazor.parameters.changed_count", changedCount);
    }
}
