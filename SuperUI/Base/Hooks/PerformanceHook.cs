using System.Diagnostics;
using SuperUI.Base;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для логирования производительности рендера.
/// Логирует время рендера если оно превышает 1 кадр (16 мс).
/// </summary>
public sealed class PerformanceHook : IAsyncComponentHook, IRenderHook
{
    private long _renderStart;

    // IComponentHook
    public void OnInitialized(SgComponentBase component) { }

    public void OnParametersSet(SgComponentBase component) { }

    // IRenderHook
    public bool ShouldRender(SgComponentBase component)
    {
        _renderStart = Stopwatch.GetTimestamp();
        return true;
    }

    public void OnAfterRender(SgComponentBase component, bool firstRender)
    {
        var elapsed = Stopwatch.GetElapsedTime(_renderStart).TotalMilliseconds;
        if (elapsed > 16) // > 1 frame at 60fps
            Console.WriteLine($"[PERF] {component.ComponentId}: {elapsed:F1}ms");
    }

    // IAsyncComponentHook
    public Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}
