using SuperUI.Diagnostics;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для телеметрии (OpenTelemetry).
/// Записывает метрики и трассировку жизненного цикла.
/// </summary>
public sealed class TelemetryHook : IComponentHook
{
    public ValueTask OnInitializedAsync(object component, string componentName)
    {
        ComponentDiagnostics.RecordRender(componentName, firstRender: true);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnParametersSetAsync(object component, string componentName, int changedCount)
    {
        ComponentDiagnostics.RecordParametersSet(componentName, changedCount);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnRenderAsync(object component, string componentName, bool firstRender)
    {
        ComponentDiagnostics.RecordRender(componentName, firstRender);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDisposedAsync(object component, string componentName)
    {
        // Можно записать метрику времени жизни компонента
        return ValueTask.CompletedTask;
    }
}
