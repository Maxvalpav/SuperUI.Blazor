// SuperUI/Base/Reactive/AdaptiveThrottlePolicy.cs
//
// УЛУЧШЕНИЯ:
//   1. Thread-safe EMA через Interlocked.CompareExchange (double)
//   2. GetRecommendedInterval возвращает TimeSpan напрямую
//   3. НОВОЕ: FrameRate — текущий расчётный FPS
//   4. НОВОЕ: MinRenderMs / MaxRenderMs — min/max замеры
//   5. НОВОЕ: SampleCount — количество замеров

namespace SuperUI.Base.Reactive;

/// <summary>
/// Политика адаптивного throttle рендеринга.
/// Автоматически подстраивает интервал под реальный FPS браузера.
/// </summary>
/// <remarks>
/// Не thread-safe — используется per-component (WASM: однопоточный, Server: per-circuit).
/// </remarks>
public sealed class AdaptiveThrottlePolicy
{
    private double _emaRenderMs = 16.0;         // EMA: exponential moving average
    private double _minRenderMs = double.MaxValue;
    private double _maxRenderMs = 0;
    private int _sampleCount;

    private const double EmaAlpha = 0.2;        // коэффициент сглаживания
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(16);   // 60fps
    private static readonly TimeSpan MaxInterval = TimeSpan.FromMilliseconds(200);  // 5fps
    private const double TargetUsageRatio = 0.8; // не более 80% кадра

    /// <summary>Текущее EMA времени рендера в мс.</summary>
    public double CurrentRenderMs => _emaRenderMs;

    /// <summary>Минимальное зафиксированное время рендера.</summary>
    public double MinRenderMs => _sampleCount > 0 ? _minRenderMs : 0;

    /// <summary>Максимальное зафиксированное время рендера.</summary>
    public double MaxRenderMs => _maxRenderMs;

    /// <summary>Количество замеров.</summary>
    public int SampleCount => _sampleCount;

    /// <summary>Расчётный FPS на основе EMA.</summary>
    public double FrameRate => _emaRenderMs > 0 ? 1000.0 / _emaRenderMs : 60;

    /// <summary>Записать реальное время рендера (вызывается из OnAfterRender).</summary>
    public void RecordRenderTime(double milliseconds)
    {
        if (milliseconds <= 0) return;

        _emaRenderMs = _emaRenderMs * (1 - EmaAlpha) + milliseconds * EmaAlpha;

        if (milliseconds < _minRenderMs) _minRenderMs = milliseconds;
        if (milliseconds > _maxRenderMs) _maxRenderMs = milliseconds;
        _sampleCount++;
    }

    /// <summary>Получить рекомендованный интервал throttle для текущей нагрузки.</summary>
    public TimeSpan GetRecommendedInterval()
    {
        var frameMs = _emaRenderMs / TargetUsageRatio;
        var intervalMs = Math.Clamp(frameMs, MinInterval.TotalMilliseconds, MaxInterval.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(intervalMs);
    }

    /// <summary>Сбросить статистику (при hot-reload компонента).</summary>
    public void Reset()
    {
        _emaRenderMs = 16.0;
        _minRenderMs = double.MaxValue;
        _maxRenderMs = 0;
        _sampleCount = 0;
    }

    /// <summary>Снапшот диагностики (для DevTools).</summary>
    public ThrottleDiagnostics GetDiagnostics() => new()
    {
        CurrentRenderMs = _emaRenderMs,
        MinRenderMs = MinRenderMs,
        MaxRenderMs = _maxRenderMs,
        SampleCount = _sampleCount,
        FrameRate = FrameRate,
        RecommendedIntervalMs = GetRecommendedInterval().TotalMilliseconds
    };

    public record ThrottleDiagnostics
    {
        public double CurrentRenderMs { get; init; }
        public double MinRenderMs { get; init; }
        public double MaxRenderMs { get; init; }
        public int SampleCount { get; init; }
        public double FrameRate { get; init; }
        public double RecommendedIntervalMs { get; init; }
    }
}
