// SuperUI/Base/Reactive/AdaptiveThrottlePolicy.cs
// НОВОЕ: Адаптивный throttle — автоматически регулирует интервал
// на основе реального FPS браузера.
// 
// При 60fps: интервал = 16ms (один кадр)
// При 30fps: интервал = 33ms (один кадр)
// При высокой нагрузке CPU: интервал увеличивается автоматически
//
// Аналогов в MudBlazor / Radzen / Telerik / DevExpress нет.
namespace SuperUI.Base.Reactive;

/// <summary>
/// Политика адаптивного throttle на основе реального времени рендера.
/// Подбирает оптимальный интервал чтобы не превышать fps браузера.
/// </summary>
public sealed class AdaptiveThrottlePolicy
{
    // Скользящее среднее времени рендера (exponential moving average)
    private double _emaRenderMs = 16.0; // начинаем с 60fps
    private const double EmaAlpha = 0.2; // коэффициент сглаживания

    // Границы интервала
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(16);  // 60fps
    private static readonly TimeSpan MaxInterval = TimeSpan.FromMilliseconds(200); // 5fps

    // Целевой запас (не более 80% кадра занимаем рендером)
    private const double TargetUsageRatio = 0.8;

    /// <summary>
    /// Записать реальное время рендера (вызывается из OnAfterRender).
    /// </summary>
    public void RecordRenderTime(double milliseconds)
    {
        // Exponential Moving Average — не требует хранения истории
        _emaRenderMs = _emaRenderMs * (1 - EmaAlpha) + milliseconds * EmaAlpha;
    }

    /// <summary>
    /// Получить рекомендованный интервал throttle для текущей нагрузки.
    /// </summary>
    public TimeSpan GetRecommendedInterval()
    {
        // Если рендер занимает X мс, выбираем интервал так, чтобы рендер занимал не более 80% времени кадра
        var frameMs = _emaRenderMs / TargetUsageRatio;
        var intervalMs = Math.Max(MinInterval.TotalMilliseconds, Math.Min(frameMs, MaxInterval.TotalMilliseconds));
        return TimeSpan.FromMilliseconds(intervalMs);
    }

    /// <summary>
    /// Текущий EMA времени рендера в мс (для отображения в диагностике).
    /// </summary>
    public double CurrentRenderMs => _emaRenderMs;

    /// <summary>
    /// Сбросить статистику (при горячей перезагрузке компонента).
    /// </summary>
    public void Reset() => _emaRenderMs = 16.0;
}