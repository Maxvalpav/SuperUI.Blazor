// SuperUI/Base/Hooks/ThrottledRenderHook.cs
//
// ДОРАБОТКИ:
// 1. Reset() — сброс таймера (для тестов)
// 2. LastRenderTime — публичное свойство для диагностики
// 3. Документация ⚠️ особенностей

using System.Diagnostics;
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для ограничения частоты рендеринга (throttle).
/// Первый рендер всегда проходит.
/// </summary>
/// <remarks>
/// ⚠️ Blazor может вызывать ShouldRender() несколько раз при изменении параметров.
/// ThrottledRenderHook гарантирует не более одного рендера за MinInterval.
/// При minInterval ≤ 16ms подходит для анимаций (~60fps).
/// </remarks>
public sealed class ThrottledRenderHook : IRenderHook, IComponentHook
{
    private readonly long _minIntervalTicks;
    private long _lastRenderTicks;

    public TimeSpan MinInterval { get; }

    public ThrottledRenderHook(TimeSpan minInterval)
    {
        if (minInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minInterval), "Must be positive.");
        MinInterval       = minInterval;
        _minIntervalTicks = (long)(minInterval.TotalSeconds * Stopwatch.Frequency);
    }

    public bool ShouldRender(SgComponentBase c)
    {
        var now  = Stopwatch.GetTimestamp();
        var last = Interlocked.Read(ref _lastRenderTicks);
        if (now - last < _minIntervalTicks) return false;
        return Interlocked.CompareExchange(ref _lastRenderTicks, now, last) == last;
    }

    /// <summary>Сброс таймера (для тестов или принудительного рендера).</summary>
    public void Reset() => Interlocked.Exchange(ref _lastRenderTicks, 0);

    /// <summary>Время последнего рендера (для диагностики).</summary>
    public TimeSpan TimeSinceLastRender =>
        Stopwatch.GetElapsedTime(Interlocked.Read(ref _lastRenderTicks));

    // IComponentHook default-реализации
    public void OnInitialized(SgComponentBase c)             { }
    public void OnParametersSet(SgComponentBase c)           { }
    public void OnAfterRender(SgComponentBase c, bool first) { }
}
