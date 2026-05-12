// SuperUI/Base/Hooks/AdaptiveRenderHook.cs
//
// Адаптивный рендеринг: throttle по времени + управление видимостью.
// WASM и Server совместим.

using System.Diagnostics;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Адаптивный рендеринг — ограничивает частоту рендеров при высокой нагрузке.
/// Комбинирует throttle по времени и управление видимостью.
/// </summary>
/// <remarks>
/// Server: _lastRenderTicks и _isVisible — per-instance поля, изолированы per-circuit.
/// WASM: однопоточный — volatile не нужен логически, но помогает JIT-оптимизатору.
/// </remarks>
public sealed class AdaptiveRenderHook : IAsyncComponentHook, IRenderHook
{
    private volatile bool _isVisible = true;
    private long _lastRenderTicks;
    private readonly long _minIntervalTicks;

    /// <summary>Минимальный интервал между рендерами. По умолчанию 16 мс (≈ 60fps).</summary>
    public AdaptiveRenderHook(TimeSpan? minInterval = null)
    {
        var interval = minInterval ?? TimeSpan.FromMilliseconds(16);
        if (interval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minInterval));
        _minIntervalTicks = (long)(interval.TotalSeconds * Stopwatch.Frequency);
    }

    public bool ShouldRender(SgComponentBase component)
    {
        if (!_isVisible) return false;

        var now = Stopwatch.GetTimestamp();
        var last = Interlocked.Read(ref _lastRenderTicks);
        if (last != 0 && now - last < _minIntervalTicks) return false;

        Interlocked.Exchange(ref _lastRenderTicks, now);
        return true;
    }

    /// <summary>
    /// Управляет видимостью компонента.
    /// При isVisible=false — рендеры пропускаются.
    /// </summary>
    public void SetVisible(bool isVisible) => _isVisible = isVisible;

    // IComponentHook default-реализации (используются из интерфейса)
    public void OnInitialized(SgComponentBase c) { }
    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool first) { }

    // IAsyncComponentHook default-реализации
    public Task OnInitializedAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender) => Task.CompletedTask;
    public Task OnFirstRenderAsync(SgComponentBase c) => Task.CompletedTask;
}
