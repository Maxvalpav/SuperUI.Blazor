// SuperUI/Base/Services/AdaptiveRenderBudgetService.cs
//
// Улучшенная версия IRenderBudgetService с учётом загрузки CPU на сервере.
// Автоматически адаптирует бюджет рендера при высокой нагрузке процессора.
//
// IMPLEMENTS v3:
// ✅ RenderBudgetPriority (отдельный от RenderPriority) — без CS0019
// ✅ CurrentRenderCount — публичное свойство
// ✅ EffectiveLimit — публичное свойство
// ✅ CPU-aware debounce/throttle интервалы

using System.Diagnostics;

namespace SuperUI.Base.Services;

/// <summary>
/// Адаптивный сервис бюджета рендера, учитывающий загрузку CPU.
/// На WASM работает как обычный RenderBudgetService (CPU мониторинг недоступен).
/// На Server-side Blazor — мониторит CPU каждые 5 секунд и корректирует бюджет.
/// </summary>
public sealed class AdaptiveRenderBudgetService : IRenderBudgetService, IDisposable
{
    private int _rendersThisSecond;
    private long _windowStartTick = Stopwatch.GetTimestamp();
    private readonly Lock _lock = new();
    private readonly Timer? _cpuMonitor;
    private float _currentCpuLoad;

    // CPU мониторинг
    private Process? _currentProcess;
    private TimeSpan _lastCpuTime;
    private long _lastCpuCheckTick;

    public RenderBudgetPolicy Policy { get; set; } = RenderBudgetPolicy.Balanced;
    public int MaxRendersPerSecond { get; set; } = 60;

    public AdaptiveRenderBudgetService()
    {
        if (!OperatingSystem.IsBrowser())
        {
            try
            {
                _currentProcess = Process.GetCurrentProcess();
                _lastCpuTime = _currentProcess.TotalProcessorTime;
                _lastCpuCheckTick = Stopwatch.GetTimestamp();

                // Мониторим CPU каждые 5 секунд
                _cpuMonitor = new Timer(_ => UpdateCpuLoad(), null,
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Недоступно (например, в некоторых контейнерах)
            }
        }
    }

    private void UpdateCpuLoad()
    {
        if (_currentProcess is null) return;

        try
        {
            _currentProcess.Refresh();
            var currentCpuTime = _currentProcess.TotalProcessorTime;
            var elapsed = Stopwatch.GetElapsedTime(_lastCpuCheckTick);

            if (elapsed.TotalMilliseconds > 0)
            {
                var cpuUsedMs = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
                _currentCpuLoad = (float)(cpuUsedMs /
                    (elapsed.TotalMilliseconds * Environment.ProcessorCount));

                // Адаптивная корректировка бюджета
                if (_currentCpuLoad > 0.8f)
                {
                    // Высокая нагрузка — автоматически снижаем бюджет
                    _rendersThisSecond = Math.Max(_rendersThisSecond,
                        EffectiveLimit - 1);
                }
            }

            _lastCpuTime = currentCpuTime;
            _lastCpuCheckTick = Stopwatch.GetTimestamp();
        }
        catch { /* ignore */ }
    }

    public int CurrentRenderCount
    {
        get
        {
            lock (_lock) return _rendersThisSecond;
        }
    }

    public int EffectiveLimit => Policy switch
    {
        RenderBudgetPolicy.Balanced => MaxRendersPerSecond,
        RenderBudgetPolicy.Conservative => MaxRendersPerSecond / 2,
        RenderBudgetPolicy.Minimal => MaxRendersPerSecond / 4,
        _ => int.MaxValue
    };

    public bool TryAcquireRenderSlot(RenderBudgetPriority priority, string? componentId = null)
    {
        if (priority == RenderBudgetPriority.Critical) return true;
        if (Policy == RenderBudgetPolicy.Unrestricted) return true;

        lock (_lock)
        {
            var elapsed = Stopwatch.GetElapsedTime(_windowStartTick);
            if (elapsed.TotalSeconds >= 1.0)
            {
                _rendersThisSecond = 0;
                _windowStartTick = Stopwatch.GetTimestamp();
            }

            var limit = EffectiveLimit;

            if (priority == RenderBudgetPriority.Idle)
            {
                // Idle-приоритет — половина бюджета
                limit /= 2;

                // Дополнительно снижаем Idle при высокой нагрузке CPU
                if (_currentCpuLoad > 0.7f)
                    limit /= 2;
            }

            if (_rendersThisSecond >= limit) return false;
            _rendersThisSecond++;
            return true;
        }
    }

    public void ResetWindow()
    {
        lock (_lock)
        {
            _rendersThisSecond = 0;
            _windowStartTick = Stopwatch.GetTimestamp();
        }
    }

    public TimeSpan GetRecommendedDebounceInterval() => Policy switch
    {
        RenderBudgetPolicy.Conservative => TimeSpan.FromMilliseconds(
            _currentCpuLoad > 0.7f ? 800 : 500),
        RenderBudgetPolicy.Minimal => TimeSpan.FromMilliseconds(1000),
        _ => TimeSpan.FromMilliseconds(
            _currentCpuLoad > 0.7f ? 450 : 300)
    };

    public TimeSpan GetRecommendedThrottleInterval() => Policy switch
    {
        RenderBudgetPolicy.Conservative => TimeSpan.FromMilliseconds(
            _currentCpuLoad > 0.7f ? 350 : 200),
        RenderBudgetPolicy.Minimal => TimeSpan.FromMilliseconds(500),
        _ => TimeSpan.FromMilliseconds(
            _currentCpuLoad > 0.7f ? 200 : 100)
    };

    /// <summary>Текущая загрузка CPU (0.0 — 1.0).</summary>
    public float CurrentCpuLoad => _currentCpuLoad;

    public void Dispose() => _cpuMonitor?.Dispose();
}