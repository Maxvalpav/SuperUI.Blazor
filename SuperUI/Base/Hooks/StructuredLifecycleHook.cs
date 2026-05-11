using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SuperUI.Base;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для структурированного логирования жизненного цикла компонента.
/// </summary>
public sealed class StructuredLifecycleHook : IAsyncComponentHook, IRenderHook
{
    private readonly ILogger _logger;
    private readonly Stopwatch _sw = new();

    public StructuredLifecycleHook(ILogger logger) => _logger = logger;

    // IComponentHook
    public void OnInitialized(SgComponentBase c)
    {
        _sw.Restart();
        _logger.LogDebug("Component {ComponentType} {ComponentId} initialized",
            c.GetType().Name, c.ComponentId);
    }

    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }

    // IAsyncComponentHook
    public Task OnInitializedAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase c) => Task.CompletedTask;

    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender)
    {
        if (firstRender)
        {
            _logger.LogDebug(
                "Component {ComponentType} {ComponentId} first render: {ElapsedMs}ms",
                c.GetType().Name, c.ComponentId, _sw.ElapsedMilliseconds);
        }
        return Task.CompletedTask;
    }

    // IRenderHook
    public bool ShouldRender(SgComponentBase c) => true;
}