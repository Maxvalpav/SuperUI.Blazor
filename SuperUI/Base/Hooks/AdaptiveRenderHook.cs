using Microsoft.JSInterop;
using SuperUI.Base;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Адаптивный рендеринг — throttle рендеры при высокой нагрузке.
/// </summary>
public sealed class AdaptiveRenderHook : IAsyncComponentHook, IRenderHook
{
    private bool _isVisible = true;
    private int _renderCount = 0;
    private DateTime _lastRender = DateTime.MinValue;
    private readonly TimeSpan _minInterval;

    public AdaptiveRenderHook(TimeSpan? minInterval = null)
        => _minInterval = minInterval ?? TimeSpan.FromMilliseconds(16);

    // IRenderHook
    public bool ShouldRender(SgComponentBase c)
    {
        if (!_isVisible) return false;
        var now = DateTime.UtcNow;
        if (now - _lastRender < _minInterval) return false;
        _lastRender = now;
        _renderCount++;
        return true;
    }

    [JSInvokable]
    public void OnVisibilityChanged(bool isVisible) => _isVisible = isVisible;

    // IComponentHook
    public void OnInitialized(SgComponentBase component) { }
    public void OnParametersSet(SgComponentBase component) { }
    public void OnAfterRender(SgComponentBase component, bool firstRender) { }

    // IAsyncComponentHook
    public Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}