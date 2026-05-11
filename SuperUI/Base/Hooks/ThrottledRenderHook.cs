// SuperUI/Base/Hooks/ThrottledRenderHook.cs
// ИСПРАВЛЕНО:
// 1. DateTime.UtcNow → Stopwatch.GetTimestamp() (точнее, без аллокаций)
// 2. _lastRenderTicks: long + Interlocked.Read/Exchange (thread-safe, ARM-safe)
using System.Diagnostics;
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для ограничения частоты рендеринга (throttle).
/// Пропускает рендер если с последнего прошло меньше <see cref="MinInterval"/>.
/// </summary>
public sealed class ThrottledRenderHook : IRenderHook
{
    private readonly long _minIntervalTicks;
    private long _lastRenderTicks;

    /// <summary>Минимальный интервал между рендерами.</summary>
    public TimeSpan MinInterval { get; }

    public ThrottledRenderHook(TimeSpan minInterval)
    {
        if (minInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minInterval), "Must be positive.");

        MinInterval = minInterval;
        _minIntervalTicks = (long)(minInterval.TotalSeconds * Stopwatch.Frequency);
    }

    // IRenderHook
    public bool ShouldRender(SgComponentBase c)
    {
        var now = Stopwatch.GetTimestamp();
        var last = Interlocked.Read(ref _lastRenderTicks);

        if (now - last < _minIntervalTicks) return false;

        Interlocked.Exchange(ref _lastRenderTicks, now);
        return true;
    }

    // IComponentHook — default-реализации (методы не нужны)
    public void OnInitialized(SgComponentBase c) { }
    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }
}