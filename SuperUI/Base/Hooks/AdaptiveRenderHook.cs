using Microsoft.JSInterop;
using SuperUI.Base;

namespace SuperUI.Hooks;

/// <summary>
/// Адаптивный рендеринг — автоматически throttle рендеры
/// при высокой нагрузке (например, быстрые обновления данных).
/// 
/// Использует Intersection Observer для полного отключения рендеров
/// когда компонент вне viewport.
/// </summary>
public sealed class AdaptiveRenderHook : IAsyncComponentHook, IRenderHook
{
    private bool _isVisible = true;
    private int _renderCount = 0;
    private DateTime _lastRender = DateTime.MinValue;
    private readonly TimeSpan _minInterval;

    public AdaptiveRenderHook(TimeSpan? minInterval = null)
        => _minInterval = minInterval ?? TimeSpan.FromMilliseconds(16); // 60fps cap

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
    public void OnVisibilityChanged(bool isVisible)
    {
        _isVisible = isVisible;
    }
}