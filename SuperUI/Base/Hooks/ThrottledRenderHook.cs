// SuperUI/Base/Hooks/ThrottledRenderHook.cs
//
// ИСПРАВЛЕНИЯ:
//   1. Stopwatch.GetTimestamp() — точнее DateTime.UtcNow, нет аллокаций
//   2. Reset() — идемпотентен через Interlocked
//   3. Документация ⚠️ особенностей

using System.Diagnostics;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Ограничитель частоты рендеринга (throttle).
/// Первый рендер всегда проходит.
/// </summary>
/// <remarks>
/// ⚠️ Blazor может вызывать ShouldRender() несколько раз при изменении параметров.
/// ThrottledRenderHook гарантирует не более одного рендера за MinInterval.
///
/// ⚠️ При minInterval ≤ 16ms подходит для анимаций (~60fps).
/// ⚠️ Server: _lastRenderTicks per-instance, изолирован per-circuit.
/// </remarks>
public sealed class ThrottledRenderHook : IRenderHook, IComponentHook
{
    private readonly long _minIntervalTicks;
    private long _lastRenderTicks;   // 0 = никогда не рендерился

    public TimeSpan MinInterval { get; }

    public ThrottledRenderHook(TimeSpan minInterval)
    {
        if (minInterval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minInterval), "Must be >= 0");
        MinInterval = minInterval;
        _minIntervalTicks = (long)(minInterval.TotalSeconds * Stopwatch.Frequency);
    }

    public bool ShouldRender(SgComponentBase component)
    {
        var now = Stopwatch.GetTimestamp();
        var last = Interlocked.Read(ref _lastRenderTicks);

        // Первый рендер — всегда
        if (last == 0 || now - last >= _minIntervalTicks)
        {
            Interlocked.Exchange(ref _lastRenderTicks, now);
            return true;
        }
        return false;
    }

    /// <summary>Сброс таймера (для тестов или принудительного рендера).</summary>
    public void Reset() => Interlocked.Exchange(ref _lastRenderTicks, 0);

    /// <summary>Время с последнего рендера.</summary>
    public TimeSpan TimeSinceLastRender
        => Stopwatch.GetElapsedTime(Interlocked.Read(ref _lastRenderTicks));

    // IComponentHook default-реализации
    public void OnInitialized(SgComponentBase c) { }
    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool first) { }
}
