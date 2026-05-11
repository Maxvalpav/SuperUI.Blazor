using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для логирования жизненного цикла компонентов.
/// Включается только в режиме Debug или при явной настройке.
/// </summary>
public sealed class LifecycleLoggingHook : IComponentHook
{
    private readonly ILogger<LifecycleLoggingHook> _logger;

    public LifecycleLoggingHook(ILogger<LifecycleLoggingHook> logger)
    {
        _logger = logger;
    }

    public ValueTask OnInitializedAsync(object component, string componentName)
    {
        _logger.LogTrace("[{Component}] Initialized", componentName);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnParametersSetAsync(object component, string componentName, int changedCount)
    {
        if (changedCount > 0)
            _logger.LogTrace("[{Component}] ParametersSet ({Count} changed)", componentName, changedCount);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnRenderAsync(object component, string componentName, bool firstRender)
    {
        if (firstRender)
            _logger.LogTrace("[{Component}] Rendered (first)", componentName);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDisposedAsync(object component, string componentName)
    {
        _logger.LogTrace("[{Component}] Disposed", componentName);
        return ValueTask.CompletedTask;
    }
}
