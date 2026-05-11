using SuperUI.Base;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для ограничения частоты рендеринга (throttle).
/// </summary>
public sealed class ThrottledRenderHook : IRenderHook
{
    private readonly TimeSpan _minInterval;
    private DateTime _lastRender = DateTime.MinValue;

    public ThrottledRenderHook(TimeSpan minInterval) => _minInterval = minInterval;

    // IRenderHook
    public bool ShouldRender(SgComponentBase c)
    {
        var now = DateTime.UtcNow;
        if (now - _lastRender < _minInterval) return false;
        _lastRender = now;
        return true;
    }

    // IComponentHook (required by IRenderHook)
    public void OnInitialized(SgComponentBase c) { }
    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }
}
