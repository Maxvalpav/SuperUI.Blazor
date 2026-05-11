using System.Diagnostics;
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для логирования производительности рендера.
/// Логирует время рендера если оно превышает 1 кадр (16 мс).
/// </summary>
public sealed class PerformanceHook : IAsyncComponentHook, IRenderHook
{
    private long _renderStart;

    public void OnInitialized(SgComponentBase component) { }

    public bool ShouldRender(SgComponentBase component)
    {
        _renderStart = Stopwatch.GetTimestamp();
        return true;
    }

    public void OnAfterRender(SgComponentBase component, bool firstRender)
    {
        var elapsed = Stopwatch.GetElapsedTime(_renderStart).TotalMilliseconds;
        if (elapsed > 16) // > 1 frame
            Console.WriteLine($"[PERF] {component.ComponentId}: {elapsed:F1}ms");
    }

    public Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
    public void OnParametersSet(SgComponentBase component) { }
}
