using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SuperUI.Base;

namespace SuperUI.Hooks;

/// <summary>
/// Хук для логирования производительности первого рендера.
/// </summary>
public sealed class PerformanceHook : IAsyncComponentHook
{
    private readonly ILogger _logger;
    private readonly Stopwatch _sw = new();

    public PerformanceHook(ILogger logger) => _logger = logger;

    public void OnInitialized(SgComponentBase c)
        => _sw.Restart();

    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender)
    {
        if (firstRender)
            _logger.LogDebug("[{Id}] First render: {Ms}ms", c.ComponentId, _sw.ElapsedMilliseconds);
        return Task.CompletedTask;
    }

    public bool ShouldRender(SgComponentBase c) => true;
}
