// SuperUI/Base/Hooks/AdaptiveRenderHook.cs
// ИСПРАВЛЕНО:
// 1. Убран лишний using Microsoft.JSInterop
// 2. Убраны дублирующие using SuperUI.Base / SuperUI.Base.Hooks
// 3. Throttle через Stopwatch.GetTimestamp() вместо DateTime.UtcNow (точнее, нет аллокаций)
// 4. Убраны пустые методы — используются default-реализации IAsyncComponentHook
// 5. SetVisible документирован
using System.Diagnostics;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Адаптивный рендеринг — ограничивает частоту рендеров при высокой нагрузке.
/// Комбинирует throttle по времени и управление видимостью.
/// </summary>
/// <remarks>
/// Совместим с WASM и Server.
/// На Server: _lastRenderTicks и _isVisible — per-instance поля, изолированы per-circuit.
/// </remarks>
public sealed class AdaptiveRenderHook : IAsyncComponentHook, IRenderHook
{
    private volatile bool _isVisible = true;
    private long _lastRenderTicks;
    private readonly long _minIntervalTicks;

    /// <param name="minInterval">Минимальный интервал между рендерами. По умолчанию 16 мс (≈ 60 fps).</param>
    public AdaptiveRenderHook(TimeSpan? minInterval = null)
        => _minIntervalTicks = (minInterval ?? TimeSpan.FromMilliseconds(16)).Ticks
                               * Stopwatch.Frequency / TimeSpan.TicksPerSecond;

    // IRenderHook
    public bool ShouldRender(SgComponentBase c)
    {
        if (!_isVisible) return false;

        var now = Stopwatch.GetTimestamp();
        // Interlocked.Read для 64-bit на 32-bit платформах (WASM x86, ARM)
        var last = Interlocked.Read(ref _lastRenderTicks);
        if (now - last < _minIntervalTicks) return false;

        Interlocked.Exchange(ref _lastRenderTicks, now);
        return true;
    }

    /// <summary>Управляет видимостью компонента. При <see langword="false"/> рендеры пропускаются.</summary>
    public void SetVisible(bool isVisible) => _isVisible = isVisible;

    // IComponentHook — default-реализации используются (методы не нужно объявлять)
    // IAsyncComponentHook — default-реализации используются
}