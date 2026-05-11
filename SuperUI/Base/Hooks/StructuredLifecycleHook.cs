using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SuperUI.Base;

namespace SuperUI.Hooks;

/// <summary>
/// Хук для структурированного логирования жизненного цикла компонента.
/// Встраивается в SgComponentBase через [Conditional("DEBUG")].
/// </summary>
public sealed class StructuredLifecycleHook : IAsyncComponentHook, IRenderHook
{
    private readonly ILogger _logger;
    private readonly Stopwatch _sw = new();

    public StructuredLifecycleHook(ILogger logger) => _logger = logger;

    public void OnInitialized(SgComponentBase c)
    {
        _sw.Restart();
        _logger.LogDebug("Component {ComponentType} {ComponentId} initialized",
            c.GetType().Name, c.ComponentId);
    }

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

    public bool ShouldRender(SgComponentBase c) => true;
}